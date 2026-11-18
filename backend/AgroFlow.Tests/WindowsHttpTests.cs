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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Ventanas y cupos de LocalDemo (B39, #44) sobre la aplicación real y una
/// SQLite temporal migrada y sembrada con datos ficticios, con el reloj fijo
/// en 2026-09-28 07:00 (-03:00) para que "hoy" no dependa de la ejecución.
/// </summary>
public sealed class WindowsHttpTests : IDisposable
{
    private const string Route = "/api/v1/windows";
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";
    private const string Today = "2026-09-28";
    private const string Tomorrow = "2026-09-29";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-28T07:00:00-03:00");

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-windows-{Guid.NewGuid():N}.db");

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Without_date_lists_every_empty_window_of_the_local_day_in_order_with_full_capacity()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var windows = await ListAsync(client, Route);

        // Seed de demo: 08:00-18:00 en franjas de 30 minutos, cupo 2.
        Assert.Equal(20, windows.Length);
        Assert.Equal($"{Today}T08:00:00-03:00", windows[0].GetProperty("startAt").GetString());
        Assert.Equal($"{Today}T08:30:00-03:00", windows[0].GetProperty("endAt").GetString());
        Assert.Equal($"{Today}T17:30:00-03:00", windows[^1].GetProperty("startAt").GetString());
        Assert.Equal($"{Today}T18:00:00-03:00", windows[^1].GetProperty("endAt").GetString());
        Assert.Equal(windows.Select(w => w.GetProperty("startAt").GetDateTimeOffset()).Order(),
            windows.Select(w => w.GetProperty("startAt").GetDateTimeOffset()));
        Assert.All(windows, w =>
        {
            Assert.Equal(2, w.GetProperty("capacity").GetInt32());
            Assert.Equal(0, w.GetProperty("occupied").GetInt32());
            Assert.Equal(2, w.GetProperty("available").GetInt32());
            Assert.NotEqual(Guid.Empty, w.GetProperty("id").GetGuid());
            // Sólo los campos del contrato: sin ingenio ni columnas internas.
            Assert.Equal(["available", "capacity", "endAt", "id", "occupied", "startAt"],
                w.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        });
    }

    [Fact]
    public async Task Explicit_date_selects_that_local_day_and_a_date_without_windows_is_an_empty_array()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var tomorrow = await ListAsync(client, $"{Route}?date={Tomorrow}");
        var none = await Dashboard(client, $"{Route}?date=2026-10-05");

        Assert.Equal(20, tomorrow.Length);
        Assert.All(tomorrow, w => Assert.StartsWith(Tomorrow, w.GetProperty("startAt").GetString()));
        Assert.Equal(HttpStatusCode.OK, none.StatusCode);
        var body = await none.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task Booking_updates_the_remaining_capacity_and_cancelling_releases_it_on_the_next_read()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await ListAsync(client, Route);

        var first = await CreateAppointmentAsync(client, "+5493815550101", "AF123BC");
        var afterFirst = await ListAsync(client, Route);
        Assert.Equal((1, 1), Cupos(afterFirst[0]));

        // Segundo turno: la franja de las 08:00 queda llena y se sigue listando.
        await CreateAppointmentAsync(client, "+5493815550121", "AH301AB");
        var afterSecond = await ListAsync(client, Route);
        Assert.Equal((2, 0), Cupos(afterSecond[0]));
        Assert.Equal(20, afterSecond.Length);
        Assert.All(afterSecond.Skip(1), w => Assert.Equal((0, 2), Cupos(w)));

        // Tercer turno: pasa a la siguiente franja sin tocar la llena.
        await CreateAppointmentAsync(client, "+5493815550122", "AH302AB");
        var afterThird = await ListAsync(client, Route);
        Assert.Equal((2, 0), Cupos(afterThird[0]));
        Assert.Equal((1, 1), Cupos(afterThird[1]));

        using var cancel = await client.PostAsJsonAsync($"/api/v1/appointments/{first}/transitions",
            new { newStatus = "CANCELADO" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var afterCancel = await ListAsync(client, Route);
        Assert.Equal((1, 1), Cupos(afterCancel[0]));
        Assert.Equal((1, 1), Cupos(afterCancel[1]));
        Assert.All(afterCancel, w =>
            Assert.Equal(w.GetProperty("capacity").GetInt32() - w.GetProperty("occupied").GetInt32(),
                w.GetProperty("available").GetInt32()));
    }

    // Una ventana al final del día local cae en el día siguiente en UTC: la
    // fecha pedida se interpreta en la zona del ingenio, no en UTC.
    [Fact]
    public async Task The_requested_date_is_the_local_day_of_the_ingenio_not_the_utc_day()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await ListAsync(client, Route);
        var lateToday = DateTimeOffset.Parse($"{Today}T23:30:00-03:00");
        var midnight = DateTimeOffset.Parse($"{Tomorrow}T00:00:00-03:00");
        await using (var db = CreateContext())
        {
            var ingenioId = (await db.Ingenios.SingleAsync()).Id;
            db.Ventanas.AddRange(
                new Ventana(ingenioId, lateToday, lateToday.AddMinutes(30), 3),
                new Ventana(ingenioId, midnight, midnight.AddMinutes(30), 4));
            await db.SaveChangesAsync();
        }

        var today = await ListAsync(client, $"{Route}?date={Today}");
        var tomorrow = await ListAsync(client, $"{Route}?date={Tomorrow}");

        Assert.Equal(21, today.Length);
        Assert.Equal($"{Today}T23:30:00-03:00", today[^1].GetProperty("startAt").GetString());
        Assert.Equal(3, today[^1].GetProperty("capacity").GetInt32());
        Assert.Equal(21, tomorrow.Length);
        Assert.Equal($"{Tomorrow}T00:00:00-03:00", tomorrow[0].GetProperty("startAt").GetString());
        Assert.Equal(4, tomorrow[0].GetProperty("capacity").GetInt32());
    }

    [Fact]
    public async Task Requires_the_dashboard_key_and_rejects_the_channel_key()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var anonymous = await client.GetAsync(Route);
        using var wrongKey = await Send(client, Route, ApiKeySchemes.DashboardHeader, "wrong-key-wrong-key-wrong-key-wrong-key");
        using var channelKeyAsDashboard = await Send(client, Route, ApiKeySchemes.DashboardHeader, ChannelKey);
        using var channelHeader = await Send(client, Route, ApiKeySchemes.ChannelHeader, ChannelKey);

        Assert.All(new[] { anonymous, wrongKey, channelKeyAsDashboard, channelHeader }, response =>
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        });
    }

    [Theory]
    [InlineData("?date=2026-13-01", "date")]
    [InlineData("?date=2026-02-30", "date")]
    [InlineData("?date=28-09-2026", "date")]
    [InlineData("?date=2026-9-28", "date")]
    [InlineData("?date=2026-09-28T00:00:00", "date")]
    [InlineData("?date=hoy", "date")]
    [InlineData("?date=", "date")]
    [InlineData("?date=2026-09-28&date=2026-09-29", "date")]
    [InlineData("?Date=2026-09-28", "Date")]
    [InlineData("?from=2026-09-28", "from")]
    [InlineData("?date=2026-09-28&page=1", "page")]
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
    [InlineData("0001-01-01")]
    [InlineData("9999-12-31")]
    public async Task Extreme_valid_dates_answer_without_server_errors(string date)
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var response = await Dashboard(client, $"{Route}?date={date}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Only_the_read_is_published(string method)
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var windows = await ListAsync(client, Route);
        var id = windows[0].GetProperty("id").GetGuid();
        var before = await SnapshotAsync();

        foreach (var path in new[] { Route, $"{Route}/{id}" })
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
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact]
    public async Task Listing_does_not_modify_data()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        await CreateAppointmentAsync(client, "+5493815550101", "AF123BC");
        var before = await SnapshotAsync();

        await ListAsync(client, Route);
        await ListAsync(client, $"{Route}?date={Tomorrow}");
        await ListAsync(client, $"{Route}?date=2026-10-05");
        using var invalid = await Dashboard(client, $"{Route}?date=nope");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    // Fuera de LocalDemo (perfil MVP) la ruta no se publica: ni siquiera con
    // una clave de dashboard válida ni en el contrato Swagger.
    [Fact]
    public async Task The_route_is_not_published_outside_local_demo()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("LocalDemo:Enabled", "false");
            builder.UseSetting("Jwt:Key", "test-only-key-for-b39-surface-check-123456789");
            builder.UseSetting("ConnectionStrings:AgroFlowMvpDb", $"Data Source={_dbPath}");
            builder.UseSetting(ApiKeySchemes.DashboardKeySetting, DashboardKey);
        });
        var client = factory.CreateClient();

        using var response = await Dashboard(client, Route);
        using var withDate = await Dashboard(client, $"{Route}?date={Today}");
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, withDate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.DoesNotContain("/windows", await swagger.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Local_demo_publishes_only_the_get_operation_in_swagger()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");

        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var operations = document.RootElement.GetProperty("paths").GetProperty(Route).EnumerateObject();
        Assert.Equal(["get"], operations.Select(o => o.Name));
    }

    private static (int Occupied, int Available) Cupos(JsonElement window) =>
        (window.GetProperty("occupied").GetInt32(), window.GetProperty("available").GetInt32());

    private static async Task<Guid> CreateAppointmentAsync(HttpClient client, string phone, string plate)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            carrierPhone = phone,
            truckPlate = plate,
            farmCode = "FINCA-NORTE",
            cutAt = "2026-09-28T05:30:00-03:00",
            estimatedLoadTons = 28.5
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<string> SnapshotAsync()
    {
        await using var db = CreateContext();
        var windows = await db.Ventanas.AsNoTracking().OrderBy(v => v.Id)
            .Select(v => new { v.Id, v.IngenioId, v.StartAt, v.EndAt, v.StartAtUtc, v.Capacity, v.Occupied }).ToListAsync();
        var turnos = await db.Turnos.AsNoTracking().OrderBy(t => t.Id)
            .Select(t => new { t.Id, t.VentanaId, t.Status, t.IsActive }).ToListAsync();
        return JsonSerializer.Serialize(new { windows, turnos });
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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedClock());
        });
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
