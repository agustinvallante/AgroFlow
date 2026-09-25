using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Application.MasterData;

public sealed record DriverData(string Name, string Dni, string WhatsApp);
public sealed record TruckData(string Plate, string FleetType);
public sealed record FarmData(string Code, string Name, string LocationReference);
public sealed record AssociationData(Guid DriverId, Guid TruckId);
public sealed record MasterDataItem(Guid Id, Guid IngenioId, string Name, string? Code, string? Identifier, string? SecondaryIdentifier, bool IsActive, DateTime CreatedAtUtc, DateTime? InactivatedAtUtc);
public sealed record AssociationItem(Guid Id, Guid DriverId, Guid TruckId, bool IsActive, DateTime CreatedAtUtc, DateTime? InactivatedAtUtc);
public sealed record MutationReceipt(Guid ResourceId, string ResourceType, string Outcome);

public sealed class MasterDataConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class MasterDataService(IMasterDataUnitOfWork unitOfWork, ITenantContext tenant)
{
    private static readonly JsonSerializerOptions HashJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<MasterDataItem>> Drivers(bool? active, CancellationToken ct) =>
        (await unitOfWork.ListTransportistas(tenant.IngenioId, active, ct)).Select(DriverItem).ToArray();
    public async Task<IReadOnlyList<MasterDataItem>> Trucks(bool? active, CancellationToken ct) =>
        (await unitOfWork.ListCamiones(tenant.IngenioId, active, ct)).Select(TruckItem).ToArray();
    public async Task<IReadOnlyList<MasterDataItem>> Farms(bool? active, CancellationToken ct) =>
        (await unitOfWork.ListFincas(tenant.IngenioId, active, ct)).Select(FarmItem).ToArray();
    public async Task<IReadOnlyList<AssociationItem>> Associations(bool? active, CancellationToken ct) =>
        (await unitOfWork.ListAssociations(tenant.IngenioId, active, ct)).Select(AssociationItemOf).ToArray();
    public async Task<AssociationItem> GetAssociation(Guid id, CancellationToken ct) =>
        AssociationItemOf(await unitOfWork.FindAssociation(tenant.IngenioId, id, ct) ?? throw NotFound());

    public async Task<MasterDataItem> GetDriver(Guid id, CancellationToken ct) => DriverItem(await FindDriver(id, ct));
    public async Task<MasterDataItem> GetTruck(Guid id, CancellationToken ct) => TruckItem(await FindTruck(id, ct));
    public async Task<MasterDataItem> GetFarm(Guid id, CancellationToken ct) => FarmItem(await FindFarm(id, ct));

    public Task<MutationReceipt> CreateDriver(DriverData data, string key, CancellationToken ct) => Mutate("drivers.create", key, data, async () =>
    {
        ValidateDriver(data);
        await EnsureDriverIdentifiers(data, null, ct);
        var entity = new Transportista(tenant.IngenioId, data.Name, data.Dni, data.WhatsApp);
        unitOfWork.Add(entity);
        return DriverItem(entity);
    }, ct);

    public Task<MutationReceipt> UpdateDriver(Guid id, DriverData data, string key, CancellationToken ct) => Mutate($"drivers.update:{id:D}", key, data, async () =>
    {
        var entity = await FindDriver(id, ct);
        EnsureActive(entity.IsActive);
        ValidateDriver(data);
        await EnsureDriverIdentifiers(data, id, ct);
        entity.UpdateContactDetails(data.Name, data.Dni, data.WhatsApp);
        return DriverItem(entity);
    }, ct);

    public Task<MutationReceipt> CreateTruck(TruckData data, string key, CancellationToken ct) => Mutate("trucks.create", key, data, async () =>
    {
        ValidateTruck(data);
        var normalized = Camion.NormalizePlate(data.Plate);
        if (normalized.Length == 0) throw new ArgumentException("Plate must contain at least one letter or digit.", nameof(data));
        await EnsureUniquePlate(normalized, null, ct);
        var entity = new Camion(tenant.IngenioId, data.Plate, data.FleetType);
        unitOfWork.Add(entity);
        return TruckItem(entity);
    }, ct);

    public Task<MutationReceipt> UpdateTruck(Guid id, TruckData data, string key, CancellationToken ct) => Mutate($"trucks.update:{id:D}", key, data, async () =>
    {
        var entity = await FindTruck(id, ct);
        EnsureActive(entity.IsActive);
        ValidateTruck(data);
        var normalizedPlate = Camion.NormalizePlate(data.Plate);
        if (normalizedPlate.Length == 0) throw new ArgumentException("Plate must contain at least one letter or digit.", nameof(data));
        await EnsureUniquePlate(normalizedPlate, id, ct);
        entity.UpdateDetails(data.Plate, data.FleetType);
        return TruckItem(entity);
    }, ct);

    public Task<MutationReceipt> CreateFarm(FarmData data, string key, CancellationToken ct) => Mutate("farms.create", key, data, async () =>
    {
        ValidateFarm(data);
        var code = data.Code.Trim();
        if (await unitOfWork.HasFincaCode(tenant.IngenioId, code, null, ct)) throw Conflict("duplicate_code", "A farm with this code already exists.");
        var entity = new Finca(tenant.IngenioId, data.Code, data.Name, data.LocationReference);
        unitOfWork.Add(entity);
        return FarmItem(entity);
    }, ct);

    public Task<MutationReceipt> UpdateFarm(Guid id, FarmData data, string key, CancellationToken ct) => Mutate($"farms.update:{id:D}", key, data, async () =>
    {
        var entity = await FindFarm(id, ct);
        EnsureActive(entity.IsActive);
        ValidateFarm(data);
        if (await unitOfWork.HasFincaCode(tenant.IngenioId, data.Code.Trim(), id, ct)) throw Conflict("duplicate_code", "A farm with this code already exists.");
        entity.UpdateDetails(data.Code, data.Name, data.LocationReference);
        return FarmItem(entity);
    }, ct);

    public Task<MutationReceipt> SetDriverActive(Guid id, bool active, string key, CancellationToken ct) => Mutate($"drivers.{(active ? "reactivate" : "inactivate")}:{id:D}", key, new { id, active }, async () =>
    {
        var entity = await FindDriver(id, ct);
        if (active)
        {
            await EnsureDriverIdentifiers(new DriverData(entity.Name, entity.DNI, entity.WhatsApp), id, ct);
            entity.Reactivate();
        }
        else entity.Inactivate(DateTime.UtcNow);
        return DriverItem(entity);
    }, ct);

    public Task<MutationReceipt> SetTruckActive(Guid id, bool active, string key, CancellationToken ct) => Mutate($"trucks.{(active ? "reactivate" : "inactivate")}:{id:D}", key, new { id, active }, async () =>
    {
        var entity = await FindTruck(id, ct);
        if (active)
        {
            await EnsureUniquePlate(entity.NormalizedPlate, id, ct);
            entity.Reactivate();
        }
        else entity.Inactivate(DateTime.UtcNow);
        return TruckItem(entity);
    }, ct);

    public Task<MutationReceipt> SetFarmActive(Guid id, bool active, string key, CancellationToken ct) => Mutate($"farms.{(active ? "reactivate" : "inactivate")}:{id:D}", key, new { id, active }, async () =>
    {
        var entity = await FindFarm(id, ct);
        if (active)
        {
            if (await unitOfWork.HasFincaCode(tenant.IngenioId, entity.Code, id, ct)) throw Conflict("duplicate_code", "A farm with this code already exists.");
            entity.Reactivate();
        }
        else entity.Inactivate(DateTime.UtcNow);
        return FarmItem(entity);
    }, ct);

    public Task<MutationReceipt> CreateAssociation(AssociationData data, string key, CancellationToken ct) => Mutate("associations.create", key, data, async () =>
    {
        var driver = await unitOfWork.FindTransportista(tenant.IngenioId, data.DriverId, ct);
        var truck = await unitOfWork.FindCamion(tenant.IngenioId, data.TruckId, ct);
        if (driver is null || truck is null || !driver.IsActive || !truck.IsActive) throw NotFound();
        if (await unitOfWork.FindAssociation(tenant.IngenioId, data.DriverId, data.TruckId, activeOnly: true, ct) is not null)
            throw Conflict("duplicate_association", "An active association already exists.");
        var entity = new TransportistaCamion(driver, truck);
        unitOfWork.Add(entity);
        return AssociationItemOf(entity);
    }, ct);

    public Task<MutationReceipt> SetAssociationActive(Guid id, bool active, string key, CancellationToken ct) => Mutate($"associations.{(active ? "reactivate" : "inactivate")}:{id:D}", key, new { id, active }, async () =>
    {
        var entity = await unitOfWork.FindAssociation(tenant.IngenioId, id, ct) ?? throw NotFound();
        if (active)
        {
            if (!entity.Transportista.IsActive || !entity.Camion.IsActive) throw Conflict("inactive_endpoint", "Both associated resources must be active.");
            var duplicate = await unitOfWork.FindAssociation(tenant.IngenioId, entity.TransportistaId, entity.CamionId, activeOnly: true, ct);
            if (duplicate is not null && duplicate.Id != entity.Id) throw Conflict("duplicate_association", "An active association already exists.");
            entity.Reactivate();
        }
        else entity.Inactivate(DateTime.UtcNow);
        return AssociationItemOf(entity);
    }, ct);

    private async Task<MutationReceipt> Mutate<T>(string operation, string key, object input, Func<Task<T>> mutation, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Idempotency-Key is required and must be at most 200 characters.", nameof(key));
        key = key.Trim();
        if (key.Length > 200) throw new ArgumentException("Idempotency-Key is required and must be at most 200 characters.", nameof(key));
        var scope = tenant.IngenioId.ToString("D");
        var actor = tenant.ActorIdentity;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input, HashJsonOptions))));
        await using var transaction = await unitOfWork.BeginTransaction(ct);
        var prior = await unitOfWork.FindIdempotency(scope, actor, operation, key, ct);
        if (prior is not null)
        {
            if (!StringComparer.Ordinal.Equals(prior.InputHash, hash)) throw Conflict("idempotency_key_reused", "Idempotency-Key was already used with different input.");
            var replay = Replay(operation, prior);
            await transaction.Commit(ct);
            return replay;
        }

        var result = await mutation();
        var id = result switch
        {
            MasterDataItem item => item.Id,
            AssociationItem association => association.Id,
            _ => throw new InvalidOperationException("Unsupported master-data result type.")
        };
        var resourceType = ResourceType(operation);
        var outcome = Outcome(operation);
        var now = DateTime.UtcNow;
        unitOfWork.Add(new IdempotencyRecord(scope, actor, operation, key, hash, outcome, id.ToString("D"), now, now));
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        unitOfWork.Add(new AuditEvent(tenant.IngenioId, actor, now, operation,
            resourceType == "association" ? "DriverTruckAssociation" : "MasterData", id.ToString("D"), outcome, correlationId));
        await unitOfWork.Commit(ct);
        await transaction.Commit(ct);
        return new MutationReceipt(id, resourceType, outcome);
    }

    private static MutationReceipt Replay(string operation, IdempotencyRecord prior)
    {
        if (!Guid.TryParse(prior.ResultReference, out var id)) throw new InvalidOperationException("Stored idempotency result is invalid.");
        var resourceType = ResourceType(operation);
        if (!StringComparer.Ordinal.Equals(prior.ResultCode, Outcome(operation)))
            throw new InvalidOperationException("Stored idempotency outcome is invalid.");
        return new MutationReceipt(id, resourceType, prior.ResultCode);
    }

    private static string ResourceType(string operation) => operation.Split('.', 2)[0] switch
    {
        "drivers" => "driver",
        "trucks" => "truck",
        "farms" => "farm",
        "associations" => "association",
        _ => throw new InvalidOperationException("Unsupported master-data operation.")
    };

    private static string Outcome(string operation)
    {
        var action = operation.Split('.', 2)[1].Split(':', 2)[0];
        return action switch
        {
            "create" => "created",
            "update" => "updated",
            "inactivate" => "inactivated",
            "reactivate" => "reactivated",
            _ => throw new InvalidOperationException("Unsupported master-data operation.")
        };
    }

    private async Task EnsureUniquePlate(string normalizedPlate, Guid? exceptId, CancellationToken ct)
    {
        var conflicts = await unitOfWork.FindCamionPlateConflicts(tenant.IngenioId, normalizedPlate, exceptId, ct);
        if (conflicts.InUseInIngenio) throw Conflict("duplicate_plate", "A truck with this plate already exists.");
        if (conflicts.UsedGlobally) throw Conflict("duplicate_identifier", "An identifier is already in use.");
    }

    private async Task EnsureDriverIdentifiers(DriverData data, Guid? exceptId, CancellationToken ct)
    {
        var normalizedDni = Transportista.NormalizeDni(data.Dni);
        var normalizedWhatsApp = Transportista.NormalizeWhatsApp(data.WhatsApp);
        if (normalizedDni.Length == 0 || normalizedWhatsApp.Length == 0)
            throw new ArgumentException("DNI and WhatsApp must contain normalized characters.");
        var conflicts = await unitOfWork.FindTransportistaIdentifierConflicts(tenant.IngenioId,
            normalizedDni, normalizedWhatsApp, exceptId, ct);
        if (conflicts.DniInUseInIngenio) throw Conflict("duplicate_dni", "A driver with this DNI already exists.");
        if (conflicts.WhatsAppInUseInIngenio) throw Conflict("duplicate_whatsapp", "A driver with this WhatsApp already exists.");
        if (conflicts.UsedGlobally) throw Conflict("duplicate_identifier", "An identifier is already in use.");
    }

    private async Task<Transportista> FindDriver(Guid id, CancellationToken ct) => await unitOfWork.FindTransportista(tenant.IngenioId, id, ct) ?? throw NotFound();
    private async Task<Camion> FindTruck(Guid id, CancellationToken ct) => await unitOfWork.FindCamion(tenant.IngenioId, id, ct) ?? throw NotFound();
    private async Task<Finca> FindFarm(Guid id, CancellationToken ct) => await unitOfWork.FindFinca(tenant.IngenioId, id, ct) ?? throw NotFound();
    private static void ValidateDriver(DriverData data)
    {
        ValidateRequired(data.Name, nameof(data.Name), 160);
        ValidateRequired(data.Dni, nameof(data.Dni), 40);
        ValidateRequired(data.WhatsApp, nameof(data.WhatsApp), 40);
    }

    private static void ValidateTruck(TruckData data)
    {
        ValidateRequired(data.Plate, nameof(data.Plate), 30);
        ValidateRequired(data.FleetType, nameof(data.FleetType), 20);
    }

    private static void ValidateFarm(FarmData data)
    {
        ValidateRequired(data.Code, nameof(data.Code), 40);
        ValidateRequired(data.Name, nameof(data.Name), 160);
        ValidateRequired(data.LocationReference, nameof(data.LocationReference), 300);
    }

    private static void ValidateRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required values are missing.", parameterName);
        if (value.Trim().Length > maxLength) throw new ArgumentException($"Value must be at most {maxLength} characters.", parameterName);
    }
    private static void EnsureActive(bool active) { if (!active) throw Conflict("resource_inactive", "Inactive resources cannot be edited."); }
    private static MasterDataConflictException Conflict(string code, string message) => new(code, message);
    private static KeyNotFoundException NotFound() => new("The requested resource was not found.");
    private static MasterDataItem DriverItem(Transportista x) => new(x.Id, x.IngenioId, x.Name, null, x.DNI, x.WhatsApp, x.IsActive, x.CreatedAtUtc, x.InactivatedAtUtc);
    private static MasterDataItem TruckItem(Camion x) => new(x.Id, x.IngenioId, x.Plate, x.FleetType, x.NormalizedPlate, null, x.IsActive, x.CreatedAtUtc, x.InactivatedAtUtc);
    private static MasterDataItem FarmItem(Finca x) => new(x.Id, x.IngenioId, x.Name, x.Code, x.LocationReference, null, x.IsActive, x.CreatedAtUtc, x.InactivatedAtUtc);
    private static AssociationItem AssociationItemOf(TransportistaCamion x) => new(x.Id, x.TransportistaId, x.CamionId, x.IsActive, x.CreatedAtUtc, x.InactivatedAtUtc);
}
