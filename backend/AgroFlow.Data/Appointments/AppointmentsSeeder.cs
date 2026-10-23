using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Seed ficticio y repetible de la demo local. Los datos maestros se resuelven
/// por una clave estable registrada en <see cref="SeedRecord"/> (no por
/// WhatsApp, DNI, patente o código, que el usuario puede editar), así correr
/// el seed varias veces no duplica datos maestros, asociaciones ni ventanas,
/// no sobrescribe ediciones y nunca toca turnos existentes.
/// </summary>
public static class AppointmentsSeeder
{
    /// <summary>
    /// Posición estable de la demo: un transportista ficticio con su camión y
    /// una asociación activa. <see cref="SeedId"/> es el identificador público
    /// que usa la precarga privada del equipo (<see cref="LocalDemoTeamPreload"/>).
    /// </summary>
    public sealed record DemoPosition(string SeedId, string Name, string Dni, string WhatsApp, string Plate, string FleetType)
    {
        public string TransportistaKey => $"transportista:{SeedId}";
        public string CamionKey => $"camion:{SeedId}";
        public string AsociacionKey => $"asociacion:{SeedId}";
    }

    /// <summary>
    /// Cinco posiciones ficticias (rango telefónico 555 y DNI inventados). Las
    /// dos primeras son las originales de la demo; sus claves no cambian.
    /// </summary>
    public static readonly IReadOnlyList<DemoPosition> DemoPositions =
    [
        new("demo-uno", "Transportista Demo Uno", "20111111111", "+5493815550101", "AF123BC", Camion.FleetTypePropia),
        new("demo-dos", "Transportista Demo Dos", "20222222222", "+5493815550102", "AF456DE", Camion.FleetTypePropia),
        new("demo-tres", "Transportista Demo Tres", "20313131313", "+5493815550121", "AH301AB", Camion.FleetTypePropia),
        new("demo-cuatro", "Transportista Demo Cuatro", "20414141414", "+5493815550122", "AH302AB", Camion.FleetTypeTerceros),
        new("demo-cinco", "Transportista Demo Cinco", "20515151515", "+5493815550123", "AH303AB", Camion.FleetTypeTerceros),
    ];

    private const string DefaultTimeZoneId = "America/Argentina/Tucuman";
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(30);
    private const int WindowCapacity = 2;
    private static readonly TimeSpan DayStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan DayEnd = TimeSpan.FromHours(18);

    public static async Task SeedAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default) =>
        await SeedAsync(db, timeProvider, whatsAppInstance: null, cancellationToken);

    /// <param name="whatsAppInstance">
    /// Nombre de la instancia de Evolution habilitada para el ingenio sembrado
    /// (configuración, no secreto). Sin valor no se habilita ningún canal.
    /// </param>
    public static async Task SeedAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        string? whatsAppInstance,
        CancellationToken cancellationToken = default)
    {
        var ingenio = await db.Ingenios.FirstOrDefaultAsync(cancellationToken);
        if (ingenio is null)
        {
            ingenio = new Ingenio("Ingenio Demo", DefaultTimeZoneId);
            db.Ingenios.Add(ingenio);
            await db.SaveChangesAsync(cancellationToken);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var position in DemoPositions)
        {
            var transportista = await SeedTransportistaAsync(
                db, position.TransportistaKey, ingenio.Id, position.Name, position.Dni, position.WhatsApp, now, cancellationToken);
            var camion = await SeedCamionAsync(
                db, position.CamionKey, ingenio.Id, position.Plate, position.FleetType, now, cancellationToken);
            await SeedAsociacionAsync(db, position.AsociacionKey, ingenio.Id, transportista, camion, now, cancellationToken);
        }

        await SeedFincaAsync(db, "finca:norte", ingenio.Id, "FINCA-NORTE", "Finca Norte", "Ruta 9, km 12", now, cancellationToken);
        await SeedFincaAsync(db, "finca:sur", ingenio.Id, "FINCA-SUR", "Finca Sur", "Ruta 38, km 5", now, cancellationToken);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(ingenio.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).Date);
        await SeedWindowsForLocalDayAsync(db, ingenio.Id, timeZone, today, cancellationToken);
        await SeedWindowsForLocalDayAsync(db, ingenio.Id, timeZone, today.AddDays(1), cancellationToken);

        if (!string.IsNullOrWhiteSpace(whatsAppInstance))
        {
            await GetOrCreateChannelInstanceAsync(db, ingenio.Id, whatsAppInstance, cancellationToken);
        }
    }

    private static async Task GetOrCreateChannelInstanceAsync(
        AgroFlowDbContext db, Guid ingenioId, string instanceName, CancellationToken ct)
    {
        var normalizedName = ChannelInstance.NormalizeInstanceName(instanceName);
        var exists = await db.ChannelInstances.AnyAsync(
            c => c.Provider == ChannelInstance.ProviderEvolution && c.InstanceName == normalizedName, ct);
        if (exists)
        {
            // Una instancia existente conserva su ingenio y su estado: el seed
            // no la reasigna ni la reactiva por su cuenta.
            return;
        }

        db.ChannelInstances.Add(new ChannelInstance(ingenioId, ChannelInstance.ProviderEvolution, normalizedName));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Resuelve una fila del seed por su clave estable. Con la clave
    /// registrada devuelve la fila tal como está (editada o inhabilitada por
    /// el usuario) y nunca la recrea ni la sobrescribe. Sin clave, adopta una
    /// sola vez la fila que una versión anterior del seed creó con los valores
    /// originales, o la crea; si los valores ya pertenecen a otra fila no crea
    /// nada, para no violar la unicidad ni pisar datos ajenos.
    /// </summary>
    private static async Task<T?> SeedEntityAsync<T>(
        AgroFlowDbContext db, string key, DateTime nowUtc, CancellationToken ct,
        Func<IQueryable<T>, IQueryable<T>> legacyMatch, Func<Task<T?>> create) where T : EntityBase
    {
        var record = await db.SeedRecords.FirstOrDefaultAsync(r => r.Key == key, ct);
        if (record is not null)
        {
            return await db.Set<T>().FirstOrDefaultAsync(e => e.Id == record.EntityId, ct);
        }

        var claimed = db.SeedRecords.Select(r => r.EntityId);
        var entity = await legacyMatch(db.Set<T>()).Where(e => !claimed.Contains(e.Id)).FirstOrDefaultAsync(ct)
            ?? await create();
        if (entity is null)
        {
            return null;
        }

        db.SeedRecords.Add(new SeedRecord(key, entity.Id, nowUtc));
        await db.SaveChangesAsync(ct);
        return entity;
    }

    private static Task<Transportista?> SeedTransportistaAsync(
        AgroFlowDbContext db, string key, Guid ingenioId, string name, string dni, string whatsApp, DateTime nowUtc,
        CancellationToken ct)
    {
        var phone = Transportista.NormalizeWhatsApp(whatsApp);
        var normalizedDni = Transportista.NormalizeDni(dni);
        return SeedEntityAsync<Transportista>(db, key, nowUtc, ct,
            query => query.Where(t => t.IngenioId == ingenioId && (t.NormalizedWhatsApp == phone || t.NormalizedDni == normalizedDni)),
            async () =>
            {
                // DNI y WhatsApp son únicos en todo el sistema.
                var inUse = await db.Transportistas.AnyAsync(t => t.NormalizedWhatsApp == phone || t.NormalizedDni == normalizedDni, ct);
                if (inUse)
                {
                    return null;
                }

                var transportista = new Transportista(ingenioId, name, dni, whatsApp);
                db.Transportistas.Add(transportista);
                return transportista;
            });
    }

    private static Task<Camion?> SeedCamionAsync(
        AgroFlowDbContext db, string key, Guid ingenioId, string plate, string fleetType, DateTime nowUtc, CancellationToken ct)
    {
        var normalizedPlate = Camion.NormalizePlate(plate);
        return SeedEntityAsync<Camion>(db, key, nowUtc, ct,
            query => query.Where(c => c.IngenioId == ingenioId && c.NormalizedPlate == normalizedPlate),
            () =>
            {
                var camion = new Camion(ingenioId, plate, fleetType);
                db.Camiones.Add(camion);
                return Task.FromResult<Camion?>(camion);
            });
    }

    private static Task<Finca?> SeedFincaAsync(
        AgroFlowDbContext db, string key, Guid ingenioId, string code, string name, string locationReference,
        DateTime nowUtc, CancellationToken ct)
    {
        var normalizedCode = MasterDataValidation.NormalizeUpperInvariant(code, nameof(code));
        return SeedEntityAsync<Finca>(db, key, nowUtc, ct,
            query => query.Where(f => f.IngenioId == ingenioId && f.NormalizedCode == normalizedCode),
            () =>
            {
                var finca = new Finca(ingenioId, code, name, locationReference);
                db.Fincas.Add(finca);
                return Task.FromResult<Finca?>(finca);
            });
    }

    private static async Task SeedAsociacionAsync(
        AgroFlowDbContext db, string key, Guid ingenioId, Transportista? transportista, Camion? camion, DateTime nowUtc,
        CancellationToken ct)
    {
        if (transportista is null || camion is null)
        {
            return;
        }

        await SeedEntityAsync<TransportistaCamion>(db, key, nowUtc, ct,
            // Idempotente pero no correctivo: una asociación del par que
            // alguien inactivó a propósito se adopta tal como está.
            query => query.Where(a => a.TransportistaId == transportista.Id && a.CamionId == camion.Id),
            async () =>
            {
                // Un camión tiene a lo sumo un transportista activo: si ya
                // tiene otro, el seed no lo reasigna.
                var truckTaken = await db.Asociaciones.AnyAsync(a => a.CamionId == camion.Id && a.IsActive, ct);
                if (truckTaken)
                {
                    return null;
                }

                var asociacion = new TransportistaCamion(ingenioId, transportista.Id, camion.Id);
                db.Asociaciones.Add(asociacion);
                return asociacion;
            });
    }

    private static async Task SeedWindowsForLocalDayAsync(
        AgroFlowDbContext db, Guid ingenioId, TimeZoneInfo timeZone, DateOnly localDay, CancellationToken ct)
    {
        var existingStarts = (await db.Ventanas
            .Where(v => v.IngenioId == ingenioId)
            .Select(v => v.StartAt)
            .ToListAsync(ct))
            .ToHashSet();

        var localMidnight = localDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var added = false;

        for (var slotStart = DayStart; slotStart < DayEnd; slotStart += WindowDuration)
        {
            var localStartUnspecified = localMidnight.Add(slotStart);
            var offset = timeZone.GetUtcOffset(localStartUnspecified);
            var startAt = new DateTimeOffset(localStartUnspecified, offset);

            // DateTimeOffset equality compares the UTC instant, so this
            // correctly matches windows seeded on a previous run.
            if (existingStarts.Contains(startAt))
            {
                continue;
            }

            db.Ventanas.Add(new Ventana(ingenioId, startAt, startAt + WindowDuration, WindowCapacity));
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
