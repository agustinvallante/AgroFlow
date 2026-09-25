namespace Dsw2025Tpi.Domain.Entities;

public sealed class Camion : EntityBase
{
    public const string FleetTypePropia = "PROPIA";
    public const string FleetTypeTerceros = "TERCEROS";

    private Camion()
    {
    }

    public Camion(Guid ingenioId, string plate, string fleetType)
    {
        IngenioId = Transportista.RequireIngenioId(ingenioId);
        Plate = Transportista.RequireValue(plate, nameof(plate));
        NormalizedPlate = NormalizePlate(Plate);
        FleetType = NormalizeFleetType(fleetType);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid IngenioId { get; private set; }
    public string Plate { get; private set; } = string.Empty;
    public string NormalizedPlate { get; private set; } = string.Empty;
    public string FleetType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? InactivatedAtUtc { get; private set; }

    public void UpdateDetails(string plate, string fleetType)
    {
        Plate = Transportista.RequireValue(plate, nameof(plate));
        NormalizedPlate = NormalizePlate(Plate);
        FleetType = NormalizeFleetType(fleetType);
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

    public static string NormalizePlate(string value)
    {
        var trimmed = Transportista.RequireValue(value, nameof(value));
        return new string(trimmed.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private static string NormalizeFleetType(string value)
    {
        var normalized = Transportista.RequireValue(value, nameof(value)).ToUpperInvariant();
        return normalized is FleetTypePropia or FleetTypeTerceros
            ? normalized
            : throw new ArgumentException("Fleet type must be PROPIA or TERCEROS.", nameof(value));
    }
}
