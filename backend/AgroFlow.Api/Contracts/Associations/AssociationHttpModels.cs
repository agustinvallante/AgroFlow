using System.Text.Json.Serialization;

namespace AgroFlow.Api.Contracts.Associations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AssociationInputRequest
{
    public Guid? DriverId { get; init; }
    public Guid? TruckId { get; init; }
}

/// <summary>
/// Explicit driver-truck association. Inactive associations are kept as
/// history; a truck has at most one active association at a time. Timestamps
/// are not persisted yet (B02), so the contract does not expose them.
/// </summary>
public sealed record AssociationResponse(Guid Id, Guid DriverId, Guid TruckId, bool IsActive);

public sealed record AssociationMutationResponse(Guid ResourceId, string ResourceType, string Outcome);
