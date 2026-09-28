namespace Dsw2025Tpi.Domain.Entities;

/// <summary>
/// Estados propios de Domain. No dependen del enum de Application: el
/// adaptador de persistencia traduce entre ambos.
/// </summary>
public enum TurnoEstado
{
    Asignado,
    EnCamino,
    EnEspera,
    Ingresado,
    EnDescarga,
    Finalizado,
    Cancelado
}

public sealed class Turno : EntityBase
{
    private Turno()
    {
    }

    public Turno(
        Guid ingenioId,
        Guid transportistaId,
        Guid camionId,
        Guid fincaId,
        Guid ventanaId,
        DateTimeOffset cutAt,
        double estimatedLoadTons,
        DateTimeOffset createdAt)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        TransportistaId = transportistaId;
        CamionId = camionId;
        FincaId = fincaId;
        VentanaId = ventanaId;
        CutAt = cutAt;
        EstimatedLoadTons = estimatedLoadTons;
        CreatedAt = createdAt;
        Status = TurnoEstado.Asignado;
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public Guid TransportistaId { get; private set; }
    public Guid CamionId { get; private set; }
    public Guid FincaId { get; private set; }
    public Guid VentanaId { get; private set; }
    public DateTimeOffset CutAt { get; private set; }
    public double EstimatedLoadTons { get; private set; }
    public TurnoEstado Status { get; private set; }

    /// <summary>
    /// Falso desde que Status entra en un estado terminal (Finalizado o
    /// Cancelado). Respalda la restricción real de "un turno no terminal por
    /// camión" con un índice único filtrado, en vez de depender sólo de una
    /// comprobación en memoria.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void SetStatus(TurnoEstado status)
    {
        Status = status;
        IsActive = status is not (TurnoEstado.Finalizado or TurnoEstado.Cancelado);
    }
}
