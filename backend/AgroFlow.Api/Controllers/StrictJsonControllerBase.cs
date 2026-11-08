using System.Text.Json;
using AgroFlow.Api.Contracts.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Frontera HTTP estricta compartida por el canal, las conversaciones y los
/// transportistas: mismo criterio que la API de turnos. Sólo se acepta
/// `application/json` con un objeto sin propiedades desconocidas ni repetidas,
/// y todo error sale como `application/problem+json` con un `code` estable.
/// </summary>
public abstract class StrictJsonControllerBase : ControllerBase
{
    public const string ValidationErrorCode = "VALIDATION_ERROR";
    public const string InternalErrorCode = "INTERNAL_ERROR";

    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false
    };

    /// <summary>Título legible de cada código; los desconocidos se informan como error interno.</summary>
    protected abstract string TitleFor(string code);

    protected async Task<(T? Request, IActionResult? Failure)> ReadRequestBodyAsync<T>(
        IReadOnlySet<string> allowedKeys,
        CancellationToken cancellationToken) where T : class
    {
        var mediaType = Request.ContentType?.Split(';', 2)[0].Trim();
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return (null, ProblemResult(400, ValidationErrorCode));
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, ProblemResult(400, ValidationErrorCode));
            }

            // Un ingenio declarado en el cuerpo es una propiedad desconocida:
            // el ámbito nunca sale de la solicitud.
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
                return (null, ProblemResult(400, ValidationErrorCode, errors));
            }

            try
            {
                var request = document.RootElement.Deserialize<T>(RequestJsonOptions);
                return request is null
                    ? (null, ProblemResult(400, ValidationErrorCode))
                    : (request, null);
            }
            catch (JsonException exception)
            {
                var path = exception.Path;
                var field = path is not null && path.StartsWith("$.", StringComparison.Ordinal)
                    ? path[2..]
                    : null;
                return field is not null && allowedKeys.Contains(field)
                    ? (null, ValidationProblemFor(field, "El tipo de dato no cumple el contrato."))
                    : (null, ProblemResult(400, ValidationErrorCode));
            }
        }
        catch (JsonException)
        {
            return (null, ProblemResult(400, ValidationErrorCode));
        }
    }

    /// <summary>Rechaza parámetros de consulta fuera del contrato.</summary>
    protected IActionResult? RejectUnknownQueryKeys(IReadOnlySet<string> allowedKeys)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var key in Request.Query.Keys)
        {
            if (!allowedKeys.Contains(key))
            {
                errors[key] = ["Parámetro no admitido."];
            }
            else if (Request.Query[key].Count != 1 || string.IsNullOrWhiteSpace(Request.Query[key][0]))
            {
                errors[key] = ["Debe indicarse exactamente un valor no vacío."];
            }
        }

        return errors.Count > 0 ? ProblemResult(400, ValidationErrorCode, errors) : null;
    }

    protected IActionResult ValidationProblemFor(string field, string message) =>
        ProblemResult(
            400,
            ValidationErrorCode,
            new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] });

    protected ObjectResult ProblemResult(
        int status,
        string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var result = new ObjectResult(new AppointmentProblemDetails
        {
            Title = code == ValidationErrorCode ? "Solicitud inválida" : TitleFor(code),
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
