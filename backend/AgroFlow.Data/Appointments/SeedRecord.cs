namespace AgroFlow.Data.Appointments;

/// <summary>
/// Identidad estable de una fila creada por el seed de la demo. El seed
/// resuelve sus datos por esta clave fija y no por atributos editables
/// (WhatsApp, DNI, patente, código), así una edición hecha por la API nunca
/// hace que el seed intente recrear la fila ni que la sobrescriba.
/// </summary>
public sealed class SeedRecord
{
    public const int MaxKeyLength = 100;

    private SeedRecord()
    {
    }

    public SeedRecord(string key, Guid entityId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
            throw new ArgumentException("Seed key is required.", nameof(key));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity identifier is required.", nameof(entityId));
        Key = key;
        EntityId = entityId;
        CreatedAtUtc = createdAtUtc;
    }

    public string Key { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
