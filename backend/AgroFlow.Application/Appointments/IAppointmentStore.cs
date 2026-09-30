namespace AgroFlow.Application.Appointments;

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
/// Todos los DateTimeOffset devueltos en turnos y ventanas, incluidos cutAt y createdAt,
/// deben llevar el desplazamiento explícito de la zona del ingenio sembrado; el
/// servicio no debe fijar ni inferir ese desplazamiento.
/// </summary>
public interface IAppointmentStore
{
    /// <summary>
    /// Resuelve teléfono exacto en E.164, patente ignorando mayúsculas y
    /// separadores (comparando sólo letras y dígitos), y código de finca sin
    /// distinguir mayúsculas pero sin otra normalización. Comprueba actividad y
    /// asociación transportista-camión, y lee turno activo y ventanas del ingenio.
    /// Devuelve null ante referencia inválida sin mutar capacidad. Las referencias
    /// en las respuestas conservan la forma canónica almacenada en el seed.
    /// </summary>
    Task<AppointmentCreationContext?> GetCreationContextAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revalida las referencias con las mismas reglas de comparación y asociación,
    /// ausencia de turno activo, ventana futura
    /// seleccionada y cupo; inserta ASIGNADO con ventana y ocupa cupo en una única
    /// transacción. Los rechazos no deben dejar reserva ni turno parcial. En conflicto
    /// concurrente de cupo devuelve CapacityChanged para probar otra ventana.
    /// </summary>
    Task<AppointmentCreateResult> TryCreateAssignedAsync(
        CreateAppointmentCommand command,
        Guid windowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtra por conjunción y ordena por inicio de ventana. Si Date es null,
    /// siempre usa la fecha local actual del ingenio sembrado, aunque haya otros
    /// filtros; Date corresponde a la fecha local de inicio de la ventana. Phone
    /// se compara exactamente en E.164, TruckPlate ignora mayúsculas y separadores.
    /// La falta de coincidencias devuelve una lista vacía. La consulta no muta datos.
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
