namespace Dsw2025Tpi.Domain.Entities;

public sealed class Finca : EntityBase
{
    private Finca()
    {
    }

    public Finca(Guid ingenioId, string code, string name, string locationReference)
    {
        IngenioId = Transportista.RequireIngenioId(ingenioId);
        Code = Transportista.RequireValue(code, nameof(code));
        Name = Transportista.RequireValue(name, nameof(name));
        LocationReference = Transportista.RequireValue(locationReference, nameof(locationReference));
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid IngenioId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LocationReference { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }

    public void UpdateDetails(string code, string name, string locationReference)
    {
        Code = Transportista.RequireValue(code, nameof(code));
        Name = Transportista.RequireValue(name, nameof(name));
        LocationReference = Transportista.RequireValue(locationReference, nameof(locationReference));
    }

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
