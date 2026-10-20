using AgroFlow.Api.Authorization;
using AgroFlow.Api.Contract;
using AgroFlow.Application.MasterData;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Preparatory catalog operations retained from PR 47. NonController is
/// intentional: public route/schema, Identity/B02 dependencies, pagination,
/// editable fields, and association permissions are unresolved.
/// </summary>
[NonController]
[ApiController]
[Route("api/v1")]
[Authorize]
[MasterDataExceptionFilter]
public sealed class MasterDataController(MasterDataService service) : ControllerBase
{
    [HttpGet("drivers"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Drivers([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Drivers(active, ct)).Select(MasterDataResponse.From));
    [HttpGet("drivers/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Driver(Guid id, CancellationToken ct) => Ok(MasterDataResponse.From(await service.GetDriver(id, ct)));
    [HttpPost("drivers"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateDriver([FromBody] DriverRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateDriver(new(request.Name, request.Dni, request.WhatsApp), Key(), ct));
        return CreatedAtAction(nameof(Driver), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("drivers/{id:guid}"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateDriver(Guid id, DriverRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateDriver(id, new(request.Name, request.Dni, request.WhatsApp), Key(), ct)));
    [HttpPost("drivers/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateDriver(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetDriverActive(id, false, Key(), ct)));
    [HttpGet("trucks"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Trucks([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Trucks(active, ct)).Select(MasterDataResponse.From));
    [HttpGet("trucks/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Truck(Guid id, CancellationToken ct) => Ok(MasterDataResponse.From(await service.GetTruck(id, ct)));
    [HttpPost("trucks"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateTruck(TruckRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateTruck(new(request.Plate, request.FleetType), Key(), ct));
        return CreatedAtAction(nameof(Truck), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("trucks/{id:guid}"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateTruck(Guid id, TruckRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateTruck(id, new(request.Plate, request.FleetType), Key(), ct)));
    [HttpPost("trucks/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateTruck(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetTruckActive(id, false, Key(), ct)));
    [HttpGet("farms"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Farms([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Farms(active, ct)).Select(MasterDataResponse.From));
    [HttpGet("farms/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Farm(Guid id, CancellationToken ct) => Ok(MasterDataResponse.From(await service.GetFarm(id, ct)));
    [HttpPost("farms"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateFarm(FarmRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateFarm(new(request.Code, request.Name, request.LocationReference), Key(), ct));
        return CreatedAtAction(nameof(Farm), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("farms/{id:guid}"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateFarm(Guid id, FarmRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateFarm(id, new(request.Code, request.Name, request.LocationReference), Key(), ct)));
    [HttpPost("farms/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.CatalogManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateFarm(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetFarmActive(id, false, Key(), ct)));

    private string Key() => Request.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : string.Empty;
}

public sealed class MasterDataExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (status, title, detail, code) = context.Exception switch
        {
            MasterDataConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", conflict.Message, conflict.Code),
            MasterDataPersistenceConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", "A master-data uniqueness conflict occurred.", conflict.Code),
            ArgumentException exception => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message, "validation_error"),
            KeyNotFoundException exception => (StatusCodes.Status404NotFound, "Not Found", exception.Message, "not_found"),
            _ => (0, string.Empty, string.Empty, string.Empty)
        };
        if (status == 0) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code }
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
