using Dsw2025Tpi.Data.Appointments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.Tests;

/// <summary>
/// Un token ya cancelado fuerza una falla determinística en
/// MigrateAsync/SeedAsync sin corromper ningún archivo: alcanza para probar
/// el contrato de failFast sin depender del motor real de migraciones.
/// </summary>
public sealed class AppointmentsStartupTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-startup-tests-{Guid.NewGuid():N}.db");

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

    private AgroFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        return new AgroFlowDbContext(options);
    }

    [Fact]
    public async Task FailFast_true_propagates_initialization_failures()
    {
        await using var db = CreateContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AppointmentsStartup.InitializeAsync(db, TimeProvider.System, failFast: true, cts.Token));
    }

    [Fact]
    public async Task FailFast_false_swallows_initialization_failures()
    {
        await using var db = CreateContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // No debe lanzar: conserva el comportamiento heredado de loguear y continuar.
        await AppointmentsStartup.InitializeAsync(db, TimeProvider.System, failFast: false, cts.Token);
    }
}
