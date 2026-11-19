using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgroFlow.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Incidentes en ruta (cambio reportar-incidentes-en-ruta) de punta a punta en
/// LocalDemo, con la API real y SQLite propio de cada prueba.
/// </summary>
public sealed class RouteIncidentsLocalDemoHttpTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-incidents-{Guid.NewGuid():N}.db");

    private static readonly object FlatTire = new
    {
        type = "PINCHAZO",
        canContinue = true,
        estimatedDelayMinutes = 30
    };

    [Fact]
    public async Task Reporting_on_an_en_camino_appointment_returns_201_with_the_open_incident_and_unchanged_status_and_window()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);

        using var response = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(appointment.GetProperty("id").GetGuid(), body.GetProperty("id").GetGuid());
        Assert.Equal("EN_CAMINO", body.GetProperty("status").GetString());
        Assert.Equal(appointment.GetProperty("window").GetRawText(), body.GetProperty("window").GetRawText());
        var incident = body.GetProperty("openIncident");
        Assert.NotEqual(Guid.Empty, incident.GetProperty("id").GetGuid());
        Assert.Equal("PINCHAZO", incident.GetProperty("type").GetString());
        Assert.True(incident.GetProperty("canContinue").GetBoolean());
        Assert.Equal(30, incident.GetProperty("estimatedDelayMinutes").GetInt32());
        Assert.Equal(JsonValueKind.Null, incident.GetProperty("description").ValueKind);
        var reportedAt = incident.GetProperty("reportedAt").GetString()!;
        Assert.EndsWith("-03:00", reportedAt);
        Assert.InRange(DateTimeOffset.Parse(reportedAt), DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(6, incident.EnumerateObject().Count());

        // The detail shows the same incident and the row is persisted open.
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}");
        Assert.Equal(incident.GetRawText(), detail.GetProperty("openIncident").GetRawText());
        Assert.Equal("EN_CAMINO", detail.GetProperty("status").GetString());
        Assert.Equal((1L, 1L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task Reporting_keeps_the_optional_description()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);

        using var response = await client.PostAsJsonAsync(IncidentsUrl(appointment), new
        {
            type = "FALLA_MECANICA",
            canContinue = false,
            estimatedDelayMinutes = 1440,
            description = new string('x', 500)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var incident = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("openIncident");
        Assert.Equal("FALLA_MECANICA", incident.GetProperty("type").GetString());
        Assert.False(incident.GetProperty("canContinue").GetBoolean());
        Assert.Equal(1440, incident.GetProperty("estimatedDelayMinutes").GetInt32());
        Assert.Equal(new string('x', 500), incident.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Reporting_on_an_assigned_appointment_is_incident_not_allowed_and_records_nothing()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateAsync(client, "+5493815550101", "AF123BC");

        using var response = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "INCIDENT_NOT_ALLOWED");
        Assert.Equal((0L, 0L), await CountIncidentsAsync());
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}");
        Assert.Equal("ASIGNADO", detail.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("openIncident").ValueKind);
    }

    [Fact]
    public async Task A_second_report_is_open_incident_exists_and_keeps_the_first_incident()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);
        using var first = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);
        var firstIncident = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("openIncident");

        using var second = await client.PostAsJsonAsync(IncidentsUrl(appointment), new
        {
            type = "ACCIDENTE",
            canContinue = false,
            estimatedDelayMinutes = 120
        });

        await AssertProblemAsync(second, HttpStatusCode.Conflict, "OPEN_INCIDENT_EXISTS");
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}");
        Assert.Equal(firstIncident.GetRawText(), detail.GetProperty("openIncident").GetRawText());
        Assert.Equal((1L, 1L), await CountIncidentsAsync());
    }

    [Theory]
    [InlineData("""{"type":"GRANIZO","canContinue":true,"estimatedDelayMinutes":30}""", "type")]
    [InlineData("""{"type":"pinchazo","canContinue":true,"estimatedDelayMinutes":30}""", "type")]
    [InlineData("""{"canContinue":true,"estimatedDelayMinutes":30}""", "type")]
    [InlineData("""{"type":"PINCHAZO","estimatedDelayMinutes":30}""", "canContinue")]
    [InlineData("""{"type":"PINCHAZO","canContinue":"si","estimatedDelayMinutes":30}""", "canContinue")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true}""", "estimatedDelayMinutes")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":-1}""", "estimatedDelayMinutes")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":1441}""", "estimatedDelayMinutes")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":12.5}""", "estimatedDelayMinutes")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":"30"}""", "estimatedDelayMinutes")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":30,"description":5}""", "description")]
    [InlineData("""{"type":"PINCHAZO","canContinue":true,"estimatedDelayMinutes":30,"status":"EN_ESPERA"}""", "status")]
    public async Task Invalid_report_is_a_field_validation_error_and_records_nothing(string json, string field)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);

        using var response = await client.PostAsync(IncidentsUrl(appointment),
            new StringContent(json, Encoding.UTF8, "application/json"));

        await AssertFieldErrorAsync(response, field);
        Assert.Equal((0L, 0L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task A_description_longer_than_500_characters_is_a_validation_error()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);

        using var response = await client.PostAsJsonAsync(IncidentsUrl(appointment), new
        {
            type = "OTRO",
            canContinue = true,
            estimatedDelayMinutes = 0,
            description = new string('x', 501)
        });

        await AssertFieldErrorAsync(response, "description");
        Assert.Equal((0L, 0L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task Unknown_appointment_is_appointment_not_found_for_report_and_resolution()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var missing = Guid.NewGuid();

        using var report = await client.PostAsJsonAsync($"/api/v1/appointments/{missing}/incidents", FlatTire);
        using var resolution = await client.PostAsJsonAsync($"/api/v1/appointments/{missing}/incidents/resolution", new { });
        using var badId = await client.PostAsJsonAsync("/api/v1/appointments/not-a-uuid/incidents", FlatTire);

        await AssertProblemAsync(report, HttpStatusCode.NotFound, "APPOINTMENT_NOT_FOUND");
        await AssertProblemAsync(resolution, HttpStatusCode.NotFound, "APPOINTMENT_NOT_FOUND");
        await AssertFieldErrorAsync(badId, "id");
    }

    [Fact]
    public async Task Resolution_returns_200_with_no_open_incident_and_keeps_the_resolved_record()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);
        using var report = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);

        using var response = await client.PostAsJsonAsync($"{IncidentsUrl(appointment)}/resolution", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EN_CAMINO", body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("openIncident").ValueKind);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}");
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("openIncident").ValueKind);
        // Kept with its resolution time: one row, none open.
        Assert.Equal((1L, 0L), await CountIncidentsAsync());

        // After resolving, a new incident can be reported again.
        using var again = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal((2L, 1L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task Resolution_without_an_open_incident_is_no_open_incident()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);

        using var response = await client.PostAsJsonAsync($"{IncidentsUrl(appointment)}/resolution", new { });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "NO_OPEN_INCIDENT");
        Assert.Equal((0L, 0L), await CountIncidentsAsync());
    }

    [Theory]
    [InlineData("""{"note":"listo"}""", "note")]
    public async Task Resolution_body_is_strict(string json, string field)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);
        using var report = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);

        using var response = await client.PostAsync($"{IncidentsUrl(appointment)}/resolution",
            new StringContent(json, Encoding.UTF8, "application/json"));

        await AssertFieldErrorAsync(response, field);
        Assert.Equal((1L, 1L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task Arriving_at_en_espera_resolves_the_open_incident_in_the_same_transition()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);
        using var report = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);

        using var transition = await client.PostAsJsonAsync(
            $"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}/transitions", new { newStatus = "EN_ESPERA" });

        Assert.Equal(HttpStatusCode.OK, transition.StatusCode);
        var body = await transition.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EN_ESPERA", body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("openIncident").ValueKind);
        Assert.Equal((1L, 0L), await CountIncidentsAsync());
        var listed = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/v1/appointments?active=true")).EnumerateArray());
        Assert.Equal(JsonValueKind.Null, listed.GetProperty("openIncident").ValueKind);
    }

    [Fact]
    public async Task Cancelling_an_en_camino_appointment_also_resolves_its_open_incident()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var appointment = await CreateEnCaminoAsync(client);
        using var report = await client.PostAsJsonAsync(IncidentsUrl(appointment), FlatTire);

        using var transition = await client.PostAsJsonAsync(
            $"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}/transitions", new { newStatus = "CANCELADO" });

        Assert.Equal(HttpStatusCode.OK, transition.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await transition.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("openIncident").ValueKind);
        Assert.Equal((1L, 0L), await CountIncidentsAsync());
    }

    [Fact]
    public async Task List_includes_open_incident_only_for_the_appointment_that_has_one()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var withIncident = await CreateEnCaminoAsync(client);
        var withoutIncident = await CreateAsync(client, "+5493815550102", "AF456DE");
        using var report = await client.PostAsJsonAsync(IncidentsUrl(withIncident), FlatTire);
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);

        var listed = (await client.GetFromJsonAsync<JsonElement>("/api/v1/appointments?active=true")).EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetGuid());

        Assert.Equal(2, listed.Count);
        var open = listed[withIncident.GetProperty("id").GetGuid()].GetProperty("openIncident");
        Assert.Equal("PINCHAZO", open.GetProperty("type").GetString());
        Assert.Equal(30, open.GetProperty("estimatedDelayMinutes").GetInt32());
        var none = listed[withoutIncident.GetProperty("id").GetGuid()];
        Assert.Equal(JsonValueKind.Null, none.GetProperty("openIncident").ValueKind);
    }

    [Fact]
    public async Task Existing_transition_and_list_keep_their_previous_shape_plus_local_demo_fields()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateAsync(client, "+5493815550101", "AF123BC");
        Assert.Equal(JsonValueKind.Null, created.GetProperty("openIncident").ValueKind);

        using var transition = await client.PostAsJsonAsync(
            $"/api/v1/appointments/{created.GetProperty("id").GetGuid()}/transitions", new { newStatus = "EN_CAMINO" });
        Assert.Equal(HttpStatusCode.OK, transition.StatusCode);
        var moved = await transition.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EN_CAMINO", moved.GetProperty("status").GetString());
        Assert.Equal(
            new[] { "id", "carrier", "truck", "farm", "cutAt", "estimatedLoadTons", "window", "status", "createdAt", "openIncident", "departedAt", "stops" },
            moved.EnumerateObject().Select(property => property.Name));
        Assert.Equal(JsonValueKind.Null, moved.GetProperty("openIncident").ValueKind);

        using var invalid = await client.PostAsJsonAsync(
            $"/api/v1/appointments/{created.GetProperty("id").GetGuid()}/transitions", new { newStatus = "FINALIZADO" });
        await AssertProblemAsync(invalid, HttpStatusCode.Conflict, "INVALID_TRANSITION");

        var summary = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/v1/appointments?active=true")).EnumerateArray());
        Assert.Equal(
            new[] { "id", "truck", "window", "status", "openIncident", "departedAt", "stops" },
            summary.EnumerateObject().Select(property => property.Name));
        Assert.Equal("EN_CAMINO", summary.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("openIncident").ValueKind);
    }

    private static string IncidentsUrl(JsonElement appointment) =>
        $"/api/v1/appointments/{appointment.GetProperty("id").GetGuid()}/incidents";

    private static async Task<JsonElement> CreateEnCaminoAsync(HttpClient client)
    {
        var created = await CreateAsync(client, "+5493815550101", "AF123BC");
        using var transition = await client.PostAsJsonAsync(
            $"/api/v1/appointments/{created.GetProperty("id").GetGuid()}/transitions", new { newStatus = "EN_CAMINO" });
        Assert.Equal(HttpStatusCode.OK, transition.StatusCode);
        return await transition.Content.ReadFromJsonAsync<JsonElement>();
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

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors.{field} missing");
    }

    /// <summary>(total rows, open rows) read straight from the test database.</summary>
    private async Task<(long Total, long Open)> CountIncidentsAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath, Pooling = false, DefaultTimeout = 5
        }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*), COALESCE(SUM(CASE WHEN ResolvedAtUtc IS NULL THEN 1 ELSE 0 END), 0) FROM IncidentesRuta";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("LocalDemo:Enabled", "true");
            // Nunca la precarga privada real del equipo en una prueba.
            builder.UseSetting("LocalDemo:TeamFile", TestTeamFile.Absent);
            builder.UseSetting("ConnectionStrings:AgroFlowDb", new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath, Pooling = false, DefaultTimeout = 5
            }.ToString());
        });

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            File.Delete(_dbPath + suffix);
    }
}
