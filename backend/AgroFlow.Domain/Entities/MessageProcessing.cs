namespace AgroFlow.Domain.Entities;

public enum ProcessingStatus
{
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Autorización única de procesamiento de un mensaje entrante. Se crea en la
/// misma transacción que el mensaje y sólo su titular (quien conoce el token)
/// puede cerrarla. Su vencimiento es informativo: nunca habilita reprocesar.
/// </summary>
public sealed class MessageProcessing : EntityBase
{
    public const int MaxErrorCodeLength = 64;

    /// <summary>Catálogo cerrado: ningún texto libre, identidad ni payload.</summary>
    public static readonly IReadOnlySet<string> ErrorCodes =
        new HashSet<string>(["AGENT_ERROR", "REPLY_NOT_SENT", "WORKFLOW_ERROR"], StringComparer.Ordinal);

    private MessageProcessing()
    {
    }

    public MessageProcessing(Guid ingenioId, Guid inboundMessageId, string claimTokenHash,
        DateTime claimedAtUtc, DateTime claimExpiresAtUtc)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        if (inboundMessageId == Guid.Empty)
            throw new ArgumentException("Inbound message identifier is required.", nameof(inboundMessageId));
        MasterDataValidation.EnsureUtc(claimedAtUtc, nameof(claimedAtUtc));
        MasterDataValidation.EnsureUtc(claimExpiresAtUtc, nameof(claimExpiresAtUtc));
        if (claimExpiresAtUtc <= claimedAtUtc)
            throw new ArgumentException("Claim expiration must follow the claim.", nameof(claimExpiresAtUtc));

        InboundMessageId = inboundMessageId;
        ClaimTokenHash = MasterDataValidation.RequireText(claimTokenHash, nameof(claimTokenHash));
        ClaimedAtUtc = claimedAtUtc;
        ClaimExpiresAtUtc = claimExpiresAtUtc;
        Status = ProcessingStatus.Processing;
    }

    public Guid IngenioId { get; private set; }
    public Guid InboundMessageId { get; private set; }
    public ProcessingStatus Status { get; private set; }

    /// <summary>SHA-256 del token entregado al titular; el token no se persiste.</summary>
    public string ClaimTokenHash { get; private set; } = string.Empty;

    public DateTime ClaimedAtUtc { get; private set; }
    public DateTime ClaimExpiresAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }

    public bool IsExpired(DateTime nowUtc) => Status == ProcessingStatus.Processing && nowUtc >= ClaimExpiresAtUtc;

    public bool CanBeClosedBy(DateTime nowUtc) => Status == ProcessingStatus.Processing && !IsExpired(nowUtc);

    public void Complete(DateTime occurredAtUtc)
    {
        MasterDataValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (Status != ProcessingStatus.Processing)
            throw new InvalidOperationException("Only an open processing claim can be completed.");
        Status = ProcessingStatus.Completed;
        CompletedAtUtc = occurredAtUtc;
    }

    public void Fail(string errorCode, DateTime occurredAtUtc)
    {
        MasterDataValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (!ErrorCodes.Contains(errorCode))
            throw new ArgumentException("Unknown processing error code.", nameof(errorCode));
        if (Status != ProcessingStatus.Processing)
            throw new InvalidOperationException("Only an open processing claim can fail.");
        Status = ProcessingStatus.Failed;
        CompletedAtUtc = occurredAtUtc;
        LastErrorCode = errorCode;
    }
}
