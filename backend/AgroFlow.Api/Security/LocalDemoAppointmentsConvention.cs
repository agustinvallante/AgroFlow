using AgroFlow.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AgroFlow.Api.Security;

/// <summary>
/// La API de turnos sin identidad pertenece exclusivamente al perfil de demo;
/// el login Identity heredado se conserva sólo fuera de ese perfil hasta B03.
/// </summary>
public sealed class LocalDemoAppointmentsConvention(bool isLocalDemo) : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var excludedType = isLocalDemo
            ? typeof(AuthenticateController)
            : typeof(AppointmentsController);
        var controller = application.Controllers.FirstOrDefault(
            candidate => candidate.ControllerType.AsType() == excludedType);
        if (controller is not null)
        {
            application.Controllers.Remove(controller);
        }
    }
}
