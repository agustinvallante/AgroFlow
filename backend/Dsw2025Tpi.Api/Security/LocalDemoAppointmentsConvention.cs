using Dsw2025Tpi.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Dsw2025Tpi.Api.Security;

/// <summary>
/// La API de turnos sin identidad pertenece exclusivamente al perfil de demo.
/// Fuera de él, MVC no crea acciones ni rutas para AppointmentsController.
/// </summary>
public sealed class LocalDemoAppointmentsConvention(bool isLocalDemo) : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        if (isLocalDemo)
        {
            return;
        }

        var controller = application.Controllers.FirstOrDefault(
            candidate => candidate.ControllerType.AsType() == typeof(AppointmentsController));
        if (controller is not null)
        {
            application.Controllers.Remove(controller);
        }
    }
}
