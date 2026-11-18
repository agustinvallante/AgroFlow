using System.Data;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using AgroFlow.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgroFlow.Data.Repositories;

/// <summary>
/// Adapter over the appointments SQLite context. Audit and idempotency rows are
/// migrated (AddMasterDataFoundationTables); the API registers it only under
/// LocalDemo, where the drivers routes are published.
/// </summary>
public sealed class EfMasterDataUnitOfWork(AgroFlowDbContext context) : IMasterDataUnitOfWork
{
    public async Task<IReadOnlyList<Transportista>> ListTransportistas(Guid ingenioId, bool? active, CancellationToken ct) =>
        await context.Transportistas.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Camion>> ListCamiones(Guid ingenioId, bool? active, CancellationToken ct) =>
        await context.Camiones.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Plate).ToListAsync(ct);

    public async Task<IReadOnlyList<Finca>> ListFincas(Guid ingenioId, bool? active, CancellationToken ct) =>
        await context.Fincas.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<TransportistaCamion>> ListAssociations(Guid ingenioId, bool? active, CancellationToken ct) =>
        await context.Asociaciones.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<ActiveTruckAssociationProjection>> ListActiveTrucksForTransportista(Guid ingenioId, Guid transportistaId, CancellationToken ct)
    {
        var driverExists = await context.Transportistas.AsNoTracking().AnyAsync(x => x.IngenioId == ingenioId && x.Id == transportistaId, ct);
        if (!driverExists) return Array.Empty<ActiveTruckAssociationProjection>();
        var rows = from association in context.Asociaciones.AsNoTracking()
                   join truck in context.Camiones.AsNoTracking() on association.CamionId equals truck.Id
                   where association.IngenioId == ingenioId && association.TransportistaId == transportistaId && association.IsActive
                       && truck.IngenioId == ingenioId
                   orderby truck.Plate, association.CamionId
                   select new ActiveTruckAssociationProjection(association.CamionId, truck.Plate, truck.FleetType, association.IsActive, truck.IsActive);
        return await rows.ToListAsync(ct);
    }

    public Task<Transportista?> FindTransportista(Guid ingenioId, Guid id, CancellationToken ct) =>
        context.Transportistas.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, ct);
    public Task<Camion?> FindCamion(Guid ingenioId, Guid id, CancellationToken ct) =>
        context.Camiones.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, ct);
    public Task<Finca?> FindFinca(Guid ingenioId, Guid id, CancellationToken ct) =>
        context.Fincas.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, ct);
    public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid id, CancellationToken ct) =>
        context.Asociaciones.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, ct);
    public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid driverId, Guid truckId, bool activeOnly, CancellationToken ct) =>
        context.Asociaciones.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.TransportistaId == driverId && x.CamionId == truckId && (!activeOnly || x.IsActive), ct);
    public Task<TransportistaCamion?> FindActiveAssociationForTruck(Guid ingenioId, Guid truckId, CancellationToken ct) =>
        context.Asociaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.CamionId == truckId && x.IsActive, ct);

    public async Task<TransportistaIdentifierConflicts> FindTransportistaIdentifierConflicts(Guid ingenioId, string dni, string whatsApp, Guid? exceptId, CancellationToken ct)
    {
        var query = context.Transportistas.AsNoTracking().Where(x => !exceptId.HasValue || x.Id != exceptId.Value);
        var localDni = await query.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedDni == dni, ct);
        var localPhone = await query.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedWhatsApp == whatsApp, ct);
        var global = await query.AnyAsync(x => x.NormalizedDni == dni || x.NormalizedWhatsApp == whatsApp, ct);
        return new TransportistaIdentifierConflicts(localDni, localPhone, global);
    }

    public async Task<CamionPlateConflicts> FindCamionPlateConflicts(Guid ingenioId, string normalizedPlate, Guid? exceptId, CancellationToken ct)
    {
        var query = context.Camiones.AsNoTracking().Where(x => !exceptId.HasValue || x.Id != exceptId.Value);
        var local = await query.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedPlate == normalizedPlate, ct);
        var global = await query.AnyAsync(x => x.NormalizedPlate == normalizedPlate, ct);
        return new CamionPlateConflicts(local, global);
    }

    public Task<bool> HasFincaCode(Guid ingenioId, string code, Guid? exceptId, CancellationToken ct)
    {
        var normalizedCode = MasterDataValidation.NormalizeUpperInvariant(code, nameof(code));
        return context.Fincas.AsNoTracking().AnyAsync(x => x.IngenioId == ingenioId &&
            (!exceptId.HasValue || x.Id != exceptId.Value) && x.NormalizedCode == normalizedCode, ct);
    }

    public Task<IdempotencyRecord?> FindIdempotency(string scope, string actor, string operation, string key, CancellationToken ct)
    {
        RequireFoundationEntity<IdempotencyRecord>("B02 idempotency schema");
        return context.Set<IdempotencyRecord>().SingleOrDefaultAsync(x => x.Scope == scope && x.ActorIdentity == actor && x.Operation == operation && x.Key == key, ct);
    }

    public Task<Transportista?> FindTransportistaById(Guid id, CancellationToken ct) => context.Transportistas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Camion?> FindCamionById(Guid id, CancellationToken ct) => context.Camiones.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Finca?> FindFincaById(Guid id, CancellationToken ct) => context.Fincas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public void Add(Transportista entity) => context.Transportistas.Add(entity);
    public void Add(Camion entity) => context.Camiones.Add(entity);
    public void Add(Finca entity) => context.Fincas.Add(entity);
    public void Add(TransportistaCamion entity) => context.Asociaciones.Add(entity);
    public void Add(AuditEvent entity)
    {
        RequireFoundationEntity<AuditEvent>("B02 audit schema");
        context.Set<AuditEvent>().Add(entity);
    }
    public void Add(IdempotencyRecord entity)
    {
        RequireFoundationEntity<IdempotencyRecord>("B02 idempotency schema");
        context.Set<IdempotencyRecord>().Add(entity);
    }

    public async Task<IMasterDataTransaction> BeginTransaction(CancellationToken ct) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct));

    public async Task Commit(CancellationToken ct)
    {
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (SqliteConstraintErrors.IsUniqueOrPrimaryKeyViolation(exception))
        {
            // Keep the persistence signal generic. SQLite uniqueness can be
            // distinguished by its extended result code; FK/check/trigger and
            // BUSY/LOCKED failures must retain their original provider exception.
            throw new MasterDataPersistenceConflictException("duplicate_identifier");
        }
    }

    private void RequireFoundationEntity<TEntity>(string dependency) where TEntity : class
    {
        if (context.Model.FindEntityType(typeof(TEntity)) is null)
            throw new InvalidOperationException($"Master-data persistence is disabled until the {dependency} is approved and migrated.");
    }

    private sealed class EfTransaction(IDbContextTransaction transaction) : IMasterDataTransaction
    {
        public Task Commit(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
