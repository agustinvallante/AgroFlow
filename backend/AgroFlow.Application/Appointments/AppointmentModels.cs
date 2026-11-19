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

/// <summary>Tipos de incidente en ruta definidos por docs/contracts/openapi.yaml.</summary>
public enum RouteIncidentType
{
    Pinchazo,
    FallaMecanica,
    Accidente,
    Otro
}

public sealed record ReportRouteIncidentCommand(
    RouteIncidentType Type,
    bool CanContinue,
    int EstimatedDelayMinutes,
    string? Description);

/// <summary>Incidente abierto de un turno; ReportedAt lleva el desplazamiento del ingenio.</summary>
public sealed record RouteIncident(
    Guid Id,
    RouteIncidentType Type,
    bool CanContinue,
    int EstimatedDelayMinutes,
    string? Description,
    DateTimeOffset ReportedAt);

/// <summary>
/// A resolved route incident seen as a stop: from its report to its resolution,
/// both with the mill's offset.
/// </summary>
public sealed record AppointmentStop(DateTimeOffset StartedAt, DateTimeOffset EndedAt);

/// <summary>
/// DepartedAt (EN_CAMINO instant) and Stops (resolved incidents ordered by start)
/// are LocalDemo read fields; null Stops means "not loaded" and maps to an empty list.
/// </summary>
public sealed record AppointmentSummary(
    Guid Id,
    AppointmentTruckReference Truck,
    AppointmentWindow Window,
    AppointmentStatus Status,
    RouteIncident? OpenIncident = null,
    DateTimeOffset? DepartedAt = null,
    IReadOnlyList<AppointmentStop>? Stops = null);

public sealed record AppointmentSnapshot(
    Guid Id,
    AppointmentReference Carrier,
    AppointmentTruckReference Truck,
    AppointmentReference Farm,
    DateTimeOffset CutAt,
    double EstimatedLoadTons,
    AppointmentWindow Window,
    AppointmentStatus Status,
    DateTimeOffset CreatedAt,
    RouteIncident? OpenIncident = null,
    DateTimeOffset? DepartedAt = null,
    IReadOnlyList<AppointmentStop>? Stops = null);
