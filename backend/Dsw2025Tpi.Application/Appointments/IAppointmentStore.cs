namespace Dsw2025Tpi.Application.Appointments;

public sealed record AppointmentCandidateWindow(
    Guid Id,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Capacity,
    int Occupied);

/// <summary>
/// Null en GetCreationContextAsync significa que alguna referencia no existe, no está
/// activa o que el camión no tiene asociación activa con el transportista indicado.
/// </summary>
public sealed record AppointmentCreationContext(
    bool HasActiveAppointment,
    IReadOnlyList<AppointmentCandidateWindow> Windows);

public enum AppointmentCreateOutcome
{
    Success,
    ReferenceChanged,
    ActiveAppointmentExists,
    CapacityChanged
}

public sealed record AppointmentCreateResult(
    AppointmentCreateOutcome Outcome,
    AppointmentSnapshot? Appointment);

public enum AppointmentTransitionOutcome
{
    Success,
    NotFound,
    StateChanged
}

public sealed record AppointmentTransitionResult(
    AppointmentTransitionOutcome Outcome,
    AppointmentSnapshot? Appointment);

/// <summary>
/// Puerto que implementará Persona 2 con persistencia real. Las lecturas suministran
/// estado para que la capa Application aplique las reglas; las escrituras deben volver a
/// comprobar invariantes en una transacción, pues la lectura puede quedar desactualizada.
/// </summary>
public interface IAppointmentStore
{
    /// <summary>
    /// Resuelve teléfono, patente y código de finca del seed, comprueba actividad y
    /// asociación transportista-camión, y lee turno activo y ventanas del ingenio.
    /// Devuelve null ante referencia inválida sin mutar capacidad.
    /// </summary>
    Task<AppointmentCreationContext?> GetCreationContextAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revalida referencias y asociación, ausencia de turno activo, ventana futura
    /// seleccionada y cupo; inserta ASIGNADO con ventana y ocupa cupo en una única
    /// transacción. Los rechazos no deben dejar reserva ni turno parcial. En conflicto
    /// concurrente de cupo devuelve CapacityChanged para probar otra ventana.
    /// </summary>
    Task<AppointmentCreateResult> TryCreateAssignedAsync(
        CreateAppointmentCommand command,
        Guid windowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtra por conjunción y ordena por inicio de ventana. Sin ningún filtro,
    /// usa la fecha local actual del ingenio sembrado. Las consultas no mutan datos.
    /// </summary>
    Task<IReadOnlyList<AppointmentSummary>> ListAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default);

    Task<AppointmentSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compara el estado persistido con expectedStatus y, solo si coincide, persiste
    /// nextStatus. En CANCELADO libera cupo de forma atómica; conserva el turno para
    /// futuras lecturas. StateChanged indica conflicto concurrente, no éxito.
    /// </summary>
    Task<AppointmentTransitionResult> TryTransitionAsync(
        Guid id,
        AppointmentStatus expectedStatus,
        AppointmentStatus nextStatus,
        bool releaseCapacity,
        CancellationToken cancellationToken = default);
}
