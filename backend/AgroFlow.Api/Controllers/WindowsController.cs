using System.Globalization;
using AgroFlow.Api.Contracts.Windows;
using AgroFlow.Api.Security;
using AgroFlow.Application.Appointments;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Ventanas y cupos del ingenio para una fecha, en sólo lectura y exclusivo de
/// LocalDemo detrás de la clave del dashboard (primera entrega de B39, #44).
/// Informa lo persistido, incluidas las franjas vacías. Sin alta, edición ni
/// baja de ventanas, y sin indisponibilidad por interrupciones (B33, #37). La
/// versión MVP con JWT, roles y calendario configurable queda para un cambio
/// posterior.
/// </summary>
[Route("api/v1/windows")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class WindowsController : StrictJsonControllerBase
{
    private static readonly HashSet<string> ListQueryKeys = new(["date"], StringComparer.Ordinal);

    private readonly IWindowReader _windows;
    private readonly ITenantContext _tenant;
    private readonly ILogger<WindowsController>? _logger;

    public WindowsController(IWindowReader windows, ITenantContext tenant, ILogger<WindowsController>? logger = null)
    {
        _windows = windows;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (RejectUnknownQueryKeys(ListQueryKeys) is { } failure)
        {
            return failure;
        }

        DateOnly? date = null;
        if (Request.Query.TryGetValue("date", out var values))
        {
            if (!DateOnly.TryParseExact(values[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return ValidationProblemFor("date", "Debe tener el formato yyyy-MM-dd.");
            }

            date = parsed;
        }

        try
        {
            var windows = await _windows.ListWindowsAsync(_tenant.IngenioId, date, cancellationToken);
            return Ok(windows
                .Select(window => new WindowResponse(
                    window.Id, window.StartAt, window.EndAt, window.Capacity, window.Occupied,
                    window.Capacity - window.Occupied))
                .ToArray());
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError("Error inesperado en el listado de ventanas: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    protected override string TitleFor(string code) => "Error interno";
}
