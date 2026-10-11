using AgroFlow.Application.MasterData;
using AgroFlow.Domain.Entities;
using AgroFlow.Domain.Interfaces;
using Xunit;

namespace AgroFlow.Tests;

public sealed class MasterDataServiceTests
{
    [Fact]
    public async Task Repeating_mutation_replays_an_equivalent_receipt_without_duplicate_audit_or_record()
    {
        var store = new FakeMasterDataUnitOfWork();
        var service = Service(store, Guid.NewGuid(), "actor-1");
        var request = new DriverData("Driver One", "DNI 123", "+54 11 1234");

        var first = await service.CreateDriver(request, "request-1", CancellationToken.None);
        var replay = await service.CreateDriver(request, "request-1", CancellationToken.None);

        Assert.Equal(first, replay);
        Assert.Equal("driver", first.ResourceType);
        Assert.Equal("created", first.Outcome);
        Assert.Single(store.Transportistas);
        Assert.Single(store.IdempotencyRecords);
        Assert.Single(store.AuditEvents);
    }

    [Fact]
    public async Task Replay_returns_original_receipt_after_resource_is_updated()
    {
        var store = new FakeMasterDataUnitOfWork();
        var service = Service(store, Guid.NewGuid(), "actor-1");
        var request = new DriverData("Driver One", "DNI 123", "+54 11 1234");

        var originalReceipt = await service.CreateDriver(request, "request-1", CancellationToken.None);
        await service.UpdateDriver(originalReceipt.ResourceId,
            new DriverData("Updated Driver", "DNI 456", "+54 11 5678"), "request-2", CancellationToken.None);
        var replay = await service.CreateDriver(request, "request-1", CancellationToken.None);

        Assert.Equal(originalReceipt, replay);
        Assert.Equal("Updated Driver", store.Transportistas.Single().Name);
        Assert.Equal("DNI 456", store.Transportistas.Single().DNI);
    }

    [Fact]
    public async Task Idempotency_record_stores_no_request_or_response_personal_data()
    {
        var store = new FakeMasterDataUnitOfWork();
        var service = Service(store, Guid.NewGuid(), "actor-1");
        await service.CreateDriver(new DriverData("Private Driver", "DNI 998877", "+54 11 998877"), "private-key", CancellationToken.None);

        var serializedRecord = System.Text.Json.JsonSerializer.Serialize(store.IdempotencyRecords.Single());

        Assert.DoesNotContain("Private Driver", serializedRecord);
        Assert.DoesNotContain("DNI 998877", serializedRecord);
        Assert.DoesNotContain("+54 11 998877", serializedRecord);
        Assert.Equal("created", store.IdempotencyRecords.Single().ResultCode);
        Assert.Equal(store.Transportistas.Single().Id.ToString("D"), store.IdempotencyRecords.Single().ResultReference);
    }

    [Fact]
    public async Task Reusing_key_with_different_input_is_rejected_before_mutation()
    {
        var store = new FakeMasterDataUnitOfWork();
        var service = Service(store, Guid.NewGuid(), "actor-1");
        await service.CreateDriver(new DriverData("Driver One", "DNI 123", "+54 11 1234"), "request-1", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<MasterDataConflictException>(() =>
            service.CreateDriver(new DriverData("Driver Two", "DNI 456", "+54 11 5678"), "request-1", CancellationToken.None));

        Assert.Equal("idempotency_key_reused", exception.Code);
        Assert.Single(store.Transportistas);
        Assert.Equal(0, store.ActiveTransactions);
        Assert.Equal(2, store.DisposedTransactions);
        Assert.Equal(1, store.CommittedTransactions);
    }

    [Fact]
    public async Task Global_duplicate_is_rejected_without_disclosing_cross_tenant_record()
    {
        var store = new FakeMasterDataUnitOfWork();
        var firstTenant = Guid.NewGuid();
        var secondTenant = Guid.NewGuid();
        Service(store, firstTenant, "actor-1");
        store.Add(new Transportista(firstTenant, "Private Driver", "DNI 123", "+54 11 1234"));
        var service = Service(store, secondTenant, "actor-2");

        var exception = await Assert.ThrowsAsync<MasterDataConflictException>(() =>
            service.CreateDriver(new DriverData("Another Driver", "DNI123", "+54 11 9999"), "request-2", CancellationToken.None));

        Assert.Equal("duplicate_identifier", exception.Code);
        Assert.Equal("An identifier is already in use.", exception.Message);
        Assert.Single(store.Transportistas);
    }

    [Fact]
    public async Task Same_tenant_duplicate_identifies_the_conflicting_driver_field()
    {
        var store = new FakeMasterDataUnitOfWork();
        var ingenioId = Guid.NewGuid();
        store.Add(new Transportista(ingenioId, "Existing Driver", "DNI 123", "+54 11 1234"));
        var service = Service(store, ingenioId, "actor-1");

        var exception = await Assert.ThrowsAsync<MasterDataConflictException>(() =>
            service.CreateDriver(new DriverData("New Driver", "DNI123", "+54 11 9999"), "request-2", CancellationToken.None));

        Assert.Equal("duplicate_dni", exception.Code);
        Assert.Single(store.Transportistas);
    }

    [Fact]
    public async Task Oversized_request_values_are_rejected_before_persistence()
    {
        var store = new FakeMasterDataUnitOfWork();
        var service = Service(store, Guid.NewGuid(), "actor-1");

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateFarm(
            new FarmData("FARM", new string('x', 161), "Location"), "farm-1", CancellationToken.None));

        Assert.Empty(store.Fincas);
        Assert.Empty(store.AuditEvents);
        Assert.Empty(store.IdempotencyRecords);
    }

    [Fact]
    public async Task Resource_from_another_tenant_is_not_readable_or_mutable()
    {
        var store = new FakeMasterDataUnitOfWork();
        var firstTenant = Guid.NewGuid();
        var driver = new Transportista(firstTenant, "Private Driver", "DNI 123", "+54 11 1234");
        store.Add(driver);
        var service = Service(store, Guid.NewGuid(), "actor-2");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetDriver(driver.Id, CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateDriver(driver.Id,
            new DriverData("Changed", "DNI 123", "+54 11 1234"), "request-3", CancellationToken.None));
        Assert.Equal("Private Driver", driver.Name);
    }

    [Fact]
    public async Task Association_requires_active_endpoints_from_the_effective_tenant()
    {
        var store = new FakeMasterDataUnitOfWork();
        var currentTenant = Guid.NewGuid();
        store.Add(new Transportista(Guid.NewGuid(), "Other Driver", "DNI 123", "+54 11 1234"));
        store.Add(new Camion(Guid.NewGuid(), "ABC123", Camion.FleetTypePropia));
        var service = Service(store, currentTenant, "actor-1");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAssociation(
            new AssociationData(store.Transportistas[0].Id, store.Camiones[0].Id), "association-1", CancellationToken.None));
        Assert.Empty(store.Associations);
    }

    [Fact]
    public async Task Truck_can_have_only_one_active_association_but_keeps_inactive_history()
    {
        var store = new FakeMasterDataUnitOfWork();
        var ingenioId = Guid.NewGuid();
        var firstDriver = new Transportista(ingenioId, "Driver One", "DNI 101", "+54 11 101");
        var secondDriver = new Transportista(ingenioId, "Driver Two", "DNI 102", "+54 11 102");
        var thirdDriver = new Transportista(ingenioId, "Driver Three", "DNI 103", "+54 11 103");
        var truck = new Camion(ingenioId, "AA123BB", Camion.FleetTypePropia);
        store.Add(firstDriver);
        store.Add(secondDriver);
        store.Add(thirdDriver);
        store.Add(truck);
        var service = Service(store, ingenioId, "actor-1");

        await service.CreateAssociation(new AssociationData(firstDriver.Id, truck.Id), "association-1", CancellationToken.None);
        var conflict = await Assert.ThrowsAsync<MasterDataConflictException>(() =>
            service.CreateAssociation(new AssociationData(secondDriver.Id, truck.Id), "association-2", CancellationToken.None));
        Assert.Equal("duplicate_association", conflict.Code);
        Assert.Single(store.Associations, association => association.IsActive);

        await service.SetAssociationActive(store.Associations[0].Id, false, "association-off-1", CancellationToken.None);
        await service.CreateAssociation(new AssociationData(secondDriver.Id, truck.Id), "association-3", CancellationToken.None);
        await service.SetAssociationActive(store.Associations[1].Id, false, "association-off-2", CancellationToken.None);
        await service.CreateAssociation(new AssociationData(thirdDriver.Id, truck.Id), "association-4", CancellationToken.None);

        Assert.Equal(3, store.Associations.Count);
        Assert.All(store.Associations.Take(2), association => Assert.False(association.IsActive));
        Assert.True(store.Associations[2].IsActive);
    }

    [Fact]
    public async Task Inactive_association_can_be_reactivated_when_truck_has_no_other_active_association()
    {
        var store = new FakeMasterDataUnitOfWork();
        var ingenioId = Guid.NewGuid();
        var driver = new Transportista(ingenioId, "Driver One", "DNI 101", "+54 11 101");
        var truck = new Camion(ingenioId, "AA123BB", Camion.FleetTypePropia);
        store.Add(driver);
        store.Add(truck);
        var service = Service(store, ingenioId, "actor-1");

        await service.CreateAssociation(new AssociationData(driver.Id, truck.Id), "association-1", CancellationToken.None);
        var association = store.Associations.Single();
        await service.SetAssociationActive(association.Id, false, "association-off", CancellationToken.None);
        await service.SetAssociationActive(association.Id, true, "association-on", CancellationToken.None);

        Assert.True(association.IsActive);
        Assert.Null(association.InactivatedAtUtc);
    }

    [Fact]
    public async Task Reactivation_is_rejected_when_another_driver_has_the_active_truck_association()
    {
        var store = new FakeMasterDataUnitOfWork();
        var ingenioId = Guid.NewGuid();
        var firstDriver = new Transportista(ingenioId, "Driver One", "DNI 101", "+54 11 101");
        var secondDriver = new Transportista(ingenioId, "Driver Two", "DNI 102", "+54 11 102");
        var truck = new Camion(ingenioId, "AA123BB", Camion.FleetTypePropia);
        store.Add(firstDriver);
        store.Add(secondDriver);
        store.Add(truck);
        var service = Service(store, ingenioId, "actor-1");

        await service.CreateAssociation(new AssociationData(firstDriver.Id, truck.Id), "association-1", CancellationToken.None);
        var historical = store.Associations.Single();
        await service.SetAssociationActive(historical.Id, false, "association-off", CancellationToken.None);
        await service.CreateAssociation(new AssociationData(secondDriver.Id, truck.Id), "association-2", CancellationToken.None);

        var conflict = await Assert.ThrowsAsync<MasterDataConflictException>(() =>
            service.SetAssociationActive(historical.Id, true, "association-on", CancellationToken.None));

        Assert.Equal("duplicate_association", conflict.Code);
        Assert.False(historical.IsActive);
        Assert.Single(store.Associations, association => association.IsActive);
    }

    [Fact]
    public async Task Active_truck_projection_maps_details_states_and_only_returns_current_tenant_active_associations()
    {
        var store = new FakeMasterDataUnitOfWork();
        var tenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();
        var localDriver = new Transportista(tenantId, "Local Driver", "DNI 201", "+54 11 201");
        var foreignDriver = new Transportista(foreignTenantId, "Foreign Driver", "DNI 202", "+54 11 202");
        var activeTruck = new Camion(tenantId, "ZZ123AA", Camion.FleetTypePropia);
        var inactiveTruck = new Camion(tenantId, "AA456BB", Camion.FleetTypeTerceros);
        inactiveTruck.Inactivate(DateTime.UtcNow);
        var foreignTruck = new Camion(foreignTenantId, "FF789GG", Camion.FleetTypePropia);
        store.Add(localDriver);
        store.Add(foreignDriver);
        store.Add(activeTruck);
        store.Add(inactiveTruck);
        store.Add(foreignTruck);
        var activeAssociation = new TransportistaCamion(localDriver, activeTruck);
        var inactiveTruckAssociation = new TransportistaCamion(localDriver, inactiveTruck);
        var revokedAssociation = new TransportistaCamion(localDriver, new Camion(tenantId, "CC111DD", Camion.FleetTypePropia));
        revokedAssociation.Inactivate(DateTime.UtcNow);
        store.Add(activeAssociation);
        store.Add(inactiveTruckAssociation);
        store.Add(revokedAssociation);
        store.Add(new TransportistaCamion(foreignDriver, foreignTruck));
        var service = Service(store, tenantId, "actor-1");

        var rows = await service.ActiveTrucksForTransportista(localDriver.Id, CancellationToken.None);
        var foreignRows = await service.ActiveTrucksForTransportista(foreignDriver.Id, CancellationToken.None);

        Assert.Collection(rows.OrderBy(x => x.Plate),
            row => Assert.Equal(new TransportistaActiveTruck(inactiveTruck.Id, "AA456BB", Camion.FleetTypeTerceros, true, false), row),
            row => Assert.Equal(new TransportistaActiveTruck(activeTruck.Id, "ZZ123AA", Camion.FleetTypePropia, true, true), row));
        Assert.Empty(foreignRows);
    }

    [Fact]
    public async Task Active_truck_projection_returns_empty_for_unknown_transportista()
    {
        var service = Service(new FakeMasterDataUnitOfWork(), Guid.NewGuid(), "actor-1");

        Assert.Empty(await service.ActiveTrucksForTransportista(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Active_association_conflict_lookup_is_scoped_to_the_effective_tenant()
    {
        var store = new FakeMasterDataUnitOfWork();
        var currentTenant = Guid.NewGuid();
        var foreignTenant = Guid.NewGuid();
        var foreignDriver = new Transportista(foreignTenant, "Private Driver", "DNI 101", "+54 11 101");
        var foreignTruck = new Camion(foreignTenant, "AA123BB", Camion.FleetTypePropia);
        var foreignAssociation = new TransportistaCamion(foreignDriver, foreignTruck);
        store.Add(foreignDriver);
        store.Add(foreignTruck);
        store.Add(foreignAssociation);
        var localDriver = new Transportista(currentTenant, "Local Driver", "DNI 102", "+54 11 102");
        var localTruck = new Camion(currentTenant, "CC456DD", Camion.FleetTypeTerceros);
        store.Add(localDriver);
        store.Add(localTruck);
        var service = Service(store, currentTenant, "actor-1");

        var receipt = await service.CreateAssociation(new AssociationData(localDriver.Id, localTruck.Id), "association-1", CancellationToken.None);

        Assert.Equal(localDriver.Id, store.Associations.Single(association => association.Id == receipt.ResourceId).TransportistaId);
        Assert.Equal(2, store.Associations.Count(association => association.IsActive));
        Assert.DoesNotContain("Private Driver", System.Text.Json.JsonSerializer.Serialize(receipt));
    }

    private static MasterDataService Service(FakeMasterDataUnitOfWork unitOfWork, Guid ingenioId, string actor) =>
        new(unitOfWork, new TestTenantContext(ingenioId, actor));

    private sealed record TestTenantContext(Guid IngenioId, string ActorIdentity) : ITenantContext;

    internal sealed class FakeMasterDataUnitOfWork : IMasterDataUnitOfWork
    {
        public List<Transportista> Transportistas { get; } = [];
        public List<Camion> Camiones { get; } = [];
        public List<Finca> Fincas { get; } = [];
        public List<TransportistaCamion> Associations { get; } = [];
        public List<AuditEvent> AuditEvents { get; } = [];
        public List<IdempotencyRecord> IdempotencyRecords { get; } = [];
        public int ActiveTransactions { get; private set; }
        public int DisposedTransactions { get; private set; }
        public int CommittedTransactions { get; private set; }

        public Task<IReadOnlyList<Transportista>> ListTransportistas(Guid ingenioId, bool? active, CancellationToken ct) => Task.FromResult<IReadOnlyList<Transportista>>(Transportistas.Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active)).ToArray());
        public Task<IReadOnlyList<Camion>> ListCamiones(Guid ingenioId, bool? active, CancellationToken ct) => Task.FromResult<IReadOnlyList<Camion>>(Camiones.Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active)).ToArray());
        public Task<IReadOnlyList<Finca>> ListFincas(Guid ingenioId, bool? active, CancellationToken ct) => Task.FromResult<IReadOnlyList<Finca>>(Fincas.Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active)).ToArray());
        public Task<IReadOnlyList<TransportistaCamion>> ListAssociations(Guid ingenioId, bool? active, CancellationToken ct) => Task.FromResult<IReadOnlyList<TransportistaCamion>>(Associations.Where(x => x.IngenioId == ingenioId && (!active.HasValue || x.IsActive == active)).ToArray());
        public Task<IReadOnlyList<ActiveTruckAssociationProjection>> ListActiveTrucksForTransportista(Guid ingenioId, Guid transportistaId, CancellationToken ct)
        {
            if (!Transportistas.Any(x => x.IngenioId == ingenioId && x.Id == transportistaId))
                return Task.FromResult<IReadOnlyList<ActiveTruckAssociationProjection>>(Array.Empty<ActiveTruckAssociationProjection>());
            IReadOnlyList<ActiveTruckAssociationProjection> rows = Associations
                .Where(x => x.IngenioId == ingenioId && x.TransportistaId == transportistaId && x.IsActive)
                .Join(Camiones, association => association.CamionId, truck => truck.Id,
                    (association, truck) => new { association, truck })
                .OrderBy(x => x.truck.Plate)
                .ThenBy(x => x.association.CamionId)
                .Select(x => new ActiveTruckAssociationProjection(x.association.CamionId, x.truck.Plate, x.truck.FleetType, x.association.IsActive, x.truck.IsActive))
                .ToArray();
            return Task.FromResult(rows);
        }
        public Task<Transportista?> FindTransportista(Guid ingenioId, Guid id, CancellationToken ct) => Task.FromResult(Transportistas.SingleOrDefault(x => x.IngenioId == ingenioId && x.Id == id));
        public Task<Camion?> FindCamion(Guid ingenioId, Guid id, CancellationToken ct) => Task.FromResult(Camiones.SingleOrDefault(x => x.IngenioId == ingenioId && x.Id == id));
        public Task<Finca?> FindFinca(Guid ingenioId, Guid id, CancellationToken ct) => Task.FromResult(Fincas.SingleOrDefault(x => x.IngenioId == ingenioId && x.Id == id));
        public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid id, CancellationToken ct) => Task.FromResult(Associations.SingleOrDefault(x => x.IngenioId == ingenioId && x.Id == id));
        public Task<TransportistaCamion?> FindAssociation(Guid ingenioId, Guid driverId, Guid truckId, bool activeOnly, CancellationToken ct) => Task.FromResult(Associations.SingleOrDefault(x => x.IngenioId == ingenioId && x.TransportistaId == driverId && x.CamionId == truckId && (!activeOnly || x.IsActive)));
        public Task<TransportistaCamion?> FindActiveAssociationForTruck(Guid ingenioId, Guid truckId, CancellationToken ct) => Task.FromResult(Associations.SingleOrDefault(x => x.IngenioId == ingenioId && x.CamionId == truckId && x.IsActive));
        public Task<TransportistaIdentifierConflicts> FindTransportistaIdentifierConflicts(Guid ingenioId, string dni, string whatsApp, Guid? exceptId, CancellationToken ct)
        {
            var candidates = Transportistas.Where(x => x.Id != exceptId);
            var localDni = candidates.Any(x => x.IngenioId == ingenioId && x.NormalizedDni == dni);
            var localPhone = candidates.Any(x => x.IngenioId == ingenioId && x.NormalizedWhatsApp == whatsApp);
            var global = candidates.Any(x => x.NormalizedDni == dni || x.NormalizedWhatsApp == whatsApp);
            return Task.FromResult(new TransportistaIdentifierConflicts(localDni, localPhone, global));
        }
        public Task<CamionPlateConflicts> FindCamionPlateConflicts(Guid ingenioId, string plate, Guid? exceptId, CancellationToken ct)
        {
            var candidates = Camiones.Where(x => x.Id != exceptId && x.NormalizedPlate == plate).ToArray();
            return Task.FromResult(new CamionPlateConflicts(candidates.Any(x => x.IngenioId == ingenioId), candidates.Length > 0));
        }
        public Task<bool> HasFincaCode(Guid ingenioId, string code, Guid? exceptId, CancellationToken ct) => Task.FromResult(Fincas.Any(x => x.IngenioId == ingenioId && x.Id != exceptId && x.Code == code));
        public Task<IdempotencyRecord?> FindIdempotency(string scope, string actor, string operation, string key, CancellationToken ct) => Task.FromResult(IdempotencyRecords.SingleOrDefault(x => x.Scope == scope && x.ActorIdentity == actor && x.Operation == operation && x.Key == key));
        public Task<Transportista?> FindTransportistaById(Guid id, CancellationToken ct) => Task.FromResult(Transportistas.SingleOrDefault(x => x.Id == id));
        public Task<Camion?> FindCamionById(Guid id, CancellationToken ct) => Task.FromResult(Camiones.SingleOrDefault(x => x.Id == id));
        public Task<Finca?> FindFincaById(Guid id, CancellationToken ct) => Task.FromResult(Fincas.SingleOrDefault(x => x.Id == id));
        public void Add(Transportista entity) => Transportistas.Add(entity);
        public void Add(Camion entity) => Camiones.Add(entity);
        public void Add(Finca entity) => Fincas.Add(entity);
        public void Add(TransportistaCamion entity) => Associations.Add(entity);
        public void Add(AuditEvent entity) => AuditEvents.Add(entity);
        public void Add(IdempotencyRecord entity) => IdempotencyRecords.Add(entity);
        public Task<IMasterDataTransaction> BeginTransaction(CancellationToken ct)
        {
            ActiveTransactions++;
            return Task.FromResult<IMasterDataTransaction>(new FakeTransaction(this));
        }
        public Task Commit(CancellationToken ct) => Task.CompletedTask;

        private sealed class FakeTransaction(FakeMasterDataUnitOfWork store) : IMasterDataTransaction
        {
            private bool _disposed;
            public Task Commit(CancellationToken cancellationToken)
            {
                store.CommittedTransactions++;
                return Task.CompletedTask;
            }
            public ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    store.ActiveTransactions--;
                    store.DisposedTransactions++;
                }
                return ValueTask.CompletedTask;
            }
        }
    }
}
