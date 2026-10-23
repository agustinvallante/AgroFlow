using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using AgroFlow.Application.Conversations;
using AgroFlow.Domain.Entities;

namespace AgroFlow.Api.Contracts.Conversations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ConversationEventRequest
{
    public string? Provider { get; init; }
    public string? Instance { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? RemoteAddress { get; init; }
    public string? ProviderTimestamp { get; init; }
    public string? ContentType { get; init; }
    public string? Text { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReplyIntentRequest
{
    public string? InboundMessageId { get; init; }
    public string? ClaimToken { get; init; }
    public string? Text { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SendStatusRequest
{
    public string? Status { get; init; }
    public string? ProviderMessageId { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProcessingResultRequest
{
    public string? ClaimToken { get; init; }
    public string? Status { get; init; }
    public string? ErrorCode { get; init; }
}

public sealed record ProcessingResponse(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClaimToken,
    DateTimeOffset ExpiresAt);

public sealed record SenderResponse(
    bool Recognized,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? TransportistaId);

public sealed record ConversationEventResponse(
    string Disposition,
    Guid ConversationId,
    Guid MessageId,
    ProcessingResponse Processing,
    SenderResponse Sender);

public sealed record ReplyIntentResponse(
    Guid ReplyId,
    Guid ConversationId,
    Guid InboundMessageId,
    string Instance,
    string RecipientAddress,
    string Text,
    string SendStatus,
    string? ProviderMessageId);

public sealed record ProcessingResultResponse(Guid MessageId, string Status, string? ErrorCode);

public sealed record ConversationSenderResponse(bool Recognized, Guid? TransportistaId, string? Name);

public sealed record ConversationSummaryResponse(
    Guid Id,
    string AddressKind,
    string RemoteAddressMasked,
    ConversationSenderResponse Sender,
    DateTimeOffset LastMessageAt,
    string? LastMessagePreview,
    string? LastMessageDirection,
    bool NeedsAttention);

public sealed record ConversationMessageResponse(
    Guid Id,
    string Direction,
    string Text,
    DateTimeOffset? ProviderTimestamp,
    DateTimeOffset PersistedAt,
    Guid? InReplyToMessageId,
    string? SendStatus,
    string? ProcessingStatus);

public sealed record PageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);

public static class ConversationHttpMapper
{
    public static ConversationEventResponse ToResponse(InboundRegistration registration) => new(
        registration.Disposition == InboundDisposition.Process ? "process" : "duplicate_stop",
        registration.ConversationId,
        registration.MessageId,
        new ProcessingResponse(
            ToWire(registration.Processing.State),
            registration.Processing.ClaimToken,
            registration.Processing.ClaimExpiresAt),
        new SenderResponse(registration.Sender.Recognized, registration.Sender.TransportistaId));

    public static ReplyIntentResponse ToResponse(ReplyIntent intent) => new(
        intent.ReplyId,
        intent.ConversationId,
        intent.InboundMessageId,
        intent.Instance,
        intent.RecipientAddress,
        intent.Text,
        ToWire(intent.SendStatus),
        intent.ProviderMessageId);

    public static ProcessingResultResponse ToResponse(ProcessingResult result) =>
        new(result.InboundMessageId, ToWire(result.State), result.ErrorCode);

    public static ConversationSummaryResponse ToResponse(ConversationSummary summary) => new(
        summary.Id,
        ToWire(summary.AddressKind),
        summary.RemoteAddressMasked,
        new ConversationSenderResponse(summary.Sender.Recognized, summary.Sender.TransportistaId, summary.Sender.Name),
        summary.LastMessageAt,
        summary.LastMessagePreview,
        summary.LastMessageDirection is { } direction ? ToWire(direction) : null,
        summary.NeedsAttention);

    public static ConversationMessageResponse ToResponse(ConversationMessageView message) => new(
        message.Id,
        ToWire(message.Direction),
        message.Text,
        message.ProviderTimestamp,
        message.PersistedAt,
        message.InReplyToMessageId,
        message.SendStatus is { } sendStatus ? ToWire(sendStatus) : null,
        message.ProcessingState is { } state ? ToWire(state) : null);

    public static bool TryParseReportedSendStatus(string? value, out MessageSendStatus status)
    {
        (var valid, status) = value switch
        {
            "accepted" => (true, MessageSendStatus.Accepted),
            "failed" => (true, MessageSendStatus.Failed),
            "unknown" => (true, MessageSendStatus.Unknown),
            _ => (false, default)
        };
        return valid;
    }

    public static string ToWire(ProcessingState state) => state switch
    {
        ProcessingState.Processing => "PROCESSING",
        ProcessingState.Completed => "COMPLETED",
        ProcessingState.Failed => "FAILED",
        ProcessingState.Expired => "EXPIRED",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    public static string ToWire(MessageSendStatus status) => status switch
    {
        MessageSendStatus.PendingSend => "PENDING_SEND",
        MessageSendStatus.Accepted => "ACCEPTED",
        MessageSendStatus.Failed => "FAILED",
        MessageSendStatus.Unknown => "UNKNOWN",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string ToWire(MessageDirection direction) => direction switch
    {
        MessageDirection.Inbound => "INBOUND",
        MessageDirection.Outbound => "OUTBOUND",
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    public static string ToWire(ConversationAddressKind kind) => kind switch
    {
        ConversationAddressKind.Phone => "PHONE",
        ConversationAddressKind.Lid => "LID",
        ConversationAddressKind.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

/// <summary>
/// Cursor opaco: base64url de "ticks UTC.Id". No es un secreto ni una firma; el
/// almacén siempre lo combina con el filtro de ingenio y valida su anclaje.
/// </summary>
public static class PageCursorCodec
{
    public static string Encode(PageCursor cursor)
    {
        var raw = Encoding.ASCII.GetBytes(
            $"{cursor.TimestampUtc.Ticks.ToString(CultureInfo.InvariantCulture)}.{cursor.Id:N}");
        return Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string? value, out PageCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrEmpty(value) || value.Length > 64)
            return false;

        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        var buffer = new byte[Base64.GetMaxDecodedFromUtf8Length(base64.Length)];
        if (!Convert.TryFromBase64String(base64, buffer, out var written))
            return false;

        var parts = Encoding.ASCII.GetString(buffer, 0, written).Split('.');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks
            || !Guid.TryParseExact(parts[1], "N", out var id))
            return false;

        cursor = new PageCursor(new DateTime(ticks, DateTimeKind.Utc), id);
        // Rechaza codificaciones alternativas del mismo valor.
        return Encode(cursor) == value;
    }
}
