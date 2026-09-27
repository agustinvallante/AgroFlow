using System.Text.RegularExpressions;

namespace Dsw2025Tpi.Application.Appointments;

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
        if (string.IsNullOrWhiteSpace(command.TruckPlate) || command.TruckPlate.Length is < 6 or > 10)
            errors["truckPlate"] = ["Debe tener entre 6 y 10 caracteres."];
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
        if (query.TruckPlate is { } plate && (string.IsNullOrWhiteSpace(plate) || plate.Length is < 6 or > 10))
            errors["truckPlate"] = ["Debe tener entre 6 y 10 caracteres."];
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

        var result = await _store.TryTransitionAsync(
            id,
            current.Status,
            newStatus,
            releaseCapacity: newStatus == AppointmentStatus.Cancelado,
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
