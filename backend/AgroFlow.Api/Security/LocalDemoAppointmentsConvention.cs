using AgroFlow.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AgroFlow.Api.Security;

/// <summary>
/// Publica una sola variante de las rutas de turnos para el perfil activo.
/// </summary>
public sealed class LocalDemoAppointmentsConvention(bool isLocalDemo) : IApplicationModelConvention
{
    private static readonly Type[] LocalDemoOnlyControllers =
    [
        typeof(AppointmentsController),
        typeof(ChannelController),
        typeof(ConversationsController),
        typeof(DriversController),
        typeof(FarmsController)
    ];

    public void Apply(ApplicationModel application)
    {
        Type[] excludedTypes = isLocalDemo
            ? [typeof(AuthenticateController), typeof(MvpAppointmentReadsController)]
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
