namespace AgroFlow.Domain.Entities;

/// <summary>
/// Instancia habilitada de un proveedor de WhatsApp. Es la única fuente del
/// ingenio de un evento del canal: el cuerpo de la solicitud nunca lo decide.
/// </summary>
public sealed class ChannelInstance : EntityBase
{
    public const string ProviderEvolution = "EVOLUTION";
    public const int MaxInstanceNameLength = 100;

    private ChannelInstance()
    {
    }

    public ChannelInstance(Guid ingenioId, string provider, string instanceName)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        Provider = NormalizeProvider(provider);
        InstanceName = NormalizeInstanceName(instanceName);
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string InstanceName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    public static string NormalizeProvider(string value) =>
        MasterDataValidation.NormalizeUpperInvariant(value, nameof(value));

    public static string NormalizeInstanceName(string value)
    {
        var normalized = MasterDataValidation.RequireText(value, nameof(value));
        return normalized.Length > MaxInstanceNameLength
            ? throw new ArgumentException("Instance name is too long.", nameof(value))
            : normalized;
    }
}
