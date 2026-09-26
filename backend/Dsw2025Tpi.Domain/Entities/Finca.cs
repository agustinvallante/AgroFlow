namespace Dsw2025Tpi.Domain.Entities;

public sealed class Finca : EntityBase
{
    private Finca()
    {
    }

    public Finca(Guid ingenioId, string code, string name, string locationReference)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Apply(code, name, locationReference);
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LocationReference { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string locationReference) => Apply(code, name, locationReference);

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private void Apply(string code, string name, string locationReference)
    {
        var validatedCode = MasterDataValidation.RequireText(code, nameof(code));
        var validatedName = MasterDataValidation.RequireText(name, nameof(name));
        var validatedLocation = MasterDataValidation.RequireText(locationReference, nameof(locationReference));

        Code = validatedCode;
        Name = validatedName;
        LocationReference = validatedLocation;
    }
}
