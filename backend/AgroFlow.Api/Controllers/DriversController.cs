using System.Text.RegularExpressions;
using AgroFlow.Api.Contracts.Drivers;
using AgroFlow.Api.Security;
using AgroFlow.Application.MasterData;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Gestión de transportistas del ingenio sobre <see cref="MasterDataService"/>
/// (PR #47), sin un CRUD paralelo. Sólo se publican las operaciones de
/// transportistas; camiones, fincas y asociaciones siguen sin ruta. Las
/// mutaciones exigen `Idempotency-Key` y el WhatsApp en formato E.164, que es
/// el valor que reconoce al remitente del canal.
/// </summary>
[Route("api/v1/drivers")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class DriversController : StrictJsonControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly Regex PhonePattern = new(
        @"^\+[1-9][0-9]{7,14}\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> DriverBodyKeys = new(["name", "dni", "whatsApp"], StringComparer.Ordinal);
    private static readonly HashSet<string> ListQueryKeys = new(["active"], StringComparer.Ordinal);

    private readonly MasterDataService _service;
    private readonly ILogger<DriversController>? _logger;

    public DriversController(MasterDataService service, ILogger<DriversController>? logger = null)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (RejectUnknownQueryKeys(ListQueryKeys) is { } failure)
        {
            return failure;
        }

        bool? active = null;
        if (Request.Query.TryGetValue("active", out var values))
        {
            if (values[0] is not ("true" or "false"))
            {
                return ValidationProblemFor("active", "Debe ser true o false.");
            }

            active = values[0] == "true";
        }

        return await ExecuteAsync(async () =>
        {
            var drivers = await _service.Drivers(active, cancellationToken);
            var responses = new List<DriverResponse>(drivers.Count);
            foreach (var driver in drivers)
                responses.Add(await ToResponseAsync(driver, cancellationToken));
            return Ok(responses);
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var driverId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        return await ExecuteAsync(async () =>
            Ok(await ToResponseAsync(await _service.GetDriver(driverId, cancellationToken), cancellationToken)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var (data, failure) = await ReadDriverAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return await ExecuteAsync(async () =>
        {
            var receipt = await _service.CreateDriver(data!, IdempotencyKey()!, cancellationToken);
            return Created($"/api/v1/drivers/{receipt.ResourceId:D}", ToResponse(receipt));
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var driverId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        var (data, failure) = await ReadDriverAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return await ExecuteAsync(async () =>
            Ok(ToResponse(await _service.UpdateDriver(driverId, data!, IdempotencyKey()!, cancellationToken))));
    }

    [HttpPost("{id}/inactivation")]
    public async Task<IActionResult> Inactivate(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var driverId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        if (IdempotencyKey() is null)
        {
            return MissingIdempotencyKey();
        }

        return await ExecuteAsync(async () =>
            Ok(ToResponse(await _service.SetDriverActive(driverId, false, IdempotencyKey()!, cancellationToken))));
    }

    protected override string TitleFor(string code) => code switch
    {
        "DRIVER_NOT_FOUND" => "Transportista inexistente",
        "DUPLICATE_DNI" => "Ya existe un transportista con ese DNI",
        "DUPLICATE_WHATSAPP" => "Ya existe un transportista con ese WhatsApp",
        "DUPLICATE_IDENTIFIER" => "El identificador ya está en uso",
        "IDEMPOTENCY_KEY_REUSED" => "La clave de idempotencia ya se usó con otros datos",
        "RESOURCE_INACTIVE" => "El transportista está inhabilitado",
        _ => "Error interno"
    };

    private async Task<(DriverData? Data, IActionResult? Failure)> ReadDriverAsync(CancellationToken cancellationToken)
    {
        if (IdempotencyKey() is null)
        {
            return (null, MissingIdempotencyKey());
        }

        var (request, failure) = await ReadRequestBodyAsync<DriverInputRequest>(DriverBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request!.Name) || request.Name.Trim().Length > 150)
        {
            errors["name"] = ["Debe contener entre 1 y 150 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(request.Dni) || request.Dni.Trim().Length > 20 || !request.Dni.Any(char.IsDigit))
        {
            errors["dni"] = ["Debe contener dígitos y hasta 20 caracteres."];
        }

        if (request.WhatsApp is null || !PhonePattern.IsMatch(request.WhatsApp))
        {
            errors["whatsApp"] = ["Debe ser un teléfono completo en formato E.164."];
        }

        return errors.Count > 0
            ? (null, ProblemResult(400, ValidationErrorCode, errors))
            : (new DriverData(request.Name!, request.Dni!, request.WhatsApp!), null);
    }

    private string? IdempotencyKey() =>
        Request.Headers.TryGetValue(IdempotencyKeyHeader, out var values)
            && values.Count == 1
            && !string.IsNullOrWhiteSpace(values[0])
            && values[0]!.Trim().Length <= 200
            ? values[0]
            : null;

    private IActionResult MissingIdempotencyKey() =>
        ValidationProblemFor(IdempotencyKeyHeader, "Debe indicar una clave de hasta 200 caracteres.");

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (MasterDataConflictException conflict)
        {
            return ProblemResult(409, conflict.Code.ToUpperInvariant());
        }
        catch (MasterDataPersistenceConflictException)
        {
            // Otra solicitud ganó la carrera por el mismo DNI o WhatsApp.
            return ProblemResult(409, "DUPLICATE_IDENTIFIER");
        }
        catch (KeyNotFoundException)
        {
            return ProblemResult(404, "DRIVER_NOT_FOUND");
        }
        catch (ArgumentException)
        {
            return ProblemResult(400, ValidationErrorCode);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Sólo el tipo: la excepción podría citar DNI o teléfono.
            _logger?.LogError("Error inesperado en la gestión de transportistas: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    private async Task<DriverResponse> ToResponseAsync(MasterDataItem item, CancellationToken cancellationToken)
    {
        var plates = (await _service.ActiveTrucksForTransportista(item.Id, cancellationToken))
            .Where(truck => truck.AssociationIsActive && truck.TruckIsActive)
            .Select(truck => truck.Plate)
            .ToArray();
        return new DriverResponse(item.Id, item.Name, item.Identifier ?? string.Empty,
            item.SecondaryIdentifier ?? string.Empty, item.IsActive, plates, item.IsActive && plates.Length > 0);
    }

    private static DriverMutationResponse ToResponse(MutationReceipt receipt) =>
        new(receipt.ResourceId, receipt.ResourceType, receipt.Outcome);
}
