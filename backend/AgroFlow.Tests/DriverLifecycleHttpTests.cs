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
/// Ciclo completo de un transportista sobre la aplicación real en LocalDemo:
/// alta y edición por la API, reinicio del host y creación de turnos según su
/// asociación con un camión. SQLite temporal migrada; datos ficticios.
/// </summary>
public sealed class DriverLifecycleHttpTests : IDisposable
{
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-driver-lifecycle-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Editing_a_seeded_whatsapp_survives_restarts_without_duplicates_and_keeps_its_truck()
    {
        Guid driverId;
        using (var factory = CreateFactory())
        {
            var client = factory.CreateClient();
            var uno = (await Drivers(client)).Single(d => d.GetProperty("whatsApp").GetString() == "+5493815550101");
            driverId = uno.GetProperty("id").GetGuid();

            using var update = await Send(client, HttpMethod.Put, $"/api/v1/drivers/{driverId}",
                new { name = "Transportista Demo Uno", dni = "20111111111", whatsApp = "+5493815550111" }, "fixture-edit-1");
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        }

        // Reinicio real: un host nuevo vuelve a migrar y sembrar la misma base.
        for (var restart = 0; restart < 2; restart++)
        {
            using var factory = CreateFactory();
            var client = factory.CreateClient();
            using var health = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);

            var drivers = await Drivers(client);
            Assert.Equal(5, drivers.Count);
            var edited = drivers.Single(d => d.GetProperty("id").GetGuid() == driverId);
            Assert.Equal("+5493815550111", edited.GetProperty("whatsApp").GetString());
            Assert.Equal(["AF123BC"], edited.GetProperty("activeTruckPlates").EnumerateArray().Select(p => p.GetString()));
            Assert.True(edited.GetProperty("canRequestAppointments").GetBoolean());
            Assert.DoesNotContain(drivers, d => d.GetProperty("whatsApp").GetString() == "+5493815550101");
        }

        using (var factory = CreateFactory())
        {
            // El número editado sigue resolviendo al mismo transportista y su camión.
            using var appointment = await factory.CreateClient().PostAsJsonAsync("/api/v1/appointments",
                Appointment("+5493815550111", "AF123BC"));
            Assert.Equal(HttpStatusCode.Created, appointment.StatusCode);
        }
    }

    [Fact]
    public async Task A_driver_registered_by_the_api_needs_an_active_truck_association_to_request_appointments()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        using var created = await Send(client, HttpMethod.Post, "/api/v1/drivers",
            new { name = "Transportista Alta API", dni = "20555000155", whatsApp = "+5493815550155" }, "fixture-create-1");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var driverId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid();

        var withoutTruck = (await Drivers(client)).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.Empty(withoutTruck.GetProperty("activeTruckPlates").EnumerateArray());
        Assert.False(withoutTruck.GetProperty("canRequestAppointments").GetBoolean());
        using var rejected = await client.PostAsJsonAsync("/api/v1/appointments", Appointment("+5493815550155", "AG777AA"));
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        Assert.Equal("REFERENCE_NOT_FOUND", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // No hay alta de camiones por HTTP: la prueba crea camión y asociación
        // directamente (la ruta de asociaciones la cubre AssociationsHttpTests).
        await using (var db = CreateContext())
        {
            var driver = await db.Transportistas.SingleAsync(t => t.Id == driverId);
            var truck = new Camion(driver.IngenioId, "AG777AA", Camion.FleetTypeTerceros);
            db.Camiones.Add(truck);
            db.Asociaciones.Add(new TransportistaCamion(driver, truck));
            await db.SaveChangesAsync();
        }

        var withTruck = (await Drivers(client)).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.True(withTruck.GetProperty("canRequestAppointments").GetBoolean());
        using var accepted = await client.PostAsJsonAsync("/api/v1/appointments", Appointment("+5493815550155", "AG777AA"));
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var body = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(driverId, body.GetProperty("carrier").GetProperty("id").GetGuid());
        Assert.Equal("ASIGNADO", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task An_inactivated_driver_cannot_request_appointments_even_with_a_truck()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var dos = (await Drivers(client)).Single(d => d.GetProperty("whatsApp").GetString() == "+5493815550102");

        using var inactivation = await Send(client, HttpMethod.Post,
            $"/api/v1/drivers/{dos.GetProperty("id").GetGuid()}/inactivation", null, "fixture-inactivate-1");
        Assert.Equal(HttpStatusCode.OK, inactivation.StatusCode);

        var after = (await Drivers(client)).Single(d => d.GetProperty("id").GetGuid() == dos.GetProperty("id").GetGuid());
        Assert.False(after.GetProperty("canRequestAppointments").GetBoolean());
        using var rejected = await client.PostAsJsonAsync("/api/v1/appointments", Appointment("+5493815550102", "AF456DE"));
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
    }

    [Fact]
    public async Task Reactivation_is_idempotent_and_restores_booking_for_the_same_driver_after_restart()
    {
        const string phone = "+5493815550102";
        const string plate = "AF456DE";
        const string key = "fixture-reactivate-1";
        Guid driverId;
        using (var factory = CreateFactory())
        {
            var client = factory.CreateClient();
            driverId = (await Drivers(client)).Single(d => d.GetProperty("whatsApp").GetString() == phone)
                .GetProperty("id").GetGuid();
            using var inactivated = await Send(client, HttpMethod.Post,
                $"/api/v1/drivers/{driverId}/inactivation", null, "fixture-inactivate-2");
            Assert.Equal(HttpStatusCode.OK, inactivated.StatusCode);

            var route = $"/api/v1/drivers/{driverId}/reactivation";
            using var reactivated = await Send(client, HttpMethod.Post, route, null, key);
            using var repeated = await Send(client, HttpMethod.Post, route, null, key);
            Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
            Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
            var receipt = await reactivated.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(driverId, receipt.GetProperty("resourceId").GetGuid());
            Assert.Equal("reactivated", receipt.GetProperty("outcome").GetString());
            Assert.Equal(receipt.GetRawText(), (await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());

            await using var db = CreateContext();
            Assert.Equal(1, await db.AuditEvents.CountAsync(e => e.Action == $"drivers.reactivate:{driverId:D}"));
        }

        using var restarted = CreateFactory();
        var restartedClient = restarted.CreateClient();
        var driver = (await Drivers(restartedClient)).Single(d => d.GetProperty("whatsApp").GetString() == phone);
        Assert.Equal(driverId, driver.GetProperty("id").GetGuid());
        Assert.True(driver.GetProperty("isActive").GetBoolean());
        Assert.True(driver.GetProperty("canRequestAppointments").GetBoolean());
        Assert.Equal([plate], driver.GetProperty("activeTruckPlates").EnumerateArray().Select(p => p.GetString()));

        using var appointment = await restartedClient.PostAsJsonAsync("/api/v1/appointments", Appointment(phone, plate));
        Assert.Equal(HttpStatusCode.Created, appointment.StatusCode);
        var body = await appointment.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(driverId, body.GetProperty("carrier").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Reactivation_requires_dashboard_key_and_leaves_an_unassociated_driver_without_booking()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        const string phone = "+5493815550155";
        using var created = await Send(client, HttpMethod.Post, "/api/v1/drivers",
            new { name = "Transportista sin camión", dni = "20555000155", whatsApp = phone }, "fixture-create-2");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var driverId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid();
        using var inactivated = await Send(client, HttpMethod.Post,
            $"/api/v1/drivers/{driverId}/inactivation", null, "fixture-inactivate-3");
        Assert.Equal(HttpStatusCode.OK, inactivated.StatusCode);

        var route = $"/api/v1/drivers/{driverId}/reactivation";
        using var unauthorizedRequest = new HttpRequestMessage(HttpMethod.Post, route);
        unauthorizedRequest.Headers.Add(ApiKeySchemes.ChannelHeader, ChannelKey);
        unauthorizedRequest.Headers.Add("Idempotency-Key", "fixture-reactivate-2");
        using var unauthorized = await client.SendAsync(unauthorizedRequest);
        using var missingKey = await Send(client, HttpMethod.Post, route, null, null);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        var inactive = (await Drivers(client)).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.False(inactive.GetProperty("isActive").GetBoolean());

        using var reactivated = await Send(client, HttpMethod.Post, route, null, "fixture-reactivate-2");
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        var active = (await Drivers(client)).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.True(active.GetProperty("isActive").GetBoolean());
        Assert.Empty(active.GetProperty("activeTruckPlates").EnumerateArray());
        Assert.False(active.GetProperty("canRequestAppointments").GetBoolean());
        using var rejected = await client.PostAsJsonAsync("/api/v1/appointments", Appointment(phone, "AF456DE"));
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        Assert.Equal("REFERENCE_NOT_FOUND", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private static object Appointment(string phone, string plate) => new
    {
        carrierPhone = phone,
        truckPlate = plate,
        farmCode = "FINCA-NORTE",
        cutAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
        estimatedLoadTons = 25.5
    };

    private static async Task<List<JsonElement>> Drivers(HttpClient client)
    {
        using var response = await Send(client, HttpMethod.Get, "/api/v1/drivers", null, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? body, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add(ApiKeySchemes.DashboardHeader, DashboardKey);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("LocalDemo:Enabled", "true");
        // Nunca la precarga privada real del equipo en una prueba.
        builder.UseSetting("LocalDemo:TeamFile", TestTeamFile.Absent);
        builder.UseSetting("LocalDemo:WhatsAppInstance", "fixture-instance-A");
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
