using Dsw2025Tpi.Domain.Entities;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class MasterDataDomainTests
{
    [Fact]
    public void Constructors_preserve_generated_ids_and_tenant_scope()
    {
        var ingenioId = Guid.NewGuid();
        var entities = new EntityBase[]
        {
            new Transportista(ingenioId, "Ana", "12.345.678", "+54 (381) 555-0123"),
            new Camion(ingenioId, "AB 123 CD", Camion.FleetTypePropia),
            new Finca(ingenioId, "F-01", "La Esperanza", "Ruta 9, km 12")
        };

        Assert.All(entities, entity => Assert.NotEqual(Guid.Empty, entity.Id));
        Assert.Equal(ingenioId, Assert.IsType<Transportista>(entities[0]).IngenioId);
        Assert.Equal(ingenioId, Assert.IsType<Camion>(entities[1]).IngenioId);
        Assert.Equal(ingenioId, Assert.IsType<Finca>(entities[2]).IngenioId);
        Assert.All(entities, entity => Assert.True(GetIsActive(entity)));
    }

    [Fact]
    public void Transportista_normalizes_dni_and_whatsapp_for_identity_comparison()
    {
        var transportista = new Transportista(
            Guid.NewGuid(), "  Ana Pérez  ", " 12.345.678 ", " +54 (381) 555-0123 ");

        Assert.Equal("Ana Pérez", transportista.Name);
        Assert.Equal("12.345.678", transportista.DNI);
        Assert.Equal("12345678", transportista.NormalizedDni);
        Assert.Equal("+54 (381) 555-0123", transportista.WhatsApp);
        Assert.Equal("543815550123", transportista.NormalizedWhatsApp);
    }

    [Fact]
    public void Camion_normalizes_plate_and_enforces_the_approved_fleet_types()
    {
        var camion = new Camion(Guid.NewGuid(), " ab-123.cd ", "propia");

        Assert.Equal("AB123CD", camion.NormalizedPlate);
        Assert.Equal(Camion.FleetTypePropia, camion.FleetType);
        Assert.Throws<ArgumentException>(() =>
            new Camion(Guid.NewGuid(), "AB123CD", "ALQUILADA"));
    }

    [Fact]
    public void Updates_preserve_entity_and_tenant_identity()
    {
        var ingenio = new Ingenio("Norte");
        var ingenioId = ingenio.Id;
        var transportista = new Transportista(ingenioId, "Ana", "123", "+54111");
        var camion = new Camion(ingenioId, "AA123BB", Camion.FleetTypePropia);
        var finca = new Finca(ingenioId, "F-01", "Norte", "Ruta 1");
        var originalIds = new[] { transportista.Id, camion.Id, finca.Id };

        ingenio.Update("Sur");
        transportista.Update("Bea", "45.678.901", "+54 222");
        camion.Update("cc-456-dd", Camion.FleetTypeTerceros);
        finca.Update("F-02", "Sur", "Ruta 2");

        Assert.Equal(ingenioId, ingenio.Id);
        Assert.Equal("Sur", ingenio.Name);
        Assert.Equal(originalIds, new[] { transportista.Id, camion.Id, finca.Id });
        Assert.All(new[] { transportista.IngenioId, camion.IngenioId, finca.IngenioId },
            actual => Assert.Equal(ingenioId, actual));
        Assert.Equal("45678901", transportista.NormalizedDni);
        Assert.Equal("CC456DD", camion.NormalizedPlate);
        Assert.Equal("F-02", finca.Code);
    }

    [Fact]
    public void Master_data_can_be_inactivated_and_reactivated_without_losing_identity()
    {
        var ingenio = new Ingenio("Norte");
        var transportista = new Transportista(ingenio.Id, "Ana", "123", "+54111");
        var camion = new Camion(ingenio.Id, "AA123BB", Camion.FleetTypePropia);
        var finca = new Finca(ingenio.Id, "F-01", "Norte", "Ruta 1");
        var actions = new (Action Inactivate, Action Reactivate, Func<bool> IsActive)[]
        {
            (ingenio.Inactivate, ingenio.Reactivate, () => ingenio.IsActive),
            (transportista.Inactivate, transportista.Reactivate, () => transportista.IsActive),
            (camion.Inactivate, camion.Reactivate, () => camion.IsActive),
            (finca.Inactivate, finca.Reactivate, () => finca.IsActive)
        };

        foreach (var lifecycle in actions)
        {
            lifecycle.Inactivate();
            Assert.False(lifecycle.IsActive());
            lifecycle.Reactivate();
            Assert.True(lifecycle.IsActive());
        }
    }

    [Fact]
    public void Invalid_update_does_not_partially_change_transportista()
    {
        var transportista = new Transportista(Guid.NewGuid(), "Ana", "123", "+54111");

        Assert.Throws<ArgumentException>(() => transportista.Update("Bea", "---", "+54222"));

        Assert.Equal("Ana", transportista.Name);
        Assert.Equal("123", transportista.DNI);
        Assert.Equal("54111", transportista.NormalizedWhatsApp);
    }

    private static bool GetIsActive(EntityBase entity) => entity switch
    {
        Transportista value => value.IsActive,
        Camion value => value.IsActive,
        Finca value => value.IsActive,
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };
}
