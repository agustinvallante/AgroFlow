using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

public sealed class MasterDataTests
{
    [Fact]
    public void Transportista_normalizes_identifiers_and_tracks_activity()
    {
        var transportista = new Transportista(Guid.NewGuid(), "  Ana Pérez  ", " 12.345 678 ", " +54 (381) 555-0123 ");

        Assert.Equal("Ana Pérez", transportista.Name);
        Assert.Equal("12345678", transportista.NormalizedDni);
        Assert.Equal("543815550123", transportista.NormalizedWhatsApp);
        Assert.Equal("12345678", Transportista.NormalizeDni("12.345 678"));
        Assert.Throws<ArgumentException>(() => Transportista.NormalizeDni("---"));
        Assert.Throws<ArgumentException>(() => Transportista.NormalizeWhatsApp("+++ ()"));
        Assert.True(transportista.IsActive);
        Assert.Null(transportista.InactivatedAtUtc);

        var inactivatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        transportista.Inactivate(inactivatedAt);
        Assert.False(transportista.IsActive);
        Assert.Equal(inactivatedAt, transportista.InactivatedAtUtc);

        transportista.Reactivate();
        Assert.True(transportista.IsActive);
        Assert.Null(transportista.InactivatedAtUtc);
    }

    [Fact]
    public void Camion_normalizes_plate_and_rejects_unapproved_fleet_type()
    {
        var camion = new Camion(Guid.NewGuid(), " ab- 123.cd ", "propia");
        Assert.Equal("AB123CD", camion.NormalizedPlate);
        Assert.Equal(Camion.FleetTypePropia, camion.FleetType);
        Assert.Throws<ArgumentException>(() => new Camion(Guid.NewGuid(), "AB123CD", "ALQUILADA"));
    }

    [Fact]
    public void Finca_requires_text_location_and_keeps_tenant_code_scope()
    {
        var ingenioId = Guid.NewGuid();
        var finca = new Finca(ingenioId, " F-01 ", " La Esperanza ", " Ruta provincial, km 12 ");
        var sameCodeInAnotherIngenio = new Finca(Guid.NewGuid(), "F-01", "Otra finca", "Referencia textual");
        Assert.Equal(ingenioId, finca.IngenioId);
        Assert.Equal("F-01", finca.Code);
        Assert.Equal("F-01", finca.NormalizedCode);
        Assert.Equal("Ruta provincial, km 12", finca.LocationReference);
        Assert.Equal("F-01", sameCodeInAnotherIngenio.Code);
    }

    [Fact]
    public void TransportistaCamion_rejects_cross_tenant_pair_and_preserves_inactivation()
    {
        var tenantId = Guid.NewGuid();
        var transportista = new Transportista(tenantId, "Ana", "123", "+541234");
        var camion = new Camion(tenantId, "AA123BB", Camion.FleetTypeTerceros);
        var association = new TransportistaCamion(transportista, camion);
        Assert.Equal(tenantId, association.IngenioId);
        Assert.Equal(transportista.Id, association.TransportistaId);
        Assert.Equal(camion.Id, association.CamionId);
        Assert.Throws<ArgumentException>(() => new TransportistaCamion(
            transportista, new Camion(Guid.NewGuid(), "CC456DD", Camion.FleetTypePropia)));

        var inactivatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        association.Inactivate(inactivatedAt);
        Assert.False(association.IsActive);
        Assert.Equal(inactivatedAt, association.InactivatedAtUtc);
    }

    [Fact]
    public void SQLite_model_preserves_catalog_invariants_with_system_wide_driver_identifiers()
    {
        using var context = new AgroFlowDbContext(new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite("Data Source=:memory:").Options);
        var model = context.Model;
        var transportista = Entity(model, typeof(Transportista), "Transportistas");
        Assert.NotNull(transportista.FindProperty(nameof(Transportista.NormalizedDni)));
        Assert.NotNull(transportista.FindProperty(nameof(Transportista.NormalizedWhatsApp)));
        // Un número identifica a un único transportista en todo el sistema.
        AssertUnique(transportista, nameof(Transportista.NormalizedWhatsApp));
        AssertUnique(transportista, nameof(Transportista.NormalizedDni));

        var camion = Entity(model, typeof(Camion), "Camiones");
        AssertUnique(camion, nameof(Camion.IngenioId), nameof(Camion.NormalizedPlate));
        var finca = Entity(model, typeof(Finca), "Fincas");
        Assert.NotNull(finca.FindProperty(nameof(Finca.NormalizedCode)));
        AssertUnique(finca, nameof(Finca.IngenioId), nameof(Finca.NormalizedCode));

        var association = Entity(model, typeof(TransportistaCamion), "TransportistaCamiones");
        AssertUnique(association, nameof(TransportistaCamion.TransportistaId), nameof(TransportistaCamion.CamionId));
        var activeTruck = AssertUnique(association, nameof(TransportistaCamion.CamionId));
        Assert.Equal("IsActive = 1", activeTruck.GetFilter());
        Assert.Empty(association.GetForeignKeys());
        Entity(model, typeof(AuditEvent), "AuditEvents");
        var idempotency = Entity(model, typeof(IdempotencyRecord), "IdempotencyRecords");
        AssertUnique(idempotency, nameof(IdempotencyRecord.Scope), nameof(IdempotencyRecord.ActorIdentity),
            nameof(IdempotencyRecord.Operation), nameof(IdempotencyRecord.Key));
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType Entity(Microsoft.EntityFrameworkCore.Metadata.IModel model, Type type, string table)
    {
        var entity = Assert.Single(model.GetEntityTypes(), candidate => candidate.ClrType == type);
        Assert.Equal(table, entity.GetTableName());
        return entity;
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IIndex AssertUnique(Microsoft.EntityFrameworkCore.Metadata.IEntityType entity, params string[] properties) =>
        Assert.Single(entity.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(properties));
}
