using System.Globalization;
using AgroFlow.Api.Contracts.Appointments;
using AgroFlow.Api.Security;
using AgroFlow.Application.Appointments;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Authorize(Policy = AppointmentsAuthorizationPolicy.MvpRead)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MvpAppointmentReadsController(IAppointmentReader reader, ITenantContext tenant) : ControllerBase
{
    private static readonly HashSet<string> QueryKeys = new(
        ["date", "status", "truckPlate", "page", "pageSize"], StringComparer.Ordinal);

    [HttpGet]
    public async Task<ActionResult<AppointmentPageResponse>> List(CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var key in Request.Query.Keys.Where(key => !QueryKeys.Contains(key)))
            errors[key] = ["Filtro no admitido."];

        var dateText = AppointmentQueryParameters.ReadSingle(Request.Query, "date", errors);
        DateOnly? date = null;
        if (dateText is not null)
        {
            if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                date = parsed;
            else
                errors["date"] = ["Debe ser una fecha válida en formato YYYY-MM-DD."];
        }

        var statusText = AppointmentQueryParameters.ReadSingle(Request.Query, "status", errors);
        AppointmentStatus? status = null;
        if (statusText is not null)
        {
            if (AppointmentHttpMapper.TryParseStatus(statusText, out var parsed))
                status = parsed;
            else
                errors["status"] = ["Estado no reconocido."];
        }

        var plate = AppointmentQueryParameters.ReadSingle(Request.Query, "truckPlate", errors);
        if (plate is not null && (plate.Length is < 6 or > 10 || !plate.Any(char.IsLetterOrDigit)))
            errors["truckPlate"] = ["Debe contener entre 6 y 10 caracteres y una letra o dígito."];

        var page = ReadPageParameter("page", 1, int.MaxValue, errors);
        var pageSize = ReadPageParameter("pageSize", 20, 50, errors);
        if (errors.Count != 0)
            return AppointmentProblems.Result(HttpContext, 400, AppointmentErrorCodes.ValidationError, errors);

        var result = await reader.ListAsync(tenant.IngenioId,
            new AppointmentReadQuery(date, status, plate, page, pageSize), cancellationToken);
        return new AppointmentPageResponse(result.Items.Select(AppointmentHttpMapper.ToResponse).ToArray(),
            page, pageSize, result.TotalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponse>> Get(string id, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var key in Request.Query.Keys)
            errors[key] = ["Parámetro no admitido."];
        if (!Guid.TryParseExact(id, "D", out var appointmentId))
            errors["id"] = ["Debe ser un UUID válido."];

        if (errors.Count != 0)
            return AppointmentProblems.Result(HttpContext, 400, AppointmentErrorCodes.ValidationError, errors);

        var result = await reader.GetAsync(tenant.IngenioId, appointmentId, cancellationToken);
        if (result is null)
            return AppointmentProblems.Result(HttpContext, 404, AppointmentErrorCodes.AppointmentNotFound);
        return AppointmentHttpMapper.ToResponse(result);
    }

    private int ReadPageParameter(string name, int defaultValue, int maximum, Dictionary<string, string[]> errors)
    {
        var value = AppointmentQueryParameters.ReadSingle(Request.Query, name, errors);
        if (value is null)
            return defaultValue;

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
            number >= 1 && number <= maximum)
            return number;

        errors[name] = [$"Debe ser un entero entre 1 y {maximum}."];
        return defaultValue;
    }
}
