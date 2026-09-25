namespace Dsw2025Tpi.Domain.Entities;

public sealed class Transportista : EntityBase
{
    private Transportista()
    {
    }

    public Transportista(Guid ingenioId, string name, string dni, string whatsapp)
    {
        IngenioId = RequireIngenioId(ingenioId);
        Name = RequireValue(name, nameof(name));
        DNI = RequireValue(dni, nameof(dni));
        NormalizedDni = NormalizeDni(DNI);
        WhatsApp = RequireValue(whatsapp, nameof(whatsapp));
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

    public void UpdateContactDetails(string name, string dni, string whatsapp)
    {
        Name = RequireValue(name, nameof(name));
        DNI = RequireValue(dni, nameof(dni));
        NormalizedDni = NormalizeDni(DNI);
        WhatsApp = RequireValue(whatsapp, nameof(whatsapp));
        NormalizedWhatsApp = NormalizeWhatsApp(WhatsApp);
    }

    public void Inactivate(DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        IsActive = false;
        InactivatedAtUtc = occurredAtUtc;
    }

    public void Reactivate()
    {
        IsActive = true;
        InactivatedAtUtc = null;
    }

    public static string NormalizeDni(string value) =>
        new(RequireValue(value, nameof(value)).Where(character => !char.IsWhiteSpace(character)).ToArray());

    public static string NormalizeWhatsApp(string value)
    {
        var trimmed = RequireValue(value, nameof(value));
        return new string(trimmed.Where(character =>
            !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')' and not '.').ToArray());
    }

    internal static Guid RequireIngenioId(Guid ingenioId) => ingenioId == Guid.Empty
        ? throw new ArgumentException("Ingenio identifier is required.", nameof(ingenioId))
        : ingenioId;

    internal static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    internal static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
