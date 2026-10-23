using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// El seed identifica sus filas por una clave estable (SeedRecords), no por
/// atributos editables. Reproduce el hallazgo P0: editar el WhatsApp de un
/// transportista sembrado hacía que el reinicio intentara recrearlo y fallara
/// por el índice único de DNI. SQLite temporal migrada; datos ficticios.
/// </summary>
public sealed class SeedIdentityTests : IDisposable
{
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-seed-identity-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Restart_after_editing_a_seeded_whatsapp_keeps_the_edit_without_duplicates()
    {
        await StartAsync();
        await using (var db = CreateContext())
        {
            var uno = await db.Transportistas.SingleAsync(t => t.NormalizedWhatsApp == "5493815550101");
            uno.UpdateContactDetails(uno.Name, uno.DNI, "+5493815550111");
            await db.SaveChangesAsync();
        }

        await StartAsync();
        await StartAsync();

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.Transportistas.CountAsync());
        var edited = await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111");
        Assert.Equal("+5493815550111", edited.WhatsApp);
        Assert.False(await verify.Transportistas.AnyAsync(t => t.NormalizedWhatsApp == "5493815550101"));
        Assert.Equal(2, await verify.Asociaciones.CountAsync());
        Assert.True(await verify.Asociaciones.AnyAsync(a => a.TransportistaId == edited.Id && a.IsActive));
    }

    [Fact]
    public async Task Restart_never_overwrites_dni_name_or_inactivation_of_seeded_rows()
    {
        await StartAsync();
        await using (var db = CreateContext())
        {
            var dos = await db.Transportistas.SingleAsync(t => t.NormalizedDni == "20222222222");
            dos.UpdateContactDetails("Nombre Editado", "20999999992", "+5493815550192");
            dos.Inactivate(Clock.GetUtcNow().UtcDateTime);
            var camion = await db.Camiones.SingleAsync(c => c.NormalizedPlate == "AF456DE");
            camion.UpdateDetails("AF999ZZ", Camion.FleetTypeTerceros);
            await db.SaveChangesAsync();
        }

        await StartAsync();

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.Transportistas.CountAsync());
        Assert.Equal(2, await verify.Camiones.CountAsync());
        var dosAfter = await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20999999992");
        Assert.Equal("Nombre Editado", dosAfter.Name);
        Assert.False(dosAfter.IsActive);
        Assert.True(await verify.Camiones.AnyAsync(c => c.NormalizedPlate == "AF999ZZ"));
        Assert.False(await verify.Camiones.AnyAsync(c => c.NormalizedPlate == "AF456DE"));
    }

    [Fact]
    public async Task A_database_seeded_before_seed_records_adopts_its_rows_even_after_a_whatsapp_edit()
    {
        // Base creada por la versión anterior del seed: mismas filas, sin SeedRecords.
        await StartAsync();
        await using (var db = CreateContext())
        {
            db.SeedRecords.RemoveRange(db.SeedRecords);
            var uno = await db.Transportistas.SingleAsync(t => t.NormalizedWhatsApp == "5493815550101");
            uno.UpdateContactDetails(uno.Name, uno.DNI, "+5493815550111");
            await db.SaveChangesAsync();
        }

        await StartAsync();

        await using var verify = CreateContext();
        Assert.Equal(2, await verify.Transportistas.CountAsync());
        Assert.Equal(2, await verify.Asociaciones.CountAsync());
        Assert.Equal(8, await verify.SeedRecords.CountAsync());
        var record = await verify.SeedRecords.SingleAsync(r => r.Key == "transportista:demo-uno");
        Assert.Equal("+5493815550111", (await verify.Transportistas.SingleAsync(t => t.Id == record.EntityId)).WhatsApp);
    }

    [Fact]
    public async Task Seed_values_owned_by_another_row_are_never_duplicated_or_reassigned()
    {
        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
            db.Ingenios.Add(new Ingenio("Ingenio Demo"));
            await db.SaveChangesAsync();
            // Otro ingenio ya usa el DNI del transportista sembrado: crearlo violaría la unicidad global.
            var foreign = new Ingenio("Ingenio Ajeno");
            db.Ingenios.Add(foreign);
            db.Transportistas.Add(new Transportista(foreign.Id, "Ajeno", "20111111111", "+5493815550177"));
            await db.SaveChangesAsync();
        }

        // El conflicto no hace fallar el arranque ni reasigna la fila ajena.
        await StartAsync();
        await StartAsync();

        await using var verify = CreateContext();
        var owner = await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111");
        Assert.Equal("Ajeno", owner.Name);
        Assert.False(await verify.SeedRecords.AnyAsync(r => r.Key == "transportista:demo-uno"));
        Assert.True(await verify.Transportistas.AnyAsync(t => t.NormalizedDni == "20222222222"));
        // Sin transportista, el seed no asocia el camión de la demo uno.
        var camionUno = await verify.Camiones.SingleAsync(c => c.NormalizedPlate == "AF123BC");
        Assert.False(await verify.Asociaciones.AnyAsync(a => a.CamionId == camionUno.Id));
    }

    [Fact]
    public async Task Seed_does_not_reassign_a_truck_that_already_has_another_active_driver()
    {
        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
            var ingenio = new Ingenio("Ingenio Demo");
            var otro = new Transportista(ingenio.Id, "Otro", "20333333333", "+5493815550133");
            var camion = new Camion(ingenio.Id, "AF123BC", Camion.FleetTypePropia);
            db.Ingenios.Add(ingenio);
            db.Transportistas.Add(otro);
            db.Camiones.Add(camion);
            db.Asociaciones.Add(new TransportistaCamion(ingenio.Id, otro.Id, camion.Id));
            await db.SaveChangesAsync();
        }

        await StartAsync();
        await StartAsync();

        await using var verify = CreateContext();
        var camionUno = await verify.Camiones.SingleAsync(c => c.NormalizedPlate == "AF123BC");
        var activa = await verify.Asociaciones.SingleAsync(a => a.CamionId == camionUno.Id && a.IsActive);
        Assert.Equal("Otro", (await verify.Transportistas.SingleAsync(t => t.Id == activa.TransportistaId)).Name);
    }

    [Fact]
    public async Task Repeated_starts_are_idempotent()
    {
        await StartAsync();
        await StartAsync();
        await StartAsync();

        await using var verify = CreateContext();
        Assert.Equal(1, await verify.Ingenios.CountAsync());
        Assert.Equal(2, await verify.Transportistas.CountAsync());
        Assert.Equal(2, await verify.Camiones.CountAsync());
        Assert.Equal(2, await verify.Fincas.CountAsync());
        Assert.Equal(2, await verify.Asociaciones.CountAsync());
        Assert.Equal(8, await verify.SeedRecords.CountAsync());
    }

    private async Task StartAsync()
    {
        await using var db = CreateContext();
        await AppointmentsStartup.InitializeAsync(db, Clock, "fixture-instance-A");
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

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
