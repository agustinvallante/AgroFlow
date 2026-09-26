namespace Dsw2025Tpi.Domain.Entities;

public sealed class Transportista : EntityBase
{
    private Transportista()
    {
    }

    public Transportista(Guid ingenioId, string name, string dni, string whatsApp)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Apply(name, dni, whatsApp);
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DNI { get; private set; } = string.Empty;
    public string NormalizedDni { get; private set; } = string.Empty;
    public string WhatsApp { get; private set; } = string.Empty;
    public string NormalizedWhatsApp { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string name, string dni, string whatsApp) => Apply(name, dni, whatsApp);

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private void Apply(string name, string dni, string whatsApp)
    {
        var validatedName = MasterDataValidation.RequireText(name, nameof(name));
        var validatedDni = MasterDataValidation.RequireText(dni, nameof(dni));
        var normalizedDni = MasterDataValidation.NormalizeDigits(validatedDni, nameof(dni));
        var validatedWhatsApp = MasterDataValidation.RequireText(whatsApp, nameof(whatsApp));
        var normalizedWhatsApp = MasterDataValidation.NormalizeDigits(validatedWhatsApp, nameof(whatsApp));

        Name = validatedName;
        DNI = validatedDni;
        NormalizedDni = normalizedDni;
        WhatsApp = validatedWhatsApp;
        NormalizedWhatsApp = normalizedWhatsApp;
    }
}

internal static class MasterDataValidation
{
    internal static Guid RequireIngenioId(Guid ingenioId) => ingenioId == Guid.Empty
        ? throw new ArgumentException("Ingenio identifier is required.", nameof(ingenioId))
        : ingenioId;

    internal static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    internal static string NormalizeDigits(string value, string parameterName)
    {
        var normalized = new string(value.Where(char.IsDigit).ToArray());
        return normalized.Length == 0
            ? throw new ArgumentException("Value must contain digits.", parameterName)
            : normalized;
    }

    internal static string NormalizeLettersAndDigits(string value, string parameterName)
    {
        var normalized = new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return normalized.Length == 0
            ? throw new ArgumentException("Value must contain letters or digits.", parameterName)
            : normalized;
    }
}
