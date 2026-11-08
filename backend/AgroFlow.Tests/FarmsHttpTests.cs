using System.Net;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Security;
using AgroFlow.Data.Appointments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Consulta de fincas sobre la aplicación real en LocalDemo, con el seed
/// ficticio (FINCA-NORTE "Finca Norte" y FINCA-SUR "Finca Sur") en SQLite
/// temporal migrada.
/// </summary>
public sealed class FarmsHttpTests : IDisposable
{
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-farms-http-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Farms_require_the_channel_or_the_dashboard_key()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var anonymous = await client.GetAsync("/api/v1/farms");
        using var wrongKey = await Get(client, "/api/v1/farms", ApiKeySchemes.ChannelHeader, "wrong-key-wrong-key-wrong-key-wrong-key");
        using var channel = await Get(client, "/api/v1/farms", ApiKeySchemes.ChannelHeader, ChannelKey);
        using var dashboard = await Get(client, "/api/v1/farms", ApiKeySchemes.DashboardHeader, DashboardKey);

        foreach (var response in new[] { anonymous, wrongKey })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("UNAUTHENTICATED", problem.RootElement.GetProperty("code").GetString());
        }

        Assert.Equal(HttpStatusCode.OK, channel.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var farms = await Farms(channel);
        Assert.Equal(["FINCA-NORTE", "FINCA-SUR"], farms.Select(f => f.GetProperty("code").GetString()).Order());
        Assert.All(farms, farm => Assert.Equal(
            ["code", "id", "isActive", "name"], farm.EnumerateObject().Select(p => p.Name).Order()));
    }

    [Theory]
    [InlineData("FINCA-NORTE", "FINCA-NORTE")]
    [InlineData("Finca Norte", "FINCA-NORTE")]
    [InlineData("norte", "FINCA-NORTE")]
    [InlineData("finca sur", "FINCA-SUR")]
    public async Task Exact_and_natural_names_return_the_single_seeded_farm(string search, string expectedCode)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await Channel(client, $"/api/v1/farms?active=true&search={Uri.EscapeDataString(search)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var farm = Assert.Single(await Farms(response));
        Assert.Equal(expectedCode, farm.GetProperty("code").GetString());
        Assert.True(farm.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task An_unknown_name_returns_an_empty_list_and_an_inactive_farm_is_not_offered()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await using (var db = CreateContext())
        {
            var sur = await db.Fincas.SingleAsync(f => f.Code == "FINCA-SUR");
            sur.Inactivate(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        using var unknown = await Channel(client, "/api/v1/farms?active=true&search=Finca%20Oeste");
        using var inactive = await Channel(client, "/api/v1/farms?active=true&search=sur");
        using var history = await Channel(client, "/api/v1/farms?search=sur");

        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Empty(await Farms(unknown));
        Assert.Empty(await Farms(inactive));
        Assert.False(Assert.Single(await Farms(history)).GetProperty("isActive").GetBoolean());
    }

    [Theory]
    [InlineData("/api/v1/farms?search=")]
    [InlineData("/api/v1/farms?search=norte&search=sur")]
    [InlineData("/api/v1/farms?active=maybe")]
    [InlineData("/api/v1/farms?code=FINCA-NORTE")]
    public async Task Invalid_queries_return_the_canonical_validation_problem(string url)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await Channel(client, url);

        await AssertProblem(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task A_search_longer_than_the_contract_is_rejected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await Channel(client, $"/api/v1/farms?search={new string('a', 101)}");

        await AssertProblem(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task A_persistence_failure_is_an_internal_error_not_an_empty_list()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using (var healthy = await Channel(client, "/api/v1/farms"))
            Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        await using (var db = CreateContext())
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Fincas RENAME TO FincasNoDisponibles");

        using var response = await Channel(client, "/api/v1/farms?active=true&search=norte");

        await AssertProblem(response, HttpStatusCode.InternalServerError, "INTERNAL_ERROR");
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
    }

    private static async Task<List<JsonElement>> Farms(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.EnumerateArray().Select(element => element.Clone()).ToList();
    }

    private static Task<HttpResponseMessage> Channel(HttpClient client, string path) =>
        Get(client, path, ApiKeySchemes.ChannelHeader, ChannelKey);

    private static Task<HttpResponseMessage> Get(HttpClient client, string path, string header, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(header, key);
        return client.SendAsync(request);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
        // Crear el cliente arranca el host, que migra y siembra la base.
        factory.CreateClient().Dispose();
        return factory;
    }

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
