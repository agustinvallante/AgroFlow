using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Data.Appointments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// #112 de punta a punta en LocalDemo, con SQLite real: una patente sin letras
/// ni dígitos es un error de validación por campo y no escribe turnos ni cupos.
/// </summary>
public sealed class TruckPlateValidationLocalDemoTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-plate-{Guid.NewGuid():N}.db");

    [Theory]
    [InlineData("------")]
    [InlineData("-- -- --")]
    public async Task Plate_without_letters_or_digits_returns_400_without_writing(string plate)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var (turnosBefore, occupiedBefore) = await CountAsync(factory);

        using var list = await client.GetAsync($"/api/v1/appointments?truckPlate={Uri.EscapeDataString(plate)}");
        using var create = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            carrierPhone = "+5493815550101",
            truckPlate = plate,
            farmCode = "FINCA-NORTE",
            cutAt = "2026-09-28T05:30:00-03:00",
            estimatedLoadTons = 28.5
        });

        await AssertPlateErrorAsync(list);
        await AssertPlateErrorAsync(create);
        Assert.Equal((turnosBefore, occupiedBefore), await CountAsync(factory));
    }

    [Theory]
    [InlineData("AF123BC")]
    [InlineData("af-123 bc")]
    public async Task Plate_with_separators_or_lowercase_is_still_a_valid_filter(string plate)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/appointments?date=2026-09-28&truckPlate={Uri.EscapeDataString(plate)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
    }

    private static async Task AssertPlateErrorAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("code").GetString());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("truckPlate", out _));
        Assert.Single(errors.EnumerateObject());
    }

    private static async Task<(int Turnos, int Occupied)> CountAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroFlowDbContext>();
        return (await db.Turnos.CountAsync(), await db.Ventanas.SumAsync(window => window.Occupied));
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
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            File.Delete(_dbPath + suffix);
    }
}
