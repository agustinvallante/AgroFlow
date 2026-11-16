namespace AgroFlow.Api.Contracts.Windows;

/// <summary>
/// Ventana del ingenio con su cupo. `available` es `capacity - occupied`,
/// calculado en el backend; las fechas llevan el desplazamiento del ingenio.
/// </summary>
public sealed record WindowResponse(
    Guid Id,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Capacity,
    int Occupied,
    int Available);
