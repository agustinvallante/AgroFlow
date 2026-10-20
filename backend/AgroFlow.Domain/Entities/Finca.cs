namespace AgroFlow.Domain.Entities;

public sealed class Finca : EntityBase
{
    private Finca()
    {
    }

    public Finca(Guid ingenioId, string code, string name, string locationReference)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Code = MasterDataValidation.RequireText(code, nameof(code));
        NormalizedCode = MasterDataValidation.NormalizeUpperInvariant(Code, nameof(code));
        Name = MasterDataValidation.RequireText(name, nameof(name));
        LocationReference = MasterDataValidation.RequireText(locationReference, nameof(locationReference));
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid IngenioId { get; private set; }
    public string Code { get; private set; } = string.Empty;

    /// <summary>Code en mayúsculas, sin otra normalización, para resolver farmCode sin distinguir mayúsculas.</summary>
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LocationReference { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }

    public void Update(string code, string name, string locationReference) =>
        UpdateDetails(code, name, locationReference);

    public void UpdateDetails(string code, string name, string locationReference)
    {
        var validatedCode = MasterDataValidation.RequireText(code, nameof(code));
        var normalizedCode = MasterDataValidation.NormalizeUpperInvariant(validatedCode, nameof(code));
        var validatedName = MasterDataValidation.RequireText(name, nameof(name));
        var validatedLocation = MasterDataValidation.RequireText(locationReference, nameof(locationReference));

        Code = validatedCode;
        NormalizedCode = normalizedCode;
        Name = validatedName;
        LocationReference = validatedLocation;
    }

    public void Inactivate() => IsActive = false;

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
