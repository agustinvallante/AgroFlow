namespace AgroFlow.Application.Appointments;

public interface IAppointmentService
{
    Task<AppointmentSnapshot> CreateAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentSummary>> ListAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default);

    Task<AppointmentSnapshot> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppointmentSnapshot> TransitionAsync(
        Guid id,
        AppointmentStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Registra un incidente en ruta sobre un turno EN_CAMINO sin cambiar su estado ni ventana.</summary>
    Task<AppointmentSnapshot> ReportIncidentAsync(
        Guid id,
        ReportRouteIncidentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Resuelve el incidente abierto del turno, conservando el registro.</summary>
    Task<AppointmentSnapshot> ResolveIncidentAsync(Guid id, CancellationToken cancellationToken = default);
}
