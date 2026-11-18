using System.Text.RegularExpressions;

namespace AgroFlow.Domain.Entities;

public enum ConversationAddressKind
{
    Phone,
    Lid,
    Other
}

/// <summary>
/// Agrupa los mensajes de una dirección remota dentro de una instancia del
/// canal. La dirección no prueba identidad: el transportista sólo se vincula
/// cuando su WhatsApp coincide con un teléfono derivado de la dirección.
/// </summary>
public sealed class Conversation : EntityBase
{
    private Conversation()
    {
    }

    public Conversation(Guid ingenioId, Guid channelInstanceId, string remoteAddress, DateTime createdAtUtc)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        if (channelInstanceId == Guid.Empty)
            throw new ArgumentException("Channel instance identifier is required.", nameof(channelInstanceId));
        MasterDataValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        var address = WhatsAppAddress.Parse(remoteAddress);
        ChannelInstanceId = channelInstanceId;
        RemoteAddress = address.Value;
        AddressKind = address.Kind;
        NormalizedPhone = address.NormalizedPhone;
        CreatedAtUtc = createdAtUtc;
        LastMessageAtUtc = createdAtUtc;
    }

    public Guid IngenioId { get; private set; }
    public Guid ChannelInstanceId { get; private set; }
    public string RemoteAddress { get; private set; } = string.Empty;
    public ConversationAddressKind AddressKind { get; private set; }

    /// <summary>Solo dígitos; existe únicamente para direcciones telefónicas.</summary>
    public string? NormalizedPhone { get; private set; }

    public Guid? TransportistaId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastMessageAtUtc { get; private set; }

    /// <summary>
    /// Devuelve el instante de persistencia del próximo mensaje y lo registra
    /// como última actividad. Es estrictamente creciente dentro de la
    /// conversación, así el orden de lectura nunca depende de empates de reloj.
    /// </summary>
    public DateTime NextMessageTimestamp(DateTime nowUtc)
    {
        MasterDataValidation.EnsureUtc(nowUtc, nameof(nowUtc));
        var next = nowUtc > LastMessageAtUtc ? nowUtc : LastMessageAtUtc.AddTicks(1);
        LastMessageAtUtc = next;
        return next;
    }

    public void LinkTransportista(Transportista transportista)
    {
        ArgumentNullException.ThrowIfNull(transportista);
        if (transportista.IngenioId != IngenioId)
            throw new ArgumentException("Transportista must belong to the conversation ingenio.", nameof(transportista));
        if (AddressKind != ConversationAddressKind.Phone || transportista.NormalizedWhatsApp != NormalizedPhone)
            throw new ArgumentException("Transportista WhatsApp does not match the conversation phone.", nameof(transportista));
        TransportistaId = transportista.Id;
    }
}

public sealed record WhatsAppAddress(string Value, ConversationAddressKind Kind, string? NormalizedPhone)
{
    public const int MaxLength = 100;

    private static readonly Regex PhoneJid = new(
        @"^(?<digits>[1-9][0-9]{7,14})@s\.whatsapp\.net\z",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly Regex LidJid = new(
        @"^[0-9]+@lid\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly Regex AllowedCharacters = new(
        @"^[A-Za-z0-9._:\-]+@[A-Za-z0-9.\-]+\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool IsGroup(string value) =>
        value.Trim().EndsWith("@g.us", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Clasifica una dirección de WhatsApp. Un LID u otra dirección nunca se
    /// convierte en teléfono; los grupos se rechazan porque el canal es 1:1.
    /// </summary>
    public static WhatsAppAddress Parse(string value)
    {
        var trimmed = MasterDataValidation.RequireText(value, nameof(value));
        if (trimmed.Length > MaxLength || !AllowedCharacters.IsMatch(trimmed))
            throw new ArgumentException("Remote address is not a valid WhatsApp address.", nameof(value));
        if (IsGroup(trimmed))
            throw new ArgumentException("Group addresses are not supported.", nameof(value));

        var normalized = trimmed.ToLowerInvariant();
        var phone = PhoneJid.Match(normalized);
        if (phone.Success)
            return new WhatsAppAddress(normalized, ConversationAddressKind.Phone, phone.Groups["digits"].Value);
        return LidJid.IsMatch(normalized)
            ? new WhatsAppAddress(normalized, ConversationAddressKind.Lid, null)
            : new WhatsAppAddress(normalized, ConversationAddressKind.Other, null);
    }
}
