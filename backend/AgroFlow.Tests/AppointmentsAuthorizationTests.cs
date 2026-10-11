using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using AgroFlow.Api.Controllers;
using AgroFlow.Api.Security;
using AgroFlow.Application.Appointments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Appointments existe sólo en LocalDemo. Fuera de él, ni siquiera un JWT
/// válido del backend heredado debe publicar la ruta de la demo sin identidad.
/// </summary>
public sealed class AppointmentsAuthorizationTests
{
    private const string TestKey = "test-only-signing-key-never-used-outside-this-test-file";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    [Fact]
    public async Task LocalDemo_true_allows_anonymous_access()
    {
        await using var app = await StartAsync(isLocalDemo: true);

        using var response = await app.Client.GetAsync("/api/v1/appointments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LocalDemo_false_does_not_publish_appointments_for_anonymous_access()
    {
        await using var app = await StartAsync(isLocalDemo: false);

        using var response = await app.Client.GetAsync("/api/v1/appointments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LocalDemo_false_does_not_publish_any_appointment_operation_for_authenticated_access()
    {
        await using var app = await StartAsync(isLocalDemo: false);
        app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());

        var id = Guid.NewGuid();
        using var list = await app.Client.GetAsync("/api/v1/appointments");
        using var create = await app.Client.PostAsync("/api/v1/appointments", new StringContent("{}"));
        using var detail = await app.Client.GetAsync($"/api/v1/appointments/{id}");
        using var transition = await app.Client.PostAsync(
            $"/api/v1/appointments/{id}/transitions", new StringContent("{}"));

        Assert.All(new[] { list, create, detail, transition },
            response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    private static string CreateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            TestIssuer,
            TestAudience,
            [new Claim(ClaimTypes.Name, "test-user")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<TestApp> StartAsync(bool isLocalDemo)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers(options =>
                            options.Conventions.Add(new LocalDemoAppointmentsConvention(isLocalDemo)))
                        .AddApplicationPart(typeof(AppointmentsController).Assembly);
                    services.AddSingleton<IAppointmentService>(new FakeAppointmentService());
                    services.AddAuthorization(options =>
                        AppointmentsAuthorizationPolicy.Configure(options, isLocalDemo));

                    if (!isLocalDemo)
                    {
                        services.AddAuthentication(o =>
                            {
                                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                            })
                            .AddJwtBearer(o =>
                            {
                                o.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = TestIssuer,
                                    ValidAudience = TestAudience,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
                                };
                            });
                    }
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    if (!isLocalDemo)
                    {
                        app.UseAuthentication();
                    }
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();
        return new TestApp(host);
    }

    private sealed class TestApp(IHost host) : IAsyncDisposable
    {
        public HttpClient Client { get; } = host.GetTestClient();

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await host.StopAsync();
            host.Dispose();
        }
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public Task<AppointmentSnapshot> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AppointmentSummary>> ListAsync(AppointmentQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AppointmentSummary>>([]);

        public Task<AppointmentSnapshot> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AppointmentSnapshot> TransitionAsync(Guid id, AppointmentStatus newStatus, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
