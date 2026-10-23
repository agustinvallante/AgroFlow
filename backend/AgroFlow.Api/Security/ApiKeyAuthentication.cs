using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AgroFlow.Api.Contracts.Appointments;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AgroFlow.Api.Security;

/// <summary>
/// Claves estáticas del perfil LocalDemo: una identifica a n8n como servicio
/// del canal y otra al dashboard hasta que B03 emita identidades por ingenio.
/// Ninguna ruta de conversaciones ni de transportistas es anónima. La clave
/// nunca se registra en logs ni viaja en el cuerpo.
/// </summary>
public static class ApiKeySchemes
{
    public const string Channel = "ChannelServiceKey";
    public const string Dashboard = "DashboardKey";

    public const string ChannelHeader = "X-AgroFlow-Service-Key";
    public const string DashboardHeader = "X-AgroFlow-Dashboard-Key";

    public const string ChannelKeySetting = "ChannelIntegration:ServiceKey";
    public const string DashboardKeySetting = "Dashboard:ApiKey";

    public const string ChannelRole = "CHANNEL_SERVICE";
    public const string DashboardRole = "DASHBOARD";

    public const int MinimumKeyBytes = 32;
}

public static class ApiKeyPolicies
{
    public const string ChannelIngest = "ChannelIngest";
    public const string DashboardAccess = "DashboardAccess";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ChannelIngest, policy => policy
            .AddAuthenticationSchemes(ApiKeySchemes.Channel)
            .RequireRole(ApiKeySchemes.ChannelRole));
        options.AddPolicy(DashboardAccess, policy => policy
            .AddAuthenticationSchemes(ApiKeySchemes.Dashboard)
            .RequireRole(ApiKeySchemes.DashboardRole));
    }
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = string.Empty;

    /// <summary>Sin clave configurada el esquema rechaza toda solicitud.</summary>
    public string? Key { get; set; }

    public string Role { get; set; } = string.Empty;
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private static readonly JsonSerializerOptions ProblemJsonOptions = new(JsonSerializerDefaults.Web);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.Key))
            return Task.FromResult(AuthenticateResult.Fail("API key not configured."));
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var values) || values.Count != 1)
            return Task.FromResult(AuthenticateResult.NoResult());

        // Comparación en tiempo constante sobre los hashes: no revela ni el
        // largo ni el prefijo de la clave configurada.
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(Options.Key));
        var received = SHA256.HashData(Encoding.UTF8.GetBytes(values[0] ?? string.Empty));
        if (!CryptographicOperations.FixedTimeEquals(expected, received))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Scheme.Name), new Claim(ClaimTypes.Role, Options.Role)],
            Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status401Unauthorized, "No autenticado", "UNAUTHENTICATED");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status403Forbidden, "Acceso denegado", "FORBIDDEN");

    private Task WriteProblemAsync(int status, string title, string code)
    {
        Response.StatusCode = status;
        Response.ContentType = "application/problem+json";
        if (status == StatusCodes.Status401Unauthorized)
            Response.Headers.WWWAuthenticate = $"ApiKey header=\"{Options.HeaderName}\"";
        var problem = new AppointmentProblemDetails
        {
            Title = title,
            Status = status,
            Code = code,
            TraceId = Context.TraceIdentifier
        };
        return Response.WriteAsync(JsonSerializer.Serialize(problem, ProblemJsonOptions));
    }
}

public static class ApiKeyAuthenticationSetup
{
    /// <summary>
    /// Registra ambos esquemas. Una clave ausente deja cerradas sus rutas (401)
    /// para que un clon limpio arranque la demo de turnos; una clave presente
    /// pero débil o repetida aborta el arranque.
    /// </summary>
    public static AuthenticationBuilder AddAgroFlowApiKeys(
        this AuthenticationBuilder builder, IConfiguration configuration)
    {
        var channelKey = ReadKey(configuration, ApiKeySchemes.ChannelKeySetting);
        var dashboardKey = ReadKey(configuration, ApiKeySchemes.DashboardKeySetting);
        if (channelKey is not null && string.Equals(channelKey, dashboardKey, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"{ApiKeySchemes.ChannelKeySetting} y {ApiKeySchemes.DashboardKeySetting} deben ser claves distintas.");

        builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeySchemes.Channel, options =>
        {
            options.HeaderName = ApiKeySchemes.ChannelHeader;
            options.Key = channelKey;
            options.Role = ApiKeySchemes.ChannelRole;
        });
        builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeySchemes.Dashboard, options =>
        {
            options.HeaderName = ApiKeySchemes.DashboardHeader;
            options.Key = dashboardKey;
            options.Role = ApiKeySchemes.DashboardRole;
        });
        return builder;
    }

    private static string? ReadKey(IConfiguration configuration, string setting)
    {
        var value = configuration[setting];
        if (string.IsNullOrEmpty(value))
            return null;
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < ApiKeySchemes.MinimumKeyBytes)
            throw new InvalidOperationException(
                $"Configure {setting} con al menos {ApiKeySchemes.MinimumKeyBytes} bytes mediante secretos o variables de entorno.");
        return value;
    }
}
