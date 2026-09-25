namespace Dsw2025Tpi.Domain.Entities;

public sealed class Transportista : EntityBase
{
    private Transportista()
    {
    }

    public Transportista(Guid ingenioId, string name, string dni, string whatsApp)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Name = MasterDataValidation.RequireText(name, nameof(name));
        DNI = MasterDataValidation.RequireText(dni, nameof(dni));
        NormalizedDni = NormalizeDni(DNI);
        WhatsApp = MasterDataValidation.RequireText(whatsApp, nameof(whatsApp));
        NormalizedWhatsApp = NormalizeWhatsApp(WhatsApp);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid IngenioId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DNI { get; private set; } = string.Empty;
    public string NormalizedDni { get; private set; } = string.Empty;
    public string WhatsApp { get; private set; } = string.Empty;
    public string NormalizedWhatsApp { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }

    public void Update(string name, string dni, string whatsApp)
    {
        var validatedName = MasterDataValidation.RequireText(name, nameof(name));
        var validatedDni = MasterDataValidation.RequireText(dni, nameof(dni));
        var normalizedDni = NormalizeDni(validatedDni);
        var validatedWhatsApp = MasterDataValidation.RequireText(whatsApp, nameof(whatsApp));
        var normalizedWhatsApp = NormalizeWhatsApp(validatedWhatsApp);

        Name = validatedName;
        DNI = validatedDni;
        NormalizedDni = normalizedDni;
        WhatsApp = validatedWhatsApp;
        NormalizedWhatsApp = normalizedWhatsApp;
    }

    public void UpdateContactDetails(string name, string dni, string whatsapp)
    {
        var validatedName = MasterDataValidation.RequireText(name, nameof(name));
        var validatedDni = MasterDataValidation.RequireText(dni, nameof(dni));
        var normalizedDni = NormalizeDni(validatedDni);
        var validatedWhatsApp = MasterDataValidation.RequireText(whatsapp, nameof(whatsapp));
        var normalizedWhatsApp = NormalizeWhatsApp(validatedWhatsApp);

        Name = validatedName;
        DNI = validatedDni;
        NormalizedDni = normalizedDni;
        WhatsApp = validatedWhatsApp;
        NormalizedWhatsApp = normalizedWhatsApp;
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

    internal static void EnsureUtc(DateTime value, string parameterName) =>
        MasterDataValidation.EnsureUtc(value, parameterName);

    public static string NormalizeDni(string value) =>
        MasterDataValidation.NormalizeDigits(
            MasterDataValidation.RequireText(value, nameof(value)), nameof(value));

    public static string NormalizeWhatsApp(string value)
    {
        var trimmed = MasterDataValidation.RequireText(value, nameof(value));
        return new string(trimmed.Where(character =>
            !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')' and not '.').ToArray());
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

    internal static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
