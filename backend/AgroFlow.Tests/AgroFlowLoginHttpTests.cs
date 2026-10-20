using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroFlow.Api.Controllers;
using AgroFlow.Application.Services;
using AgroFlow.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgroFlow.Tests;

public sealed class AgroFlowLoginHttpTests
{
    private const string Password = "Synthetic-Test-Password-47!";
    private const string InvalidCredentialsResponse = "Credenciales inválidas.";

    [Theory]
    [InlineData("operator-a", "OPERATOR-A-ID", "OPERADOR")]
    [InlineData("supervisor-a", "SUPERVISOR-A-ID", "SUPERVISOR")]
    public async Task Login_returns_jwt_for_approved_internal_identity_users(string username, string expectedIdentityId, string expectedRole)
    {
        await using var app = await LoginTestServer.CreateAsync();
        using var response = await app.Client.PostAsJsonAsync("/api/auth/login", new { Username = username, Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var tokenText = root.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(tokenText));
        Assert.DoesNotContain("customerId", root.GetRawText(), StringComparison.OrdinalIgnoreCase);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);
        Assert.Equal(username, token.Subject);
        Assert.Contains(token.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == expectedRole);
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "customerId");
        Assert.DoesNotContain(token.Claims, claim => claim.Type == JwtTokenService.IngenioIdClaimType);
        var userInfo = root.GetProperty("userInfo");
        Assert.Equal(expectedIdentityId, userInfo.GetProperty("identityId").GetString());
        Assert.Equal(expectedRole, userInfo.GetProperty("role").GetString());
        Assert.False(userInfo.TryGetProperty("customerId", out _));
        Assert.False(userInfo.TryGetProperty("ingenioId", out _));
    }

    [Theory]
    [InlineData("missing-user", "Synthetic-Test-Password-47!")]
    [InlineData("operator-a", "Wrong-Synthetic-Password-47!")]
    [InlineData("no-role-user", "Synthetic-Test-Password-47!")]
    [InlineData("legacy-user-role", "Synthetic-Test-Password-47!")]
    [InlineData("legacy-admin-role", "Synthetic-Test-Password-47!")]
    [InlineData("multiple-roles-user", "Synthetic-Test-Password-47!")]
    public async Task Login_rejects_invalid_or_ambiguous_roles_with_generic_response(string username, string password)
    {
        await using var app = await LoginTestServer.CreateAsync();
        using var response = await app.Client.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = password });
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsResponse, responseBody);
        Assert.DoesNotContain("customerId", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_registration_route_remains_unavailable()
    {
        await using var app = await LoginTestServer.CreateAsync();
        using var response = await app.Client.PostAsJsonAsync("/api/auth/register", new { Username = "synthetic" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class LoginTestServer : IAsyncDisposable
    {
        private const string TestKey = "01234567890123456789012345678901";
        private readonly SqliteConnection _connection;
        private readonly TestServer _server;

        private LoginTestServer(SqliteConnection connection, TestServer server)
        {
            _connection = connection;
            _server = server;
            Client = server.CreateClient();
        }

        public HttpClient Client { get; }

        public static async Task<LoginTestServer> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var jwtValues = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestKey,
                ["Jwt:Issuer"] = "AgroFlow.HttpTests",
                ["Jwt:Audience"] = "AgroFlow.HttpTests.Client",
                ["Jwt:ExpiresInMinutes"] = "15"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(jwtValues).Build();
            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(jwtValues))
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddDbContext<AuthenticateContext>(options => options.UseSqlite(connection));
                    services.AddIdentityCore<IdentityUser>()
                        .AddRoles<IdentityRole>()
                        .AddEntityFrameworkStores<AuthenticateContext>()
                        .AddSignInManager();
                    services.AddSingleton<JwtTokenService>();
                    services.AddControllers().AddApplicationPart(typeof(AuthenticateController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            try
            {
                var server = new TestServer(builder);
                var testServer = new LoginTestServer(connection, server);
                await using (var scope = server.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AuthenticateContext>();
                    await db.Database.EnsureCreatedAsync();
                    await SeedAsync(scope.ServiceProvider);
                }
                return testServer;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        private static async Task SeedAsync(IServiceProvider services)
        {
            var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { "OPERADOR", "SUPERVISOR", "User", "Admin" })
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
            }
            var users = services.GetRequiredService<UserManager<IdentityUser>>();
            await CreateUserAsync(users, "operator-a", "OPERATOR-A-ID", "OPERADOR");
            await CreateUserAsync(users, "supervisor-a", "SUPERVISOR-A-ID", "SUPERVISOR");
            await CreateUserAsync(users, "no-role-user", "NO-ROLE-ID");
            await CreateUserAsync(users, "legacy-user-role", "LEGACY-USER-ID", "User");
            await CreateUserAsync(users, "legacy-admin-role", "LEGACY-ADMIN-ID", "Admin");
            await CreateUserAsync(users, "multiple-roles-user", "MULTI-ROLE-ID", "OPERADOR", "SUPERVISOR");
        }

        private static async Task CreateUserAsync(UserManager<IdentityUser> users, string username, string id, params string[] assignedRoles)
        {
            var user = new IdentityUser { Id = id, UserName = username, Email = $"{username}@example.test" };
            var created = await users.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
            if (assignedRoles.Length == 0) return;
            var assigned = await users.AddToRolesAsync(user, assignedRoles);
            Assert.True(assigned.Succeeded, string.Join("; ", assigned.Errors.Select(error => error.Description)));
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            _server.Dispose();
            await _connection.DisposeAsync();
        }
    }
}
