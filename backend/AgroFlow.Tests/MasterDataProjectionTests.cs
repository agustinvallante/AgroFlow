using AgroFlow.Data.Appointments;
using AgroFlow.Data.Repositories;
using AgroFlow.Domain.Entities;
using AgroFlow.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

public sealed class MasterDataProjectionTests
{
    [Fact]
    public async Task Ef_truck_projection_excludes_cross_tenant_trucks_and_projects_valid_local_activity()
    {
        using var database = new MigratedSqliteDatabase();
        await using var context = await database.CreateContextAsync();

        var localIngenio = new Ingenio("Projection Local Ingenio");
        var foreignIngenio = new Ingenio("Projection Foreign Ingenio");
        var localDriver = new Transportista(localIngenio.Id, "Projection Local Driver", "301", "+5411301");
        var foreignDriver = new Transportista(foreignIngenio.Id, "Projection Foreign Driver", "302", "+5411302");
        var localActiveTruck = new Camion(localIngenio.Id, "AA001AA", Camion.FleetTypePropia);
        var localInactiveTruck = new Camion(localIngenio.Id, "BB002BB", Camion.FleetTypeTerceros);
        localInactiveTruck.Inactivate(DateTime.UtcNow);
        var localTruckWithRevokedAssociation = new Camion(localIngenio.Id, "CC003CC", Camion.FleetTypePropia);
        var foreignTruck = new Camion(foreignIngenio.Id, "ZZ999ZZ", Camion.FleetTypeTerceros);
        var mismatchedAssociation = new TransportistaCamion(localIngenio.Id, localDriver.Id, foreignTruck.Id);
        var revokedAssociation = new TransportistaCamion(localIngenio.Id, localDriver.Id, localTruckWithRevokedAssociation.Id);
        revokedAssociation.Inactivate(DateTime.UtcNow);

        context.AddRange(localIngenio, foreignIngenio, localDriver, foreignDriver,
            localActiveTruck, localInactiveTruck, localTruckWithRevokedAssociation, foreignTruck,
            mismatchedAssociation,
            new TransportistaCamion(localIngenio.Id, localDriver.Id, localActiveTruck.Id),
            new TransportistaCamion(localIngenio.Id, localDriver.Id, localInactiveTruck.Id),
            revokedAssociation);
        await context.SaveChangesAsync();

        var unitOfWork = new EfMasterDataUnitOfWork(context);
        var localRows = await unitOfWork.ListActiveTrucksForTransportista(
            localIngenio.Id, localDriver.Id, CancellationToken.None);
        var foreignDriverRows = await unitOfWork.ListActiveTrucksForTransportista(
            localIngenio.Id, foreignDriver.Id, CancellationToken.None);

        Assert.Collection(localRows,
            row => Assert.Equal(new ActiveTruckAssociationProjection(
                localActiveTruck.Id, "AA001AA", Camion.FleetTypePropia, true, true), row),
            row => Assert.Equal(new ActiveTruckAssociationProjection(
                localInactiveTruck.Id, "BB002BB", Camion.FleetTypeTerceros, true, false), row));
        Assert.Empty(foreignDriverRows);
    }

    [Fact]
    public async Task Ef_projection_filters_by_tenant_driver_and_active_association_and_projects_truck_fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite(connection).Options;
        await using var context = new AgroFlowDbContext(options);
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

    private sealed class MigratedSqliteDatabase : IDisposable
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(), $"agroflow-truck-projection-{Guid.NewGuid():N}.db");

        public async Task<AgroFlowDbContext> CreateContextAsync()
        {
            var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
                .UseSqlite($"Data Source={_databasePath};Default Timeout=5;Pooling=False")
                .Options;
            var context = new AgroFlowDbContext(options);
            await context.Database.MigrateAsync();
            return context;
        }

        public void Dispose()
        {
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                var path = _databasePath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task Ef_farm_code_lookup_uses_normalized_code()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite(connection).Options;
        await using var context = new AgroFlowDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var ingenio = new Ingenio("Lookup Ingenio");
        var farm = new Finca(ingenio.Id, "  finca-a  ", "Farm A", "North");
        context.AddRange(ingenio, farm);
        await context.SaveChangesAsync();

        var unitOfWork = new EfMasterDataUnitOfWork(context);
        var found = await unitOfWork.HasFincaCode(ingenio.Id, "  FiNcA-A  ", null, CancellationToken.None);
        Assert.True(found);
        Assert.Equal("finca-a", farm.Code);
        Assert.Equal("FINCA-A", farm.NormalizedCode);

        farm.UpdateDetails("  f-02  ", "Farm A Updated", "South");
        await context.SaveChangesAsync();
        Assert.Equal("f-02", farm.Code);
        Assert.Equal("F-02", farm.NormalizedCode);
        Assert.True(await unitOfWork.HasFincaCode(ingenio.Id, " f-02 ", null, CancellationToken.None));
    }
}
