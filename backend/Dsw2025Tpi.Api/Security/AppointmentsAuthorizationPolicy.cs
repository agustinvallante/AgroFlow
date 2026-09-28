using Microsoft.AspNetCore.Authorization;

namespace Dsw2025Tpi.Api.Security;

/// <summary>
/// Política de acceso a `/api/v1/appointments`. No implementa autenticación
/// nueva: reutiliza el JwtBearer heredado. Con LocalDemo=true no exige nada
/// (perfil explícitamente sin autenticación); con LocalDemo=false exige un
/// usuario autenticado, igual que cualquier otro recurso protegido del
/// backend heredado. Se expone como método estático para que Program.cs y
/// los tests de ambos perfiles configuren exactamente la misma regla.
/// </summary>
public static class AppointmentsAuthorizationPolicy
{
    public const string Name = "AppointmentsAccess";

    public static void Configure(AuthorizationOptions options, bool isLocalDemo) =>
        options.AddPolicy(Name, policy =>
            policy.RequireAssertion(ctx => isLocalDemo || (ctx.User.Identity?.IsAuthenticated ?? false)));
}
