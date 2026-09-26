namespace Dsw2025Tpi.Domain.Entities;

public sealed class Ingenio : EntityBase
{
    private Ingenio()
    {
    }

    public Ingenio(string name)
    {
        Name = MasterDataValidation.RequireText(name, nameof(name));
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string name) => Name = MasterDataValidation.RequireText(name, nameof(name));

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
