using Dsw2025Tpi.Data.Appointments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.Tests;

/// <summary>
/// Un token ya cancelado fuerza una falla determinística en
/// MigrateAsync/SeedAsync sin corromper ningún archivo: el arranque de la
/// demo debe propagarla en vez de servir con estado incompleto.
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
    public async Task Initialization_propagates_failures()
    {
        await using var db = CreateContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AppointmentsStartup.InitializeAsync(db, TimeProvider.System, cts.Token));
    }
}
