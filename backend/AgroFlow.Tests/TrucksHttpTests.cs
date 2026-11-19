using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Security;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Listado de camiones de LocalDemo (B15, #19) sobre la aplicación real y una
/// SQLite temporal migrada y sembrada con datos ficticios.
/// </summary>
public sealed class TrucksHttpTests : IDisposable
{
    private const string Route = "/api/v1/trucks";
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";

    private static readonly string[] SeededPlates = ["AF123BC", "AF456DE", "AH301AB", "AH302AB", "AH303AB",
        .. Enumerable.Range(1, AppointmentsSeeder.FreeDemoTruckCount).Select(i => $"AP{i:000}AA")];

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-trucks-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Lists_the_seeded_trucks_of_the_ingenio_ordered_by_plate()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var trucks = await ListAsync(client, Route);

        Assert.Equal(SeededPlates, trucks.Select(t => t.GetProperty("plate").GetString()));
        Assert.All(trucks, t => Assert.True(t.GetProperty("isActive").GetBoolean()));
        Assert.All(trucks, t => Assert.NotEqual(Guid.Empty, t.GetProperty("id").GetGuid()));
        var fleets = trucks.ToDictionary(t => t.GetProperty("plate").GetString()!, t => t.GetProperty("fleetType").GetString());
        Assert.Equal(Camion.FleetTypePropia, fleets["AF123BC"]);
        Assert.Equal(Camion.FleetTypeTerceros, fleets["AH303AB"]);
        // Sólo los campos del contrato: sin ingenio, patente normalizada ni fechas internas.
        Assert.Equal(["fleetType", "id", "isActive", "plate"],
            trucks[0].EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Active_filter_separates_enabled_and_disabled_trucks_from_sqlite()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await ListAsync(client, Route); // Arranque: migra y siembra la base.

        await using (var db = CreateContext())
        {
            var truck = await db.Camiones.SingleAsync(c => c.Plate == "AH302AB");
            truck.Inactivate(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        var active = await ListAsync(client, $"{Route}?active=true");
        var inactive = await ListAsync(client, $"{Route}?active=false");
        var all = await ListAsync(client, Route);

        Assert.Equal(SeededPlates.Where(p => p != "AH302AB"), active.Select(t => t.GetProperty("plate").GetString()));
        var disabled = Assert.Single(inactive);
        Assert.Equal("AH302AB", disabled.GetProperty("plate").GetString());
        Assert.False(disabled.GetProperty("isActive").GetBoolean());
        Assert.Equal(SeededPlates, all.Select(t => t.GetProperty("plate").GetString()));
    }

    [Fact]
    public async Task Without_matches_returns_an_empty_array()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var response = await Dashboard(client, $"{Route}?active=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    // LocalDemo opera con un único ingenio sembrado. Si aparece otro, el contexto
    // de ingenio no adivina: la consulta falla con un error genérico y no
    // revela camiones de ninguno de los dos. El filtro por ingenio del
    // repositorio se prueba en EfMasterDataUnitOfWorkTests.
    [Fact]
    public async Task With_more_than_one_ingenio_fails_closed_without_revealing_trucks()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await ListAsync(client, Route);

        await using (var db = CreateContext())
        {
            var other = new Ingenio("Ingenio Fixture Ajeno", "America/Argentina/Tucuman");
            db.AddRange(other, new Camion(other.Id, "ZZ999ZZ", Camion.FleetTypePropia));
            await db.SaveChangesAsync();
        }

        using var response = await Dashboard(client, Route);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("INTERNAL_ERROR", body);
        Assert.DoesNotContain("ZZ999ZZ", body);
        Assert.DoesNotContain("AF123BC", body);
    }

    [Fact]
    public async Task Requires_the_dashboard_key()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var anonymous = await client.GetAsync(Route);
        using var wrongKey = await Send(client, Route, ApiKeySchemes.DashboardHeader, "wrong-key-wrong-key-wrong-key-wrong-key");
        using var channelKey = await Send(client, Route, ApiKeySchemes.ChannelHeader, ChannelKey);

        Assert.All(new[] { anonymous, wrongKey, channelKey }, response =>
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        });
    }

    [Theory]
    [InlineData("?active=yes", "active")]
    [InlineData("?active=TRUE", "active")]
    [InlineData("?active=", "active")]
    [InlineData("?active=true&active=false", "active")]
    [InlineData("?plate=AF123BC", "plate")]
    [InlineData("?page=1", "page")]
    public async Task Invalid_parameters_are_field_validation_errors(string query, string field)
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var response = await Dashboard(client, Route + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Only_the_listing_is_published(string method)
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var trucks = await ListAsync(client, Route);
        var id = trucks[0].GetProperty("id").GetGuid();

        foreach (var path in new[] { Route, $"{Route}/{id}", $"{Route}/{id}/inactivation" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add(ApiKeySchemes.DashboardHeader, DashboardKey);
            using var response = await client.SendAsync(request);
            Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
                $"{method} {path} respondió {(int)response.StatusCode}");
        }

        using var detail = await Dashboard(client, $"{Route}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Listing_does_not_modify_data()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await ListAsync(client, Route);
        var before = await SnapshotAsync();

        await ListAsync(client, Route);
        await ListAsync(client, $"{Route}?active=true");

        Assert.Equal(before, await SnapshotAsync());
    }

    private async Task<string> SnapshotAsync()
    {
        await using var db = CreateContext();
        var trucks = await db.Camiones.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Plate, c.FleetType, c.IsActive, c.InactivatedAtUtc }).ToListAsync();
        var counts = new { Turnos = await db.Turnos.CountAsync(), Asociaciones = await db.Asociaciones.CountAsync() };
        return JsonSerializer.Serialize(new { trucks, counts });
    }

    private static async Task<JsonElement[]> ListAsync(HttpClient client, string path)
    {
        using var response = await Dashboard(client, path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
    }

    private static Task<HttpResponseMessage> Dashboard(HttpClient client, string path) =>
        Send(client, path, ApiKeySchemes.DashboardHeader, DashboardKey);

    private static Task<HttpResponseMessage> Send(HttpClient client, string path, string header, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(header, key);
        return client.SendAsync(request);
    }

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("LocalDemo:Enabled", "true");
        // Nunca la precarga privada real del equipo en una prueba.
        builder.UseSetting("LocalDemo:TeamFile", TestTeamFile.Absent);
        builder.UseSetting("ConnectionStrings:AgroFlowDb", $"Data Source={_dbPath};Default Timeout=5");
        builder.UseSetting(ApiKeySchemes.ChannelKeySetting, ChannelKey);
        builder.UseSetting(ApiKeySchemes.DashboardKeySetting, DashboardKey);
    });

    private AgroFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<AgroFlowDbContext>()
        .UseSqlite($"Data Source={_dbPath};Default Timeout=5")
        .Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            var path = _dbPath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
