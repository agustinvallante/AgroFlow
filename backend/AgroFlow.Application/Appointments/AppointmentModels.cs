namespace AgroFlow.Application.Appointments;

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
/// Una fecha nula significa el día local actual del ingenio sembrado, incluso
/// cuando se informan otros filtros, salvo con ActiveOnly: entonces sólo se
/// devuelven turnos no terminales y, sin fecha, de cualquier día (incluidos los
/// futuros). El adaptador de persistencia determina la fecha local actual y
/// compara contra la fecha local de inicio de ventana.
/// </summary>
public sealed record AppointmentQuery(
    DateOnly? Date,
    AppointmentStatus? Status,
    string? TruckPlate,
    string? Phone,
    bool ActiveOnly = false);

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
