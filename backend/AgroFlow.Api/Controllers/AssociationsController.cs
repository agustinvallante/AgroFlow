using AgroFlow.Api.Contracts.Associations;
using AgroFlow.Api.Security;
using AgroFlow.Application.MasterData;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Driver-truck association management for LocalDemo over
/// <see cref="MasterDataService"/> (OD-002/OD-013). A truck accepts a single
/// active driver: a second association is rejected with 409 and the operator
/// must inactivate the current one first. Inactivation keeps the row as
/// history and never rewrites existing appointments.
/// </summary>
[Route("api/v1/associations")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class AssociationsController : StrictJsonControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly HashSet<string> BodyKeys = new(["driverId", "truckId"], StringComparer.Ordinal);
    private static readonly HashSet<string> ListQueryKeys = new(["active"], StringComparer.Ordinal);

    private readonly MasterDataService _service;
    private readonly ILogger<AssociationsController>? _logger;

    public AssociationsController(MasterDataService service, ILogger<AssociationsController>? logger = null)
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
            Ok((await _service.Associations(active, cancellationToken)).Select(ToResponse).ToArray()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (IdempotencyKey() is null)
        {
            return MissingIdempotencyKey();
        }

        var (request, failure) = await ReadRequestBodyAsync<AssociationInputRequest>(BodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request!.DriverId is null || request.DriverId == Guid.Empty)
        {
            errors["driverId"] = ["Debe ser un UUID válido."];
        }

        if (request.TruckId is null || request.TruckId == Guid.Empty)
        {
            errors["truckId"] = ["Debe ser un UUID válido."];
        }

        if (errors.Count > 0)
        {
            return ProblemResult(400, ValidationErrorCode, errors);
        }

        return await ExecuteAsync(async () =>
        {
            var receipt = await _service.CreateAssociation(
                new AssociationData(request.DriverId!.Value, request.TruckId!.Value), IdempotencyKey()!, cancellationToken);
            return Created($"/api/v1/associations/{receipt.ResourceId:D}", ToResponse(receipt));
        });
    }

    [HttpPost("{id}/inactivation")]
    public async Task<IActionResult> Inactivate(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var associationId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        if (IdempotencyKey() is null)
        {
            return MissingIdempotencyKey();
        }

        return await ExecuteAsync(async () =>
            Ok(ToResponse(await _service.SetAssociationActive(associationId, false, IdempotencyKey()!, cancellationToken))));
    }

    protected override string TitleFor(string code) => code switch
    {
        "ASSOCIATION_REFERENCE_NOT_FOUND" => "Transportista, camión o asociación inexistente o inhabilitado",
        "DUPLICATE_ASSOCIATION" => "El camión ya tiene un transportista activo",
        "DUPLICATE_IDENTIFIER" => "La asociación ya existe",
        "IDEMPOTENCY_KEY_REUSED" => "La clave de idempotencia ya se usó con otros datos",
        _ => "Error interno"
    };

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
            // A concurrent request activated another association for the same truck.
            return ProblemResult(409, "DUPLICATE_ASSOCIATION");
        }
        catch (KeyNotFoundException)
        {
            return ProblemResult(404, "ASSOCIATION_REFERENCE_NOT_FOUND");
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
            _logger?.LogError("Error inesperado en la gestión de asociaciones: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    private static AssociationResponse ToResponse(AssociationItem item) =>
        new(item.Id, item.DriverId, item.TruckId, item.IsActive);

    private static AssociationMutationResponse ToResponse(MutationReceipt receipt) =>
        new(receipt.ResourceId, receipt.ResourceType, receipt.Outcome);
}
