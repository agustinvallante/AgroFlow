using AgroFlow.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AgroFlow.Api.Security;

/// <summary>
/// La API de turnos sin identidad y las rutas del canal, de conversaciones y de
/// transportistas (que protegen sus claves de <see cref="ApiKeySchemes"/>)
/// pertenecen exclusivamente al perfil de demo, el único que registra el
/// SQLite de AgroFlow. El login Identity heredado se conserva sólo fuera de ese
/// perfil hasta B03.
/// </summary>
public sealed class LocalDemoAppointmentsConvention(bool isLocalDemo) : IApplicationModelConvention
{
    private static readonly Type[] LocalDemoOnlyControllers =
    [
        typeof(AppointmentsController),
        typeof(ChannelController),
        typeof(ConversationsController),
        typeof(DriversController)
    ];

    public void Apply(ApplicationModel application)
    {
        Type[] excludedTypes = isLocalDemo
            ? [typeof(AuthenticateController)]
            : LocalDemoOnlyControllers;
        var excluded = application.Controllers
            .Where(candidate => excludedTypes.Contains(candidate.ControllerType.AsType()))
            .ToArray();
        foreach (var controller in excluded)
        {
            application.Controllers.Remove(controller);
        }
    }
}
