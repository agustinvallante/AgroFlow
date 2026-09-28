namespace Dsw2025Tpi.Application.Appointments;

public static class AppointmentErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ReferenceNotFound = "REFERENCE_NOT_FOUND";
    public const string AppointmentNotFound = "APPOINTMENT_NOT_FOUND";
    public const string ActiveAppointmentExists = "ACTIVE_APPOINTMENT_EXISTS";
    public const string NoCapacity = "NO_CAPACITY";
    public const string InvalidTransition = "INVALID_TRANSITION";
}

/// <summary>
/// Error esperado por la API. El adaptador de persistencia también debe usarlo para
/// los conflictos de referencias, cupo y turno activo, sin exponer detalles internos.
/// </summary>
public sealed class AppointmentServiceException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public AppointmentServiceException(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Code = code;
        Errors = errors;
    }
}
