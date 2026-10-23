namespace AgroFlow.Application.Conversations;

public static class ConversationErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ChannelInstanceNotAllowed = "CHANNEL_INSTANCE_NOT_ALLOWED";
    public const string ConversationEventConflict = "CONVERSATION_EVENT_CONFLICT";
    public const string MessageNotFound = "MESSAGE_NOT_FOUND";
    public const string ProcessingClaimInvalid = "PROCESSING_CLAIM_INVALID";
    public const string ReplyIntentConflict = "REPLY_INTENT_CONFLICT";
    public const string ReplyNotFound = "REPLY_NOT_FOUND";
    public const string InvalidSendStatusTransition = "INVALID_SEND_STATUS_TRANSITION";
    public const string ConversationNotFound = "CONVERSATION_NOT_FOUND";
}

/// <summary>
/// Error esperado del canal o del dashboard. Nunca transporta texto de mensajes,
/// direcciones ni teléfonos: sólo un código estable y, si aplica, el campo.
/// </summary>
public sealed class ConversationServiceException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public ConversationServiceException(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Code = code;
        Errors = errors;
    }
}
