namespace Dsw2025Tpi.Domain.Entities;

/// <summary>
/// Franja horaria del ingenio con cupo propio. Duración y capacidad son datos
/// del seed, no constantes de dominio: cada instancia trae los suyos.
/// </summary>
public sealed class Ventana : EntityBase
{
    private Ventana()
    {
    }

    public Ventana(Guid ingenioId, DateTimeOffset startAt, DateTimeOffset endAt, int capacity)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        if (endAt <= startAt)
            throw new ArgumentException("endAt must be after startAt.", nameof(endAt));
        if (capacity <= 0)
            throw new ArgumentException("capacity must be a positive number.", nameof(capacity));

        StartAt = startAt;
        EndAt = endAt;
        Capacity = capacity;
        Occupied = 0;
    }

    public Guid IngenioId { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public int Capacity { get; private set; }
    public int Occupied { get; private set; }
}
