namespace AgroFlow.Domain.Entities;

public enum MessageDirection
{
    Inbound,
    Outbound
}

public enum MessageSendStatus
{
    PendingSend,
    Accepted,
    Failed,
    Unknown
}

public enum SendStatusChange
{
    Applied,
    Unchanged,
    Invalid
}

/// <summary>
/// Registro de un mensaje del canal. El contenido es inmutable; en las
/// salientes sólo cambian el estado de envío y el identificador del proveedor.
/// </summary>
public sealed class ConversationMessage : EntityBase
{
    public const string ContentTypeText = "TEXT";
    public const int MaxTextLength = 4096;
    public const int MaxProviderMessageIdLength = 128;

    private ConversationMessage()
    {
    }

    private ConversationMessage(Guid ingenioId, Guid conversationId, MessageDirection direction, string text, DateTime persistedAtUtc)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        if (conversationId == Guid.Empty)
            throw new ArgumentException("Conversation identifier is required.", nameof(conversationId));
        MasterDataValidation.EnsureUtc(persistedAtUtc, nameof(persistedAtUtc));
        ConversationId = conversationId;
        Direction = direction;
        ContentType = ContentTypeText;
        Text = NormalizeText(text);
        PersistedAtUtc = persistedAtUtc;
    }

    public Guid IngenioId { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageDirection Direction { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string ContentType { get; private set; } = ContentTypeText;
    public string Text { get; private set; } = string.Empty;
    public DateTime? ProviderTimestampUtc { get; private set; }
    public DateTime PersistedAtUtc { get; private set; }
    public Guid? InReplyToMessageId { get; private set; }
    public MessageSendStatus? SendStatus { get; private set; }
    public DateTime? SendStatusUpdatedAtUtc { get; private set; }

    public static ConversationMessage Inbound(Guid ingenioId, Guid conversationId, string providerMessageId,
        string text, DateTime? providerTimestampUtc, DateTime persistedAtUtc)
    {
        if (providerTimestampUtc is { } providerTimestamp)
            MasterDataValidation.EnsureUtc(providerTimestamp, nameof(providerTimestampUtc));
        return new ConversationMessage(ingenioId, conversationId, MessageDirection.Inbound, text, persistedAtUtc)
        {
            ProviderMessageId = NormalizeProviderMessageId(providerMessageId),
            ProviderTimestampUtc = providerTimestampUtc
        };
    }

    public static ConversationMessage OutboundIntent(Guid ingenioId, Guid conversationId, Guid inReplyToMessageId,
        string text, DateTime persistedAtUtc)
    {
        if (inReplyToMessageId == Guid.Empty)
            throw new ArgumentException("Inbound message identifier is required.", nameof(inReplyToMessageId));
        return new ConversationMessage(ingenioId, conversationId, MessageDirection.Outbound, text, persistedAtUtc)
        {
            InReplyToMessageId = inReplyToMessageId,
            SendStatus = MessageSendStatus.PendingSend,
            SendStatusUpdatedAtUtc = persistedAtUtc
        };
    }

    /// <summary>
    /// Pendiente puede pasar a aceptada, fallida o incierta; una incierta sólo
    /// puede resolverse como aceptada o fallida. Repetir el estado actual no
    /// cambia nada. Nunca existe un estado de entregado sin evidencia.
    /// </summary>
    public SendStatusChange ApplySendStatus(MessageSendStatus next, string? providerMessageId, DateTime occurredAtUtc)
    {
        if (Direction != MessageDirection.Outbound || SendStatus is not { } current)
            return SendStatusChange.Invalid;
        MasterDataValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        var normalizedProviderId = string.IsNullOrWhiteSpace(providerMessageId)
            ? null
            : NormalizeProviderMessageId(providerMessageId);
        if (normalizedProviderId is not null && next != MessageSendStatus.Accepted)
            return SendStatusChange.Invalid;

        if (current == next)
        {
            return normalizedProviderId is null || normalizedProviderId == ProviderMessageId
                ? SendStatusChange.Unchanged
                : SendStatusChange.Invalid;
        }

        var allowed = (current, next) switch
        {
            (MessageSendStatus.PendingSend, MessageSendStatus.Accepted or MessageSendStatus.Failed or MessageSendStatus.Unknown) => true,
            (MessageSendStatus.Unknown, MessageSendStatus.Accepted or MessageSendStatus.Failed) => true,
            _ => false
        };
        if (!allowed)
            return SendStatusChange.Invalid;

        SendStatus = next;
        SendStatusUpdatedAtUtc = occurredAtUtc;
        if (normalizedProviderId is not null)
            ProviderMessageId = normalizedProviderId;
        return SendStatusChange.Applied;
    }

    public bool HasSameInboundContent(string text, DateTime? providerTimestampUtc) =>
        Direction == MessageDirection.Inbound
        && string.Equals(Text, NormalizeText(text), StringComparison.Ordinal)
        && ProviderTimestampUtc == providerTimestampUtc;

    public static string NormalizeText(string value)
    {
        var trimmed = MasterDataValidation.RequireText(value, nameof(value));
        return trimmed.Length > MaxTextLength
            ? throw new ArgumentException("Text exceeds the maximum length.", nameof(value))
            : trimmed;
    }

    public static string NormalizeProviderMessageId(string value)
    {
        var trimmed = MasterDataValidation.RequireText(value, nameof(value));
        return trimmed.Length > MaxProviderMessageIdLength
            ? throw new ArgumentException("Provider message identifier is too long.", nameof(value))
            : trimmed;
    }
}
