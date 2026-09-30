using System.Net;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Application.Appointments;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Prueba la aplicación real, no un host reducido que sólo registra turnos.
/// Verifica el límite de B01 en el perfil que usa frontend y n8n.
/// </summary>
public sealed class BackendSurfaceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"agroflow-surface-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Local_demo_exposes_appointments_but_not_ecommerce_or_legacy_registration()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("LocalDemo:Enabled", "true");
                builder.UseSetting("ConnectionStrings:AgroFlowDb", $"Data Source={_dbPath}");
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var health = await client.GetAsync("/health");
        using var appointments = await client.GetAsync("/api/v1/appointments");
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, appointments.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);

        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToArray();
        Assert.Contains(paths, path => path.Contains("/appointments", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/products", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/orders", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase));

        foreach (var retiredPath in new[] { "/api/products", "/api/orders" })
        {
            using var response = await client.GetAsync(retiredPath);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        using var retiredRegistration = await client.PostAsync(
            "/api/auth/register", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, retiredRegistration.StatusCode);
        using var excludedLogin = await client.PostAsync(
            "/api/auth/login", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, excludedLogin.StatusCode);
    }

    [Fact]
    public async Task Identity_scaffold_keeps_login_but_not_customer_registration()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("LocalDemo:Enabled", "false");
                builder.UseSetting("Jwt:Key", "test-only-key-for-b01-surface-check-123456789");
            });
        using var client = factory.CreateClient();
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToArray();
        Assert.Contains(paths, path => path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/appointments", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/products", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, path => path.Contains("/orders", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_assemblies_contain_no_commerce_entities()
    {
        var assemblies = new[]
        {
            typeof(Program).Assembly,
            typeof(AppointmentService).Assembly,
            typeof(AgroFlowDbContext).Assembly,
            typeof(Turno).Assembly
        };
        var retiredTypes = new[]
        {
            "Product", "Order", "OrderItem", "Customer",
            "ProductsController", "OrdersController"
        };

        foreach (var assembly in assemblies)
        {
            Assert.DoesNotContain(assembly.GetExportedTypes(),
                type => retiredTypes.Contains(type.Name, StringComparer.Ordinal));
        }
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            var path = _dbPath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
