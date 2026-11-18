using AgroFlow.Application.Conversations;
using AgroFlow.Domain.Entities;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>Invariantes de las entidades del canal, sin persistencia. Datos ficticios.</summary>
public sealed class ConversationDomainTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Phone_address_derives_digits_only_from_a_phone_jid()
    {
        var address = WhatsAppAddress.Parse(" 5493815550101@S.WhatsApp.net ");

        Assert.Equal("5493815550101@s.whatsapp.net", address.Value);
        Assert.Equal(ConversationAddressKind.Phone, address.Kind);
        Assert.Equal("5493815550101", address.NormalizedPhone);
    }

    [Theory]
    [InlineData("123456789012345@lid", ConversationAddressKind.Lid)]
    [InlineData("status@broadcast", ConversationAddressKind.Other)]
    [InlineData("0123456789@s.whatsapp.net", ConversationAddressKind.Other)]
    public void Non_phone_addresses_never_derive_a_phone(string value, ConversationAddressKind kind)
    {
        var address = WhatsAppAddress.Parse(value);

        Assert.Equal(kind, address.Kind);
        Assert.Null(address.NormalizedPhone);
    }

    [Theory]
    [InlineData("120363000000000000@g.us")]
    [InlineData("not-an-address")]
    [InlineData("5493815550101@s.whatsapp.net; DROP")]
    [InlineData("")]
    public void Group_and_malformed_addresses_are_rejected(string value) =>
        Assert.Throws<ArgumentException>(() => WhatsAppAddress.Parse(value));

    [Fact]
    public void Message_timestamps_are_strictly_increasing_even_with_clock_ties_or_regressions()
    {
        var conversation = new Conversation(Guid.NewGuid(), Guid.NewGuid(), "5493815550101@s.whatsapp.net", Now);

        var first = conversation.NextMessageTimestamp(Now);
        var tie = conversation.NextMessageTimestamp(Now);
        var regression = conversation.NextMessageTimestamp(Now.AddSeconds(-30));
        var later = conversation.NextMessageTimestamp(Now.AddMinutes(1));

        Assert.True(first < tie && tie < regression && regression < later);
        Assert.Equal(later, conversation.LastMessageAtUtc);
    }

    [Fact]
    public void Transportista_links_only_within_the_same_ingenio_and_matching_phone()
    {
        var ingenioId = Guid.NewGuid();
        var conversation = new Conversation(ingenioId, Guid.NewGuid(), "5493815550101@s.whatsapp.net", Now);
        var foreign = new Transportista(Guid.NewGuid(), "Ajeno", "1", "+5493815550101");
        var otherPhone = new Transportista(ingenioId, "Otro", "2", "+5493815550199");
        var match = new Transportista(ingenioId, "Propio", "3", "+5493815550101");

        Assert.Throws<ArgumentException>(() => conversation.LinkTransportista(foreign));
        Assert.Throws<ArgumentException>(() => conversation.LinkTransportista(otherPhone));
        conversation.LinkTransportista(match);
        Assert.Equal(match.Id, conversation.TransportistaId);
    }

    [Fact]
    public void Lid_conversation_never_links_a_transportista()
    {
        var ingenioId = Guid.NewGuid();
        var conversation = new Conversation(ingenioId, Guid.NewGuid(), "5493815550101@lid", Now);

        Assert.Throws<ArgumentException>(() =>
            conversation.LinkTransportista(new Transportista(ingenioId, "Propio", "3", "+5493815550101")));
    }

    [Theory]
    [InlineData(MessageSendStatus.Accepted)]
    [InlineData(MessageSendStatus.Failed)]
    [InlineData(MessageSendStatus.Unknown)]
    public void Pending_reply_accepts_any_reported_result(MessageSendStatus next)
    {
        var reply = NewReply();

        Assert.Equal(SendStatusChange.Applied, reply.ApplySendStatus(next, null, Now.AddSeconds(1)));
        Assert.Equal(next, reply.SendStatus);
    }

    [Fact]
    public void Unknown_result_can_be_resolved_but_final_results_cannot_change()
    {
        var reply = NewReply();
        reply.ApplySendStatus(MessageSendStatus.Unknown, null, Now.AddSeconds(1));

        Assert.Equal(SendStatusChange.Applied, reply.ApplySendStatus(MessageSendStatus.Accepted, "fixture-out-1", Now.AddSeconds(2)));
        Assert.Equal("fixture-out-1", reply.ProviderMessageId);
        Assert.Equal(SendStatusChange.Unchanged, reply.ApplySendStatus(MessageSendStatus.Accepted, null, Now.AddSeconds(3)));
        Assert.Equal(SendStatusChange.Invalid, reply.ApplySendStatus(MessageSendStatus.Failed, null, Now.AddSeconds(4)));
        Assert.Equal(SendStatusChange.Invalid, reply.ApplySendStatus(MessageSendStatus.Accepted, "fixture-out-2", Now.AddSeconds(5)));
        Assert.Equal(MessageSendStatus.Accepted, reply.SendStatus);
    }

    [Fact]
    public void Provider_identifier_is_only_evidence_of_acceptance()
    {
        var reply = NewReply();

        Assert.Equal(SendStatusChange.Invalid, reply.ApplySendStatus(MessageSendStatus.Failed, "fixture-out-1", Now));
        Assert.Equal(MessageSendStatus.PendingSend, reply.SendStatus);
    }

    [Fact]
    public void Inbound_message_rejects_empty_or_oversized_text()
    {
        Assert.Throws<ArgumentException>(() => NewInbound("   "));
        Assert.Throws<ArgumentException>(() => NewInbound(new string('x', ConversationMessage.MaxTextLength + 1)));
        Assert.Equal(ConversationMessage.MaxTextLength, NewInbound(new string('x', ConversationMessage.MaxTextLength)).Text.Length);
    }

    [Fact]
    public void Processing_claim_expiry_is_informative_and_closing_requires_an_open_claim()
    {
        var processing = new MessageProcessing(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Now.AddMinutes(5));

        Assert.True(processing.CanBeClosedBy(Now.AddMinutes(4)));
        Assert.True(processing.IsExpired(Now.AddMinutes(5)));
        Assert.False(processing.CanBeClosedBy(Now.AddMinutes(5)));
        Assert.Throws<ArgumentException>(() => processing.Fail("free text", Now));

        processing.Fail("AGENT_ERROR", Now.AddMinutes(1));
        Assert.Equal(ProcessingStatus.Failed, processing.Status);
        Assert.False(processing.IsExpired(Now.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() => processing.Complete(Now.AddMinutes(2)));
    }

    [Theory]
    [InlineData(ConversationAddressKind.Phone, "5493815550101@s.whatsapp.net", "5493815550101", "+54•••••••0101")]
    [InlineData(ConversationAddressKind.Lid, "123456789012345@lid", null, "••••2345@lid")]
    [InlineData(ConversationAddressKind.Other, "ab@broadcast", null, "••••@broadcast")]
    public void Listing_mask_never_returns_the_full_address(
        ConversationAddressKind kind, string address, string? phone, string expected)
    {
        var masked = ConversationAddressMask.Mask(kind, address, phone);

        Assert.Equal(expected, masked);
        Assert.DoesNotContain(phone ?? address, masked);
    }

    private static ConversationMessage NewReply() =>
        ConversationMessage.OutboundIntent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "[SINTÉTICO] respuesta", Now);

    private static ConversationMessage NewInbound(string text) =>
        ConversationMessage.Inbound(Guid.NewGuid(), Guid.NewGuid(), "fixture-in-001", text, Now, Now);
}
