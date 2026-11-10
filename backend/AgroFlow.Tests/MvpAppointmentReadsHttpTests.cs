using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Authorization;
using AgroFlow.Api.Contracts.Appointments;
using AgroFlow.Application.Services;
using AgroFlow.Data.Appointments;
using AgroFlow.Data.Identity;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AgroFlow.Tests;

public sealed class MvpAppointmentReadsHttpTests : IDisposable
{
    private const string Route = "/api/v1/appointments";
    private const string Day = "2026-07-15";
    private const string OperatorA = "operador-mvp-a";
    private const string SupervisorA = "supervisor-mvp-a";
    private const string OperatorB = "operador-mvp-b";
    private const string Password = "Fixture-Only-2026!";
    private const string Key = "fixture-key-for-appointment-read-http-tests";
    private const string Issuer = "AgroFlow.MvpReadTests";
    private const string Audience = "AgroFlow.MvpReadTestClients";
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-mvp-reads-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Both_roles_can_read_their_ingenio_and_foreign_detail_matches_missing_detail()
    {
        using var factory = CreateFactory();
        using var operatorA = factory.CreateClient();
        using var supervisorA = factory.CreateClient();
        using var operatorB = factory.CreateClient();
        await LoginAsync(operatorA, OperatorA);
        await LoginAsync(supervisorA, SupervisorA);
        await LoginAsync(operatorB, OperatorB);

        var pageA = await ListAsync(operatorA, $"date={Day}");
        var pageB = await ListAsync(operatorB, $"date={Day}");
        var turnA = Assert.Single(pageA.Items);
        var turnB = Assert.Single(pageB.Items);
        Assert.NotEqual(turnA.Id, turnB.Id);
        Assert.Equal(1, pageA.TotalCount);
        Assert.Equal(1, pageB.TotalCount);
        Assert.Equal(pageA.Items, (await ListAsync(supervisorA, $"date={Day}")).Items);
        Assert.Equal(turnA, await supervisorA.GetFromJsonAsync<AppointmentResponse>($"{Route}/{turnA.Id}"));
        Assert.Equal(turnB, await operatorB.GetFromJsonAsync<AppointmentResponse>($"{Route}/{turnB.Id}"));
        Assert.Equal("CANCELADO", turnA.Status);
        Assert.Equal("FINALIZADO", turnB.Status);
        Assert.Equal("Transportista de prueba A", turnA.Carrier.Name);
        Assert.Equal("Finca de prueba A", turnA.Farm.Name);
        Assert.Equal(TimeSpan.FromHours(-3), turnA.Window.StartAt.Offset);

        using var foreign = await supervisorA.GetAsync($"{Route}/{turnB.Id}");
        using var missing = await supervisorA.GetAsync($"{Route}/{Guid.NewGuid()}");
        var foreignProblem = await AssertProblemAsync(foreign, 404, "APPOINTMENT_NOT_FOUND");
        var missingProblem = await AssertProblemAsync(missing, 404, "APPOINTMENT_NOT_FOUND");
        Assert.Equal(missingProblem.Title, foreignProblem.Title);
        Assert.DoesNotContain(turnB.Id.ToString(), await foreign.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_applies_local_midnight_filters_stable_pagination_and_empty_pages()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await LoginAsync(client, OperatorA);
        var seed = Assert.Single((await ListAsync(client, $"date={Day}")).Items);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgroFlowDbContext>();
            var source = await db.Turnos.SingleAsync(turno => turno.Id == seed.Id);
            var start = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.FromHours(-3));
            AddHistoricalTurn(db, source, start);
            AddHistoricalTurn(db, source, start);
            AddHistoricalTurn(db, source, start.AddDays(1));
            AddHistoricalTurn(db, source, start.AddTicks(-1));
            await db.SaveChangesAsync();
        }

        var filters = "status=CANCELADO&truckPlate=ah-401 ab";
        var page = await ListAsync(client, filters);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(seed.Id, page.Items.Last().Id);
        Assert.Equal(page.Items.Take(2).Select(turn => turn.Id).OrderBy(id => id.ToString("D")),
            page.Items.Take(2).Select(turn => turn.Id));
        Assert.All(page.Items, turn => Assert.Equal(Day, turn.Window.StartAt.ToString("yyyy-MM-dd")));
        var second = await ListAsync(client, $"{filters}&pageSize=1&page=2");
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(2, second.Page);
        Assert.Equal(page.Items[1], Assert.Single(second.Items));
        var beyond = await ListAsync(client, $"date={Day}&page=2147483647&pageSize=50");
        Assert.Empty(beyond.Items);
        Assert.Equal(3, beyond.TotalCount);
        var noMatch = await ListAsync(client, $"date={Day}&status=INGRESADO");
        Assert.Empty(noMatch.Items);
        Assert.Equal(0, noMatch.TotalCount);
        Assert.Empty((await ListAsync(client, "date=9999-12-31")).Items);
    }

    [Theory]
    [InlineData("?phone=%2B5493815550171", "phone")]
    [InlineData("?active=true", "active")]
    [InlineData("?ingenioId=other", "ingenioId")]
    [InlineData("?date=2026-02-30", "date")]
    [InlineData("?status=cancelado", "status")]
    [InlineData("?truckPlate=------", "truckPlate")]
    [InlineData("?date=&page=1", "date")]
    [InlineData("?page=1&page=2", "page")]
    [InlineData("?page=0", "page")]
    [InlineData("?pageSize=51", "pageSize")]
    [InlineData("/no-es-un-uuid", "id")]
    public async Task Invalid_parameters_return_problem_details(string suffix, string field)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await LoginAsync(client, OperatorA);
        using var response = await client.GetAsync(Route + suffix);
        var problem = await AssertProblemAsync(response, 400, "VALIDATION_ERROR");
        Assert.Contains(field, problem.Errors!.Keys);
    }

    [Fact]
    public async Task Missing_invalid_expired_and_mismatched_identity_return_401_before_query_validation()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-AgroFlow-Dashboard-Key", "only-a-test-placeholder-key");
        await CheckUnauthorizedAsync(client, Route + "?page=0");

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AgroFlowUser>>();
        var userA = (await users.FindByNameAsync(OperatorA))!;
        var userB = (await users.FindByNameAsync(OperatorB))!;
        var future = DateTime.UtcNow.AddMinutes(10);
        var badTokens = new[]
        {
            "bad-token",
            SignToken(userA.Id, userA.IngenioId, AgroFlowRoles.Operador, DateTime.UtcNow.AddSeconds(-1)),
            SignToken(userA.Id, null, AgroFlowRoles.Operador, future),
            SignToken(userA.Id, userB.IngenioId, AgroFlowRoles.Operador, future)
        };
        foreach (var token in badTokens)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await CheckUnauthorizedAsync(client, $"{Route}/{Guid.NewGuid()}");
        }

        await LoginAsync(client, OperatorA);
        userA.IsActive = false;
        Assert.True((await users.UpdateAsync(userA)).Succeeded);
        await CheckUnauthorizedAsync(client, Route);
    }

    [Fact]
    public async Task Valid_identity_without_read_role_returns_403_for_both_operations()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AgroFlowUser>>();
        var user = (await users.FindByNameAsync(OperatorA))!;
        Assert.True((await roles.CreateAsync(new IdentityRole(AgroFlowRoles.Gerente))).Succeeded);
        Assert.True((await users.RemoveFromRoleAsync(user, AgroFlowRoles.Operador)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, AgroFlowRoles.Gerente)).Succeeded);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            SignToken(user.Id, user.IngenioId, AgroFlowRoles.Gerente, DateTime.UtcNow.AddMinutes(10)));

        using var list = await client.GetAsync(Route);
        using var detail = await client.GetAsync($"{Route}/{Guid.NewGuid()}");
        await AssertProblemAsync(list, 403, "FORBIDDEN");
        await AssertProblemAsync(detail, 403, "FORBIDDEN");
    }

    [Fact]
    public async Task An_inconsistent_historical_reference_never_exposes_another_ingenio()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await LoginAsync(client, OperatorA);
        var own = Assert.Single((await ListAsync(client, $"date={Day}")).Items);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroFlowDbContext>();
        var turn = await db.Turnos.SingleAsync(turn => turn.Id == own.Id);
        var otherCarrier = await db.Transportistas.SingleAsync(carrier => carrier.IngenioId != turn.IngenioId);
        var broken = new Turno(turn.IngenioId, otherCarrier.Id, turn.CamionId, turn.FincaId, turn.VentanaId,
            turn.CutAt, turn.EstimatedLoadTons, turn.CreatedAt);
        broken.SetStatus(TurnoEstado.Cancelado);
        db.Turnos.Add(broken);
        await db.SaveChangesAsync();

        var page = await ListAsync(client, $"date={Day}");
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(own, Assert.Single(page.Items));
        using var detail = await client.GetAsync($"{Route}/{broken.Id}");
        await AssertProblemAsync(detail, 404, "APPOINTMENT_NOT_FOUND");
    }

    [Fact]
    public async Task Unexpected_read_error_is_a_generic_problem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await LoginAsync(client, OperatorA);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroFlowDbContext>();
        var ingenio = await db.Ingenios.SingleAsync(ingenio => ingenio.Name == "Ingenio de prueba A");
        const string brokenTimeZone = "internal-value-invalid-time-zone";
        ingenio.UpdateTimeZone(brokenTimeZone);
        await db.SaveChangesAsync();

        using var response = await client.GetAsync(Route);
        await AssertProblemAsync(response, 500, "INTERNAL_ERROR");
        Assert.DoesNotContain(brokenTimeZone, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Upgrade_preserves_existing_turn_and_restarts_without_reseeding()
    {
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite(ConnectionString).Options;
        Guid oldTurnId;
        Guid oldWindowId;
        await using (var db = new AgroFlowDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync("20261023190313_AddLocalDemoTeamAssignments");
            var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
            await AppointmentsSeeder.SeedAsync(db, new FrozenClock(now));
            var ingenio = await db.Ingenios.SingleAsync();
            var window = await db.Ventanas.FirstAsync();
            var carrier = await db.Transportistas.FirstAsync();
            var truck = await db.Camiones.FirstAsync();
            var farm = await db.Fincas.FirstAsync();
            var oldTurn = new Turno(ingenio.Id, carrier.Id, truck.Id, farm.Id, window.Id, now.AddHours(-2), 12, now);
            oldTurn.SetStatus(TurnoEstado.Cancelado);
            db.Turnos.Add(oldTurn);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE Ventanas SET StartAtUtc = NULL");
            oldTurnId = oldTurn.Id;
            oldWindowId = window.Id;
        }

        AppointmentResponse first;
        using (var factory = CreateFactory())
        using (var client = factory.CreateClient())
        {
            await LoginAsync(client, OperatorA);
            first = Assert.Single((await ListAsync(client, $"date={Day}")).Items);
        }

        using var restarted = CreateFactory();
        using var after = restarted.CreateClient();
        await LoginAsync(after, OperatorA);
        Assert.Equal(first, Assert.Single((await ListAsync(after, $"date={Day}")).Items));
        Assert.Equal(first, await after.GetFromJsonAsync<AppointmentResponse>($"{Route}/{first.Id}"));
        await using var verification = new AgroFlowDbContext(options);
        Assert.Equal(TurnoEstado.Cancelado, (await verification.Turnos.SingleAsync(turn => turn.Id == oldTurnId)).Status);
        var migrated = await verification.Ventanas.SingleAsync(window => window.Id == oldWindowId);
        Assert.Equal(migrated.StartAt.UtcDateTime, migrated.StartAtUtc);
        Assert.Equal(3, await verification.Turnos.CountAsync());
        Assert.Equal(3, await verification.Users.CountAsync());
    }

    private static void AddHistoricalTurn(AgroFlowDbContext db, Turno source, DateTimeOffset start)
    {
        var window = new Ventana(source.IngenioId, start, start.AddMinutes(30), 2);
        var turn = new Turno(source.IngenioId, source.TransportistaId, source.CamionId, source.FincaId,
            window.Id, start.AddHours(-3), 20, start.AddHours(-2));
        turn.SetStatus(TurnoEstado.Cancelado);
        db.AddRange(window, turn);
    }

    private string ConnectionString => $"Data Source={_dbPath};Foreign Keys=True;Pooling=False";

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("LocalDemo:Enabled", "false");
            builder.UseSetting("ConnectionStrings:AgroFlowMvpDb", ConnectionString);
            builder.UseSetting("MvpSeed:Password", Password);
            builder.UseSetting("Jwt:Key", Key);
            builder.UseSetting("Jwt:Issuer", Issuer);
            builder.UseSetting("Jwt:Audience", Audience);
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(
                new FrozenClock(new DateTimeOffset(2026, 7, 16, 1, 0, 0, TimeSpan.Zero))));
        });

    private static async Task LoginAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { Username = name, Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
    }

    private static async Task<AppointmentPageResponse> ListAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync($"{Route}?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentPageResponse>())!;
    }

    private static async Task CheckUnauthorizedAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        await AssertProblemAsync(response, 401, "UNAUTHENTICATED");
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    private static async Task<AppointmentProblemDetails> AssertProblemAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = (await response.Content.ReadFromJsonAsync<AppointmentProblemDetails>())!;
        Assert.Equal(status, body.Status);
        Assert.Equal(code, body.Code);
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
        return body;
    }

    private static string SignToken(string subject, Guid? tenant, string role, DateTime expires)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, subject), new(ClaimTypes.Role, role) };
        if (tenant is not null)
            claims.Add(new Claim(JwtTokenService.IngenioIdClaimType, tenant.Value.ToString("D")));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(Issuer, Audience, claims,
            expires: expires, signingCredentials: credentials));
    }

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            File.Delete(_dbPath + suffix);
    }

    private sealed class FrozenClock(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}
