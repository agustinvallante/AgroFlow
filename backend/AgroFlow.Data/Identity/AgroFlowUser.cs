using Microsoft.AspNetCore.Identity;

namespace AgroFlow.Data.Identity;

/// <summary>
/// Usuario interno de Identity asociado a un único ingenio.
/// </summary>
public sealed class AgroFlowUser : IdentityUser
{
    public Guid IngenioId { get; set; }
    public bool IsActive { get; set; } = true;
}
