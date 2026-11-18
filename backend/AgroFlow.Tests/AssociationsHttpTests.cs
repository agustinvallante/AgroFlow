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
/// Driver-truck association management in LocalDemo over the real application
/// and a temporary migrated SQLite database seeded with fictitious data.
/// </summary>
public sealed class AssociationsHttpTests : IDisposable
{
    private const string Route = "/api/v1/associations";
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-associations-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Associating_a_free_truck_lets_a_new_driver_request_appointments()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550177", "20555000177");
        var truckId = await AddTruck("AG888AA");

        using var created = await Send(client, HttpMethod.Post, Route, new { driverId, truckId }, "assoc-create-1");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = await created.Content.ReadFromJsonAsync<JsonElement>();
        var associationId = receipt.GetProperty("resourceId").GetGuid();
        Assert.Equal($"/api/v1/associations/{associationId:D}", created.Headers.Location?.OriginalString);

        var listed = (await List(client, $"{Route}?active=true")).Single(a => a.GetProperty("id").GetGuid() == associationId);
        Assert.Equal(driverId, listed.GetProperty("driverId").GetGuid());
        Assert.Equal(truckId, listed.GetProperty("truckId").GetGuid());
        Assert.True(listed.GetProperty("isActive").GetBoolean());

        var driver = (await List(client, "/api/v1/drivers")).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.Equal(["AG888AA"], driver.GetProperty("activeTruckPlates").EnumerateArray().Select(p => p.GetString()));
        Assert.True(driver.GetProperty("canRequestAppointments").GetBoolean());
    }

    [Fact]
    public async Task A_truck_with_an_active_driver_rejects_a_second_association_without_changing_it()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550178", "20555000178");
        var seeded = (await List(client, $"{Route}?active=true")).First();

        using var rejected = await Send(client, HttpMethod.Post, Route,
            new { driverId, truckId = seeded.GetProperty("truckId").GetGuid() }, "assoc-dup-1");

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("DUPLICATE_ASSOCIATION", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var still = (await List(client, $"{Route}?active=true")).Single(a => a.GetProperty("id").GetGuid() == seeded.GetProperty("id").GetGuid());
        Assert.Equal(seeded.GetProperty("driverId").GetGuid(), still.GetProperty("driverId").GetGuid());
    }

    [Fact]
    public async Task A_driver_can_hold_several_trucks()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550179", "20555000179");
        var first = await AddTruck("AG901AA");
        var second = await AddTruck("AG902AA");

        using var one = await Send(client, HttpMethod.Post, Route, new { driverId, truckId = first }, "assoc-multi-1");
        using var two = await Send(client, HttpMethod.Post, Route, new { driverId, truckId = second }, "assoc-multi-2");

        Assert.Equal(HttpStatusCode.Created, one.StatusCode);
        Assert.Equal(HttpStatusCode.Created, two.StatusCode);
        var driver = (await List(client, "/api/v1/drivers")).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.Equal(["AG901AA", "AG902AA"], driver.GetProperty("activeTruckPlates").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task Inactivating_an_association_keeps_it_as_history_and_frees_the_truck()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550180", "20555000180");
        var truckId = await AddTruck("AG903AA");
        using var created = await Send(client, HttpMethod.Post, Route, new { driverId, truckId }, "assoc-hist-1");
        var associationId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid();

        using var inactivated = await Send(client, HttpMethod.Post, $"{Route}/{associationId}/inactivation", null, "assoc-hist-2");

        Assert.Equal(HttpStatusCode.OK, inactivated.StatusCode);
        var history = (await List(client, $"{Route}?active=false")).Single(a => a.GetProperty("id").GetGuid() == associationId);
        Assert.False(history.GetProperty("isActive").GetBoolean());
        Assert.Equal(["driverId", "id", "isActive", "truckId"],
            history.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        var driver = (await List(client, "/api/v1/drivers")).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.False(driver.GetProperty("canRequestAppointments").GetBoolean());

        var otherDriver = await CreateDriver(client, "+5493815550181", "20555000181");
        using var reassigned = await Send(client, HttpMethod.Post, Route, new { driverId = otherDriver, truckId }, "assoc-hist-3");
        Assert.Equal(HttpStatusCode.Created, reassigned.StatusCode);
    }

    [Fact]
    public async Task Re_associating_the_same_pair_reactivates_the_historical_association()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550183", "20555000183");
        var truckId = await AddTruck("AG905AA");
        using var created = await Send(client, HttpMethod.Post, Route, new { driverId, truckId }, "assoc-again-1");
        var associationId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid();
        using var _ = await Send(client, HttpMethod.Post, $"{Route}/{associationId}/inactivation", null, "assoc-again-2");

        using var again = await Send(client, HttpMethod.Post, Route, new { driverId, truckId }, "assoc-again-3");

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal(associationId, (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid());
        var pair = (await List(client, Route)).Where(a => a.GetProperty("truckId").GetGuid() == truckId).ToList();
        Assert.True(Assert.Single(pair).GetProperty("isActive").GetBoolean());
        var driver = (await List(client, "/api/v1/drivers")).Single(d => d.GetProperty("id").GetGuid() == driverId);
        Assert.True(driver.GetProperty("canRequestAppointments").GetBoolean());
    }

    [Fact]
    public async Task Unknown_or_inactive_resources_return_not_found()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var driverId = await CreateDriver(client, "+5493815550182", "20555000182");
        var inactiveTruck = await AddTruck("AG904AA", active: false);

        using var unknownDriver = await Send(client, HttpMethod.Post, Route, new { driverId = Guid.NewGuid(), truckId = inactiveTruck }, "assoc-404-1");
        using var inactive = await Send(client, HttpMethod.Post, Route, new { driverId, truckId = inactiveTruck }, "assoc-404-2");
        using var unknownAssociation = await Send(client, HttpMethod.Post, $"{Route}/{Guid.NewGuid()}/inactivation", null, "assoc-404-3");

        foreach (var response in new[] { unknownDriver, inactive, unknownAssociation })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("ASSOCIATION_REFERENCE_NOT_FOUND", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Mutations_validate_body_and_require_an_idempotency_key()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var withoutKey = await Send(client, HttpMethod.Post, Route, new { driverId = Guid.NewGuid(), truckId = Guid.NewGuid() }, null);
        using var emptyIds = await Send(client, HttpMethod.Post, Route, new { driverId = Guid.Empty, truckId = (Guid?)null }, "assoc-val-1");
        using var unknownField = await Send(client, HttpMethod.Post, Route, new { driverId = Guid.NewGuid(), truckId = Guid.NewGuid(), plate = "X" }, "assoc-val-2");
        using var badId = await Send(client, HttpMethod.Post, $"{Route}/not-a-guid/inactivation", null, "assoc-val-3");
        using var inactivationWithoutKey = await Send(client, HttpMethod.Post, $"{Route}/{Guid.NewGuid()}/inactivation", null, null);

        foreach (var response in new[] { withoutKey, emptyIds, unknownField, badId, inactivationWithoutKey })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("VALIDATION_ERROR", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Requires_the_dashboard_key()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> AddTruck(string plate, bool active = true)
    {
        await using var db = CreateContext();
        var ingenioId = await db.Transportistas.Select(t => t.IngenioId).FirstAsync();
        var truck = new Camion(ingenioId, plate, Camion.FleetTypeTerceros);
        if (!active) truck.Inactivate(DateTime.UtcNow);
        db.Camiones.Add(truck);
        await db.SaveChangesAsync();
        return truck.Id;
    }

    private static async Task<Guid> CreateDriver(HttpClient client, string phone, string dni)
    {
        using var created = await Send(client, HttpMethod.Post, "/api/v1/drivers",
            new { name = $"Transportista {dni}", dni, whatsApp = phone }, $"driver-{dni}");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resourceId").GetGuid();
    }

    private static async Task<List<JsonElement>> List(HttpClient client, string path)
    {
        using var response = await Send(client, HttpMethod.Get, path, null, null);
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
