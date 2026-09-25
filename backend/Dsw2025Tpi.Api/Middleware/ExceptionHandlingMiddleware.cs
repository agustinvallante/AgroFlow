using Dsw2025Tpi.Api.Contract;
using Dsw2025Tpi.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace Dsw2025Tpi.Api.NewFolder;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Se capturó una excepción no controlada: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title, code, detail) = exception switch
        {
            InvalidOrderDataException or
            InvalidDataException or
            InvalidOrderStatusException or
            DuplicatedEntityException or
            ArgumentException or
            InsufficientStockException or
            ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Bad Request",
                ErrorCodes.Validation,
                exception.Message),

            CustomerNotFoundException or
            OrderNotFoundException or
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Not Found",
                ErrorCodes.NotFound,
                exception.Message),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "Forbidden",
                ErrorCodes.Forbidden,
                "No tienes permiso para acceder a este recurso."),

            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                ErrorCodes.Unexpected,
                "Ocurrió un error inesperado en el servidor.")
        };

        var error = new ApiError(code, detail)
        {
            Status = (int)status,
            Title = title,
            Instance = context.Request.Path.Value ?? "/"
        };

        var result = JsonSerializer.Serialize(error, SerializerOptions);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;

        return context.Response.WriteAsync(result);
    }
}
