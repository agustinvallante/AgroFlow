namespace AgroFlow.Application.Appointments;

/// <summary>
/// Ventana persistida con su cupo: inicio y fin ya con el desplazamiento del
/// ingenio, capacidad configurada y cupos ocupados tal como están guardados.
/// </summary>
public sealed record WindowOccupancy(Guid Id, DateTimeOffset StartAt, DateTimeOffset EndAt, int Capacity, int Occupied);

/// <summary>
/// Lectura sin efectos de las ventanas de un ingenio (B39, #44). Sólo consulta
/// lo persistido: no genera calendario ni recalcula la ocupación.
/// </summary>
public interface IWindowReader
{
    /// <summary>
    /// Ventanas del ingenio cuyo inicio cae en la fecha local indicada, o en la
    /// fecha local actual del ingenio si es null (de la medianoche local incluida
    /// a la siguiente excluida), ordenadas por inicio. Incluye vacías y ocupadas;
    /// sin ventanas devuelve una lista vacía.
    /// </summary>
    Task<IReadOnlyList<WindowOccupancy>> ListWindowsAsync(
        Guid ingenioId, DateOnly? date, CancellationToken cancellationToken);
}
