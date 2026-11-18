namespace AgroFlow.Application.Conversations;

/// <summary>Hash del token de procesamiento a otorgar y su vigencia.</summary>
public sealed record ClaimGrant(string TokenHash, TimeSpan Ttl);

/// <summary>
/// Persistencia atómica del canal. Cada escritura corre en una transacción
/// propia y descansa en los índices únicos de la base para la idempotencia;
/// los conflictos esperados se informan con <see cref="ConversationServiceException"/>.
/// Los tokens de procesamiento llegan siempre como hash: el almacén nunca ve el token.
/// </summary>
public interface IConversationStore
{
    /// <remarks>
    /// El resultado nunca incluye el token: el servicio lo agrega sólo cuando
    /// esta llamada creó el mensaje.
    /// </remarks>
    Task<InboundRegistration> RegisterInboundAsync(
        RegisterInboundCommand command, ClaimGrant grant, CancellationToken cancellationToken = default);

    Task<ReplyIntent> CreateReplyIntentAsync(
        Guid inboundMessageId, string claimTokenHash, string text, CancellationToken cancellationToken = default);

    Task<ReplyIntent> UpdateSendStatusAsync(
        UpdateSendStatusCommand command, CancellationToken cancellationToken = default);

    Task<ProcessingResult> ReportProcessingFailureAsync(
        Guid inboundMessageId, string claimTokenHash, string errorCode, CancellationToken cancellationToken = default);

    Task<Page<ConversationSummary>> ListConversationsAsync(
        Guid ingenioId, PageRequest page, CancellationToken cancellationToken = default);

    Task<ConversationSummary?> GetConversationAsync(
        Guid ingenioId, Guid conversationId, CancellationToken cancellationToken = default);

    /// <returns><c>null</c> si la conversación no existe en el ingenio.</returns>
    Task<Page<ConversationMessageView>?> ListMessagesAsync(
        Guid ingenioId, Guid conversationId, PageRequest page, CancellationToken cancellationToken = default);
}
