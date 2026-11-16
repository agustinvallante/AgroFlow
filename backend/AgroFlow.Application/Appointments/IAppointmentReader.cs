namespace AgroFlow.Application.Appointments;

public sealed record AppointmentReadQuery(DateOnly? Date, AppointmentStatus? Status, string? TruckPlate,
    int Page, int PageSize);

public sealed record AppointmentPage(IReadOnlyList<AppointmentSnapshot> Items, long TotalCount);

public interface IAppointmentReader
{
    Task<AppointmentPage> ListAsync(Guid ingenioId, AppointmentReadQuery query, CancellationToken cancellationToken);
    Task<AppointmentSnapshot?> GetAsync(Guid ingenioId, Guid id, CancellationToken cancellationToken);
}
