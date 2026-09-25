namespace Dsw2025Tpi.Domain.Entities;

public sealed class TransportistaCamion : EntityBase
{
    private TransportistaCamion()
    {
    }

    public TransportistaCamion(Transportista transportista, Camion camion)
    {
        ArgumentNullException.ThrowIfNull(transportista);
        ArgumentNullException.ThrowIfNull(camion);
        if (transportista.IngenioId != camion.IngenioId)
        {
            throw new ArgumentException("Transportista and camion must belong to the same ingenio.", nameof(camion));
        }

        IngenioId = transportista.IngenioId;
        TransportistaId = transportista.Id;
        CamionId = camion.Id;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        Transportista = transportista;
        Camion = camion;
    }

    public Guid IngenioId { get; private set; }
    public Guid TransportistaId { get; private set; }
    public Guid CamionId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }
    public Transportista Transportista { get; private set; } = null!;
    public Camion Camion { get; private set; } = null!;

    public void Inactivate(DateTime occurredAtUtc)
    {
        Transportista.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        IsActive = false;
        InactivatedAtUtc = occurredAtUtc;
    }

    public void Reactivate()
    {
        IsActive = true;
        InactivatedAtUtc = null;
    }
}
