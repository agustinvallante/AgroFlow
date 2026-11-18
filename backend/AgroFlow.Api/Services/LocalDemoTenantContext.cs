using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Api.Services;

/// <summary>
/// Ingenio efectivo del perfil LocalDemo: el único sembrado. Sólo se usa detrás
/// de la clave del dashboard; con más de un ingenio no hay forma segura de
/// elegir y la solicitud falla en lugar de adivinar.
/// </summary>
public sealed class LocalDemoTenantContext(AgroFlowDbContext db) : ITenantContext
{
    public const string Actor = "local-demo";

    private Guid? _ingenioId;

    public Guid IngenioId => _ingenioId ??= ResolveSingleIngenio();

    public string ActorIdentity => Actor;

    private Guid ResolveSingleIngenio()
    {
        var ids = db.Ingenios.AsNoTracking().Select(i => i.Id).Take(2).ToList();
        return ids.Count == 1
            ? ids[0]
            : throw new InvalidOperationException("LocalDemo requiere exactamente un ingenio sembrado.");
    }
}
