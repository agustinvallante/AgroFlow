using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Dsw2025Tpi.Api.Contract;

public class ApiError : ProblemDetails
{
    public ApiError(string? code, string? message)
    {
        Code = code;
        Message = message;
    }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message
    {
        get => Detail;
        set => Detail = value;
    }
}
