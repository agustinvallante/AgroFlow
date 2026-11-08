using System.Security.Cryptography;
using System.Text;
using AgroFlow.Domain.Entities;

namespace AgroFlow.Application.Conversations;

public sealed class ConversationOptions
{
    public const string SectionName = "ChannelIntegration";

    /// <summary>Vigencia informativa de la autorización de procesamiento (DD-4).</summary>
    public TimeSpan ClaimTtl { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Casos de uso del canal de WhatsApp y de su consulta. Valida el contrato de
/// aplicación y delega la atomicidad al almacén; nunca registra en logs texto,
/// direcciones ni teléfonos.
/// </summary>
public sealed class ConversationService
{
    public const string ProviderEvolution = "evolution";
    public const int MaxPageSize = 50;
    public const int MaxClaimTokenLength = 128;

    // WhatsApp no existe antes de 2009; un día de tolerancia cubre relojes desfasados.
    private static readonly DateTime MinProviderTimestampUtc = new(2009, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxProviderClockSkew = TimeSpan.FromDays(1);

    private readonly IConversationStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _claimTtl;

    public ConversationService(IConversationStore store, TimeProvider? timeProvider = null, ConversationOptions? options = null)
    {
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _claimTtl = (options ?? new ConversationOptions()).ClaimTtl;
        if (_claimTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "Claim TTL must be positive.");
    }

    public async Task<InboundRegistration> IngestAsync(
        RegisterInboundCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!string.Equals(command.Provider?.Trim(), ProviderEvolution, StringComparison.OrdinalIgnoreCase))
            errors["provider"] = ["Proveedor no admitido."];
        var instance = Normalize(() => ChannelInstance.NormalizeInstanceName(command.Instance),
            "instance", "Debe indicar una instancia de hasta 100 caracteres.", errors);
        var providerMessageId = Normalize(() => ConversationMessage.NormalizeProviderMessageId(command.ProviderMessageId),
            "providerMessageId", "Debe indicar un identificador de hasta 128 caracteres.", errors);
        var text = Normalize(() => ConversationMessage.NormalizeText(command.Text),
            "text", "Debe contener entre 1 y 4096 caracteres.", errors);
        ValidateRemoteAddress(command.RemoteAddress, errors);
        ValidateProviderTimestamp(command.ProviderTimestampUtc, errors);
        ThrowIfInvalid(errors);

        var (token, tokenHash) = NewClaimToken();
        var registration = await _store.RegisterInboundAsync(
            command with
            {
                Provider = ChannelInstance.ProviderEvolution,
                Instance = instance!,
                ProviderMessageId = providerMessageId!,
                Text = text!
            },
            new ClaimGrant(tokenHash, _claimTtl),
            cancellationToken);

        // El token se entrega una única vez, a quien ganó la inserción.
        return registration.Created
            ? registration with { Processing = registration.Processing with { ClaimToken = token } }
            : registration;
    }

    public Task<ReplyIntent> CreateReplyIntentAsync(
        CreateReplyIntentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (command.InboundMessageId == Guid.Empty)
            errors["inboundMessageId"] = ["Debe ser un UUID válido."];
        ValidateClaimToken(command.ClaimToken, errors);
        var text = Normalize(() => ConversationMessage.NormalizeText(command.Text),
            "text", "Debe contener entre 1 y 4096 caracteres.", errors);
        ThrowIfInvalid(errors);

        return _store.CreateReplyIntentAsync(
            command.InboundMessageId, HashClaimToken(command.ClaimToken), text!, cancellationToken);
    }

    public Task<ReplyIntent> UpdateSendStatusAsync(
        UpdateSendStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (command.ReplyId == Guid.Empty)
            errors["replyId"] = ["Debe ser un UUID válido."];
        if (command.Status == MessageSendStatus.PendingSend)
            errors["status"] = ["Debe ser accepted, failed o unknown."];
        if (command.ProviderMessageId is not null)
        {
            if (command.Status != MessageSendStatus.Accepted)
                errors["providerMessageId"] = ["Solo se admite con el estado accepted."];
            else
                Normalize(() => ConversationMessage.NormalizeProviderMessageId(command.ProviderMessageId),
                    "providerMessageId", "Debe indicar un identificador de hasta 128 caracteres.", errors);
        }
        ThrowIfInvalid(errors);

        return _store.UpdateSendStatusAsync(command, cancellationToken);
    }

    public Task<ProcessingResult> ReportProcessingFailureAsync(
        ReportProcessingFailureCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (command.InboundMessageId == Guid.Empty)
            errors["messageId"] = ["Debe ser un UUID válido."];
        ValidateClaimToken(command.ClaimToken, errors);
        if (command.ErrorCode is null || !MessageProcessing.ErrorCodes.Contains(command.ErrorCode))
            errors["errorCode"] = ["Código de error no admitido."];
        ThrowIfInvalid(errors);

        return _store.ReportProcessingFailureAsync(
            command.InboundMessageId, HashClaimToken(command.ClaimToken), command.ErrorCode!, cancellationToken);
    }

    public Task<Page<ConversationSummary>> ListConversationsAsync(
        Guid ingenioId, PageRequest page, CancellationToken cancellationToken = default)
    {
        ValidatePage(page);
        return _store.ListConversationsAsync(ingenioId, page, cancellationToken);
    }

    public async Task<ConversationSummary> GetConversationAsync(
        Guid ingenioId, Guid conversationId, CancellationToken cancellationToken = default) =>
        await _store.GetConversationAsync(ingenioId, conversationId, cancellationToken)
            ?? throw ConversationNotFound();

    public async Task<Page<ConversationMessageView>> ListMessagesAsync(
        Guid ingenioId, Guid conversationId, PageRequest page, CancellationToken cancellationToken = default)
    {
        ValidatePage(page);
        return await _store.ListMessagesAsync(ingenioId, conversationId, page, cancellationToken)
            ?? throw ConversationNotFound();
    }

    /// <summary>SHA-256 en hexadecimal minúscula; es lo único que se persiste del token.</summary>
    public static string HashClaimToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static (string Token, string Hash) NewClaimToken()
    {
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (token, HashClaimToken(token));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private void ValidateProviderTimestamp(DateTime? value, Dictionary<string, string[]> errors)
    {
        if (value is not { } timestamp)
            return;
        var latest = _timeProvider.GetUtcNow().UtcDateTime + MaxProviderClockSkew;
        if (timestamp.Kind != DateTimeKind.Utc || timestamp < MinProviderTimestampUtc || timestamp > latest)
            errors["providerTimestamp"] = ["Debe ser una fecha y hora UTC dentro del rango admitido."];
    }

    private static void ValidateRemoteAddress(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors["remoteAddress"] = ["Debe indicar la dirección remota."];
            return;
        }

        if (WhatsAppAddress.IsGroup(value))
        {
            errors["remoteAddress"] = ["No se admiten direcciones de grupo."];
            return;
        }

        Normalize(() => WhatsAppAddress.Parse(value), "remoteAddress", "No es una dirección de WhatsApp válida.", errors);
    }

    private static void ValidateClaimToken(string? token, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxClaimTokenLength)
            errors["claimToken"] = ["Debe indicar el token de procesamiento."];
    }

    private static void ValidatePage(PageRequest page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page.Limit is < 1 or > MaxPageSize)
            throw new ConversationServiceException(
                ConversationErrorCodes.ValidationError,
                "Solicitud inválida.",
                new Dictionary<string, string[]> { ["limit"] = [$"Debe estar entre 1 y {MaxPageSize}."] });
    }

    private static T? Normalize<T>(Func<T> normalize, string field, string message, Dictionary<string, string[]> errors)
    {
        try
        {
            return normalize();
        }
        catch (ArgumentException)
        {
            // El mensaje de la excepción de dominio no se propaga: sólo el campo.
            errors[field] = [message];
            return default;
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
            throw new ConversationServiceException(ConversationErrorCodes.ValidationError, "Solicitud inválida.", errors);
    }

    private static ConversationServiceException ConversationNotFound() =>
        new(ConversationErrorCodes.ConversationNotFound, "Conversación inexistente.");
}
