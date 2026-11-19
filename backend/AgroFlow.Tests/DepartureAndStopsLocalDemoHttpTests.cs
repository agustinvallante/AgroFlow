using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroFlow.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Departure instant and resolved stops (change mapa-eventos-reales) end to end
/// in LocalDemo, with the real API, a controllable clock and a per-test SQLite.
/// </summary>
public sealed class DepartureAndStopsLocalDemoHttpTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-departure-{Guid.NewGuid():N}.db");
    private readonly MutableClock _clock = new(WholeSecond(DateTimeOffset.UtcNow));

    private static readonly object FlatTire = new
    {
        type = "PINCHAZO",
        canContinue = true,
        estimatedDelayMinutes = 30
    };

    [Fact]
    public async Task Departure_is_null_until_en_camino_then_set_once_and_kept_by_later_transitions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateAsync(client, "+5493815550101", "AF123BC");
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, created.GetProperty("departedAt").ValueKind);
        Assert.Empty(created.GetProperty("stops").EnumerateArray());
        var assigned = await GetAsync(client, id);
        Assert.Equal(JsonValueKind.Null, assigned.GetProperty("departedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await ListedAsync(client, id)).GetProperty("departedAt").ValueKind);

        _clock.Advance(TimeSpan.FromMinutes(5));
        var departure = _clock.GetUtcNow();
        var enCamino = await TransitionAsync(client, id, "EN_CAMINO");

        var departedAt = enCamino.GetProperty("departedAt").GetString()!;
        Assert.EndsWith("-03:00", departedAt);
        Assert.Equal(departure, DateTimeOffset.Parse(departedAt));
        Assert.Empty(enCamino.GetProperty("stops").EnumerateArray());
        Assert.Equal(departedAt, (await GetAsync(client, id)).GetProperty("departedAt").GetString());
        Assert.Equal(departedAt, (await ListedAsync(client, id)).GetProperty("departedAt").GetString());

        foreach (var next in new[] { "EN_ESPERA", "INGRESADO" })
        {
            _clock.Advance(TimeSpan.FromMinutes(10));
            var moved = await TransitionAsync(client, id, next);
            Assert.Equal(departedAt, moved.GetProperty("departedAt").GetString());
        }

        Assert.Equal(departedAt, (await GetAsync(client, id)).GetProperty("departedAt").GetString());
        Assert.Equal(departedAt, (await ListedAsync(client, id)).GetProperty("departedAt").GetString());
    }

    [Fact]
    public async Task Stops_list_each_resolved_incident_with_its_report_and_resolution_instants_but_not_the_open_one()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = (await CreateEnCaminoAsync(client)).GetProperty("id").GetGuid();

        _clock.Advance(TimeSpan.FromMinutes(2));
        var firstStart = _clock.GetUtcNow();
        var reported = await ReportAsync(client, id);
        Assert.Empty(reported.GetProperty("stops").EnumerateArray());
        Assert.Equal(firstStart, DateTimeOffset.Parse(reported.GetProperty("openIncident").GetProperty("reportedAt").GetString()!));
        Assert.Empty((await GetAsync(client, id)).GetProperty("stops").EnumerateArray());
        Assert.Empty((await ListedAsync(client, id)).GetProperty("stops").EnumerateArray());

        _clock.Advance(TimeSpan.FromMinutes(3));
        var firstEnd = _clock.GetUtcNow();
        var resolved = await ResolveAsync(client, id);
        Assert.Equal(JsonValueKind.Null, resolved.GetProperty("openIncident").ValueKind);
        var stop = Assert.Single(resolved.GetProperty("stops").EnumerateArray());
        AssertStop(stop, firstStart, firstEnd);
        Assert.Equal(
            reported.GetProperty("openIncident").GetProperty("reportedAt").GetString(),
            stop.GetProperty("startedAt").GetString());

        _clock.Advance(TimeSpan.FromMinutes(7));
        var secondStart = _clock.GetUtcNow();
        await ReportAsync(client, id);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var secondEnd = _clock.GetUtcNow();
        var twice = await ResolveAsync(client, id);

        foreach (var view in new[] { twice, await GetAsync(client, id), await ListedAsync(client, id) })
        {
            var stops = view.GetProperty("stops").EnumerateArray().ToArray();
            Assert.Equal(2, stops.Length);
            AssertStop(stops[0], firstStart, firstEnd);
            AssertStop(stops[1], secondStart, secondEnd);
        }
    }

    [Theory]
    [InlineData("EN_ESPERA")]
    [InlineData("CANCELADO")]
    public async Task Leaving_en_camino_with_an_open_incident_turns_it_into_a_stop_ended_by_the_transition(string next)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = (await CreateEnCaminoAsync(client)).GetProperty("id").GetGuid();
        _clock.Advance(TimeSpan.FromMinutes(4));
        var start = _clock.GetUtcNow();
        await ReportAsync(client, id);

        _clock.Advance(TimeSpan.FromMinutes(20));
        var end = _clock.GetUtcNow();
        var moved = await TransitionAsync(client, id, next);

        Assert.Equal(JsonValueKind.Null, moved.GetProperty("openIncident").ValueKind);
        AssertStop(Assert.Single(moved.GetProperty("stops").EnumerateArray()), start, end);
        AssertStop(Assert.Single((await GetAsync(client, id)).GetProperty("stops").EnumerateArray()), start, end);
    }

    [Fact]
    public async Task Stops_are_ordered_by_start_even_when_stored_out_of_order()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = (await CreateEnCaminoAsync(client)).GetProperty("id").GetGuid();
        var later = _clock.GetUtcNow().AddMinutes(30);
        var earlier = _clock.GetUtcNow().AddMinutes(10);
        // Inserted later-first, so storage order differs from start order.
        await InsertResolvedIncidentAsync(id, later, later.AddMinutes(5));
        await InsertResolvedIncidentAsync(id, earlier, earlier.AddMinutes(2));

        foreach (var view in new[] { await GetAsync(client, id), await ListedAsync(client, id) })
        {
            var stops = view.GetProperty("stops").EnumerateArray().ToArray();
            Assert.Equal(2, stops.Length);
            AssertStop(stops[0], earlier, earlier.AddMinutes(2));
            AssertStop(stops[1], later, later.AddMinutes(5));
        }
    }

    private static void AssertStop(JsonElement stop, DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        Assert.Equal(["startedAt", "endedAt"], stop.EnumerateObject().Select(property => property.Name));
        var start = stop.GetProperty("startedAt").GetString()!;
        var end = stop.GetProperty("endedAt").GetString()!;
        Assert.EndsWith("-03:00", start);
        Assert.EndsWith("-03:00", end);
        Assert.Equal(startedAt, DateTimeOffset.Parse(start));
        Assert.Equal(endedAt, DateTimeOffset.Parse(end));
    }

    private async Task<JsonElement> CreateEnCaminoAsync(HttpClient client)
    {
        var created = await CreateAsync(client, "+5493815550101", "AF123BC");
        return await TransitionAsync(client, created.GetProperty("id").GetGuid(), "EN_CAMINO");
    }

    private static async Task<JsonElement> CreateAsync(HttpClient client, string phone, string plate)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            carrierPhone = phone,
            truckPlate = plate,
            farmCode = "FINCA-NORTE",
            cutAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            estimatedLoadTons = 25.5
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> TransitionAsync(HttpClient client, Guid id, string newStatus)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/appointments/{id}/transitions", new { newStatus });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> ReportAsync(HttpClient client, Guid id)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/appointments/{id}/incidents", FlatTire);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> ResolveAsync(HttpClient client, Guid id)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/appointments/{id}/incidents/resolution", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, Guid id) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/v1/appointments/{id}");

    /// <summary>The appointment's summary in the LocalDemo list of active appointments.</summary>
    private static async Task<JsonElement> ListedAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/appointments?active=true")).EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == id);

    /// <summary>Writes a resolved incident row straight into the test database.</summary>
    private async Task InsertResolvedIncidentAsync(Guid turnoId, DateTimeOffset reportedAt, DateTimeOffset resolvedAt)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath, Pooling = false, DefaultTimeout = 5
        }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO IncidentesRuta
                (Id, IngenioId, TurnoId, Type, CanContinue, EstimatedDelayMinutes, Description, ReportedAtUtc, ResolvedAtUtc)
            SELECT $id, IngenioId, Id, 'Pinchazo', 1, 10, NULL, $reported, $resolved FROM Turnos WHERE Id = $turno
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("$turno", turnoId.ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("$reported", reportedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"));
        command.Parameters.AddWithValue("$resolved", resolvedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"));
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("LocalDemo:Enabled", "true");
            // Never the team's real private preload in a test.
            builder.UseSetting("LocalDemo:TeamFile", TestTeamFile.Absent);
            builder.UseSetting("ConnectionStrings:AgroFlowDb", new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath, Pooling = false, DefaultTimeout = 5
            }.ToString());
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(_clock));
        });

    private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            File.Delete(_dbPath + suffix);
    }

    private sealed class MutableClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
