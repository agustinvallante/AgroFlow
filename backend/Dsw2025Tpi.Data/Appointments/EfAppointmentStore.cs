using Dsw2025Tpi.Application.Appointments;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data.Appointments;

/// <summary>
/// Adaptador de persistencia real de Persona 2 para <see cref="IAppointmentStore"/>.
/// Toda escritura crítica corre en una única transacción con actualizaciones
/// condicionales (`UPDATE ... WHERE`), nunca lectura-en-memoria seguida de
/// escritura sin control.
/// </summary>
public sealed class EfAppointmentStore : IAppointmentStore
{
    private readonly AgroFlowDbContext _db;
    private readonly TimeProvider _timeProvider;

    public EfAppointmentStore(AgroFlowDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AppointmentCreationContext?> GetCreationContextAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveReferencesAsync(command, cancellationToken);
        if (resolved is null)
        {
            return null;
        }

        var hasActiveAppointment = await _db.Turnos.AsNoTracking()
            .AnyAsync(t => t.CamionId == resolved.CamionId && t.IsActive, cancellationToken);

        // El proveedor SQLite no traduce comparaciones ">" entre columnas y
        // parámetros DateTimeOffset; el conjunto por ingenio es chico para la
        // demo, así que el filtro de "futuro" se aplica en memoria.
        var now = _timeProvider.GetUtcNow();
        var windows = (await _db.Ventanas.AsNoTracking()
            .Where(v => v.IngenioId == resolved.IngenioId)
            .ToListAsync(cancellationToken))
            .Where(v => v.StartAt > now)
            .Select(v => new AppointmentCandidateWindow(v.Id, v.StartAt, v.EndAt, v.Capacity, v.Occupied))
            .ToList();

        return new AppointmentCreationContext(hasActiveAppointment, windows);
    }

    public async Task<AppointmentCreateResult> TryCreateAssignedAsync(
        CreateAppointmentCommand command,
        Guid windowId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Revalidación completa: la lectura de GetCreationContextAsync puede
        // haber quedado desactualizada entre la elección de ventana y este commit.
        var resolved = await ResolveReferencesAsync(command, cancellationToken);
        if (resolved is null)
        {
            return new AppointmentCreateResult(AppointmentCreateOutcome.ReferenceChanged, null);
        }

        var hasActiveAppointment = await _db.Turnos.AsNoTracking()
            .AnyAsync(t => t.CamionId == resolved.CamionId && t.IsActive, cancellationToken);
        if (hasActiveAppointment)
        {
            return new AppointmentCreateResult(AppointmentCreateOutcome.ActiveAppointmentExists, null);
        }

        // StartAtUtc (no StartAt) porque el proveedor SQLite no traduce ">"
        // entre dos DateTimeOffset; comparar contra el espejo UTC mantiene
        // esta revalidación dentro de la misma sentencia atómica. Una
        // ventana que dejó de ser futura entre la lectura y este commit se
        // trata igual que un cupo perdido: el servicio prueba la siguiente.
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var reservedRows = await _db.Ventanas
            .Where(v => v.Id == windowId && v.IngenioId == resolved.IngenioId
                && v.Occupied < v.Capacity && v.StartAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Occupied, v => v.Occupied + 1), cancellationToken);
        if (reservedRows == 0)
        {
            return new AppointmentCreateResult(AppointmentCreateOutcome.CapacityChanged, null);
        }

        var turno = new Turno(
            resolved.IngenioId,
            resolved.TransportistaId,
            resolved.CamionId,
            resolved.FincaId,
            windowId,
            command.CutAt,
            command.EstimatedLoadTons,
            _timeProvider.GetUtcNow());

        try
        {
            _db.Turnos.Add(turno);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            // Otro request insertó un turno activo para el mismo camión entre
            // la comprobación de arriba y este insert: el índice filtrado de
            // Turnos.CamionId es la última barrera real, no sólo en memoria.
            return new AppointmentCreateResult(AppointmentCreateOutcome.ActiveAppointmentExists, null);
        }

        var snapshot = await BuildSnapshotAsync(turno, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AppointmentCreateResult(AppointmentCreateOutcome.Success, snapshot);
    }

    public async Task<IReadOnlyList<AppointmentSummary>> ListAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var ingenio = await _db.Ingenios.AsNoTracking().FirstAsync(cancellationToken);
        var timeZone = ResolveTimeZone(ingenio.TimeZoneId);
        var effectiveDate = query.Date ?? DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), timeZone).Date);

        IQueryable<Turno> turnos = _db.Turnos.AsNoTracking().Where(t => t.IngenioId == ingenio.Id);

        if (query.Status is { } status)
        {
            var domainStatus = AppointmentStatusMapper.ToDomain(status);
            turnos = turnos.Where(t => t.Status == domainStatus);
        }

        if (query.TruckPlate is { } plate)
        {
            var normalizedPlate = MasterDataValidation.NormalizeLettersAndDigits(plate, nameof(query.TruckPlate));
            turnos = turnos.Where(t => _db.Camiones
                .Any(c => c.Id == t.CamionId && c.NormalizedPlate == normalizedPlate));
        }

        if (query.Phone is { } phone)
        {
            var normalizedPhone = MasterDataValidation.NormalizeDigits(phone, nameof(query.Phone));
            turnos = turnos.Where(t => _db.Transportistas
                .Any(x => x.Id == t.TransportistaId && x.NormalizedWhatsApp == normalizedPhone));
        }

        var candidates = await turnos
            .Join(_db.Ventanas.AsNoTracking(), t => t.VentanaId, v => v.Id, (t, v) => new { Turno = t, Ventana = v })
            .ToListAsync(cancellationToken);

        var matches = candidates
            .Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.Ventana.StartAt, timeZone).Date) == effectiveDate)
            .OrderBy(x => x.Ventana.StartAt)
            .ToList();

        if (matches.Count == 0)
        {
            return [];
        }

        var camionIds = matches.Select(x => x.Turno.CamionId).Distinct().ToArray();
        var camiones = await _db.Camiones.AsNoTracking()
            .Where(c => camionIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return matches.Select(x => new AppointmentSummary(
            x.Turno.Id,
            new AppointmentTruckReference(x.Turno.CamionId, camiones[x.Turno.CamionId].Plate),
            new AppointmentWindow(
                TimeZoneInfo.ConvertTime(x.Ventana.StartAt, timeZone),
                TimeZoneInfo.ConvertTime(x.Ventana.EndAt, timeZone)),
            AppointmentStatusMapper.ToApplication(x.Turno.Status)))
            .ToArray();
    }

    public async Task<AppointmentSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var turno = await _db.Turnos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return turno is null ? null : await BuildSnapshotAsync(turno, cancellationToken);
    }

    public async Task<AppointmentTransitionResult> TryTransitionAsync(
        Guid id,
        AppointmentStatus expectedStatus,
        AppointmentStatus nextStatus,
        bool releaseCapacity,
        CancellationToken cancellationToken = default)
    {
        var expectedDomain = AppointmentStatusMapper.ToDomain(expectedStatus);
        var nextDomain = AppointmentStatusMapper.ToDomain(nextStatus);
        var isTerminal = nextDomain is TurnoEstado.Finalizado or TurnoEstado.Cancelado;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var updatedRows = await _db.Turnos
            .Where(t => t.Id == id && t.Status == expectedDomain)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, nextDomain)
                .SetProperty(t => t.IsActive, !isTerminal), cancellationToken);

        if (updatedRows == 0)
        {
            var exists = await _db.Turnos.AsNoTracking().AnyAsync(t => t.Id == id, cancellationToken);
            return new AppointmentTransitionResult(
                exists ? AppointmentTransitionOutcome.StateChanged : AppointmentTransitionOutcome.NotFound,
                null);
        }

        if (releaseCapacity)
        {
            var ventanaId = await _db.Turnos.AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => t.VentanaId)
                .FirstAsync(cancellationToken);

            await _db.Ventanas
                .Where(v => v.Id == ventanaId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Occupied, v => v.Occupied - 1), cancellationToken);
        }

        var snapshot = await BuildSnapshotAsync(id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AppointmentTransitionResult(AppointmentTransitionOutcome.Success, snapshot);
    }

    private sealed record ResolvedReferences(Guid IngenioId, Guid TransportistaId, Guid CamionId, Guid FincaId);

    private async Task<ResolvedReferences?> ResolveReferencesAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = MasterDataValidation.NormalizeDigits(command.CarrierPhone, nameof(command.CarrierPhone));
        var normalizedPlate = MasterDataValidation.NormalizeLettersAndDigits(command.TruckPlate, nameof(command.TruckPlate));
        var normalizedFarmCode = MasterDataValidation.NormalizeUpperInvariant(command.FarmCode, nameof(command.FarmCode));

        var transportista = await _db.Transportistas.AsNoTracking()
            .FirstOrDefaultAsync(t => t.NormalizedWhatsApp == normalizedPhone && t.IsActive, cancellationToken);
        var camion = await _db.Camiones.AsNoTracking()
            .FirstOrDefaultAsync(c => c.NormalizedPlate == normalizedPlate && c.IsActive, cancellationToken);
        var finca = await _db.Fincas.AsNoTracking()
            .FirstOrDefaultAsync(f => f.NormalizedCode == normalizedFarmCode && f.IsActive, cancellationToken);

        if (transportista is null || camion is null || finca is null)
        {
            return null;
        }

        if (camion.IngenioId != transportista.IngenioId || finca.IngenioId != transportista.IngenioId)
        {
            return null;
        }

        var associationActive = await _db.Asociaciones.AsNoTracking().AnyAsync(
            a => a.TransportistaId == transportista.Id && a.CamionId == camion.Id && a.IsActive,
            cancellationToken);
        if (!associationActive)
        {
            return null;
        }

        return new ResolvedReferences(transportista.IngenioId, transportista.Id, camion.Id, finca.Id);
    }

    private async Task<AppointmentSnapshot> BuildSnapshotAsync(Guid turnoId, CancellationToken cancellationToken)
    {
        var turno = await _db.Turnos.AsNoTracking().FirstAsync(t => t.Id == turnoId, cancellationToken);
        return await BuildSnapshotAsync(turno, cancellationToken);
    }

    private async Task<AppointmentSnapshot> BuildSnapshotAsync(Turno turno, CancellationToken cancellationToken)
    {
        var ingenio = await _db.Ingenios.AsNoTracking().FirstAsync(i => i.Id == turno.IngenioId, cancellationToken);
        var timeZone = ResolveTimeZone(ingenio.TimeZoneId);
        var transportista = await _db.Transportistas.AsNoTracking()
            .FirstAsync(t => t.Id == turno.TransportistaId, cancellationToken);
        var camion = await _db.Camiones.AsNoTracking().FirstAsync(c => c.Id == turno.CamionId, cancellationToken);
        var finca = await _db.Fincas.AsNoTracking().FirstAsync(f => f.Id == turno.FincaId, cancellationToken);
        var ventana = await _db.Ventanas.AsNoTracking().FirstAsync(v => v.Id == turno.VentanaId, cancellationToken);

        return new AppointmentSnapshot(
            turno.Id,
            new AppointmentReference(transportista.Id, transportista.Name),
            new AppointmentTruckReference(camion.Id, camion.Plate),
            new AppointmentReference(finca.Id, finca.Name),
            TimeZoneInfo.ConvertTime(turno.CutAt, timeZone),
            turno.EstimatedLoadTons,
            new AppointmentWindow(
                TimeZoneInfo.ConvertTime(ventana.StartAt, timeZone),
                TimeZoneInfo.ConvertTime(ventana.EndAt, timeZone)),
            AppointmentStatusMapper.ToApplication(turno.Status),
            TimeZoneInfo.ConvertTime(turno.CreatedAt, timeZone));
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId) =>
        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}
