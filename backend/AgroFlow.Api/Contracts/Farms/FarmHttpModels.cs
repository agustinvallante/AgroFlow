namespace AgroFlow.Api.Contracts.Farms;

/// <summary>Finca del ingenio en sólo lectura: código canónico para el alta de turnos y nombre visible.</summary>
public sealed record FarmResponse(Guid Id, string Code, string Name, bool IsActive);
