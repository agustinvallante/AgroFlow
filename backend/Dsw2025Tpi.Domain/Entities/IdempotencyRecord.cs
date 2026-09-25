namespace Dsw2025Tpi.Domain.Entities;

/// <summary>
/// Durable metadata for replaying an operation without retaining request or response bodies.
/// </summary>
public sealed class IdempotencyRecord : EntityBase
{
    private IdempotencyRecord()
    {
    }

    public IdempotencyRecord(
        string scope,
        string actorIdentity,
        string operation,
        string key,
        string inputHash,
        string resultCode,
        string? resultReference,
        DateTime createdAtUtc,
        DateTime completedAtUtc)
    {
        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation timestamp must be UTC.", nameof(createdAtUtc));
        }

        if (completedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Completion timestamp must be UTC.", nameof(completedAtUtc));
        }

        if (completedAtUtc < createdAtUtc)
        {
            throw new ArgumentException("Completion timestamp cannot precede creation.", nameof(completedAtUtc));
        }

        Scope = RequireValue(scope, nameof(scope));
        ActorIdentity = RequireValue(actorIdentity, nameof(actorIdentity));
        Operation = RequireValue(operation, nameof(operation));
        Key = RequireValue(key, nameof(key));
        InputHash = RequireValue(inputHash, nameof(inputHash));
        ResultCode = RequireValue(resultCode, nameof(resultCode));
        ResultReference = NormalizeOptional(resultReference);
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public string Scope { get; private set; } = string.Empty;
    public string ActorIdentity { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string InputHash { get; private set; } = string.Empty;
    public string ResultCode { get; private set; } = string.Empty;
    public string? ResultReference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
