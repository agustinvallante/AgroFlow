using AgroFlow.Domain.Entities;
using ApplicationStatus = AgroFlow.Application.Appointments.AppointmentStatus;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Traduce entre el enum de Domain (<see cref="TurnoEstado"/>) y el de
/// Application (<see cref="ApplicationStatus"/>). Domain no referencia
/// Application: este mapeo es responsabilidad exclusiva del adaptador.
/// </summary>
internal static class AppointmentStatusMapper
{
    public static TurnoEstado ToDomain(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Asignado => TurnoEstado.Asignado,
        ApplicationStatus.EnCamino => TurnoEstado.EnCamino,
        ApplicationStatus.EnEspera => TurnoEstado.EnEspera,
        ApplicationStatus.Ingresado => TurnoEstado.Ingresado,
        ApplicationStatus.EnDescarga => TurnoEstado.EnDescarga,
        ApplicationStatus.Finalizado => TurnoEstado.Finalizado,
        ApplicationStatus.Cancelado => TurnoEstado.Cancelado,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static ApplicationStatus ToApplication(TurnoEstado status) => status switch
    {
        TurnoEstado.Asignado => ApplicationStatus.Asignado,
        TurnoEstado.EnCamino => ApplicationStatus.EnCamino,
        TurnoEstado.EnEspera => ApplicationStatus.EnEspera,
        TurnoEstado.Ingresado => ApplicationStatus.Ingresado,
        TurnoEstado.EnDescarga => ApplicationStatus.EnDescarga,
        TurnoEstado.Finalizado => ApplicationStatus.Finalizado,
        TurnoEstado.Cancelado => ApplicationStatus.Cancelado,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
