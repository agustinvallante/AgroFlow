namespace AgroFlow.Domain.Entities;

/// <summary>Tipos de incidente en ruta propios de Domain; el adaptador traduce los de Application.</summary>
public enum IncidenteRutaTipo
{
    Pinchazo,
    FallaMecanica,
    Accidente,
    Otro
}

/// <summary>
/// Incidente en ruta de un turno EN_CAMINO. No cambia el estado ni la ventana
/// del turno. Resolverlo conserva el registro con <see cref="ResolvedAtUtc"/>;
/// un índice único filtrado garantiza a lo sumo un incidente abierto por turno.
/// </summary>
public sealed class IncidenteRuta : EntityBase
{
    public const int MaxDescriptionLength = 500;
    public const int MaxEstimatedDelayMinutes = 1440;

    private IncidenteRuta()
    {
    }

    public IncidenteRuta(
        Guid ingenioId,
        Guid turnoId,
        IncidenteRutaTipo type,
        bool canContinue,
        int estimatedDelayMinutes,
        string? description,
        DateTime reportedAtUtc)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));
        if (estimatedDelayMinutes is < 0 or > MaxEstimatedDelayMinutes)
            throw new ArgumentOutOfRangeException(nameof(estimatedDelayMinutes));
        if (description is { Length: > MaxDescriptionLength })
            throw new ArgumentOutOfRangeException(nameof(description));

        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        TurnoId = turnoId;
        Type = type;
        CanContinue = canContinue;
        EstimatedDelayMinutes = estimatedDelayMinutes;
        Description = description;
        ReportedAtUtc = reportedAtUtc;
    }

    public Guid IngenioId { get; private set; }
    public Guid TurnoId { get; private set; }
    public IncidenteRutaTipo Type { get; private set; }
    public bool CanContinue { get; private set; }
    public int EstimatedDelayMinutes { get; private set; }
    public string? Description { get; private set; }
    public DateTime ReportedAtUtc { get; private set; }

    /// <summary>Nulo mientras el incidente está abierto.</summary>
    public DateTime? ResolvedAtUtc { get; private set; }
}
