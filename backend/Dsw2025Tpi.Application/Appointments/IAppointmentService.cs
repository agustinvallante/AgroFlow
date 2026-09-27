namespace Dsw2025Tpi.Application.Appointments;

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
}
