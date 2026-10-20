namespace AgroFlow.Domain.Interfaces;

/// <summary>Validated tenant and actor claims for protected master-data operations.</summary>
public interface ITenantContext
{
    Guid IngenioId { get; }
    string ActorIdentity { get; }
}
