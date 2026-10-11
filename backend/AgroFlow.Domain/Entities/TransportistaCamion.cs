namespace AgroFlow.Domain.Entities;

/// <summary>Historical association between a transportista and a truck.</summary>
public sealed class TransportistaCamion : EntityBase
{
    private TransportistaCamion() { }

    public TransportistaCamion(Transportista transportista, Camion camion)
    {
        ArgumentNullException.ThrowIfNull(transportista);
        ArgumentNullException.ThrowIfNull(camion);
        if (transportista.IngenioId != camion.IngenioId)
            throw new ArgumentException("Transportista and camion must belong to the same ingenio.", nameof(camion));

        IngenioId = transportista.IngenioId;
        TransportistaId = transportista.Id;
        CamionId = camion.Id;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public TransportistaCamion(Guid ingenioId, Guid transportistaId, Guid camionId)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        if (transportistaId == Guid.Empty) throw new ArgumentException("Transportista identifier is required.", nameof(transportistaId));
        if (camionId == Guid.Empty) throw new ArgumentException("Camion identifier is required.", nameof(camionId));
        TransportistaId = transportistaId;
        CamionId = camionId;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid IngenioId { get; private set; }
    public Guid TransportistaId { get; private set; }
    public Guid CamionId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }
    public void Inactivate() => Inactivate(DateTime.UtcNow);

    public void Inactivate(DateTime occurredAtUtc)
    {
        MasterDataValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        IsActive = false;
        InactivatedAtUtc = occurredAtUtc;
    }

    public void Reactivate()
    {
        IsActive = true;
        InactivatedAtUtc = null;
    }
}
