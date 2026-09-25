namespace Dsw2025Tpi.Domain.Entities;

/// <summary>
/// Immutable audit evidence. DetailsReference must identify compact, non-sensitive metadata only.
/// </summary>
public sealed class AuditEvent : EntityBase
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        Guid ingenioId,
        string actorIdentity,
        DateTime occurredAtUtc,
        string action,
        string entityType,
        string entityId,
        string result,
        string correlationId,
        string? reason = null,
        string? detailsReference = null)
    {
        if (ingenioId == Guid.Empty)
        {
            throw new ArgumentException("Ingenio identifier is required.", nameof(ingenioId));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Audit timestamp must be UTC.", nameof(occurredAtUtc));
        }

        IngenioId = ingenioId;
        ActorIdentity = RequireValue(actorIdentity, nameof(actorIdentity));
        OccurredAtUtc = occurredAtUtc;
        Action = RequireValue(action, nameof(action));
        EntityType = RequireValue(entityType, nameof(entityType));
        EntityId = RequireValue(entityId, nameof(entityId));
        Result = RequireValue(result, nameof(result));
        CorrelationId = RequireValue(correlationId, nameof(correlationId));
        Reason = NormalizeOptional(reason);
        DetailsReference = NormalizeOptional(detailsReference);
    }

    public Guid IngenioId { get; private set; }
    public string ActorIdentity { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Result { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string? DetailsReference { get; private set; }

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
