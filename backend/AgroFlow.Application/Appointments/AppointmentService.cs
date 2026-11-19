using System.Text.RegularExpressions;
using AgroFlow.Domain.Entities;

namespace AgroFlow.Application.Appointments;

/// <summary>
/// Casos de uso de la demo local. La asignación y escritura atómicas requieren
/// el adaptador de persistencia de Persona 2; este servicio no simula cupos.
/// </summary>
public sealed class AppointmentService : IAppointmentService
{
    private static readonly Regex PhonePattern = new(@"\A\+[1-9][0-9]{7,14}\z", RegexOptions.Compiled);
    private static readonly Regex FarmCodePattern = new(@"\A[A-Za-z0-9][A-Za-z0-9_-]*\z", RegexOptions.Compiled);

    private readonly IAppointmentStore _store;
    private readonly TimeProvider _timeProvider;

    public AppointmentService(IAppointmentStore store, TimeProvider? timeProvider = null)
    {
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AppointmentSnapshot> CreateAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        if (!IsValidPhone(command.CarrierPhone))
            errors["carrierPhone"] = ["Debe ser un teléfono E.164 válido."];
        if (!IsValidTruckPlate(command.TruckPlate))
            errors["truckPlate"] = [TruckPlateError];
        if (string.IsNullOrEmpty(command.FarmCode) || command.FarmCode.Length > 50 ||
            !FarmCodePattern.IsMatch(command.FarmCode))
            errors["farmCode"] = ["Debe ser un código de finca válido de hasta 50 caracteres."];
        if (!double.IsFinite(command.EstimatedLoadTons) || command.EstimatedLoadTons <= 0)
            errors["estimatedLoadTons"] = ["Debe ser mayor que cero."];

        if (errors.Count > 0)
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ValidationError,
                "Solicitud inválida.",
                errors);

        var context = await _store.GetCreationContextAsync(command, cancellationToken);
        if (context is null)
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ReferenceNotFound,
                "Referencia inexistente o sin asociación activa.");
        if (context.HasActiveAppointment)
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ActiveAppointmentExists,
                "El camión ya posee un turno activo.");

        var now = _timeProvider.GetUtcNow();
        var candidates = context.Windows
            .Where(window => window.StartAt > now && window.Occupied < window.Capacity)
            .OrderBy(window => window.StartAt)
            .ThenBy(window => window.Id);

        foreach (var window in candidates)
        {
            var result = await _store.TryCreateAssignedAsync(command, window.Id, cancellationToken);
            switch (result.Outcome)
            {
                case AppointmentCreateOutcome.Success when result.Appointment is not null:
                    return result.Appointment;
                case AppointmentCreateOutcome.Success:
                    throw new InvalidOperationException("El adaptador confirmó un turno sin respuesta persistida.");
                case AppointmentCreateOutcome.ReferenceChanged:
                    throw new AppointmentServiceException(
                        AppointmentErrorCodes.ReferenceNotFound,
                        "Referencia inexistente o sin asociación activa.");
                case AppointmentCreateOutcome.ActiveAppointmentExists:
                    throw new AppointmentServiceException(
                        AppointmentErrorCodes.ActiveAppointmentExists,
                        "El camión ya posee un turno activo.");
                case AppointmentCreateOutcome.CapacityChanged:
                    // Otro turno ocupó la ventana entre lectura y escritura: probar la siguiente.
                    break;
                default:
                    throw new InvalidOperationException("El adaptador devolvió un resultado de alta desconocido.");
            }
        }

        throw new AppointmentServiceException(
            AppointmentErrorCodes.NoCapacity,
            "No hay capacidad disponible en las ventanas futuras sembradas.");
    }

    public async Task<IReadOnlyList<AppointmentSummary>> ListAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new Dictionary<string, string[]>();
        if (query.Status is { } status && !Enum.IsDefined(status))
            errors["status"] = ["Debe ser un estado válido."];
        if (query.TruckPlate is { } plate && !IsValidTruckPlate(plate))
            errors["truckPlate"] = [TruckPlateError];
        if (query.Phone is { } phone && !IsValidPhone(phone))
            errors["phone"] = ["Debe ser un teléfono E.164 válido."];

        if (errors.Count > 0)
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ValidationError,
                "Filtros inválidos.",
                errors);

        var appointments = await _store.ListAsync(query, cancellationToken);
        return appointments
            .OrderBy(appointment => appointment.Window.StartAt)
            .ThenBy(appointment => appointment.Id)
            .ToArray();
    }

    public async Task<AppointmentSnapshot> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetAsync(id, cancellationToken)
            ?? throw new AppointmentServiceException(
                AppointmentErrorCodes.AppointmentNotFound,
                "Turno inexistente.");
    }

    public async Task<AppointmentSnapshot> TransitionAsync(
        Guid id,
        AppointmentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(newStatus))
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ValidationError,
                "Estado inválido.",
                new Dictionary<string, string[]> { ["newStatus"] = ["Debe ser un estado válido."] });

        var current = await GetAsync(id, cancellationToken);
        if (!CanTransition(current.Status, newStatus))
            throw new AppointmentServiceException(
                AppointmentErrorCodes.InvalidTransition,
                "Transición inválida.");

        // Leaving EN_CAMINO (arrival or cancellation) closes the open route incident
        // in the same persistence unit as the status change.
        var result = await _store.TryTransitionAsync(
            id,
            current.Status,
            newStatus,
            releaseCapacity: newStatus == AppointmentStatus.Cancelado,
            resolveOpenIncident: current.Status == AppointmentStatus.EnCamino && newStatus != AppointmentStatus.EnCamino,
            cancellationToken);

        return result.Outcome switch
        {
            AppointmentTransitionOutcome.Success when result.Appointment is not null => result.Appointment,
            AppointmentTransitionOutcome.NotFound => throw new AppointmentServiceException(
                AppointmentErrorCodes.AppointmentNotFound,
                "Turno inexistente."),
            AppointmentTransitionOutcome.StateChanged => throw new AppointmentServiceException(
                AppointmentErrorCodes.InvalidTransition,
                "El estado del turno cambió antes de completar la transición."),
            _ => throw new InvalidOperationException("El adaptador devolvió una transición incompleta.")
        };
    }

    public async Task<AppointmentSnapshot> ReportIncidentAsync(
        Guid id,
        ReportRouteIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ValidateIncident(command);
        if (errors.Count > 0)
            throw new AppointmentServiceException(
                AppointmentErrorCodes.ValidationError,
                "Solicitud inválida.",
                errors);

        var current = await GetAsync(id, cancellationToken);
        if (current.Status != AppointmentStatus.EnCamino)
            throw IncidentNotAllowed();
        if (current.OpenIncident is not null)
            throw OpenIncidentExists();

        var result = await _store.TryReportIncidentAsync(id, command, cancellationToken);
        return ToIncidentResponse(result);
    }

    public async Task<AppointmentSnapshot> ResolveIncidentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current.OpenIncident is null)
            throw NoOpenIncident();

        var result = await _store.TryResolveIncidentAsync(id, cancellationToken);
        return ToIncidentResponse(result);
    }

    /// <summary>Reglas de entrada de un incidente en ruta, también usadas por la API para responder por campo.</summary>
    public static Dictionary<string, string[]> ValidateIncident(ReportRouteIncidentCommand command)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!Enum.IsDefined(command.Type))
            errors["type"] = [IncidentTypeError];
        if (command.EstimatedDelayMinutes is < 0 or > MaxIncidentDelayMinutes)
            errors["estimatedDelayMinutes"] = [IncidentDelayError];
        if (command.Description is { Length: > MaxIncidentDescriptionLength })
            errors["description"] = [IncidentDescriptionError];
        return errors;
    }

    public const int MaxIncidentDelayMinutes = IncidenteRuta.MaxEstimatedDelayMinutes;
    public const int MaxIncidentDescriptionLength = IncidenteRuta.MaxDescriptionLength;
    public const string IncidentTypeError = "Debe ser PINCHAZO, FALLA_MECANICA, ACCIDENTE u OTRO.";
    public const string IncidentDelayError = "Debe ser un entero entre 0 y 1440.";
    public const string IncidentDescriptionError = "Debe tener como máximo 500 caracteres.";

    private static AppointmentSnapshot ToIncidentResponse(AppointmentIncidentResult result) =>
        result.Outcome switch
        {
            AppointmentIncidentOutcome.Success when result.Appointment is not null => result.Appointment,
            AppointmentIncidentOutcome.NotFound => throw new AppointmentServiceException(
                AppointmentErrorCodes.AppointmentNotFound,
                "Turno inexistente."),
            AppointmentIncidentOutcome.NotEnCamino => throw IncidentNotAllowed(),
            AppointmentIncidentOutcome.OpenIncidentExists => throw OpenIncidentExists(),
            AppointmentIncidentOutcome.NoOpenIncident => throw NoOpenIncident(),
            _ => throw new InvalidOperationException("El adaptador devolvió un incidente incompleto.")
        };

    private static AppointmentServiceException IncidentNotAllowed() => new(
        AppointmentErrorCodes.IncidentNotAllowed,
        "Sólo se puede reportar un incidente de un turno EN_CAMINO.");

    private static AppointmentServiceException OpenIncidentExists() => new(
        AppointmentErrorCodes.OpenIncidentExists,
        "El turno ya tiene un incidente abierto.");

    private static AppointmentServiceException NoOpenIncident() => new(
        AppointmentErrorCodes.NoOpenIncident,
        "El turno no tiene un incidente abierto.");

    /// <summary>Mensaje único para una patente que no cumple <see cref="IsValidTruckPlate"/>.</summary>
    public const string TruckPlateError = "Debe tener entre 6 y 10 caracteres y al menos una letra o dígito.";

    /// <summary>
    /// Regla de entrada de la patente: 6 a 10 caracteres y al menos una letra o
    /// dígito, para que la normalización (sin separadores, en mayúsculas) nunca
    /// quede vacía.
    /// </summary>
    public static bool IsValidTruckPlate(string? plate) =>
        !string.IsNullOrWhiteSpace(plate) && plate.Length is >= 6 and <= 10 && plate.Any(char.IsLetterOrDigit);

    private static bool IsValidPhone(string? phone) =>
        phone is not null && PhonePattern.IsMatch(phone);

    private static bool CanTransition(AppointmentStatus from, AppointmentStatus to) =>
        (from, to) switch
        {
            (AppointmentStatus.Asignado, AppointmentStatus.EnCamino) => true,
            (AppointmentStatus.EnCamino, AppointmentStatus.EnEspera) => true,
            (AppointmentStatus.EnEspera, AppointmentStatus.Ingresado) => true,
            (AppointmentStatus.Ingresado, AppointmentStatus.EnDescarga) => true,
            (AppointmentStatus.EnDescarga, AppointmentStatus.Finalizado) => true,
            (AppointmentStatus.Asignado or AppointmentStatus.EnCamino or AppointmentStatus.EnEspera,
                AppointmentStatus.Cancelado) => true,
            _ => false
        };
}
