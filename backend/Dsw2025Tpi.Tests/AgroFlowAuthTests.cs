using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dsw2025Tpi.Api.Authorization;
using Dsw2025Tpi.Api.Controllers;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class AgroFlowAuthTests
{
    [Fact]
    public async Task Catalog_policies_allow_only_operator_and_supervisor()
    {
        using var provider = CreateAuthorizationProvider();
        var policy = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var expected = new[] { AgroFlowRoles.Operador, AgroFlowRoles.Supervisor };

        foreach (var policyName in new[] { AgroFlowPolicies.Read, AgroFlowPolicies.CatalogManagement })
        {
            var roleRequirement = Assert.IsType<RolesAuthorizationRequirement>(
                Assert.Single((await policy.GetPolicyAsync(policyName))!.Requirements));
            Assert.Equal(expected, roleRequirement.AllowedRoles);
        }
    }

    [Fact]
    public void Controller_exposes_only_approved_catalog_operations_and_future_policies_fail_closed()
    {
        var exposedOperations = typeof(MasterDataController).GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>()
                .SelectMany(attribute => attribute.HttpMethods.Select(verb => $"{verb} {attribute.Template}")))
            .OrderBy(operation => operation)
            .ToArray();
        var approvedOperations = new[]
        {
            "GET drivers", "GET drivers/{id:guid}", "POST drivers", "PUT drivers/{id:guid}", "POST drivers/{id:guid}/inactivation",
            "GET trucks", "GET trucks/{id:guid}", "POST trucks", "PUT trucks/{id:guid}", "POST trucks/{id:guid}/inactivation",
            "GET farms", "GET farms/{id:guid}", "POST farms", "PUT farms/{id:guid}", "POST farms/{id:guid}/inactivation"
        }.OrderBy(operation => operation).ToArray();
        Assert.Equal(approvedOperations, exposedOperations);

        var catalogMethods = typeof(MasterDataController).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true).Length > 0);
        Assert.All(catalogMethods, method =>
        {
            var expectedPolicy = method.GetCustomAttributes(typeof(HttpGetAttribute), true).Length > 0
                ? AgroFlowPolicies.Read
                : AgroFlowPolicies.CatalogManagement;
            Assert.Equal(expectedPolicy,
                Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Policy);
        });

        Assert.IsType<AssertionRequirement>(Assert.Single(GetPolicy(AgroFlowPolicies.AssociationManagement).Requirements));
        Assert.IsType<AssertionRequirement>(Assert.Single(GetPolicy(AgroFlowPolicies.UnapprovedOperation).Requirements));
    }

    [Theory]
    [InlineData("OPERADOR")]
    [InlineData("SUPERVISOR")]
    [InlineData("ADMIN")]
    public async Task Association_policy_denies_every_role(string role)
    {
        using var provider = CreateAuthorizationProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, role) },
            "test-authentication"));

        foreach (var policy in new[] { AgroFlowPolicies.AssociationManagement, AgroFlowPolicies.UnapprovedOperation })
        {
            var result = await authorization.AuthorizeAsync(principal, null, policy);
            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public void Authentication_controller_exposes_only_login_post_route()
    {
        var postRoutes = typeof(AuthenticateController).GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>())
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Equal(new string?[] { "login" }, postRoutes);
    }

    [Fact]
    public void Login_response_has_required_tenant_and_no_customer_id()
    {
        var userInfo = typeof(Dsw2025Tpi.Api.Contract.LoginUserInfo);
        Assert.Equal(typeof(Guid), userInfo.GetProperty("IngenioId")!.PropertyType);
        Assert.Null(userInfo.GetProperty("CustomerId"));
    }

    [Fact]
    public void Jwt_contains_stable_user_role_and_tenant_but_no_customer_id()
    {
        var service = CreateTokenService();
        var tenantId = Guid.NewGuid();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            service.GenerateToken("identity-123", AgroFlowRoles.Operador, tenantId));

        Assert.Equal("identity-123", token.Subject);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == AgroFlowRoles.Operador);
        Assert.Contains(token.Claims, claim => claim.Type == JwtTokenService.IngenioIdClaimType && claim.Value == tenantId.ToString("D"));
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "customerId");
        Assert.Equal(DateTimeKind.Utc, token.ValidTo.Kind);
    }

    [Theory]
    [InlineData("", "OPERADOR", false)]
    [InlineData("identity-123", "GERENTE", false)]
    [InlineData("identity-123", "Admin", false)]
    [InlineData("identity-123", "User", false)]
    [InlineData("identity-123", "USER", false)]
    [InlineData("identity-123", "SUPERVISOR", true)]
    public void Jwt_rejects_missing_identity_or_unapproved_role(string identity, string role, bool valid)
    {
        var service = CreateTokenService();
        var tenantId = Guid.NewGuid();

        if (valid)
        {
            Assert.False(string.IsNullOrWhiteSpace(service.GenerateToken(identity, role, tenantId)));
            return;
        }

        var exception = Assert.Throws<ArgumentException>(() => service.GenerateToken(identity, role, tenantId));
        Assert.Equal(identity.Length == 0 ? "userId" : "role", exception.ParamName);
    }

    [Fact]
    public void Jwt_rejects_empty_tenant()
    {
        var service = CreateTokenService();

        var exception = Assert.Throws<ArgumentException>(() =>
            service.GenerateToken("identity-123", AgroFlowRoles.Operador, Guid.Empty));

        Assert.Equal("ingenioId", exception.ParamName);
    }

    private static ServiceProvider CreateAuthorizationProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAgroFlowAuthorizationPolicies();
        return services.BuildServiceProvider();
    }

    private static AuthorizationPolicy GetPolicy(string name) =>
        CreateAuthorizationProvider().GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync(name).GetAwaiter().GetResult()!;

    private static JwtTokenService CreateTokenService()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "01234567890123456789012345678901",
            ["Jwt:Issuer"] = "AgroFlow.Tests",
            ["Jwt:Audience"] = "AgroFlow.Tests.Client",
            ["Jwt:ExpiresInMinutes"] = "15"
        };
        return new JwtTokenService(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), NullLogger<JwtTokenService>.Instance);
    }
}
