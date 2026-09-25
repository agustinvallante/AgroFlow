namespace Dsw2025Tpi.Domain.Interfaces;

public interface ITenantContext
{
    Guid IngenioId { get; }
    string ActorIdentity { get; }
}
