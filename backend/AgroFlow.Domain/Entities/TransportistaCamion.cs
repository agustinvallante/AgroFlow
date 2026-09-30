namespace AgroFlow.Domain.Entities;

/// <summary>
/// Asociación activa muchos-a-muchos entre transportista y camión. Un camión
/// puede tener varias asociaciones activas; la demo sólo exige que exista una
/// vigente entre el par indicado en la solicitud.
/// </summary>
public sealed class TransportistaCamion : EntityBase
{
    private TransportistaCamion()
    {
    }

    public TransportistaCamion(Guid ingenioId, Guid transportistaId, Guid camionId)
    {
        IngenioId = MasterDataValidation.RequireIngenioId(ingenioId);
        TransportistaId = transportistaId;
        CamionId = camionId;
        IsActive = true;
    }

    public Guid IngenioId { get; private set; }
    public Guid TransportistaId { get; private set; }
    public Guid CamionId { get; private set; }
    public bool IsActive { get; private set; }

    public void Inactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
