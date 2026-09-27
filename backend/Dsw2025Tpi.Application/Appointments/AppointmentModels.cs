namespace Dsw2025Tpi.Application.Appointments;

/// <summary>Estados observables definidos por docs/contracts/openapi.yaml.</summary>
public enum AppointmentStatus
{
    Asignado,
    EnCamino,
    EnEspera,
    Ingresado,
    EnDescarga,
    Finalizado,
    Cancelado
}

public sealed record CreateAppointmentCommand(
    string CarrierPhone,
    string TruckPlate,
    string FarmCode,
    DateTimeOffset CutAt,
    double EstimatedLoadTons);

/// <summary>
/// Una fecha nula sin otros filtros significa el día local actual del ingenio.
/// La interpretación de fecha local pertenece al adaptador de persistencia del ingenio sembrado.
/// </summary>
public sealed record AppointmentQuery(
    DateOnly? Date,
    AppointmentStatus? Status,
    string? TruckPlate,
    string? Phone);

public sealed record AppointmentReference(Guid Id, string Name);

public sealed record AppointmentTruckReference(Guid Id, string Plate);

public sealed record AppointmentWindow(DateTimeOffset StartAt, DateTimeOffset EndAt);

public sealed record AppointmentSummary(
    Guid Id,
    AppointmentTruckReference Truck,
    AppointmentWindow Window,
    AppointmentStatus Status);

public sealed record AppointmentSnapshot(
    Guid Id,
    AppointmentReference Carrier,
    AppointmentTruckReference Truck,
    AppointmentReference Farm,
    DateTimeOffset CutAt,
    double EstimatedLoadTons,
    AppointmentWindow Window,
    AppointmentStatus Status,
    DateTimeOffset CreatedAt);
