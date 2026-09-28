using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data.Appointments;

/// <summary>
/// Seed ficticio y repetible de la demo local. Cada paso resuelve por clave
/// natural antes de insertar, así correr el seed varias veces no duplica
/// datos maestros, asociaciones ni ventanas, y nunca toca turnos existentes.
/// </summary>
public static class AppointmentsSeeder
{
    private const string DefaultTimeZoneId = "America/Argentina/Tucuman";
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(30);
    private const int WindowCapacity = 2;
    private static readonly TimeSpan DayStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan DayEnd = TimeSpan.FromHours(18);

    public static async Task SeedAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var ingenio = await db.Ingenios.FirstOrDefaultAsync(cancellationToken);
        if (ingenio is null)
        {
            ingenio = new Ingenio("Ingenio Demo", DefaultTimeZoneId);
            db.Ingenios.Add(ingenio);
            await db.SaveChangesAsync(cancellationToken);
        }

        var transportistaUno = await GetOrCreateTransportistaAsync(
            db, ingenio.Id, "Transportista Demo Uno", "20111111111", "+5493815550101", cancellationToken);
        var transportistaDos = await GetOrCreateTransportistaAsync(
            db, ingenio.Id, "Transportista Demo Dos", "20222222222", "+5493815550102", cancellationToken);

        var camionUno = await GetOrCreateCamionAsync(
            db, ingenio.Id, "AF123BC", Camion.FleetTypePropia, cancellationToken);
        var camionDos = await GetOrCreateCamionAsync(
            db, ingenio.Id, "AF456DE", Camion.FleetTypePropia, cancellationToken);

        await GetOrCreateAsociacionAsync(db, ingenio.Id, transportistaUno.Id, camionUno.Id, cancellationToken);
        await GetOrCreateAsociacionAsync(db, ingenio.Id, transportistaDos.Id, camionDos.Id, cancellationToken);

        await GetOrCreateFincaAsync(db, ingenio.Id, "FINCA-NORTE", "Finca Norte", "Ruta 9, km 12", cancellationToken);
        await GetOrCreateFincaAsync(db, ingenio.Id, "FINCA-SUR", "Finca Sur", "Ruta 38, km 5", cancellationToken);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(ingenio.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).Date);
        await SeedWindowsForLocalDayAsync(db, ingenio.Id, timeZone, today, cancellationToken);
        await SeedWindowsForLocalDayAsync(db, ingenio.Id, timeZone, today.AddDays(1), cancellationToken);
    }

    private static async Task<Transportista> GetOrCreateTransportistaAsync(
        AgroFlowDbContext db, Guid ingenioId, string name, string dni, string whatsApp, CancellationToken ct)
    {
        var normalizedPhone = MasterDataValidation.NormalizeDigits(whatsApp, nameof(whatsApp));
        var existing = await db.Transportistas
            .FirstOrDefaultAsync(t => t.IngenioId == ingenioId && t.NormalizedWhatsApp == normalizedPhone, ct);
        if (existing is not null)
        {
            return existing;
        }

        var transportista = new Transportista(ingenioId, name, dni, whatsApp);
        db.Transportistas.Add(transportista);
        await db.SaveChangesAsync(ct);
        return transportista;
    }

    private static async Task<Camion> GetOrCreateCamionAsync(
        AgroFlowDbContext db, Guid ingenioId, string plate, string fleetType, CancellationToken ct)
    {
        var normalizedPlate = MasterDataValidation.NormalizeLettersAndDigits(plate, nameof(plate));
        var existing = await db.Camiones
            .FirstOrDefaultAsync(c => c.IngenioId == ingenioId && c.NormalizedPlate == normalizedPlate, ct);
        if (existing is not null)
        {
            return existing;
        }

        var camion = new Camion(ingenioId, plate, fleetType);
        db.Camiones.Add(camion);
        await db.SaveChangesAsync(ct);
        return camion;
    }

    private static async Task<Finca> GetOrCreateFincaAsync(
        AgroFlowDbContext db, Guid ingenioId, string code, string name, string locationReference, CancellationToken ct)
    {
        var normalizedCode = MasterDataValidation.NormalizeUpperInvariant(code, nameof(code));
        var existing = await db.Fincas
            .FirstOrDefaultAsync(f => f.IngenioId == ingenioId && f.NormalizedCode == normalizedCode, ct);
        if (existing is not null)
        {
            return existing;
        }

        var finca = new Finca(ingenioId, code, name, locationReference);
        db.Fincas.Add(finca);
        await db.SaveChangesAsync(ct);
        return finca;
    }

    private static async Task GetOrCreateAsociacionAsync(
        AgroFlowDbContext db, Guid ingenioId, Guid transportistaId, Guid camionId, CancellationToken ct)
    {
        var existing = await db.Asociaciones.FirstOrDefaultAsync(
            a => a.TransportistaId == transportistaId && a.CamionId == camionId, ct);
        if (existing is not null)
        {
            // Idempotente pero no correctivo: si alguien la inactivó a
            // propósito (por ejemplo para probar el conflicto de asociación
            // única), el seed no debe reactivarla por su cuenta.
            return;
        }

        db.Asociaciones.Add(new TransportistaCamion(ingenioId, transportistaId, camionId));
        await db.SaveChangesAsync(ct);
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
