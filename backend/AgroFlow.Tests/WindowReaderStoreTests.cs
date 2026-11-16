using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Lectura de ventanas (B39, #44) contra SQLite real a nivel repositorio: es
/// donde se prueba el aislamiento entre ingenios y la zona horaria propia de
/// cada uno, porque LocalDemo sólo opera con un ingenio por base.
/// </summary>
public sealed class WindowReaderStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-window-reader-{Guid.NewGuid():N}.db");

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Each_ingenio_only_sees_its_own_windows_even_on_the_same_instants()
    {
        var start = DateTimeOffset.Parse("2026-09-28T08:00:00-03:00");
        await using var db = CreateContext();
        var own = new Ingenio("Ingenio Fixture Propio", "America/Argentina/Tucuman");
        var other = new Ingenio("Ingenio Fixture Ajeno", "America/Argentina/Tucuman");
        var ownWindow = new Ventana(own.Id, start, start.AddMinutes(30), 2);
        var otherWindow = new Ventana(other.Id, start, start.AddMinutes(30), 9);
        db.AddRange(own, other, ownWindow, otherWindow);
        await db.SaveChangesAsync();
        var store = new EfAppointmentStore(db, new FixedClock(start));

        var ownList = await store.ListWindowsAsync(own.Id, new DateOnly(2026, 9, 28), CancellationToken.None);
        var otherList = await store.ListWindowsAsync(other.Id, new DateOnly(2026, 9, 28), CancellationToken.None);

        Assert.Equal(ownWindow.Id, Assert.Single(ownList).Id);
        Assert.Equal(2, ownList[0].Capacity);
        Assert.Equal(otherWindow.Id, Assert.Single(otherList).Id);
        Assert.Equal(9, otherList[0].Capacity);
    }

    [Fact]
    public async Task Dates_and_offsets_follow_the_time_zone_of_the_ingenio()
    {
        await using var db = CreateContext();
        var tokyo = new Ingenio("Ingenio Fixture Tokio", "Asia/Tokyo");
        // 00:30 en Tokio (+09:00) es todavía 27/09 en UTC y en Tucumán.
        var early = DateTimeOffset.Parse("2026-09-28T00:30:00+09:00");
        var evening = DateTimeOffset.Parse("2026-09-28T23:30:00+09:00");
        var nextDay = DateTimeOffset.Parse("2026-09-29T00:00:00+09:00");
        db.AddRange(tokyo,
            new Ventana(tokyo.Id, nextDay, nextDay.AddMinutes(30), 2),
            new Ventana(tokyo.Id, evening, evening.AddMinutes(30), 2),
            new Ventana(tokyo.Id, early, early.AddMinutes(30), 2));
        await db.SaveChangesAsync();
        var store = new EfAppointmentStore(db, new FixedClock(DateTimeOffset.Parse("2026-09-28T12:00:00Z")));

        var day = await store.ListWindowsAsync(tokyo.Id, new DateOnly(2026, 9, 28), CancellationToken.None);
        var previous = await store.ListWindowsAsync(tokyo.Id, new DateOnly(2026, 9, 27), CancellationToken.None);

        Assert.Equal(["2026-09-28T00:30:00+09:00", "2026-09-28T23:30:00+09:00"],
            day.Select(w => w.StartAt.ToString("yyyy-MM-dd'T'HH:mm:sszzz")));
        Assert.All(day, w => Assert.Equal(TimeSpan.FromHours(9), w.StartAt.Offset));
        Assert.Empty(previous);
    }

    [Fact]
    public async Task Without_a_date_uses_the_current_local_day_of_the_ingenio()
    {
        await using var db = CreateContext();
        var ingenio = new Ingenio("Ingenio Fixture Local", "America/Argentina/Tucuman");
        var today = DateTimeOffset.Parse("2026-09-28T22:00:00-03:00");
        var tomorrow = DateTimeOffset.Parse("2026-09-29T08:00:00-03:00");
        db.AddRange(ingenio,
            new Ventana(ingenio.Id, today, today.AddMinutes(30), 2),
            new Ventana(ingenio.Id, tomorrow, tomorrow.AddMinutes(30), 2));
        await db.SaveChangesAsync();
        // 01:00 UTC del 29/09 son las 22:00 del 28/09 en el ingenio.
        var store = new EfAppointmentStore(db, new FixedClock(DateTimeOffset.Parse("2026-09-29T01:00:00Z")));

        var windows = await store.ListWindowsAsync(ingenio.Id, null, CancellationToken.None);

        Assert.Equal(today, Assert.Single(windows).StartAt);
    }

    [Fact]
    public async Task Windows_are_ordered_by_start_and_report_the_persisted_occupancy()
    {
        await using var db = CreateContext();
        var ingenio = new Ingenio("Ingenio Fixture Orden", "America/Argentina/Tucuman");
        var late = DateTimeOffset.Parse("2026-09-28T10:00:00-03:00");
        var early = DateTimeOffset.Parse("2026-09-28T08:00:00-03:00");
        db.AddRange(ingenio,
            new Ventana(ingenio.Id, late, late.AddMinutes(30), 5),
            new Ventana(ingenio.Id, early, early.AddMinutes(30), 2));
        await db.SaveChangesAsync();
        var lateId = await db.Ventanas.Where(v => v.StartAt == late).Select(v => v.Id).SingleAsync();
        await db.Ventanas.Where(v => v.Id == lateId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Occupied, 3));
        var store = new EfAppointmentStore(db, new FixedClock(early));

        var windows = await store.ListWindowsAsync(ingenio.Id, new DateOnly(2026, 9, 28), CancellationToken.None);

        Assert.Equal([early, late], windows.Select(w => w.StartAt));
        Assert.Equal([0, 3], windows.Select(w => w.Occupied));
        Assert.Equal([2, 5], windows.Select(w => w.Capacity));
    }

    private AgroFlowDbContext CreateContext()
    {
        var context = new AgroFlowDbContext(new DbContextOptionsBuilder<AgroFlowDbContext>()
            .UseSqlite($"Data Source={_dbPath};Default Timeout=5")
            .Options);
        context.Database.Migrate();
        return context;
    }

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
