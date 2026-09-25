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
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Plate = MasterDataValidation.RequireText(plate, nameof(plate));
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

    public void Update(string plate, string fleetType) => UpdateDetails(plate, fleetType);

    public void UpdateDetails(string plate, string fleetType)
    {
        var validatedPlate = MasterDataValidation.RequireText(plate, nameof(plate));
        var normalizedPlate = NormalizePlate(validatedPlate);
        var normalizedFleetType = NormalizeFleetType(fleetType);

        Plate = validatedPlate;
        NormalizedPlate = normalizedPlate;
        FleetType = normalizedFleetType;
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

    public static string NormalizePlate(string value) =>
        MasterDataValidation.NormalizeLettersAndDigits(value, nameof(value));

    private static string NormalizeFleetType(string value)
    {
        var normalized = MasterDataValidation.RequireText(value, nameof(value)).ToUpperInvariant();
        return normalized is FleetTypePropia or FleetTypeTerceros
            ? normalized
            : throw new ArgumentException("Fleet type must be PROPIA or TERCEROS.", nameof(value));
    }
}
