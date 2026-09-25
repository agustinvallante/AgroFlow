using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.Api.Authorization;

public static class AgroFlowRoles
{
    public const string Operador = "OPERADOR";
    public const string Supervisor = "SUPERVISOR";
    public const string Gerente = "GERENTE";
    public const string Administrador = "ADMINISTRADOR";

    public static readonly string[] All =
    [
        Operador,
        Supervisor,
        Gerente,
        Administrador
    ];
}

public static class AgroFlowPolicies
{
    public const string Read = "AgroFlow.Read";
    public const string OperationalMutation = "AgroFlow.OperationalMutation";
    public const string MasterDataAndCapacityManagement = "AgroFlow.MasterDataAndCapacityManagement";
    public const string AuditRead = "AgroFlow.AuditRead";
    public const string NotificationRetry = "AgroFlow.NotificationRetry";
    public const string UserRoleAdministration = "AgroFlow.UserRoleAdministration";

    public static IServiceCollection AddAgroFlowAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Read, policy => policy.RequireRole(AgroFlowRoles.All));
            options.AddPolicy(
                OperationalMutation,
                policy => policy.RequireRole(
                    AgroFlowRoles.Operador,
                    AgroFlowRoles.Supervisor,
                    AgroFlowRoles.Administrador));
            options.AddPolicy(
                MasterDataAndCapacityManagement,
                policy => policy.RequireRole(AgroFlowRoles.Supervisor, AgroFlowRoles.Administrador));
            options.AddPolicy(
                AuditRead,
                policy => policy.RequireRole(
                    AgroFlowRoles.Supervisor,
                    AgroFlowRoles.Gerente,
                    AgroFlowRoles.Administrador));
            options.AddPolicy(
                NotificationRetry,
                policy => policy.RequireRole(AgroFlowRoles.Supervisor, AgroFlowRoles.Administrador));
            options.AddPolicy(
                UserRoleAdministration,
                policy => policy.RequireRole(AgroFlowRoles.Administrador));
        });

        return services;
    }
}
