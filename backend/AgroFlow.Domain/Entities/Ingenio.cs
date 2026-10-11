namespace AgroFlow.Domain.Entities;

public sealed class Ingenio : EntityBase
{
    private Ingenio()
    {
    }

    public Ingenio(string name, string timeZoneId = "America/Argentina/Tucuman")
    {
        Name = MasterDataValidation.RequireText(name, nameof(name));
        TimeZoneId = MasterDataValidation.RequireText(timeZoneId, nameof(timeZoneId));
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador de zona horaria IANA del ingenio (ej. "America/Argentina/Tucuman").
    /// Dato del seed: las respuestas observables deben leerlo de acá, no de una
    /// constante de dominio.
    /// </summary>
    public string TimeZoneId { get; private set; } = "America/Argentina/Tucuman";

    public bool IsActive { get; private set; }

    public void Update(string name) => Name = MasterDataValidation.RequireText(name, nameof(name));

    public void UpdateTimeZone(string timeZoneId) =>
        TimeZoneId = MasterDataValidation.RequireText(timeZoneId, nameof(timeZoneId));

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
