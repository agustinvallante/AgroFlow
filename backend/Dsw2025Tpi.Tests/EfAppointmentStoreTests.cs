using Dsw2025Tpi.Application.Appointments;
using Dsw2025Tpi.Data.Appointments;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.Tests;

/// <summary>
/// Persistence/integration tests for Persona 2's real adapter, against a
/// file-backed SQLite database (never :memory:, so tests can reopen a brand
/// new DbContext/connection and prove data actually survives a restart).
/// </summary>
public sealed class EfAppointmentStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-28T12:00:00-03:00");
    private readonly string _dbPath;

    public EfAppointmentStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-tests-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite agrupa conexiones por cadena de conexión; sin
        // esto el archivo queda en uso y el delete de abajo falla.
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
        // Default Timeout (busy_timeout) evita "database is locked" cuando
        // varios contextos escriben casi al mismo tiempo contra el mismo
        // archivo, como en las pruebas de concurrencia real de abajo.
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
            .UseSqlite($"Data Source={_dbPath};Default Timeout=5")
            .Options;
        var context = new AgroFlowDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private static async Task<(Guid TransportistaId, Guid CamionId)> CreateExtraTruckAsync(
        AgroFlowDbContext db, Guid ingenioId, string phone, string plate)
    {
        var transportista = new Transportista(ingenioId, "Transportista Demo Tres", "20333333333", phone);
        var camion = new Camion(ingenioId, plate, Camion.FleetTypePropia);
        db.Transportistas.Add(transportista);
        db.Camiones.Add(camion);
        db.Asociaciones.Add(new TransportistaCamion(ingenioId, transportista.Id, camion.Id));
        await db.SaveChangesAsync();
        return (transportista.Id, camion.Id);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task SeedAsync(DateTimeOffset now)
    {
        await using var db = CreateContext();
        await AppointmentsSeeder.SeedAsync(db, new FixedClock(now));
    }

    private static readonly CreateAppointmentCommand ValidCreate = new(
        "+5493815550101", "AF123BC", "FINCA-NORTE", Now.AddHours(2), 28.5);

    [Fact]
    public async Task Seed_run_twice_does_not_duplicate_master_data_or_windows()
    {
        await SeedAsync(Now);
        await SeedAsync(Now);

        await using var db = CreateContext();
        Assert.Equal(1, await db.Ingenios.CountAsync());
        Assert.Equal(2, await db.Transportistas.CountAsync());
        Assert.Equal(2, await db.Camiones.CountAsync());
        Assert.Equal(2, await db.Fincas.CountAsync());
        Assert.Equal(2, await db.Asociaciones.CountAsync());
        var windowCountAfterTwoRuns = await db.Ventanas.CountAsync();

        await SeedAsync(Now);
        await using var db2 = CreateContext();
        Assert.Equal(windowCountAfterTwoRuns, await db2.Ventanas.CountAsync());
    }

    [Fact]
    public async Task GetCreationContext_resolves_normalized_variants_and_reports_no_active_appointment()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var command = ValidCreate with { TruckPlate = "af-123 bc", FarmCode = "finca-norte" };

        var context = await store.GetCreationContextAsync(command);

        Assert.NotNull(context);
        Assert.False(context!.HasActiveAppointment);
        Assert.NotEmpty(context.Windows);
    }

    [Theory]
    [InlineData("+5493815559999", "AF123BC", "FINCA-NORTE")]
    [InlineData("+5493815550101", "ZZ000000", "FINCA-NORTE")]
    [InlineData("+5493815550101", "AF123BC", "FINCA-INEXISTENTE")]
    public async Task GetCreationContext_returns_null_for_unknown_reference(string phone, string plate, string farmCode)
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));

        var context = await store.GetCreationContextAsync(ValidCreate with
        {
            CarrierPhone = phone,
            TruckPlate = plate,
            FarmCode = farmCode
        });

        Assert.Null(context);
    }

    [Fact]
    public async Task GetCreationContext_returns_null_when_association_is_inactive()
    {
        await SeedAsync(Now);
        await using (var db = CreateContext())
        {
            var asociacion = await db.Asociaciones.FirstAsync();
            asociacion.Inactivate();
            await db.SaveChangesAsync();
        }

        await using var readDb = CreateContext();
        var store = new EfAppointmentStore(readDb, new FixedClock(Now));

        var context = await store.GetCreationContextAsync(ValidCreate);

        Assert.Null(context);
    }

    [Fact]
    public async Task Create_reserves_capacity_and_persists_an_assigned_turno()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var context = await store.GetCreationContextAsync(ValidCreate);
        var window = Assert.Single(context!.Windows.Where(w => w.Occupied == 0).Take(1));

        var result = await store.TryCreateAssignedAsync(ValidCreate, window.Id);

        Assert.Equal(AppointmentCreateOutcome.Success, result.Outcome);
        Assert.Equal(AppointmentStatus.Asignado, result.Appointment!.Status);
        Assert.Equal("AF123BC", result.Appointment.Truck.Plate);
        Assert.Equal("Finca Norte", result.Appointment.Farm.Name);
        Assert.Equal(TimeSpan.FromHours(-3), result.Appointment.Window.StartAt.Offset);

        await using var verifyDb = CreateContext();
        var persistedWindow = await verifyDb.Ventanas.SingleAsync(v => v.Id == window.Id);
        Assert.Equal(1, persistedWindow.Occupied);
        Assert.Equal(1, await verifyDb.Turnos.CountAsync());
    }

    [Fact]
    public async Task Create_rejects_a_second_active_appointment_for_the_same_truck()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var context = await store.GetCreationContextAsync(ValidCreate);
        var windows = context!.Windows.Where(w => w.Occupied == 0).OrderBy(w => w.StartAt).ToList();
        var first = await store.TryCreateAssignedAsync(ValidCreate, windows[0].Id);
        Assert.Equal(AppointmentCreateOutcome.Success, first.Outcome);

        var second = await store.TryCreateAssignedAsync(ValidCreate, windows[1].Id);

        Assert.Equal(AppointmentCreateOutcome.ActiveAppointmentExists, second.Outcome);
        Assert.Null(second.Appointment);
        await using var verifyDb = CreateContext();
        Assert.Equal(1, await verifyDb.Turnos.CountAsync());
        Assert.Equal(0, (await verifyDb.Ventanas.SingleAsync(v => v.Id == windows[1].Id)).Occupied);
    }

    [Fact]
    public async Task Three_concurrent_attempts_on_a_window_with_capacity_two_let_exactly_two_succeed()
    {
        // Concurrencia real: tres tareas, cada una con su propio
        // AgroFlowDbContext/conexión contra el mismo archivo SQLite,
        // corriendo con Task.WhenAll. Nada de llamadas secuenciales
        // simulando una carrera: el ganador lo decide únicamente el UPDATE
        // atómico del store bajo contención real.
        await SeedAsync(Now);
        Guid windowId;
        await using (var setupDb = CreateContext())
        {
            var ingenio = await setupDb.Ingenios.SingleAsync();
            await CreateExtraTruckAsync(setupDb, ingenio.Id, "+5493815550103", "AF789GH");
            // OrderBy sobre StartAtUtc (DateTime), no StartAt (DateTimeOffset):
            // SQLite tampoco traduce ORDER BY sobre DateTimeOffset.
            var window = await setupDb.Ventanas.Where(v => v.StartAtUtc > Now.UtcDateTime)
                .OrderBy(v => v.StartAtUtc)
                .FirstAsync();
            windowId = window.Id;
            Assert.Equal(2, window.Capacity);
        }

        var commands = new[]
        {
            ValidCreate,
            new CreateAppointmentCommand("+5493815550102", "AF456DE", "FINCA-SUR", Now.AddHours(2), 12.0),
            new CreateAppointmentCommand("+5493815550103", "AF789GH", "FINCA-NORTE", Now.AddHours(2), 9.0)
        };

        var results = await Task.WhenAll(commands.Select(async command =>
        {
            await using var db = CreateContext();
            var store = new EfAppointmentStore(db, new FixedClock(Now));
            return await store.TryCreateAssignedAsync(command, windowId);
        }));

        Assert.Equal(2, results.Count(r => r.Outcome == AppointmentCreateOutcome.Success));
        Assert.Equal(1, results.Count(r => r.Outcome == AppointmentCreateOutcome.CapacityChanged));
        Assert.All(results.Where(r => r.Outcome == AppointmentCreateOutcome.CapacityChanged),
            r => Assert.Null(r.Appointment));

        await using var verifyDb = CreateContext();
        var persistedWindow = await verifyDb.Ventanas.SingleAsync(v => v.Id == windowId);
        Assert.Equal(2, persistedWindow.Capacity);
        Assert.Equal(2, persistedWindow.Occupied);
        Assert.Equal(2, await verifyDb.Turnos.CountAsync(t => t.VentanaId == windowId));
    }

    [Fact]
    public async Task Third_truck_is_assigned_the_next_window_once_the_first_is_full()
    {
        // A través de AppointmentService real (no un fake): el store sólo
        // informa CapacityChanged/ventanas; quien reintenta con la siguiente
        // ventana es el servicio, ya probado con un store fake en
        // AppointmentServiceTests. Esto prueba que ambas piezas combinan
        // bien con cupo 2 real.
        await SeedAsync(Now);
        await using var db = CreateContext();
        var ingenio = await db.Ingenios.SingleAsync();
        await CreateExtraTruckAsync(db, ingenio.Id, "+5493815550103", "AF789GH");
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var service = new AppointmentService(store, new FixedClock(Now));
        var windows = (await store.GetCreationContextAsync(ValidCreate))!.Windows
            .OrderBy(w => w.StartAt).ToList();

        var truck1 = await service.CreateAsync(ValidCreate);
        var truck2 = await service.CreateAsync(new CreateAppointmentCommand(
            "+5493815550102", "AF456DE", "FINCA-SUR", Now.AddHours(2), 12.0));
        var truck3 = await service.CreateAsync(new CreateAppointmentCommand(
            "+5493815550103", "AF789GH", "FINCA-NORTE", Now.AddHours(2), 9.0));

        Assert.Equal(windows[0].StartAt, truck1.Window.StartAt);
        Assert.Equal(windows[0].StartAt, truck2.Window.StartAt);
        Assert.Equal(windows[1].StartAt, truck3.Window.StartAt);
    }

    [Fact]
    public async Task TryCreateAssignedAsync_rejects_a_window_whose_start_time_passed_by_commit_time()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var readStore = new EfAppointmentStore(db, new FixedClock(Now));
        var context = await readStore.GetCreationContextAsync(ValidCreate);
        var window = context!.Windows.OrderBy(w => w.StartAt).First();

        // El reloj avanzó más allá del inicio de la ventana entre la lectura
        // (GetCreationContextAsync) y este intento de confirmación.
        var lateStore = new EfAppointmentStore(db, new FixedClock(window.StartAt.AddSeconds(1)));
        var result = await lateStore.TryCreateAssignedAsync(ValidCreate, window.Id);

        Assert.Equal(AppointmentCreateOutcome.CapacityChanged, result.Outcome);
        Assert.Null(result.Appointment);

        await using var verifyDb = CreateContext();
        var persistedWindow = await verifyDb.Ventanas.SingleAsync(v => v.Id == window.Id);
        Assert.Equal(0, persistedWindow.Occupied);
        Assert.Equal(0, await verifyDb.Turnos.CountAsync());
    }

    [Fact]
    public async Task TransportistaCamion_unique_index_rejects_a_second_active_association_for_the_same_truck()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var ingenio = await db.Ingenios.SingleAsync();
        var camion = await db.Camiones.SingleAsync(c => c.NormalizedPlate == "AF123BC");
        var otroTransportista = new Transportista(ingenio.Id, "Transportista Demo Otro", "20444444444", "+5493815550199");
        db.Transportistas.Add(otroTransportista);
        db.Asociaciones.Add(new TransportistaCamion(ingenio.Id, otroTransportista.Id, camion.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Seed_does_not_reactivate_an_association_disabled_on_purpose()
    {
        await SeedAsync(Now);
        await using (var db = CreateContext())
        {
            var asociacion = await db.Asociaciones.FirstAsync();
            asociacion.Inactivate();
            await db.SaveChangesAsync();
        }

        await SeedAsync(Now);

        await using var verifyDb = CreateContext();
        Assert.Contains(await verifyDb.Asociaciones.ToListAsync(), a => !a.IsActive);
    }

    [Fact]
    public async Task List_defaults_to_ingenio_local_today_even_with_other_filters_and_returns_empty_without_matches()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var context = await store.GetCreationContextAsync(ValidCreate);
        var window = context!.Windows.OrderBy(w => w.StartAt).First();
        await store.TryCreateAssignedAsync(ValidCreate, window.Id);

        var todayMatches = await store.ListAsync(new AppointmentQuery(
            null, AppointmentStatus.Asignado, "af-123 bc", "+5493815550101"));
        var noMatches = await store.ListAsync(new AppointmentQuery(
            null, null, null, "+5493815559999"));

        Assert.Single(todayMatches);
        Assert.Empty(noMatches);
    }

    [Fact]
    public async Task List_orders_by_window_start_ascending()
    {
        await SeedAsync(Now);
        await using var db = CreateContext();
        var store = new EfAppointmentStore(db, new FixedClock(Now));
        var context = await store.GetCreationContextAsync(ValidCreate);
        var windows = context!.Windows.OrderBy(w => w.StartAt).ToList();

        var second = new CreateAppointmentCommand(
            "+5493815550102", "AF456DE", "FINCA-SUR", Now.AddHours(2), 12.0);
        await store.TryCreateAssignedAsync(second, windows[1].Id);
        await store.TryCreateAssignedAsync(ValidCreate, windows[0].Id);

        var result = await store.ListAsync(new AppointmentQuery(null, null, null, null));

        Assert.True(result.Count >= 2);
        Assert.True(result[0].Window.StartAt <= result[1].Window.StartAt);
    }

    [Fact]
    public async Task Transition_persists_and_a_concurrent_state_change_is_detected()
    {
        await SeedAsync(Now);
        Guid appointmentId;
        await using (var db = CreateContext())
        {
            var store = new EfAppointmentStore(db, new FixedClock(Now));
            var context = await store.GetCreationContextAsync(ValidCreate);
            var window = context!.Windows.OrderBy(w => w.StartAt).First();
            var created = await store.TryCreateAssignedAsync(ValidCreate, window.Id);
            appointmentId = created.Appointment!.Id;
        }

        await using var db2 = CreateContext();
        var store2 = new EfAppointmentStore(db2, new FixedClock(Now));

        var success = await store2.TryTransitionAsync(
            appointmentId, AppointmentStatus.Asignado, AppointmentStatus.EnCamino, releaseCapacity: false);
        Assert.Equal(AppointmentTransitionOutcome.Success, success.Outcome);
        Assert.Equal(AppointmentStatus.EnCamino, success.Appointment!.Status);

        var stale = await store2.TryTransitionAsync(
            appointmentId, AppointmentStatus.Asignado, AppointmentStatus.EnCamino, releaseCapacity: false);
        Assert.Equal(AppointmentTransitionOutcome.StateChanged, stale.Outcome);
        Assert.Null(stale.Appointment);

        var missing = await store2.TryTransitionAsync(
            Guid.NewGuid(), AppointmentStatus.Asignado, AppointmentStatus.EnCamino, releaseCapacity: false);
        Assert.Equal(AppointmentTransitionOutcome.NotFound, missing.Outcome);
    }

    [Fact]
    public async Task Cancelling_releases_capacity_atomically_and_the_turno_stays_queryable()
    {
        await SeedAsync(Now);
        Guid appointmentId;
        Guid windowId;
        await using (var db = CreateContext())
        {
            var store = new EfAppointmentStore(db, new FixedClock(Now));
            var context = await store.GetCreationContextAsync(ValidCreate);
            var window = context!.Windows.OrderBy(w => w.StartAt).First();
            windowId = window.Id;
            var created = await store.TryCreateAssignedAsync(ValidCreate, window.Id);
            appointmentId = created.Appointment!.Id;
        }

        await using (var db = CreateContext())
        {
            var store = new EfAppointmentStore(db, new FixedClock(Now));
            var result = await store.TryTransitionAsync(
                appointmentId, AppointmentStatus.Asignado, AppointmentStatus.Cancelado, releaseCapacity: true);
            Assert.Equal(AppointmentTransitionOutcome.Success, result.Outcome);
            Assert.Equal(AppointmentStatus.Cancelado, result.Appointment!.Status);
        }

        await using var verifyDb = CreateContext();
        var window2 = await verifyDb.Ventanas.SingleAsync(v => v.Id == windowId);
        Assert.Equal(0, window2.Occupied);
        var turno = await verifyDb.Turnos.SingleAsync(t => t.Id == appointmentId);
        Assert.False(turno.IsActive);

        // El cupo liberado debe poder reutilizarse por otro camión.
        var store3 = new EfAppointmentStore(verifyDb, new FixedClock(Now));
        var other = new CreateAppointmentCommand(
            "+5493815550102", "AF456DE", "FINCA-SUR", Now.AddHours(2), 12.0);
        var reuse = await store3.TryCreateAssignedAsync(other, windowId);
        Assert.Equal(AppointmentCreateOutcome.Success, reuse.Outcome);
    }

    [Fact]
    public async Task Data_survives_reopening_a_brand_new_dbcontext_against_the_same_sqlite_file()
    {
        await SeedAsync(Now);
        Guid appointmentId;
        await using (var db = CreateContext())
        {
            var store = new EfAppointmentStore(db, new FixedClock(Now));
            var context = await store.GetCreationContextAsync(ValidCreate);
            var window = context!.Windows.OrderBy(w => w.StartAt).First();
            var created = await store.TryCreateAssignedAsync(ValidCreate, window.Id);
            appointmentId = created.Appointment!.Id;
        }

        // Simula un reinicio del proceso: contexto y conexión completamente nuevos.
        await using var freshDb = CreateContext();
        var freshStore = new EfAppointmentStore(freshDb, new FixedClock(Now));

        var snapshot = await freshStore.GetAsync(appointmentId);

        Assert.NotNull(snapshot);
        Assert.Equal(AppointmentStatus.Asignado, snapshot!.Status);
        Assert.Equal("AF123BC", snapshot.Truck.Plate);
    }
}
