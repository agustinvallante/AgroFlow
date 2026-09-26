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
        Apply(plate, fleetType);
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public string Plate { get; private set; } = string.Empty;
    public string NormalizedPlate { get; private set; } = string.Empty;
    public string FleetType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string plate, string fleetType) => Apply(plate, fleetType);

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private void Apply(string plate, string fleetType)
    {
        var validatedPlate = MasterDataValidation.RequireText(plate, nameof(plate));
        var normalizedPlate = MasterDataValidation.NormalizeLettersAndDigits(validatedPlate, nameof(plate));
        var normalizedFleetType = MasterDataValidation.RequireText(fleetType, nameof(fleetType)).ToUpperInvariant();
        if (normalizedFleetType is not FleetTypePropia and not FleetTypeTerceros)
        {
            throw new ArgumentException("Fleet type must be PROPIA or TERCEROS.", nameof(fleetType));
        }

        Plate = validatedPlate;
        NormalizedPlate = normalizedPlate;
        FleetType = normalizedFleetType;
    }
}
