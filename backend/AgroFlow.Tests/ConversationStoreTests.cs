using AgroFlow.Application.Conversations;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// <see cref="ConversationService"/> sobre <see cref="EfConversationStore"/> y un
/// SQLite migrado en archivo (como <see cref="EfAppointmentStoreTests"/>), para
/// probar índices, checks y carreras reales. Dos ingenios y datos ficticios.
/// </summary>
public sealed class ConversationStoreTests : IDisposable
{
    private const string InstanceA = "fixture-instance-A";
    private const string InstanceB = "fixture-instance-B";
    private const string RegisteredJid = "5493815550101@s.whatsapp.net";
    private const string UnregisteredJid = "5493815550199@s.whatsapp.net";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-conversations-{Guid.NewGuid():N}.db");
    private readonly MutableClock _clock = new(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private Guid _ingenioA;
    private Guid _ingenioB;
    private Guid _transportistaA;

    public ConversationStoreTests()
    {
        using var db = CreateContext();
        db.Database.Migrate();
        var ingenioA = new Ingenio("Ingenio Fixture A", "America/Argentina/Tucuman");
        var ingenioB = new Ingenio("Ingenio Fixture B", "America/Argentina/Tucuman");
        var transportista = new Transportista(ingenioA.Id, "Transportista Fixture", "20111111111", "+5493815550101");
        db.Ingenios.AddRange(ingenioA, ingenioB);
        db.Transportistas.Add(transportista);
        db.ChannelInstances.AddRange(
            new ChannelInstance(ingenioA.Id, ChannelInstance.ProviderEvolution, InstanceA),
            new ChannelInstance(ingenioB.Id, ChannelInstance.ProviderEvolution, InstanceB));
        db.SaveChanges();
        _ingenioA = ingenioA.Id;
        _ingenioB = ingenioB.Id;
        _transportistaA = transportista.Id;
    }

    [Fact]
    public async Task First_message_creates_conversation_message_and_a_single_hashed_authorization()
    {
        await using var db = CreateContext();

        var registration = await Service(db).IngestAsync(Inbound("fixture-in-001"));

        Assert.Equal(InboundDisposition.Process, registration.Disposition);
        Assert.True(registration.Created);
        Assert.Equal(ProcessingState.Processing, registration.Processing.State);
        Assert.False(string.IsNullOrEmpty(registration.Processing.ClaimToken));
        Assert.True(registration.Sender.Recognized);
        Assert.Equal(_transportistaA, registration.Sender.TransportistaId);

        await using var verify = CreateContext();
        var conversation = await verify.Conversations.SingleAsync();
        Assert.Equal(_ingenioA, conversation.IngenioId);
        Assert.Equal(_transportistaA, conversation.TransportistaId);
        var processing = await verify.MessageProcessings.SingleAsync();
        Assert.Equal(registration.MessageId, processing.InboundMessageId);
        Assert.Equal(ConversationService.HashClaimToken(registration.Processing.ClaimToken!), processing.ClaimTokenHash);
        Assert.DoesNotContain(registration.Processing.ClaimToken!, processing.ClaimTokenHash);
    }

    [Fact]
    public async Task Identical_repeat_stops_with_original_ids_and_without_a_new_authorization()
    {
        await using var db = CreateContext();
        var first = await Service(db).IngestAsync(Inbound("fixture-in-001"));

        var repeat = await Service(db).IngestAsync(Inbound("fixture-in-001"));

        Assert.Equal(InboundDisposition.DuplicateStop, repeat.Disposition);
        Assert.False(repeat.Created);
        Assert.Null(repeat.Processing.ClaimToken);
        Assert.Equal(first.ConversationId, repeat.ConversationId);
        Assert.Equal(first.MessageId, repeat.MessageId);
        await using var verify = CreateContext();
        Assert.Equal(1, await verify.ConversationMessages.CountAsync());
        Assert.Equal(1, await verify.MessageProcessings.CountAsync());
    }

    [Fact]
    public async Task Repeat_after_expiry_still_stops_and_the_conversation_needs_attention()
    {
        await using var db = CreateContext();
        await Service(db).IngestAsync(Inbound("fixture-in-001"));
        _clock.Advance(TimeSpan.FromMinutes(6));

        var repeat = await Service(db).IngestAsync(Inbound("fixture-in-001"));

        Assert.Equal(InboundDisposition.DuplicateStop, repeat.Disposition);
        Assert.Equal(ProcessingState.Expired, repeat.Processing.State);
        Assert.Null(repeat.Processing.ClaimToken);
        var summary = Assert.Single((await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(null, 20))).Items);
        Assert.True(summary.NeedsAttention);
    }

    [Theory]
    [InlineData("[SINTÉTICO] otro texto", null)]
    [InlineData("[SINTÉTICO] mensaje de prueba", "2030-01-01T11:59:00Z")]
    public async Task Same_key_with_different_content_conflicts_and_keeps_the_original(string text, string? timestamp)
    {
        await using var db = CreateContext();
        await Service(db).IngestAsync(Inbound("fixture-in-001"));

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).IngestAsync(
            Inbound("fixture-in-001", text: text,
                timestamp: timestamp is null ? DefaultTimestamp : DateTime.Parse(timestamp).ToUniversalTime())));

        Assert.Equal(ConversationErrorCodes.ConversationEventConflict, exception.Code);
        await using var verify = CreateContext();
        Assert.Equal("[SINTÉTICO] mensaje de prueba", (await verify.ConversationMessages.SingleAsync()).Text);
    }

    [Fact]
    public async Task Same_provider_id_on_another_instance_is_another_message_in_that_ingenio()
    {
        await using var db = CreateContext();
        var first = await Service(db).IngestAsync(Inbound("fixture-in-001"));

        var other = await Service(db).IngestAsync(Inbound("fixture-in-001", instance: InstanceB));

        Assert.Equal(InboundDisposition.Process, other.Disposition);
        Assert.NotEqual(first.ConversationId, other.ConversationId);
        // El transportista pertenece al ingenio A: nunca se reconoce desde B.
        Assert.False(other.Sender.Recognized);
        await using var verify = CreateContext();
        Assert.Equal(_ingenioB, (await verify.Conversations.SingleAsync(c => c.Id == other.ConversationId)).IngenioId);
    }

    [Fact]
    public async Task Concurrent_repeats_persist_one_row_and_authorize_exactly_one_caller()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = CreateContext();
            return await Service(db).IngestAsync(Inbound("fixture-in-race"));
        }));

        Assert.Single(results, r => r.Disposition == InboundDisposition.Process);
        Assert.Single(results.Select(r => r.MessageId).Distinct());
        Assert.Single(results, r => r.Processing.ClaimToken is not null);
        await using var verify = CreateContext();
        Assert.Equal(1, await verify.Conversations.CountAsync());
        Assert.Equal(1, await verify.ConversationMessages.CountAsync());
        Assert.Equal(1, await verify.MessageProcessings.CountAsync());
    }

    [Fact]
    public async Task Concurrent_new_messages_from_one_address_share_a_conversation_with_distinct_order()
    {
        await Task.WhenAll(Enumerable.Range(0, 5).Select(async i =>
        {
            await using var db = CreateContext();
            await Service(db).IngestAsync(Inbound($"fixture-in-{i}"));
        }));

        await using var verify = CreateContext();
        Assert.Equal(1, await verify.Conversations.CountAsync());
        var timestamps = await verify.ConversationMessages.Select(m => m.PersistedAtUtc).ToListAsync();
        Assert.Equal(5, timestamps.Distinct().Count());
        Assert.Equal(timestamps.Max(), (await verify.Conversations.SingleAsync()).LastMessageAtUtc);
    }

    [Fact]
    public async Task Lid_and_unregistered_numbers_are_recorded_but_not_recognized()
    {
        await using var db = CreateContext();

        var lid = await Service(db).IngestAsync(Inbound("fixture-in-001", address: "5493815550101@lid"));
        var unknown = await Service(db).IngestAsync(Inbound("fixture-in-002", address: UnregisteredJid));

        Assert.False(lid.Sender.Recognized);
        Assert.Null(lid.Sender.TransportistaId);
        Assert.False(unknown.Sender.Recognized);
        await using var verify = CreateContext();
        Assert.Null((await verify.Conversations.SingleAsync(c => c.Id == lid.ConversationId)).NormalizedPhone);
        Assert.All(await verify.Conversations.ToListAsync(), c => Assert.Null(c.TransportistaId));
    }

    [Fact]
    public async Task A_number_registered_later_links_on_the_next_message_without_rewriting_history()
    {
        await using var db = CreateContext();
        var before = await Service(db).IngestAsync(Inbound("fixture-in-001", address: UnregisteredJid));
        db.Transportistas.Add(new Transportista(_ingenioA, "Alta Posterior", "20999999999", "+5493815550199"));
        await db.SaveChangesAsync();

        var after = await Service(db).IngestAsync(Inbound("fixture-in-002", address: UnregisteredJid));

        Assert.False(before.Sender.Recognized);
        Assert.True(after.Sender.Recognized);
        Assert.Equal(before.ConversationId, after.ConversationId);
        await using var verify = CreateContext();
        Assert.Equal(2, await verify.ConversationMessages.CountAsync(m => m.ConversationId == after.ConversationId));
    }

    [Fact]
    public async Task Inactivated_transportista_keeps_the_historical_link_but_is_no_longer_recognized()
    {
        await using var db = CreateContext();
        var first = await Service(db).IngestAsync(Inbound("fixture-in-001"));
        var transportista = await db.Transportistas.SingleAsync(t => t.Id == _transportistaA);
        transportista.Inactivate(_clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();

        var next = await Service(db).IngestAsync(Inbound("fixture-in-002"));

        Assert.True(first.Sender.Recognized);
        Assert.False(next.Sender.Recognized);
        await using var verify = CreateContext();
        Assert.Equal(_transportistaA, (await verify.Conversations.SingleAsync()).TransportistaId);
        var summary = Assert.Single((await Service(verify).ListConversationsAsync(_ingenioA, new PageRequest(null, 20))).Items);
        Assert.False(summary.Sender.Recognized);
        Assert.Null(summary.Sender.Name);
    }

    [Theory]
    [InlineData("fixture-unknown-instance")]
    [InlineData(InstanceA)]
    public async Task Unknown_or_inactive_instances_are_not_allowed(string instance)
    {
        await using var db = CreateContext();
        if (instance == InstanceA)
        {
            (await db.ChannelInstances.SingleAsync(c => c.InstanceName == InstanceA)).Inactivate();
            await db.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(
            () => Service(db).IngestAsync(Inbound("fixture-in-001", instance: instance)));

        Assert.Equal(ConversationErrorCodes.ChannelInstanceNotAllowed, exception.Code);
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
    }

    [Theory]
    [InlineData("120363000000000000@g.us", "remoteAddress")]
    [InlineData("", "remoteAddress")]
    public async Task Invalid_events_are_rejected_without_persisting(string address, string field)
    {
        await using var db = CreateContext();

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(
            () => Service(db).IngestAsync(Inbound("fixture-in-001", address: address)));

        Assert.Equal(ConversationErrorCodes.ValidationError, exception.Code);
        Assert.Contains(field, exception.Errors!.Keys);
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
    }

    [Fact]
    public async Task Event_validation_reports_each_invalid_field_without_echoing_values()
    {
        await using var db = CreateContext();
        var command = new RegisterInboundCommand(
            "other", "", new string('x', 129), RegisteredJid,
            _clock.GetUtcNow().UtcDateTime.AddDays(2), new string('x', ConversationMessage.MaxTextLength + 1));

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).IngestAsync(command));

        Assert.Equal(
            new[] { "instance", "provider", "providerMessageId", "providerTimestamp", "text" },
            exception.Errors!.Keys.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(exception.Errors.Values.SelectMany(v => v), message => message.Contains("xxxx"));
    }

    [Fact]
    public async Task Reply_intent_requires_the_holder_token_and_is_unique_per_inbound_message()
    {
        await using var db = CreateContext();
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-001"));
        var token = inbound.Processing.ClaimToken!;

        var wrongToken = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, "not-the-token", "[SINTÉTICO] respuesta")));
        var intent = await Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, token, "[SINTÉTICO] respuesta"));
        var replay = await Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, token, "[SINTÉTICO] respuesta"));
        var different = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, token, "[SINTÉTICO] otra respuesta")));

        Assert.Equal(ConversationErrorCodes.ProcessingClaimInvalid, wrongToken.Code);
        Assert.True(intent.Created);
        Assert.Equal(MessageSendStatus.PendingSend, intent.SendStatus);
        Assert.Equal(InstanceA, intent.Instance);
        Assert.Equal(RegisteredJid, intent.RecipientAddress);
        Assert.False(replay.Created);
        Assert.Equal(intent.ReplyId, replay.ReplyId);
        Assert.Equal(ConversationErrorCodes.ReplyIntentConflict, different.Code);
        await using var verify = CreateContext();
        Assert.Equal(1, await verify.ConversationMessages.CountAsync(m => m.Direction == MessageDirection.Outbound));
    }

    [Fact]
    public async Task Expired_authorization_cannot_register_a_reply()
    {
        await using var db = CreateContext();
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-001"));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, inbound.Processing.ClaimToken!, "[SINTÉTICO] respuesta")));

        Assert.Equal(ConversationErrorCodes.ProcessingClaimInvalid, exception.Code);
        Assert.Equal(0, await db.ConversationMessages.CountAsync(m => m.Direction == MessageDirection.Outbound));
    }

    [Fact]
    public async Task Accepted_send_status_completes_processing_and_rejects_later_failure()
    {
        await using var db = CreateContext();
        var (inbound, intent) = await IngestAndReplyAsync(db);

        var accepted = await Service(db).UpdateSendStatusAsync(
            new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.Accepted, "fixture-out-001"));
        var repeated = await Service(db).UpdateSendStatusAsync(
            new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.Accepted, null));
        var invalid = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).UpdateSendStatusAsync(
            new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.Failed, null)));

        Assert.Equal(MessageSendStatus.Accepted, accepted.SendStatus);
        Assert.Equal("fixture-out-001", accepted.ProviderMessageId);
        Assert.Equal(MessageSendStatus.Accepted, repeated.SendStatus);
        Assert.Equal(ConversationErrorCodes.InvalidSendStatusTransition, invalid.Code);
        await using var verify = CreateContext();
        var processing = await verify.MessageProcessings.SingleAsync(p => p.InboundMessageId == inbound.MessageId);
        Assert.Equal(ProcessingStatus.Completed, processing.Status);
        Assert.Equal(MessageSendStatus.Accepted, (await verify.ConversationMessages.SingleAsync(m => m.Id == intent.ReplyId)).SendStatus);
    }

    [Fact]
    public async Task Unknown_send_result_needs_attention_until_it_is_resolved()
    {
        await using var db = CreateContext();
        var (_, intent) = await IngestAndReplyAsync(db);

        await Service(db).UpdateSendStatusAsync(new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.Unknown, null));
        var uncertain = Assert.Single((await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(null, 20))).Items);
        await Service(db).UpdateSendStatusAsync(new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.Accepted, "fixture-out-001"));
        var resolved = Assert.Single((await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(null, 20))).Items);

        Assert.True(uncertain.NeedsAttention);
        Assert.False(resolved.NeedsAttention);
    }

    [Fact]
    public async Task Pending_send_cannot_be_reported_as_a_result()
    {
        await using var db = CreateContext();
        var (_, intent) = await IngestAndReplyAsync(db);

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).UpdateSendStatusAsync(
            new UpdateSendStatusCommand(intent.ReplyId, MessageSendStatus.PendingSend, null)));

        Assert.Equal(ConversationErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task Processing_failure_uses_the_closed_catalog_and_closes_the_authorization()
    {
        await using var db = CreateContext();
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-001"));
        var token = inbound.Processing.ClaimToken!;

        var invalidCode = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).ReportProcessingFailureAsync(
            new ReportProcessingFailureCommand(inbound.MessageId, token, "texto libre")));
        var failed = await Service(db).ReportProcessingFailureAsync(
            new ReportProcessingFailureCommand(inbound.MessageId, token, "AGENT_ERROR"));
        var repeated = await Service(db).ReportProcessingFailureAsync(
            new ReportProcessingFailureCommand(inbound.MessageId, token, "AGENT_ERROR"));
        var reply = await Assert.ThrowsAsync<ConversationServiceException>(() => Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, token, "[SINTÉTICO] respuesta")));

        Assert.Equal(ConversationErrorCodes.ValidationError, invalidCode.Code);
        Assert.Equal(ProcessingState.Failed, failed.State);
        Assert.Equal("AGENT_ERROR", repeated.ErrorCode);
        Assert.Equal(ConversationErrorCodes.ProcessingClaimInvalid, reply.Code);
        Assert.True(Assert.Single((await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(null, 20))).Items).NeedsAttention);
    }

    [Fact]
    public async Task Message_pages_never_repeat_or_skip_rows_while_messages_arrive()
    {
        await using var db = CreateContext();
        var first = await Service(db).IngestAsync(Inbound("fixture-in-0"));
        for (var i = 1; i < 5; i++)
            await Service(db).IngestAsync(Inbound($"fixture-in-{i}"));

        var seen = new List<Guid>();
        PageCursor? cursor = null;
        var arrivals = 0;
        do
        {
            var page = await Service(db).ListMessagesAsync(_ingenioA, first.ConversationId, new PageRequest(cursor, 2));
            seen.AddRange(page.Items.Select(m => m.Id));
            cursor = page.Next;
            if (arrivals++ < 2)
                await Service(db).IngestAsync(Inbound($"fixture-in-late-{arrivals}"));
        } while (cursor is not null);

        await using var verify = CreateContext();
        var expected = await verify.ConversationMessages.OrderBy(m => m.PersistedAtUtc).Select(m => m.Id).ToListAsync();
        Assert.Equal(expected, seen);
        Assert.Equal(7, seen.Count);
    }

    [Fact]
    public async Task Manipulated_message_cursor_is_a_validation_error()
    {
        await using var db = CreateContext();
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-0"));
        var forged = new PageCursor(_clock.GetUtcNow().UtcDateTime, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ConversationServiceException>(() =>
            Service(db).ListMessagesAsync(_ingenioA, inbound.ConversationId, new PageRequest(forged, 2)));

        Assert.Equal(ConversationErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task Listing_is_scoped_to_the_ingenio_and_ordered_by_latest_activity()
    {
        await using var db = CreateContext();
        var older = await Service(db).IngestAsync(Inbound("fixture-in-1", address: UnregisteredJid));
        _clock.Advance(TimeSpan.FromSeconds(1));
        var newer = await Service(db).IngestAsync(Inbound("fixture-in-2"));
        var foreign = await Service(db).IngestAsync(Inbound("fixture-in-3", instance: InstanceB));

        var page = await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(null, 20));
        var notFound = await Assert.ThrowsAsync<ConversationServiceException>(
            () => Service(db).GetConversationAsync(_ingenioA, foreign.ConversationId));
        var foreignMessages = await Assert.ThrowsAsync<ConversationServiceException>(
            () => Service(db).ListMessagesAsync(_ingenioA, foreign.ConversationId, new PageRequest(null, 20)));

        Assert.Equal(new[] { newer.ConversationId, older.ConversationId }, page.Items.Select(c => c.Id));
        Assert.Null(page.Next);
        var recognized = page.Items[0];
        Assert.True(recognized.Sender.Recognized);
        Assert.Equal("Transportista Fixture", recognized.Sender.Name);
        Assert.Equal("+54•••••••0101", recognized.RemoteAddressMasked);
        Assert.Equal("[SINTÉTICO] mensaje de prueba", recognized.LastMessagePreview);
        Assert.Equal(ConversationErrorCodes.ConversationNotFound, notFound.Code);
        Assert.Equal(ConversationErrorCodes.ConversationNotFound, foreignMessages.Code);
    }

    [Fact]
    public async Task Conversation_pages_break_activity_ties_without_repeating_or_skipping()
    {
        await using var db = CreateContext();
        // Con el reloj detenido, las tres conversaciones empatan en última actividad.
        var ids = new List<Guid>();
        foreach (var suffix in new[] { "11", "12", "13" })
            ids.Add((await Service(db).IngestAsync(Inbound("fixture-in-1", address: $"54938155501{suffix}@s.whatsapp.net"))).ConversationId);

        var seen = new List<Guid>();
        PageCursor? cursor = null;
        do
        {
            var page = await Service(db).ListConversationsAsync(_ingenioA, new PageRequest(cursor, 1));
            seen.AddRange(page.Items.Select(c => c.Id));
            cursor = page.Next;
        } while (cursor is not null);

        Assert.Equal(ids.Order(), seen.Order());
        Assert.Equal(3, seen.Distinct().Count());
    }

    [Fact]
    public async Task Database_checks_reject_an_outbound_row_without_its_inbound_message()
    {
        await using var db = CreateContext();
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-1"));

        var exception = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO ConversationMessages (Id, IngenioId, ConversationId, Direction, ContentType, Text, PersistedAtUtc, SendStatus) " +
            "VALUES ({0}, {1}, {2}, 'Outbound', 'TEXT', 'x', '2031-01-01 00:00:00', 'PendingSend')",
            Guid.NewGuid().ToString().ToUpperInvariant(), _ingenioA.ToString().ToUpperInvariant(),
            inbound.ConversationId.ToString().ToUpperInvariant()));

        Assert.Contains("CK_ConversationMessages_Direction_Shape", exception.Message);
    }

    private static readonly DateTime DefaultTimestamp = new(2030, 1, 1, 11, 58, 0, DateTimeKind.Utc);

    private static RegisterInboundCommand Inbound(
        string providerMessageId,
        string instance = InstanceA,
        string address = RegisteredJid,
        string text = "[SINTÉTICO] mensaje de prueba",
        DateTime? timestamp = null) =>
        new("evolution", instance, providerMessageId, address, timestamp ?? DefaultTimestamp, text);

    private async Task<(InboundRegistration Inbound, ReplyIntent Intent)> IngestAndReplyAsync(AgroFlowDbContext db)
    {
        var inbound = await Service(db).IngestAsync(Inbound("fixture-in-001"));
        var intent = await Service(db).CreateReplyIntentAsync(
            new CreateReplyIntentCommand(inbound.MessageId, inbound.Processing.ClaimToken!, "[SINTÉTICO] respuesta"));
        return (inbound, intent);
    }

    private ConversationService Service(AgroFlowDbContext db) => new(new EfConversationStore(db, _clock), _clock);

    private AgroFlowDbContext CreateContext()
    {
        // Sin Migrate: el constructor ya migró y las tareas paralelas no deben
        // competir por el lock de migraciones.
        return new AgroFlowDbContext(new DbContextOptionsBuilder<AgroFlowDbContext>()
            .UseSqlite($"Data Source={_dbPath};Default Timeout=10")
            .Options);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            var path = _dbPath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
