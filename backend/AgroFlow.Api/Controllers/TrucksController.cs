using AgroFlow.Api.Contracts.Trucks;
using AgroFlow.Api.Security;
using AgroFlow.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Listado de camiones del ingenio sobre <see cref="MasterDataService"/>, en
/// sólo lectura y exclusivo de LocalDemo detrás de la clave del dashboard
/// (primera entrega de B15, #19). El alta, la edición, la inhabilitación y las
/// asociaciones siguen sin ruta (campos editables pendientes en OD-013 y
/// gestión de asociaciones en OD-007). La versión MVP con JWT y paginación
/// queda para un cambio posterior.
/// </summary>
[Route("api/v1/trucks")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class TrucksController : StrictJsonControllerBase
{
    private static readonly HashSet<string> ListQueryKeys = new(["active"], StringComparer.Ordinal);

    private readonly MasterDataService _service;
    private readonly ILogger<TrucksController>? _logger;

    public TrucksController(MasterDataService service, ILogger<TrucksController>? logger = null)
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

        try
        {
            var trucks = await _service.Trucks(active, cancellationToken);
            return Ok(trucks
                .Select(truck => new TruckResponse(truck.Id, truck.Name, truck.Code ?? string.Empty, truck.IsActive))
                .ToArray());
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError("Error inesperado en el listado de camiones: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    protected override string TitleFor(string code) => "Error interno";
}
