using System.Data;
using System.Security.Cryptography;
using System.Text;
using AgroFlow.Application.Conversations;
using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Adaptador EF del canal de WhatsApp. Cada escritura corre en una transacción
/// SQLite inmediata (`BEGIN IMMEDIATE`), así las escrituras se serializan y la
/// lectura previa al alta no queda expuesta a carreras; los índices únicos
/// siguen siendo la última barrera y, si alguno salta, la operación se
/// reintenta una vez releyendo lo que otro request ya persistió.
/// </summary>
public sealed class EfConversationStore : IConversationStore
{
    private const int PreviewLength = 80;

    private readonly AgroFlowDbContext _db;
    private readonly TimeProvider _timeProvider;

    public EfConversationStore(AgroFlowDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<InboundRegistration> RegisterInboundAsync(
        RegisterInboundCommand command, ClaimGrant grant, CancellationToken cancellationToken = default)
    {
        var instance = await _db.ChannelInstances.AsNoTracking().FirstOrDefaultAsync(
            c => c.Provider == command.Provider && c.InstanceName == command.Instance && c.IsActive,
            cancellationToken) ?? throw new ConversationServiceException(
                ConversationErrorCodes.ChannelInstanceNotAllowed, "Instancia del canal no habilitada.");
        var address = WhatsAppAddress.Parse(command.RemoteAddress);

        return await RetryOnUniqueViolationAsync(
            () => RegisterInboundOnceAsync(instance, address, command, grant, cancellationToken));
    }

    private async Task<InboundRegistration> RegisterInboundOnceAsync(
        ChannelInstance instance, WhatsAppAddress address, RegisterInboundCommand command, ClaimGrant grant,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = UtcNow;

        var conversation = await _db.Conversations.FirstOrDefaultAsync(
            c => c.ChannelInstanceId == instance.Id && c.RemoteAddress == address.Value, cancellationToken);
        if (conversation is not null)
        {
            var existing = await _db.ConversationMessages.AsNoTracking().FirstOrDefaultAsync(
                m => m.ConversationId == conversation.Id
                    && m.Direction == MessageDirection.Inbound
                    && m.ProviderMessageId == command.ProviderMessageId,
                cancellationToken);
            if (existing is not null)
            {
                if (!existing.HasSameInboundContent(command.Text, command.ProviderTimestampUtc))
                    throw new ConversationServiceException(
                        ConversationErrorCodes.ConversationEventConflict,
                        "El mensaje ya fue registrado con otro contenido.");

                // Repetición: no se crea nada, no se vincula nada y no se
                // otorga una nueva autorización, aunque la anterior haya vencido.
                var priorProcessing = await _db.MessageProcessings.AsNoTracking()
                    .FirstAsync(p => p.InboundMessageId == existing.Id, cancellationToken);
                var priorSender = await FindRecognizedTransportistaAsync(instance.IngenioId, conversation, cancellationToken);
                return new InboundRegistration(
                    InboundDisposition.DuplicateStop,
                    Created: false,
                    conversation.Id,
                    existing.Id,
                    ToSnapshot(priorProcessing, now),
                    new SenderIdentification(priorSender is not null, priorSender?.Id));
            }
        }
        else
        {
            conversation = new Conversation(instance.IngenioId, instance.Id, address.Value, now);
            _db.Conversations.Add(conversation);
        }

        // El vínculo se evalúa en cada mensaje nuevo: un número registrado
        // después se vincula sin reescribir mensajes previos, y un transportista
        // inhabilitado conserva el vínculo histórico pero deja de reconocerse.
        var transportista = await FindRecognizedTransportistaAsync(instance.IngenioId, conversation, cancellationToken);
        if (transportista is not null && conversation.TransportistaId != transportista.Id)
            conversation.LinkTransportista(transportista);

        var message = ConversationMessage.Inbound(
            instance.IngenioId,
            conversation.Id,
            command.ProviderMessageId,
            command.Text,
            command.ProviderTimestampUtc,
            conversation.NextMessageTimestamp(now));
        var processing = new MessageProcessing(instance.IngenioId, message.Id, grant.TokenHash, now, now + grant.Ttl);
        _db.ConversationMessages.Add(message);
        _db.MessageProcessings.Add(processing);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new InboundRegistration(
            InboundDisposition.Process,
            Created: true,
            conversation.Id,
            message.Id,
            ToSnapshot(processing, now),
            new SenderIdentification(transportista is not null, transportista?.Id));
    }

    public Task<ReplyIntent> CreateReplyIntentAsync(
        Guid inboundMessageId, string claimTokenHash, string text, CancellationToken cancellationToken = default) =>
        RetryOnUniqueViolationAsync(() => CreateReplyIntentOnceAsync(inboundMessageId, claimTokenHash, text, cancellationToken));

    private async Task<ReplyIntent> CreateReplyIntentOnceAsync(
        Guid inboundMessageId, string claimTokenHash, string text, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = UtcNow;

        var inbound = await _db.ConversationMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == inboundMessageId && m.Direction == MessageDirection.Inbound, cancellationToken)
            ?? throw MessageNotFound();
        var processing = await _db.MessageProcessings.AsNoTracking()
            .FirstAsync(p => p.InboundMessageId == inbound.Id, cancellationToken);
        if (!ClaimTokenMatches(processing, claimTokenHash))
            throw ClaimInvalid();

        var conversation = await _db.Conversations.FirstAsync(c => c.Id == inbound.ConversationId, cancellationToken);
        var instance = await _db.ChannelInstances.AsNoTracking()
            .FirstAsync(c => c.Id == conversation.ChannelInstanceId, cancellationToken);

        var existing = await _db.ConversationMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.InReplyToMessageId == inbound.Id, cancellationToken);
        if (existing is not null)
        {
            // Repetición del titular: misma intención si el texto coincide.
            if (!string.Equals(existing.Text, ConversationMessage.NormalizeText(text), StringComparison.Ordinal))
                throw new ConversationServiceException(
                    ConversationErrorCodes.ReplyIntentConflict, "El mensaje ya tiene otra respuesta registrada.");
            return ToReplyIntent(existing, conversation, instance, created: false);
        }

        if (!processing.CanBeClosedBy(now))
            throw ClaimInvalid();
        if (!instance.IsActive)
            throw new ConversationServiceException(
                ConversationErrorCodes.ChannelInstanceNotAllowed, "Instancia del canal no habilitada.");

        var reply = ConversationMessage.OutboundIntent(
            inbound.IngenioId, conversation.Id, inbound.Id, text, conversation.NextMessageTimestamp(now));
        _db.ConversationMessages.Add(reply);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToReplyIntent(reply, conversation, instance, created: true);
    }

    public async Task<ReplyIntent> UpdateSendStatusAsync(
        UpdateSendStatusCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = UtcNow;

        var reply = await _db.ConversationMessages.FirstOrDefaultAsync(
            m => m.Id == command.ReplyId && m.Direction == MessageDirection.Outbound, cancellationToken)
            ?? throw new ConversationServiceException(ConversationErrorCodes.ReplyNotFound, "Respuesta inexistente.");
        var change = reply.ApplySendStatus(command.Status, command.ProviderMessageId, now);
        if (change == SendStatusChange.Invalid)
            throw InvalidTransition();

        if (change == SendStatusChange.Applied)
        {
            // Informar el resultado del envío cierra el procesamiento del
            // entrante, salvo que su titular ya lo haya cerrado como fallido.
            var processing = await _db.MessageProcessings
                .FirstAsync(p => p.InboundMessageId == reply.InReplyToMessageId, cancellationToken);
            if (processing.Status == ProcessingStatus.Processing)
                processing.Complete(now);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (SqliteConstraintErrors.IsUniqueOrPrimaryKeyViolation(exception))
            {
                // Otro saliente de la conversación ya tiene ese identificador del proveedor.
                throw InvalidTransition();
            }
        }

        var conversation = await _db.Conversations.AsNoTracking()
            .FirstAsync(c => c.Id == reply.ConversationId, cancellationToken);
        var instance = await _db.ChannelInstances.AsNoTracking()
            .FirstAsync(c => c.Id == conversation.ChannelInstanceId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToReplyIntent(reply, conversation, instance, created: false);
    }

    public async Task<ProcessingResult> ReportProcessingFailureAsync(
        Guid inboundMessageId, string claimTokenHash, string errorCode, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = UtcNow;

        var processing = await _db.MessageProcessings
            .FirstOrDefaultAsync(p => p.InboundMessageId == inboundMessageId, cancellationToken)
            ?? throw MessageNotFound();
        if (!ClaimTokenMatches(processing, claimTokenHash))
            throw ClaimInvalid();

        if (processing.Status == ProcessingStatus.Failed
            && string.Equals(processing.LastErrorCode, errorCode, StringComparison.Ordinal))
        {
            // Repetición idéntica del titular: no cambia nada.
            await transaction.CommitAsync(cancellationToken);
            return new ProcessingResult(inboundMessageId, ProcessingState.Failed, processing.LastErrorCode);
        }

        if (!processing.CanBeClosedBy(now))
            throw ClaimInvalid();

        processing.Fail(errorCode, now);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProcessingResult(inboundMessageId, ProcessingState.Failed, processing.LastErrorCode);
    }

    public async Task<Page<ConversationSummary>> ListConversationsAsync(
        Guid ingenioId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var scoped = _db.Conversations.AsNoTracking().Where(c => c.IngenioId == ingenioId);
        var rows = new List<Conversation>();

        // Orden: última actividad descendente y, ante empates, Id descendente.
        // El desempate se resuelve en memoria con la forma textual del Id para
        // no depender de cómo el proveedor compara GUID; los grupos empatados
        // en el límite de página se leen completos.
        IQueryable<Conversation> older = scoped;
        if (page.After is { } after)
        {
            var tied = await scoped.Where(c => c.LastMessageAtUtc == after.TimestampUtc).ToListAsync(cancellationToken);
            rows.AddRange(tied.Where(c => CompareIds(c.Id, after.Id) < 0));
            older = scoped.Where(c => c.LastMessageAtUtc < after.TimestampUtc);
        }

        var olderRows = await older.OrderByDescending(c => c.LastMessageAtUtc)
            .Take(page.Limit + 1).ToListAsync(cancellationToken);
        rows.AddRange(olderRows);
        if (olderRows.Count > 0)
        {
            var boundary = olderRows[^1].LastMessageAtUtc;
            var group = await scoped.Where(c => c.LastMessageAtUtc == boundary).ToListAsync(cancellationToken);
            rows.AddRange(group.Where(c => rows.All(r => r.Id != c.Id)));
        }

        var ordered = rows
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ThenByDescending(c => c.Id.ToString("D"), StringComparer.Ordinal)
            .ToList();
        var items = ordered.Take(page.Limit).ToList();
        var next = ordered.Count > page.Limit
            ? new PageCursor(items[^1].LastMessageAtUtc, items[^1].Id)
            : null;
        return new Page<ConversationSummary>(await SummarizeAsync(items, cancellationToken), next);
    }

    public async Task<ConversationSummary?> GetConversationAsync(
        Guid ingenioId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IngenioId == ingenioId && c.Id == conversationId, cancellationToken);
        return conversation is null ? null : (await SummarizeAsync([conversation], cancellationToken))[0];
    }

    public async Task<Page<ConversationMessageView>?> ListMessagesAsync(
        Guid ingenioId, Guid conversationId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Conversations.AsNoTracking()
            .AnyAsync(c => c.IngenioId == ingenioId && c.Id == conversationId, cancellationToken);
        if (!exists)
            return null;

        var messages = _db.ConversationMessages.AsNoTracking().Where(m => m.ConversationId == conversationId);
        if (page.After is { } after)
        {
            // El cursor sólo es válido si señala un mensaje real de esta
            // conversación; cualquier otro valor es un cursor alterado.
            var anchored = await messages.AnyAsync(
                m => m.Id == after.Id && m.PersistedAtUtc == after.TimestampUtc, cancellationToken);
            if (!anchored)
                throw new ConversationServiceException(
                    ConversationErrorCodes.ValidationError,
                    "Solicitud inválida.",
                    new Dictionary<string, string[]> { ["cursor"] = ["Cursor inválido."] });
            messages = messages.Where(m => m.PersistedAtUtc > after.TimestampUtc);
        }

        // PersistedAtUtc es único dentro de la conversación: ordenar por él
        // basta para que las inserciones entre páginas no repitan ni omitan filas.
        var rows = await messages.OrderBy(m => m.PersistedAtUtc).Take(page.Limit + 1).ToListAsync(cancellationToken);
        var items = rows.Take(page.Limit).ToList();

        var inboundIds = items.Where(m => m.Direction == MessageDirection.Inbound).Select(m => m.Id).ToArray();
        var processings = await _db.MessageProcessings.AsNoTracking()
            .Where(p => inboundIds.Contains(p.InboundMessageId))
            .ToDictionaryAsync(p => p.InboundMessageId, cancellationToken);
        var now = UtcNow;

        var views = items.Select(m => new ConversationMessageView(
            m.Id,
            m.Direction,
            m.Text,
            m.ProviderTimestampUtc is { } providerTimestamp ? new DateTimeOffset(providerTimestamp) : null,
            new DateTimeOffset(m.PersistedAtUtc),
            m.InReplyToMessageId,
            m.SendStatus,
            processings.TryGetValue(m.Id, out var processing) ? ToState(processing, now) : null)).ToArray();
        var next = rows.Count > page.Limit ? new PageCursor(items[^1].PersistedAtUtc, items[^1].Id) : null;
        return new Page<ConversationMessageView>(views, next);
    }

    private async Task<IReadOnlyList<ConversationSummary>> SummarizeAsync(
        IReadOnlyList<Conversation> conversations, CancellationToken cancellationToken)
    {
        if (conversations.Count == 0)
            return [];

        var ids = conversations.Select(c => c.Id).ToArray();
        // LastMessageAtUtc es el instante del último mensaje persistido.
        var lastMessages = await _db.ConversationMessages.AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId)
                && _db.Conversations.Any(c => c.Id == m.ConversationId && c.LastMessageAtUtc == m.PersistedAtUtc))
            .ToDictionaryAsync(m => m.ConversationId, cancellationToken);

        // Requiere revisión: procesamiento fallido o vencido, o envío incierto.
        var nowUtc = UtcNow;
        var attentionByProcessing = await _db.MessageProcessings.AsNoTracking()
            .Where(p => p.Status == ProcessingStatus.Failed
                || (p.Status == ProcessingStatus.Processing && p.ClaimExpiresAtUtc <= nowUtc))
            .Join(_db.ConversationMessages.AsNoTracking(), p => p.InboundMessageId, m => m.Id, (p, m) => m.ConversationId)
            .Where(conversationId => ids.Contains(conversationId))
            .Distinct()
            .ToListAsync(cancellationToken);
        var attentionBySend = await _db.ConversationMessages.AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId) && m.SendStatus == MessageSendStatus.Unknown)
            .Select(m => m.ConversationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var needsAttention = attentionByProcessing.Concat(attentionBySend).ToHashSet();

        var transportistaIds = conversations.Where(c => c.TransportistaId.HasValue)
            .Select(c => c.TransportistaId!.Value).Distinct().ToArray();
        var transportistas = await _db.Transportistas.AsNoTracking()
            .Where(t => transportistaIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return conversations.Select(c =>
        {
            var recognized = c.TransportistaId is { } transportistaId
                && transportistas.TryGetValue(transportistaId, out var transportista)
                && transportista.IsActive && transportista.IngenioId == c.IngenioId;
            var name = recognized ? transportistas[c.TransportistaId!.Value].Name : null;
            lastMessages.TryGetValue(c.Id, out var last);
            return new ConversationSummary(
                c.Id,
                c.AddressKind,
                ConversationAddressMask.Mask(c.AddressKind, c.RemoteAddress, c.NormalizedPhone),
                new ConversationSender(recognized, recognized ? c.TransportistaId : null, name),
                new DateTimeOffset(c.LastMessageAtUtc),
                last is null ? null : Preview(last.Text),
                last?.Direction,
                needsAttention.Contains(c.Id));
        }).ToArray();
    }

    private async Task<Transportista?> FindRecognizedTransportistaAsync(
        Guid ingenioId, Conversation conversation, CancellationToken cancellationToken)
    {
        // Sólo una dirección telefónica deriva un número; un LID nunca.
        if (conversation.AddressKind != ConversationAddressKind.Phone || conversation.NormalizedPhone is null)
            return null;
        var phone = conversation.NormalizedPhone;
        return await _db.Transportistas.AsNoTracking().FirstOrDefaultAsync(
            t => t.IngenioId == ingenioId && t.NormalizedWhatsApp == phone && t.IsActive, cancellationToken);
    }

    private async Task<T> RetryOnUniqueViolationAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateException exception) when (SqliteConstraintErrors.IsUniqueOrPrimaryKeyViolation(exception))
        {
            // La transacción ya se revirtió al liberarse; se descarta el estado
            // rastreado y se relee lo que persistió el otro request.
            _db.ChangeTracker.Clear();
            return await operation();
        }
    }

    private static bool ClaimTokenMatches(MessageProcessing processing, string claimTokenHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(processing.ClaimTokenHash),
            Encoding.ASCII.GetBytes(claimTokenHash));

    private static ProcessingSnapshot ToSnapshot(MessageProcessing processing, DateTime nowUtc) =>
        new(ToState(processing, nowUtc), ClaimToken: null, new DateTimeOffset(processing.ClaimExpiresAtUtc));

    private static ProcessingState ToState(MessageProcessing processing, DateTime nowUtc) => processing.Status switch
    {
        ProcessingStatus.Completed => ProcessingState.Completed,
        ProcessingStatus.Failed => ProcessingState.Failed,
        _ => processing.IsExpired(nowUtc) ? ProcessingState.Expired : ProcessingState.Processing
    };

    private static ReplyIntent ToReplyIntent(
        ConversationMessage reply, Conversation conversation, ChannelInstance instance, bool created) => new(
        reply.Id,
        conversation.Id,
        reply.InReplyToMessageId!.Value,
        instance.InstanceName,
        conversation.RemoteAddress,
        reply.Text,
        reply.SendStatus!.Value,
        reply.ProviderMessageId,
        created);

    private static string Preview(string text) =>
        text.Length <= PreviewLength ? text : string.Concat(text.AsSpan(0, PreviewLength - 1), "…");

    private static int CompareIds(Guid left, Guid right) =>
        string.CompareOrdinal(left.ToString("D"), right.ToString("D"));

    private static ConversationServiceException MessageNotFound() =>
        new(ConversationErrorCodes.MessageNotFound, "Mensaje entrante inexistente.");

    private static ConversationServiceException ClaimInvalid() =>
        new(ConversationErrorCodes.ProcessingClaimInvalid, "Autorización de procesamiento inválida o vencida.");

    private static ConversationServiceException InvalidTransition() =>
        new(ConversationErrorCodes.InvalidSendStatusTransition, "Transición de estado de envío inválida.");
}
