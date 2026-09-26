using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class MasterDataProjectionTests
{
    [Fact]
    public async Task Ef_projection_filters_by_tenant_driver_and_active_association_and_projects_truck_fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<Dsw2025TpiContext>().UseSqlite(connection).Options;
        await using var context = new Dsw2025TpiContext(options);
        await context.Database.EnsureCreatedAsync();

        var localIngenio = new Ingenio("Local Ingenio");
        var foreignIngenio = new Ingenio("Foreign Ingenio");
        var localDriver = new Transportista(localIngenio.Id, "Local Driver", "DNI 301", "+54 11 301");
        var foreignDriver = new Transportista(foreignIngenio.Id, "Foreign Driver", "DNI 302", "+54 11 302");
        var localTruck = new Camion(localIngenio.Id, "ZZ123AA", Camion.FleetTypePropia);
        var inactiveLocalTruck = new Camion(localIngenio.Id, "AA456BB", Camion.FleetTypeTerceros);
        inactiveLocalTruck.Inactivate(DateTime.UtcNow);
        var foreignTruck = new Camion(foreignIngenio.Id, "FF789GG", Camion.FleetTypePropia);
        var revokedTruck = new Camion(localIngenio.Id, "CC111DD", Camion.FleetTypePropia);
        var revokedAssociation = new TransportistaCamion(localDriver, revokedTruck);
        revokedAssociation.Inactivate(DateTime.UtcNow);

        context.AddRange(localIngenio, foreignIngenio, localDriver, foreignDriver, localTruck,
            inactiveLocalTruck, foreignTruck, revokedTruck,
            new TransportistaCamion(localDriver, localTruck),
            new TransportistaCamion(localDriver, inactiveLocalTruck),
            revokedAssociation,
            new TransportistaCamion(foreignDriver, foreignTruck));
        await context.SaveChangesAsync();

        var unitOfWork = new EfMasterDataUnitOfWork(context);
        var rows = await unitOfWork.ListActiveTrucksForTransportista(localIngenio.Id, localDriver.Id, CancellationToken.None);
        var foreignRows = await unitOfWork.ListActiveTrucksForTransportista(localIngenio.Id, foreignDriver.Id, CancellationToken.None);

        Assert.Collection(rows,
            row => Assert.Equal(new ActiveTruckAssociationProjection(inactiveLocalTruck.Id, "AA456BB", Camion.FleetTypeTerceros, true, false), row),
            row => Assert.Equal(new ActiveTruckAssociationProjection(localTruck.Id, "ZZ123AA", Camion.FleetTypePropia, true, true), row));
        Assert.Empty(foreignRows);
    }
}
