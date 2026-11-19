using AgroFlow.Api.Security;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Api.Controllers;

[Route("api/v1/mill")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class MillController(
    AgroFlowDbContext db,
    ITenantContext tenant,
    TimeProvider clock,
    ILogger<MillController> logger) : StrictJsonControllerBase
{
    private static readonly HashSet<string> NoQueryKeys = [];

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (RejectUnknownQueryKeys(NoQueryKeys) is { } failure)
        {
            return failure;
        }

        try
        {
            var ingenioId = tenant.IngenioId;
            var mill = await db.Ingenios.AsNoTracking()
                .Where(ingenio => ingenio.Id == ingenioId)
                .Select(ingenio => new { ingenio.Name, ingenio.TimeZoneId })
                .SingleAsync(cancellationToken);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(mill.TimeZoneId);
            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).Date);
            return Ok(new { mill.Name, mill.TimeZoneId, LocalDate = localDate });
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Error inesperado al consultar el ingenio: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }

    protected override string TitleFor(string code) => "Error interno";
}
