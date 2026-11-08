namespace AgroFlow.Data.Appointments;

/// <summary>
/// Control de cambios de la precarga privada del equipo: qué versión de una
/// entrada del archivo se aplicó a cada posición de la demo. Guarda sólo una
/// huella (SHA-256), no el nombre ni el teléfono del archivo.
/// </summary>
public sealed class LocalDemoTeamAssignment
{
    public const int FingerprintLength = 64;

    private LocalDemoTeamAssignment()
    {
    }

    public LocalDemoTeamAssignment(string seedId, Guid transportistaId, string fingerprint, DateTime appliedAtUtc)
    {
        SeedId = seedId;
        TransportistaId = transportistaId;
        Fingerprint = fingerprint;
        AppliedAtUtc = appliedAtUtc;
    }

    public string SeedId { get; private set; } = string.Empty;
    public Guid TransportistaId { get; private set; }
    public string Fingerprint { get; private set; } = string.Empty;
    public DateTime AppliedAtUtc { get; private set; }

    public void Reapply(string fingerprint, DateTime appliedAtUtc)
    {
        Fingerprint = fingerprint;
        AppliedAtUtc = appliedAtUtc;
    }
}
