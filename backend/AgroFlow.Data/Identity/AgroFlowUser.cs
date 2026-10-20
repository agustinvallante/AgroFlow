using Microsoft.AspNetCore.Identity;

namespace AgroFlow.Data.Identity;

/// <summary>
/// Prepared tenant-aware Identity user model. It is intentionally not registered
/// in AuthenticateContext until the B03 Identity schema and migration are approved.
/// </summary>
public sealed class AgroFlowUser : IdentityUser
{
    public Guid? IngenioId { get; set; }
    public bool IsActive { get; set; } = true;
}
