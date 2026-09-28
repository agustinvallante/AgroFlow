using Microsoft.AspNetCore.Authorization;

namespace Dsw2025Tpi.Api.Security;

/// <summary>
/// Política de acceso a `/api/v1/appointments`. No implementa autenticación
/// nueva: con LocalDemo=true no exige credenciales. Fuera de ese perfil se
/// deniega incluso si una ruta fuese publicada accidentalmente; el convenio
/// MVC también elimina el controlador completo.
/// </summary>
public static class AppointmentsAuthorizationPolicy
{
    public const string Name = "AppointmentsAccess";

    public static void Configure(AuthorizationOptions options, bool isLocalDemo) =>
        options.AddPolicy(Name, policy =>
            policy.RequireAssertion(_ => isLocalDemo));
}
