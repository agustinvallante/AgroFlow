using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.Api.Authorization;

public static class AgroFlowRoles
{
    public const string Operador = "OPERADOR";
    public const string Supervisor = "SUPERVISOR";
    public const string Gerente = "GERENTE";
    public const string Administrador = "ADMINISTRADOR";

    public static readonly string[] ApprovedInternalLogin =
    [
        Operador,
        Supervisor
    ];
}

public static class AgroFlowPolicies
{
    public const string Read = "AgroFlow.Read";
    public const string OperationalMutation = "AgroFlow.OperationalMutation";
    public const string CatalogManagement = "AgroFlow.CatalogManagement";
    public const string AssociationManagement = "AgroFlow.AssociationManagement";
    public const string UnapprovedOperation = "AgroFlow.UnapprovedOperation";
    public const string AuditRead = "AgroFlow.AuditRead";
    public const string NotificationRetry = "AgroFlow.NotificationRetry";
    public const string UserRoleAdministration = "AgroFlow.UserRoleAdministration";

    public static IServiceCollection AddAgroFlowAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Read, policy => policy.RequireRole(AgroFlowRoles.ApprovedInternalLogin));
            options.AddPolicy(
                OperationalMutation,
                policy => policy.RequireRole(AgroFlowRoles.ApprovedInternalLogin));
            options.AddPolicy(
                CatalogManagement,
                policy => policy.RequireRole(AgroFlowRoles.ApprovedInternalLogin));
            options.AddPolicy(AssociationManagement, policy => policy.RequireAssertion(_ => false));
            options.AddPolicy(UnapprovedOperation, policy => policy.RequireAssertion(_ => false));
            options.AddPolicy(
                AuditRead,
                policy => policy.RequireAssertion(_ => false));
            options.AddPolicy(NotificationRetry, policy => policy.RequireAssertion(_ => false));
            options.AddPolicy(UserRoleAdministration, policy => policy.RequireAssertion(_ => false));
        });

        return services;
    }
}
