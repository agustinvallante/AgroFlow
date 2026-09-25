using Dsw2025Tpi.Api.Authorization;
using Dsw2025Tpi.Api.Contract;
using Dsw2025Tpi.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dsw2025Tpi.Api.Controllers;

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
    [HttpPost("drivers"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateDriver([FromBody] DriverRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateDriver(new(request.Name, request.Dni, request.WhatsApp), Key(), ct));
        return CreatedAtAction(nameof(Driver), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("drivers/{id:guid}"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateDriver(Guid id, DriverRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateDriver(id, new(request.Name, request.Dni, request.WhatsApp), Key(), ct)));
    [HttpPost("drivers/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateDriver(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetDriverActive(id, false, Key(), ct)));
    [HttpPost("drivers/{id:guid}/reactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> ReactivateDriver(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetDriverActive(id, true, Key(), ct)));

    [HttpGet("trucks"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Trucks([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Trucks(active, ct)).Select(MasterDataResponse.From));
    [HttpGet("trucks/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Truck(Guid id, CancellationToken ct) => Ok(MasterDataResponse.From(await service.GetTruck(id, ct)));
    [HttpPost("trucks"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateTruck(TruckRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateTruck(new(request.Plate, request.FleetType), Key(), ct));
        return CreatedAtAction(nameof(Truck), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("trucks/{id:guid}"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateTruck(Guid id, TruckRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateTruck(id, new(request.Plate, request.FleetType), Key(), ct)));
    [HttpPost("trucks/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateTruck(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetTruckActive(id, false, Key(), ct)));
    [HttpPost("trucks/{id:guid}/reactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> ReactivateTruck(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetTruckActive(id, true, Key(), ct)));

    [HttpGet("farms"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Farms([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Farms(active, ct)).Select(MasterDataResponse.From));
    [HttpGet("farms/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Farm(Guid id, CancellationToken ct) => Ok(MasterDataResponse.From(await service.GetFarm(id, ct)));
    [HttpPost("farms"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateFarm(FarmRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateFarm(new(request.Code, request.Name, request.LocationReference), Key(), ct));
        return CreatedAtAction(nameof(Farm), new { id = receipt.ResourceId }, receipt);
    }
    [HttpPut("farms/{id:guid}"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> UpdateFarm(Guid id, FarmRequest request, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.UpdateFarm(id, new(request.Code, request.Name, request.LocationReference), Key(), ct)));
    [HttpPost("farms/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateFarm(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetFarmActive(id, false, Key(), ct)));
    [HttpPost("farms/{id:guid}/reactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> ReactivateFarm(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetFarmActive(id, true, Key(), ct)));

    [HttpGet("driver-truck-associations"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Associations([FromQuery] bool? active, CancellationToken ct) => Ok((await service.Associations(active, ct)).Select(AssociationResponse.From));
    [HttpGet("driver-truck-associations/{id:guid}"), Authorize(Policy = AgroFlowPolicies.Read)]
    public async Task<IActionResult> Association(Guid id, CancellationToken ct) => Ok(AssociationResponse.From(await service.GetAssociation(id, ct)));
    [HttpPost("driver-truck-associations"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> CreateAssociation(DriverTruckAssociationRequest request, CancellationToken ct)
    {
        var receipt = MutationReceiptResponse.From(await service.CreateAssociation(new(request.DriverId, request.TruckId), Key(), ct));
        return Created($"/api/v1/driver-truck-associations/{receipt.ResourceId}", receipt);
    }
    [HttpPost("driver-truck-associations/{id:guid}/inactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> InactivateAssociation(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetAssociationActive(id, false, Key(), ct)));
    [HttpPost("driver-truck-associations/{id:guid}/reactivation"), Authorize(Policy = AgroFlowPolicies.MasterDataAndCapacityManagement)]
    public async Task<ActionResult<MutationReceiptResponse>> ReactivateAssociation(Guid id, CancellationToken ct) =>
        Ok(MutationReceiptResponse.From(await service.SetAssociationActive(id, true, Key(), ct)));

    private string Key() => Request.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : string.Empty;
}

public sealed class MasterDataExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (status, title, detail, code) = context.Exception switch
        {
            MasterDataConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", conflict.Message, conflict.Code),
            Dsw2025Tpi.Domain.Interfaces.MasterDataPersistenceConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", "A master-data uniqueness conflict occurred.", conflict.Code),
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
