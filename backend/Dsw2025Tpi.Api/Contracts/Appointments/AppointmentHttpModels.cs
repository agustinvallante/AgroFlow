using System.Text.Json.Serialization;
using Dsw2025Tpi.Application.Appointments;

namespace Dsw2025Tpi.Api.Contracts.Appointments;

// The demo contract deliberately rejects extra fields (notably a preferred window).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateAppointmentRequest
{
    public string? CarrierPhone { get; init; }
    public string? TruckPlate { get; init; }
    public string? FarmCode { get; init; }
    public string? CutAt { get; init; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public double? EstimatedLoadTons { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class TransitionAppointmentRequest
{
    public string? NewStatus { get; init; }
}

public sealed record AppointmentReferenceResponse(Guid Id, string Name);
public sealed record AppointmentTruckResponse(Guid Id, string Plate);
public sealed record AppointmentWindowResponse(DateTimeOffset StartAt, DateTimeOffset EndAt);

public sealed record AppointmentSummaryResponse(
    Guid Id,
    AppointmentTruckResponse Truck,
    AppointmentWindowResponse Window,
    string Status);

public sealed record AppointmentResponse(
    Guid Id,
    AppointmentReferenceResponse Carrier,
    AppointmentTruckResponse Truck,
    AppointmentReferenceResponse Farm,
    DateTimeOffset CutAt,
    double EstimatedLoadTons,
    AppointmentWindowResponse Window,
    string Status,
    DateTimeOffset CreatedAt);

public sealed class AppointmentProblemDetails
{
    public string Type { get; init; } = "about:blank";
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Code { get; init; }
    public required string TraceId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}

public static class AppointmentHttpMapper
{
    public static AppointmentResponse ToResponse(AppointmentSnapshot snapshot) => new(
        snapshot.Id,
        new AppointmentReferenceResponse(snapshot.Carrier.Id, snapshot.Carrier.Name),
        new AppointmentTruckResponse(snapshot.Truck.Id, snapshot.Truck.Plate),
        new AppointmentReferenceResponse(snapshot.Farm.Id, snapshot.Farm.Name),
        snapshot.CutAt,
        snapshot.EstimatedLoadTons,
        new AppointmentWindowResponse(snapshot.Window.StartAt, snapshot.Window.EndAt),
        ToWireStatus(snapshot.Status),
        snapshot.CreatedAt);

    public static AppointmentSummaryResponse ToResponse(AppointmentSummary summary) => new(
        summary.Id,
        new AppointmentTruckResponse(summary.Truck.Id, summary.Truck.Plate),
        new AppointmentWindowResponse(summary.Window.StartAt, summary.Window.EndAt),
        ToWireStatus(summary.Status));

    public static bool TryParseStatus(string? value, out AppointmentStatus status)
    {
        status = value switch
        {
            "ASIGNADO" => AppointmentStatus.Asignado,
            "EN_CAMINO" => AppointmentStatus.EnCamino,
            "EN_ESPERA" => AppointmentStatus.EnEspera,
            "INGRESADO" => AppointmentStatus.Ingresado,
            "EN_DESCARGA" => AppointmentStatus.EnDescarga,
            "FINALIZADO" => AppointmentStatus.Finalizado,
            "CANCELADO" => AppointmentStatus.Cancelado,
            _ => default
        };

        return value is "ASIGNADO" or "EN_CAMINO" or "EN_ESPERA" or "INGRESADO"
            or "EN_DESCARGA" or "FINALIZADO" or "CANCELADO";
    }

    public static string ToWireStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Asignado => "ASIGNADO",
        AppointmentStatus.EnCamino => "EN_CAMINO",
        AppointmentStatus.EnEspera => "EN_ESPERA",
        AppointmentStatus.Ingresado => "INGRESADO",
        AppointmentStatus.EnDescarga => "EN_DESCARGA",
        AppointmentStatus.Finalizado => "FINALIZADO",
        AppointmentStatus.Cancelado => "CANCELADO",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
