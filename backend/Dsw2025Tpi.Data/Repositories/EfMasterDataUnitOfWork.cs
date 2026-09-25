using System.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dsw2025Tpi.Data.Repositories;

public sealed class EfMasterDataUnitOfWork(Dsw2025TpiContext context) : IMasterDataUnitOfWork
{
    public Task<IReadOnlyList<Transportista>> ListTransportistas(Guid ingenioId, bool? active, CancellationToken cancellationToken) =>
        context.Transportistas.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Name).ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<Transportista>>(task => task.Result, cancellationToken);

    public Task<IReadOnlyList<Camion>> ListCamiones(Guid ingenioId, bool? active, CancellationToken cancellationToken) =>
        context.Camiones.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Plate).ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<Camion>>(task => task.Result, cancellationToken);

    public Task<IReadOnlyList<Finca>> ListFincas(Guid ingenioId, bool? active, CancellationToken cancellationToken) =>
        context.Fincas.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.Code).ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<Finca>>(task => task.Result, cancellationToken);

    public Task<IReadOnlyList<TransportistaCamion>> ListAssociations(Guid ingenioId, bool? active, CancellationToken cancellationToken) =>
        context.TransportistasCamiones.AsNoTracking().Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active.Value))
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<TransportistaCamion>>(task => task.Result, cancellationToken);

    public Task<Transportista?> FindTransportista(Guid ingenioId, Guid id, CancellationToken cancellationToken) =>
        context.Transportistas.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, cancellationToken);
    public Task<Camion?> FindCamion(Guid ingenioId, Guid id, CancellationToken cancellationToken) =>
        context.Camiones.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, cancellationToken);
    public Task<Finca?> FindFinca(Guid ingenioId, Guid id, CancellationToken cancellationToken) =>
        context.Fincas.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, cancellationToken);
    public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid id, CancellationToken cancellationToken) =>
        context.TransportistasCamiones.Include(x => x.Transportista).Include(x => x.Camion)
            .SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.Id == id, cancellationToken);
    public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid driverId, Guid truckId, bool activeOnly, CancellationToken cancellationToken) =>
        context.TransportistasCamiones.SingleOrDefaultAsync(x => x.IngenioId == ingenioId && x.TransportistaId == driverId && x.CamionId == truckId && (!activeOnly || x.IsActive), cancellationToken);

    public async Task<TransportistaIdentifierConflicts> FindTransportistaIdentifierConflicts(
        Guid ingenioId, string normalizedDni, string normalizedWhatsApp, Guid? exceptId, CancellationToken cancellationToken)
    {
        var candidates = context.Transportistas.AsNoTracking().Where(x => !exceptId.HasValue || x.Id != exceptId.Value);
        var localDni = await candidates.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedDni == normalizedDni, cancellationToken);
        var localWhatsApp = await candidates.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedWhatsApp == normalizedWhatsApp, cancellationToken);
        var usedGlobally = await candidates.AnyAsync(x => x.NormalizedDni == normalizedDni || x.NormalizedWhatsApp == normalizedWhatsApp, cancellationToken);
        return new TransportistaIdentifierConflicts(localDni, localWhatsApp, usedGlobally);
    }
    public async Task<CamionPlateConflicts> FindCamionPlateConflicts(Guid ingenioId, string normalizedPlate, Guid? exceptId, CancellationToken cancellationToken)
    {
        var candidates = context.Camiones.AsNoTracking().Where(x => !exceptId.HasValue || x.Id != exceptId.Value);
        var local = await candidates.AnyAsync(x => x.IngenioId == ingenioId && x.NormalizedPlate == normalizedPlate, cancellationToken);
        var global = await candidates.AnyAsync(x => x.NormalizedPlate == normalizedPlate, cancellationToken);
        return new CamionPlateConflicts(local, global);
    }
    public Task<bool> HasFincaCode(Guid ingenioId, string code, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Fincas.AnyAsync(x => x.IngenioId == ingenioId && (!exceptId.HasValue || x.Id != exceptId.Value) && x.Code == code, cancellationToken);

    public Task<IdempotencyRecord?> FindIdempotency(string scope, string actorIdentity, string operation, string key, CancellationToken cancellationToken) =>
        context.IdempotencyRecords.SingleOrDefaultAsync(x => x.Scope == scope && x.ActorIdentity == actorIdentity && x.Operation == operation && x.Key == key, cancellationToken);
    public Task<Transportista?> FindTransportistaById(Guid id, CancellationToken cancellationToken) => context.Transportistas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Camion?> FindCamionById(Guid id, CancellationToken cancellationToken) => context.Camiones.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Finca?> FindFincaById(Guid id, CancellationToken cancellationToken) => context.Fincas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(Transportista entity) => context.Transportistas.Add(entity);
    public void Add(Camion entity) => context.Camiones.Add(entity);
    public void Add(Finca entity) => context.Fincas.Add(entity);
    public void Add(TransportistaCamion entity) => context.TransportistasCamiones.Add(entity);
    public void Add(AuditEvent entity) => context.AuditEvents.Add(entity);
    public void Add(IdempotencyRecord entity) => context.IdempotencyRecords.Add(entity);

    public async Task<IMasterDataTransaction> BeginTransaction(CancellationToken cancellationToken) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken));

    public async Task Commit(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            var code = message.Contains("IX_Fincas_IngenioId_Code", StringComparison.OrdinalIgnoreCase)
                ? "duplicate_code"
                : message.Contains("IX_TransportistasCamiones", StringComparison.OrdinalIgnoreCase)
                    ? "duplicate_association"
                    : "duplicate_identifier";
            throw new MasterDataPersistenceConflictException(code);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var number = current.GetType().GetProperty("Number")?.GetValue(current);
            if (number is int sqlServerNumber && sqlServerNumber is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class EfTransaction(IDbContextTransaction transaction) : IMasterDataTransaction
    {
        public Task Commit(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
