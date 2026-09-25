using Dsw2025Tpi.Domain.Entities;

namespace Dsw2025Tpi.Domain.Interfaces;

public interface IMasterDataUnitOfWork
{
    Task<IReadOnlyList<Transportista>> ListTransportistas(Guid ingenioId, bool? active, CancellationToken cancellationToken);
    Task<IReadOnlyList<Camion>> ListCamiones(Guid ingenioId, bool? active, CancellationToken cancellationToken);
    Task<IReadOnlyList<Finca>> ListFincas(Guid ingenioId, bool? active, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransportistaCamion>> ListAssociations(Guid ingenioId, bool? active, CancellationToken cancellationToken);
    Task<Transportista?> FindTransportista(Guid ingenioId, Guid id, CancellationToken cancellationToken);
    Task<Camion?> FindCamion(Guid ingenioId, Guid id, CancellationToken cancellationToken);
    Task<Finca?> FindFinca(Guid ingenioId, Guid id, CancellationToken cancellationToken);
    Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid id, CancellationToken cancellationToken);
    Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid driverId, Guid truckId, bool activeOnly, CancellationToken cancellationToken);
    Task<TransportistaIdentifierConflicts> FindTransportistaIdentifierConflicts(Guid ingenioId, string normalizedDni, string normalizedWhatsApp, Guid? exceptId, CancellationToken cancellationToken);
    Task<CamionPlateConflicts> FindCamionPlateConflicts(Guid ingenioId, string normalizedPlate, Guid? exceptId, CancellationToken cancellationToken);
    Task<bool> HasFincaCode(Guid ingenioId, string code, Guid? exceptId, CancellationToken cancellationToken);
    Task<IdempotencyRecord?> FindIdempotency(string scope, string actorIdentity, string operation, string key, CancellationToken cancellationToken);
    Task<Transportista?> FindTransportistaById(Guid id, CancellationToken cancellationToken);
    Task<Camion?> FindCamionById(Guid id, CancellationToken cancellationToken);
    Task<Finca?> FindFincaById(Guid id, CancellationToken cancellationToken);

    void Add(Transportista entity);
    void Add(Camion entity);
    void Add(Finca entity);
    void Add(TransportistaCamion entity);
    void Add(AuditEvent entity);
    void Add(IdempotencyRecord entity);
    Task<IMasterDataTransaction> BeginTransaction(CancellationToken cancellationToken);
    Task Commit(CancellationToken cancellationToken);
}

public sealed record TransportistaIdentifierConflicts(bool DniInUseInIngenio, bool WhatsAppInUseInIngenio, bool UsedGlobally);
public sealed record CamionPlateConflicts(bool InUseInIngenio, bool UsedGlobally);

public interface IMasterDataTransaction : IAsyncDisposable
{
    Task Commit(CancellationToken cancellationToken);
}

public sealed class MasterDataPersistenceConflictException(string code) : Exception("Master-data uniqueness conflict.")
{
    public string Code { get; } = code;
}
