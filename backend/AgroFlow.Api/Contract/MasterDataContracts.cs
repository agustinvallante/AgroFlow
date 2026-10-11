namespace AgroFlow.Api.Contract;

/// <summary>Preparatory DTO shapes only; no HTTP endpoints currently expose them.</summary>
public sealed record DriverRequest(string Name, string Dni, string WhatsApp);
public sealed record TruckRequest(string Plate, string FleetType);
public sealed record FarmRequest(string Code, string Name, string LocationReference);
public sealed record DriverTruckAssociationRequest(Guid DriverId, Guid TruckId);
public sealed record MutationReceiptResponse(Guid ResourceId, string ResourceType, string Outcome)
{
    public static MutationReceiptResponse From(AgroFlow.Application.MasterData.MutationReceipt receipt) =>
        new(receipt.ResourceId, receipt.ResourceType, receipt.Outcome);
}

public sealed record MasterDataResponse(Guid Id, Guid IngenioId, string Name, string? Code,
    string? Identifier, string? SecondaryIdentifier, bool IsActive, DateTime CreatedAtUtc,
    DateTime? InactivatedAtUtc)
{
    public static MasterDataResponse From(AgroFlow.Application.MasterData.MasterDataItem item) =>
        new(item.Id, item.IngenioId, item.Name, item.Code, item.Identifier, item.SecondaryIdentifier,
            item.IsActive, item.CreatedAtUtc, item.InactivatedAtUtc);
}

public sealed record AssociationResponse(Guid Id, Guid DriverId, Guid TruckId, bool IsActive,
    DateTime CreatedAtUtc, DateTime? InactivatedAtUtc)
{
    public static AssociationResponse From(AgroFlow.Application.MasterData.AssociationItem item) =>
        new(item.Id, item.DriverId, item.TruckId, item.IsActive, item.CreatedAtUtc, item.InactivatedAtUtc);
}
