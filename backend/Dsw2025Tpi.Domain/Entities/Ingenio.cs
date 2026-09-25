namespace Dsw2025Tpi.Domain.Entities;

public sealed class Ingenio : EntityBase
{
    private Ingenio()
    {
    }

    public Ingenio(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Ingenio name is required.", nameof(name))
            : name.Trim();
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
