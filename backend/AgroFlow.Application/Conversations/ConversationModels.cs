using AgroFlow.Domain.Entities;

namespace AgroFlow.Application.Conversations;

public enum InboundDisposition
{
    Process,
    DuplicateStop
}

/// <summary>Estado observable del procesamiento; Expired se deriva del vencimiento.</summary>
public enum ProcessingState
{
    Processing,
    Completed,
    Failed,
    Expired
}

public sealed record RegisterInboundCommand(
    string Provider,
    string Instance,
    string ProviderMessageId,
    string RemoteAddress,
    DateTime? ProviderTimestampUtc,
    string Text);

public sealed record ProcessingSnapshot(ProcessingState State, string? ClaimToken, DateTimeOffset ClaimExpiresAt);

public sealed record SenderIdentification(bool Recognized, Guid? TransportistaId);

public sealed record InboundRegistration(
    InboundDisposition Disposition,
    bool Created,
    Guid ConversationId,
    Guid MessageId,
    ProcessingSnapshot Processing,
    SenderIdentification Sender);

public sealed record CreateReplyIntentCommand(Guid InboundMessageId, string ClaimToken, string Text);

public sealed record ReplyIntent(
    Guid ReplyId,
    Guid ConversationId,
    Guid InboundMessageId,
    string Instance,
    string RecipientAddress,
    string Text,
    MessageSendStatus SendStatus,
    string? ProviderMessageId,
    bool Created);

public sealed record UpdateSendStatusCommand(Guid ReplyId, MessageSendStatus Status, string? ProviderMessageId);

public sealed record ReportProcessingFailureCommand(Guid InboundMessageId, string ClaimToken, string ErrorCode);

public sealed record ProcessingResult(Guid InboundMessageId, ProcessingState State, string? ErrorCode);

public sealed record ConversationSender(bool Recognized, Guid? TransportistaId, string? Name);

public sealed record ConversationSummary(
    Guid Id,
    ConversationAddressKind AddressKind,
    string RemoteAddressMasked,
    ConversationSender Sender,
    DateTimeOffset LastMessageAt,
    string? LastMessagePreview,
    MessageDirection? LastMessageDirection,
    bool NeedsAttention);

public sealed record ConversationMessageView(
    Guid Id,
    MessageDirection Direction,
    string Text,
    DateTimeOffset? ProviderTimestamp,
    DateTimeOffset PersistedAt,
    Guid? InReplyToMessageId,
    MessageSendStatus? SendStatus,
    ProcessingState? ProcessingState);

public sealed record PageCursor(DateTime TimestampUtc, Guid Id);

public sealed record PageRequest(PageCursor? After, int Limit);

public sealed record Page<T>(IReadOnlyList<T> Items, PageCursor? Next);
