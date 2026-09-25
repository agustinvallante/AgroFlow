using System.Security.Claims;
using Dsw2025Tpi.Api.Services;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class HttpTenantContextTests
{
    [Fact]
    public void Authenticated_principal_returns_ingenio_and_actor_claims()
    {
        var ingenioId = Guid.NewGuid();
        var tenantContext = CreateTenantContext(
            new Claim(JwtTokenService.IngenioIdClaimType, ingenioId.ToString("D")),
            new Claim(ClaimTypes.NameIdentifier, "actor-123"));

        Assert.Equal(ingenioId, tenantContext.IngenioId);
        Assert.Equal("actor-123", tenantContext.ActorIdentity);
    }

    [Fact]
    public void Missing_http_context_fails_closed()
    {
        var tenantContext = new HttpTenantContext(new HttpContextAccessor());

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.IngenioId);
        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.ActorIdentity);
    }

    [Fact]
    public void Unauthenticated_principal_fails_closed()
    {
        var tenantContext = CreateTenantContext(
            new ClaimsIdentity(new[]
            {
                new Claim(JwtTokenService.IngenioIdClaimType, Guid.NewGuid().ToString("D")),
                new Claim(ClaimTypes.NameIdentifier, "actor-123")
            }));

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.IngenioId);
        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.ActorIdentity);
    }

    [Fact]
    public void Missing_ingenio_claim_fails_closed()
    {
        var tenantContext = CreateTenantContext(new Claim(ClaimTypes.NameIdentifier, "actor-123"));

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.IngenioId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Invalid_ingenio_claim_fails_closed(string value)
    {
        var tenantContext = CreateTenantContext(new Claim(JwtTokenService.IngenioIdClaimType, value));

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.IngenioId);
    }

    [Fact]
    public void Conflicting_ingenio_claims_fail_closed()
    {
        var tenantContext = CreateTenantContext(
            new Claim(JwtTokenService.IngenioIdClaimType, Guid.NewGuid().ToString("D")),
            new Claim(JwtTokenService.IngenioIdClaimType, Guid.NewGuid().ToString("D")));

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.IngenioId);
    }

    [Fact]
    public void Conflicting_actor_claims_fail_closed()
    {
        var tenantContext = CreateTenantContext(
            new Claim(ClaimTypes.NameIdentifier, "actor-1"),
            new Claim("sub", "actor-2"));

        Assert.Throws<UnauthorizedAccessException>(() => tenantContext.ActorIdentity);
    }

    private static HttpTenantContext CreateTenantContext(params Claim[] claims) =>
        CreateTenantContext(new ClaimsIdentity(claims, "test-authentication"));

    private static HttpTenantContext CreateTenantContext(ClaimsIdentity identity)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return new HttpTenantContext(new HttpContextAccessor { HttpContext = httpContext });
    }
}
