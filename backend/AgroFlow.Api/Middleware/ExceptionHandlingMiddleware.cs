using AgroFlow.Api.Contract;
using AgroFlow.Api.Contracts.Appointments;
using System.Net;
using System.Text.Json;

namespace AgroFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _useMvpErrors;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, bool useMvpErrors = false)
    {
        _next = next;
        _logger = logger;
        _useMvpErrors = useMvpErrors;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted &&
            !(exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested))
        {
            _logger.LogError(exception, "Se capturó una excepción no controlada: {ExceptionType}", exception.GetType().Name);
            if (_useMvpErrors)
                await AppointmentProblems.WriteAsync(context, 500, "INTERNAL_ERROR");
            else
                await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title, code, detail) = exception switch
        {
            InvalidDataException or ArgumentException or ArgumentNullException =>
                (HttpStatusCode.BadRequest, "Bad Request", ErrorCodes.Validation, exception.Message),
            KeyNotFoundException =>
                (HttpStatusCode.NotFound, "Not Found", ErrorCodes.NotFound, exception.Message),
            UnauthorizedAccessException =>
                (HttpStatusCode.Forbidden, "Forbidden", ErrorCodes.Forbidden,
                    "No tienes permiso para acceder a este recurso."),
            _ =>
                (HttpStatusCode.InternalServerError, "Internal Server Error", ErrorCodes.Unexpected,
                    "Ocurrió un error inesperado en el servidor.")
        };

        var error = new ApiError(code, detail)
        {
            Status = (int)status,
            Title = title,
            Instance = context.Request.Path.Value ?? "/"
        };
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(JsonSerializer.Serialize(error, SerializerOptions));
    }
}
