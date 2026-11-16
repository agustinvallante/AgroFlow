using AgroFlow.Application.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Contracts.Appointments;

internal static class AppointmentProblems
{
    public static ObjectResult Result(HttpContext context, int status, string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var result = new ObjectResult(Create(context, status, code, errors)) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    public static Task WriteAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(Create(context, status, code),
            options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }

    private static AppointmentProblemDetails Create(HttpContext context, int status, string code,
        IReadOnlyDictionary<string, string[]>? errors = null) => new()
    {
        Title = code switch
        {
            AppointmentErrorCodes.ValidationError => "Solicitud inválida",
            AppointmentErrorCodes.ReferenceNotFound => "Referencia inexistente",
            AppointmentErrorCodes.AppointmentNotFound => "Turno inexistente",
            AppointmentErrorCodes.ActiveAppointmentExists => "El camión ya posee un turno activo",
            AppointmentErrorCodes.NoCapacity => "No hay capacidad disponible",
            AppointmentErrorCodes.InvalidTransition => "Transición inválida",
            "UNAUTHENTICATED" => "No autenticado",
            "FORBIDDEN" => "Acceso denegado",
            _ => "Error interno"
        },
        Status = status,
        Code = code,
        TraceId = context.TraceIdentifier,
        Errors = errors
    };
}
