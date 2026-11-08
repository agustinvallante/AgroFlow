using AgroFlow.Domain.Entities;

namespace AgroFlow.Application.Conversations;

/// <summary>
/// Representación de una dirección remota apta para listados: los listados del
/// dashboard nunca devuelven la dirección completa (minimización, OD-009).
/// </summary>
public static class ConversationAddressMask
{
    private const int VisibleSuffix = 4;

    public static string Mask(ConversationAddressKind kind, string remoteAddress, string? normalizedPhone)
    {
        if (kind == ConversationAddressKind.Phone && normalizedPhone is { Length: > VisibleSuffix } phone)
            return $"+{phone[..2]}{new string('•', phone.Length - 2 - VisibleSuffix)}{phone[^VisibleSuffix..]}";

        var separator = remoteAddress.IndexOf('@');
        var local = separator > 0 ? remoteAddress[..separator] : remoteAddress;
        var domain = separator > 0 ? remoteAddress[separator..] : string.Empty;
        var suffix = local.Length > VisibleSuffix ? local[^VisibleSuffix..] : string.Empty;
        return $"••••{suffix}{domain}";
    }
}
