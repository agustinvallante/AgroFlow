using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dsw2025Tpi.Api.Contracts.Appointments;
using Dsw2025Tpi.Api.Security;
using Dsw2025Tpi.Application.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers;

/// <summary>
/// HTTP boundary for the local appointment demo. The Application service owns
/// state and business decisions; this controller only validates the wire contract.
/// Anonymous only under the LocalDemo profile: see <see cref="AppointmentsAuthorizationPolicy"/>.
/// </summary>
[Route("api/v1/appointments")]
[Authorize(Policy = AppointmentsAuthorizationPolicy.Name)]
public sealed class AppointmentsController : ControllerBase
{
    private static readonly Regex PhonePattern = new(
        @"^\+[1-9][0-9]{7,14}\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex FarmCodePattern = new(
        @"^[A-Za-z0-9][A-Za-z0-9_-]*\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex OffsetDateTimePattern = new(
        @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:[Zz]|[+-]\d{2}:\d{2})\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> SupportedQueryKeys =
        new(["date", "status", "truckPlate", "phone"], StringComparer.Ordinal);

    private static readonly HashSet<string> CreateBodyKeys =
        new(["carrierPhone", "truckPlate", "farmCode", "cutAt", "estimatedLoadTons"], StringComparer.Ordinal);

    private static readonly HashSet<string> TransitionBodyKeys =
        new(["newStatus"], StringComparer.Ordinal);

    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false
    };

    private readonly IAppointmentService _service;
    private readonly ILogger<AppointmentsController>? _logger;

    public AppointmentsController(
        IAppointmentService service,
        ILogger<AppointmentsController>? logger = null)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var (request, failure) = await ReadRequestBodyAsync<CreateAppointmentRequest>(
            CreateBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (request is null)
        {
            return ProblemResult(400, AppointmentErrorCodes.ValidationError);
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request.CarrierPhone is null || !PhonePattern.IsMatch(request.CarrierPhone))
        {
            errors["carrierPhone"] = ["Debe ser un teléfono completo en formato E.164."];
        }

        if (string.IsNullOrWhiteSpace(request.TruckPlate) ||
            request.TruckPlate.Length is < 6 or > 10)
        {
            errors["truckPlate"] = ["Debe contener entre 6 y 10 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(request.FarmCode) ||
            request.FarmCode.Length is < 1 or > 50 ||
            !FarmCodePattern.IsMatch(request.FarmCode))
        {
            errors["farmCode"] = ["Debe ser un código de 1 a 50 caracteres alfanuméricos, guiones o guiones bajos."];
        }

        DateTimeOffset cutAt = default;
        if (request.CutAt is null ||
            !OffsetDateTimePattern.IsMatch(request.CutAt) ||
            !DateTimeOffset.TryParse(
                request.CutAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out cutAt))
        {
            errors["cutAt"] = ["Debe ser una fecha y hora ISO 8601 con desplazamiento horario explícito."];
        }

        if (request.EstimatedLoadTons is null ||
            !double.IsFinite(request.EstimatedLoadTons.Value) ||
            request.EstimatedLoadTons <= 0)
        {
            errors["estimatedLoadTons"] = ["Debe ser mayor que cero."];
        }

        if (errors.Count > 0)
        {
            return ProblemResult(400, AppointmentErrorCodes.ValidationError, errors);
        }

        return await ExecuteAsync(async () =>
        {
            var created = await _service.CreateAsync(
                new CreateAppointmentCommand(
                    request.CarrierPhone!,
                    request.TruckPlate!,
                    request.FarmCode!,
                    cutAt,
                    request.EstimatedLoadTons!.Value),
                cancellationToken);

            return Created(
                $"/api/v1/appointments/{created.Id:D}",
                AppointmentHttpMapper.ToResponse(created));
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var key in Request.Query.Keys)
        {
            if (!SupportedQueryKeys.Contains(key))
            {
                errors[key] = ["Filtro no admitido."];
            }
        }

        var dateText = GetQueryValue("date", errors);
        var statusText = GetQueryValue("status", errors);
        var plate = GetQueryValue("truckPlate", errors);
        var phone = GetQueryValue("phone", errors);

        DateOnly? date = null;
        if (dateText is not null)
        {
            if (DateOnly.TryParseExact(
                dateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                date = parsedDate;
            }
            else
            {
                errors["date"] = ["Debe tener el formato yyyy-MM-dd."];
            }
        }

        AppointmentStatus? status = null;
        if (statusText is not null)
        {
            if (AppointmentHttpMapper.TryParseStatus(statusText, out var parsedStatus))
            {
                status = parsedStatus;
            }
            else
            {
                errors["status"] = ["Estado no reconocido."];
            }
        }

        if (plate is not null && (plate.Length is < 6 or > 10))
        {
            errors["truckPlate"] = ["Debe contener entre 6 y 10 caracteres."];
        }

        if (phone is not null && !PhonePattern.IsMatch(phone))
        {
            errors["phone"] = ["Debe ser un teléfono completo en formato E.164."];
        }

        if (errors.Count > 0)
        {
            return ProblemResult(400, AppointmentErrorCodes.ValidationError, errors);
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.ListAsync(
                new AppointmentQuery(date, status, plate, phone),
                cancellationToken);
            return Ok(result.Select(AppointmentHttpMapper.ToResponse).ToArray());
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var appointmentId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.GetAsync(appointmentId, cancellationToken);
            return Ok(AppointmentHttpMapper.ToResponse(result));
        });
    }

    [HttpPost("{id}/transitions")]
    public async Task<IActionResult> Transition(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var appointmentId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        var (request, failure) = await ReadRequestBodyAsync<TransitionAppointmentRequest>(
            TransitionBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (request is null)
        {
            return ProblemResult(400, AppointmentErrorCodes.ValidationError);
        }

        if (!AppointmentHttpMapper.TryParseStatus(request.NewStatus, out var newStatus))
        {
            return ValidationProblemFor("newStatus", "Debe ser un estado reconocido.");
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.TransitionAsync(
                appointmentId,
                newStatus,
                cancellationToken);
            return Ok(AppointmentHttpMapper.ToResponse(result));
        });
    }

    private string? GetQueryValue(string key, Dictionary<string, string[]> errors)
    {
        if (!Request.Query.TryGetValue(key, out var values))
        {
            return null;
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            errors[key] = ["Debe indicarse exactamente un valor no vacío."];
            return null;
        }

        return values[0];
    }

    private async Task<(T? Request, IActionResult? Failure)> ReadRequestBodyAsync<T>(
        IReadOnlySet<string> allowedKeys,
        CancellationToken cancellationToken) where T : class
    {
        // MVC's automatic body binder would emit a bare 415 or a different
        // validation payload. The demo contract requires ProblemDetails 400.
        var mediaType = Request.ContentType?.Split(';', 2)[0].Trim();
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return (null, ProblemResult(400, AppointmentErrorCodes.ValidationError));
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, ProblemResult(400, AppointmentErrorCodes.ValidationError));
            }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!allowedKeys.Contains(property.Name))
                {
                    errors[property.Name] = ["Propiedad no admitida."];
                }
                else if (!seenKeys.Add(property.Name))
                {
                    errors[property.Name] = ["La propiedad no puede repetirse."];
                }
            }

            if (errors.Count > 0)
            {
                return (null, ProblemResult(400, AppointmentErrorCodes.ValidationError, errors));
            }

            try
            {
                return (document.RootElement.Deserialize<T>(RequestJsonOptions), null);
            }
            catch (JsonException exception)
            {
                var path = exception.Path;
                var field = path is not null && path.StartsWith("$.", StringComparison.Ordinal)
                    ? path[2..]
                    : null;
                if (field is not null && allowedKeys.Contains(field))
                {
                    return (null, ValidationProblemFor(field, "El tipo de dato no cumple el contrato."));
                }

                return (null, ProblemResult(400, AppointmentErrorCodes.ValidationError));
            }
        }
        catch (JsonException)
        {
            // A syntactically malformed document cannot reliably be attributed
            // to any field, so the optional errors map stays absent.
            return (null, ProblemResult(400, AppointmentErrorCodes.ValidationError));
        }
    }

    private IActionResult ValidationProblemFor(string field, string message) =>
        ProblemResult(
            400,
            AppointmentErrorCodes.ValidationError,
            new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] });

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (AppointmentServiceException exception)
        {
            var status = exception.Code switch
            {
                AppointmentErrorCodes.ValidationError => 400,
                AppointmentErrorCodes.ReferenceNotFound or AppointmentErrorCodes.AppointmentNotFound => 404,
                AppointmentErrorCodes.ActiveAppointmentExists or AppointmentErrorCodes.NoCapacity or
                    AppointmentErrorCodes.InvalidTransition => 409,
                _ => 500
            };

            if (status == 500)
            {
                _logger?.LogError(
                    exception,
                    "Código de error no reconocido del servicio de turnos: {Code}",
                    exception.Code);
                return ProblemResult(500, "INTERNAL_ERROR");
            }

            return ProblemResult(status, exception.Code, exception.Errors);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "Error inesperado en la API de turnos.");
            return ProblemResult(500, "INTERNAL_ERROR");
        }
    }

    private ObjectResult ProblemResult(
        int status,
        string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var title = code switch
        {
            AppointmentErrorCodes.ValidationError => "Solicitud inválida",
            AppointmentErrorCodes.ReferenceNotFound => "Referencia inexistente",
            AppointmentErrorCodes.AppointmentNotFound => "Turno inexistente",
            AppointmentErrorCodes.ActiveAppointmentExists => "El camión ya posee un turno activo",
            AppointmentErrorCodes.NoCapacity => "No hay capacidad disponible",
            AppointmentErrorCodes.InvalidTransition => "Transición inválida",
            _ => "Error interno"
        };

        var result = new ObjectResult(new AppointmentProblemDetails
        {
            Title = title,
            Status = status,
            Code = code,
            TraceId = HttpContext.TraceIdentifier,
            Errors = errors
        })
        {
            StatusCode = status
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
