using AgroFlow.Domain.Entities;
using ApplicationIncidentType = AgroFlow.Application.Appointments.RouteIncidentType;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Traduce entre <see cref="IncidenteRutaTipo"/> (Domain) y el tipo de
/// Application, igual que <see cref="AppointmentStatusMapper"/> con los estados.
/// </summary>
internal static class RouteIncidentTypeMapper
{
    public static IncidenteRutaTipo ToDomain(ApplicationIncidentType type) => type switch
    {
        ApplicationIncidentType.Pinchazo => IncidenteRutaTipo.Pinchazo,
        ApplicationIncidentType.FallaMecanica => IncidenteRutaTipo.FallaMecanica,
        ApplicationIncidentType.Accidente => IncidenteRutaTipo.Accidente,
        ApplicationIncidentType.Otro => IncidenteRutaTipo.Otro,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static ApplicationIncidentType ToApplication(IncidenteRutaTipo type) => type switch
    {
        IncidenteRutaTipo.Pinchazo => ApplicationIncidentType.Pinchazo,
        IncidenteRutaTipo.FallaMecanica => ApplicationIncidentType.FallaMecanica,
        IncidenteRutaTipo.Accidente => ApplicationIncidentType.Accidente,
        IncidenteRutaTipo.Otro => ApplicationIncidentType.Otro,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
