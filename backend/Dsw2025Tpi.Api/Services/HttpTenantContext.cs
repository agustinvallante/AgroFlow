using System.Security.Claims;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Api.Services;

public sealed class HttpTenantContext : ITenantContext
{
    private const string SubjectClaimType = "sub";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid IngenioId
    {
        get
        {
            var value = GetRequiredClaimValue(JwtTokenService.IngenioIdClaimType);
            if (!Guid.TryParse(value, out var ingenioId) || ingenioId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("The authenticated identity has no valid effective ingenio.");
            }

            return ingenioId;
        }
    }

    public string ActorIdentity => GetRequiredClaimValue(ClaimTypes.NameIdentifier, SubjectClaimType);

    private string GetRequiredClaimValue(params string[] claimTypes)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("An authenticated identity is required.");
        }

        var values = claimTypes
            .SelectMany(principal.FindAll)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (values.Length != 1)
        {
            throw new UnauthorizedAccessException("The authenticated identity is missing a required unambiguous claim.");
        }

        return values[0];
    }
}
