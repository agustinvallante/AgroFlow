using System.Net;
using System.Text.Json;
using Dsw2025Tpi.Api.Contract;
using Dsw2025Tpi.Api.NewFolder;
using Dsw2025Tpi.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class ApiErrorAndExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Validation_exception_returns_problem_details_error_contract()
    {
        await AssertProblemResponse(
            new InvalidOrderDataException("The order data is invalid."),
            (int)HttpStatusCode.BadRequest,
            "Bad Request",
            "The order data is invalid.",
            ErrorCodes.Validation);
    }

    [Fact]
    public async Task Not_found_exception_returns_problem_details_error_contract()
    {
        await AssertProblemResponse(
            new CustomerNotFoundException(Guid.Parse("ba25c7ed-ae6c-4c55-8e58-04bc6516411c")),
            (int)HttpStatusCode.NotFound,
            "Not Found",
            "No se encontró el cliente con ID ba25c7ed-ae6c-4c55-8e58-04bc6516411c.",
            ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Forbidden_exception_returns_problem_details_error_contract()
    {
        await AssertProblemResponse(
            new UnauthorizedAccessException("This private exception text must not be exposed."),
            (int)HttpStatusCode.Forbidden,
            "Forbidden",
            "No tienes permiso para acceder a este recurso.",
            ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Unexpected_exception_returns_generic_detail_without_leaking_exception_text()
    {
        const string privateExceptionText = "database password or internal implementation detail";

        var response = await InvokeMiddleware(new InvalidOperationException(privateExceptionText));

        Assert.Equal((int)HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentType);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal("Ocurrió un error inesperado en el servidor.", root.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.Unexpected, root.GetProperty("code").GetString());
        Assert.Equal(root.GetProperty("detail").GetString(), root.GetProperty("message").GetString());
        Assert.DoesNotContain(privateExceptionText, response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_error_serializes_compatible_detail_and_message_with_code()
    {
        var error = new ApiError(ErrorCodes.Validation, "The request is invalid.");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;

        Assert.Equal("The request is invalid.", root.GetProperty("detail").GetString());
        Assert.Equal("The request is invalid.", root.GetProperty("message").GetString());
        Assert.Equal(ErrorCodes.Validation, root.GetProperty("code").GetString());
    }

    private static async Task AssertProblemResponse(
        Exception exception,
        int status,
        string title,
        string detail,
        string code)
    {
        var response = await InvokeMiddleware(exception);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentType);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.Equal(detail, root.GetProperty("detail").GetString());
        Assert.Equal(detail, root.GetProperty("message").GetString());
        Assert.Equal(code, root.GetProperty("code").GetString());
    }

    private static async Task<(int StatusCode, string? ContentType, string Body)> InvokeMiddleware(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/runtime-test";
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        return (context.Response.StatusCode, context.Response.ContentType, await reader.ReadToEndAsync());
    }
}
