using System.Text.Json.Serialization;

namespace AgroFlow.Api.Contracts.Drivers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DriverInputRequest
{
    public string? Name { get; init; }
    public string? Dni { get; init; }
    public string? WhatsApp { get; init; }
}

/// <param name="ActiveTruckPlates">Camiones activos con asociación activa a este transportista (sólo lectura).</param>
/// <param name="CanRequestAppointments">
/// Lo decide el backend: un turno exige transportista activo y una asociación
/// activa con un camión activo (datos-maestros). La gestión de asociaciones
/// sigue pendiente (OD-002/OD-013) y no tiene ruta publicada.
/// </param>
public sealed record DriverResponse(
    Guid Id,
    string Name,
    string Dni,
    string WhatsApp,
    bool IsActive,
    IReadOnlyList<string> ActiveTruckPlates,
    bool CanRequestAppointments);

public sealed record DriverMutationResponse(Guid ResourceId, string ResourceType, string Outcome);
