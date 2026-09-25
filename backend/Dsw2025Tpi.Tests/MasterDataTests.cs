using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class MasterDataTests
{
    [Fact]
    public void Transportista_normalizes_identifiers_and_tracks_activity()
    {
        var transportista = new Transportista(Guid.NewGuid(), "  Ana Pérez  ", " 12.345 678 ", " +54 (381) 555-0123 ");

        Assert.Equal("Ana Pérez", transportista.Name);
        Assert.Equal("12.345678", transportista.NormalizedDni);
        Assert.Equal("+543815550123", transportista.NormalizedWhatsApp);
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
            transportista,
            new Camion(Guid.NewGuid(), "CC456DD", Camion.FleetTypePropia)));

        var inactivatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        association.Inactivate(inactivatedAt);
        Assert.False(association.IsActive);
        Assert.Equal(inactivatedAt, association.InactivatedAtUtc);
    }

    [Fact]
    public void Master_data_model_enforces_global_and_tenant_scoped_uniqueness()
    {
        using var context = new Dsw2025TpiContext(new DbContextOptionsBuilder<Dsw2025TpiContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
        var model = context.Model;

        var ingenio = AssertEntity(model, typeof(Ingenio), "Ingenios");
        var auditEvent = AssertEntity(model, typeof(AuditEvent), "AuditEvents");
        AssertIngenioForeignKey(auditEvent, ingenio, nameof(AuditEvent.IngenioId));
        var transportista = AssertEntity(model, typeof(Transportista), "Transportistas");
        AssertUniqueIndex(transportista, nameof(Transportista.NormalizedDni));
        AssertUniqueIndex(transportista, nameof(Transportista.NormalizedWhatsApp));
        AssertIngenioForeignKey(transportista, ingenio, nameof(Transportista.IngenioId));

        var camion = AssertEntity(model, typeof(Camion), "Camiones");
        AssertUniqueIndex(camion, nameof(Camion.NormalizedPlate));
        AssertIngenioForeignKey(camion, ingenio, nameof(Camion.IngenioId));

        var finca = AssertEntity(model, typeof(Finca), "Fincas");
        AssertUniqueIndex(finca, nameof(Finca.IngenioId), nameof(Finca.Code));
        AssertIngenioForeignKey(finca, ingenio, nameof(Finca.IngenioId));
        Assert.Null(finca.FindProperty("Latitude"));
        Assert.Null(finca.FindProperty("Longitude"));

        var association = AssertEntity(model, typeof(TransportistaCamion), "TransportistasCamiones");
        var activePairIndex = AssertUniqueIndex(
            association,
            nameof(TransportistaCamion.IngenioId),
            nameof(TransportistaCamion.TransportistaId),
            nameof(TransportistaCamion.CamionId));
        Assert.Equal("[IsActive] = 1", activePairIndex.GetFilter());
        Assert.Equal(2, association.GetForeignKeys().Count());
        var transportistaAssociationFk = Assert.Single(association.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(TransportistaCamion.IngenioId), nameof(TransportistaCamion.TransportistaId)
            }));
        Assert.Equal(typeof(Transportista), transportistaAssociationFk.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, transportistaAssociationFk.DeleteBehavior);
        var camionAssociationFk = Assert.Single(association.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(TransportistaCamion.IngenioId), nameof(TransportistaCamion.CamionId)
            }));
        Assert.Equal(typeof(Camion), camionAssociationFk.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, camionAssociationFk.DeleteBehavior);
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(Transportista));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(Camion));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(Finca));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(TransportistaCamion));
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType AssertEntity(
        Microsoft.EntityFrameworkCore.Metadata.IModel model,
        Type entityType,
        string tableName)
    {
        var entity = Assert.Single(model.GetEntityTypes(), candidate => candidate.ClrType == entityType);
        Assert.Equal(tableName, entity.GetTableName());
        return entity;
    }

    private static void AssertIngenioForeignKey(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType dependent,
        Microsoft.EntityFrameworkCore.Metadata.IEntityType ingenio,
        string propertyName)
    {
        var foreignKey = Assert.Single(dependent.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType == ingenio &&
            candidate.Properties.Select(property => property.Name).SequenceEqual(new[] { propertyName }));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.True(foreignKey.IsRequired);
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IIndex AssertUniqueIndex(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        params string[] propertyNames)
    {
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.IsUnique && candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        return index;
    }
}
