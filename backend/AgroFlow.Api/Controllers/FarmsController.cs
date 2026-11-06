using AgroFlow.Api.Contracts.Farms;
using AgroFlow.Api.Security;
using AgroFlow.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Consulta de fincas del ingenio sobre <see cref="MasterDataService"/>, sólo
/// lectura. Permite que el agente de n8n traduzca el nombre que usa el
/// transportista al código que exige el alta de turnos, con el backend como
/// fuente de verdad. El alta, la edición y la inhabilitación de fincas siguen
/// sin ruta (campos editables pendientes en OD-013).
/// </summary>
[Route("api/v1/farms")]
[Authorize(Policy = ApiKeyPolicies.FarmCatalogRead)]
public sealed class FarmsController : StrictJsonControllerBase
{
    private static readonly HashSet<string> ListQueryKeys = new(["active", "search"], StringComparer.Ordinal);

    private readonly MasterDataService _service;
    private readonly ILogger<FarmsController>? _logger;

    public FarmsController(MasterDataService service, ILogger<FarmsController>? logger = null)
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
        if (Request.Query.TryGetValue("active", out var activeValues))
        {
            if (activeValues[0] is not ("true" or "false"))
            {
                return ValidationProblemFor("active", "Debe ser true o false.");
            }

            active = activeValues[0] == "true";
        }

        string? search = null;
        if (Request.Query.TryGetValue("search", out var searchValues))
        {
            search = searchValues[0];
            if (search!.Length > FarmSearch.MaxLength)
            {
                return ValidationProblemFor("search", $"Debe contener hasta {FarmSearch.MaxLength} caracteres.");
            }
        }

        try
        {
            var farms = await _service.SearchFarms(search, active, cancellationToken);
            return Ok(farms.Select(farm => new FarmResponse(farm.Id, farm.Code ?? string.Empty, farm.Name, farm.IsActive)).ToArray());
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
            _logger?.LogError("Error inesperado en la consulta de fincas: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    protected override string TitleFor(string code) => "Error interno";
}
