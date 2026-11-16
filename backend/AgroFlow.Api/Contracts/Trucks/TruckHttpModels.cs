namespace AgroFlow.Api.Contracts.Trucks;

/// <summary>
/// Camión del ingenio en sólo lectura: patente canónica, tipo de flota
/// (`PROPIA` o `TERCEROS`) y si está habilitado.
/// </summary>
public sealed record TruckResponse(Guid Id, string Plate, string FleetType, bool IsActive);
