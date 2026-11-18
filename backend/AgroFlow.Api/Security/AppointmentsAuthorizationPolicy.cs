using AgroFlow.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace AgroFlow.Api.Security;

/// <summary>
/// Políticas separadas para la demo local y las lecturas internas del MVP.
/// </summary>
public static class AppointmentsAuthorizationPolicy
{
    public const string Name = "AppointmentsAccess";
    public const string MvpRead = "MvpAppointmentsRead";

    public static void Configure(AuthorizationOptions options, bool isLocalDemo)
    {
        options.AddPolicy(Name, policy =>
            policy.RequireAssertion(_ => isLocalDemo));
        options.AddPolicy(MvpRead, policy => policy
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole(AgroFlowRoles.ApprovedInternalLogin));
    }
}
