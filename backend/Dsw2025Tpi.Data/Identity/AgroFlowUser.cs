using Microsoft.AspNetCore.Identity;

namespace Dsw2025Tpi.Data.Identity;

public sealed class AgroFlowUser : IdentityUser
{
    public Guid? IngenioId { get; set; }
    public bool IsActive { get; set; } = true;
}
