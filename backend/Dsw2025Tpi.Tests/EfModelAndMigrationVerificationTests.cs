using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class EfModelAndMigrationVerificationTests
{
    [Fact]
    public void Platform_context_model_has_expected_tables_columns_and_indexes()
    {
        using var context = new Dsw2025TpiContext(new DbContextOptionsBuilder<Dsw2025TpiContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
        var model = context.Model;

        var ingenios = AssertTable(model, typeof(Domain.Entities.Ingenio), "Ingenios");
        AssertProperty(ingenios, "Name", required: true, maxLength: 120);
        AssertProperty(ingenios, "IsActive", required: true);

        var auditEvents = AssertTable(model, typeof(Domain.Entities.AuditEvent), "AuditEvents");
        AssertProperty(auditEvents, "ActorIdentity", required: true, maxLength: 160);
        AssertProperty(auditEvents, "Action", required: true, maxLength: 100);
        AssertProperty(auditEvents, "EntityType", required: true, maxLength: 100);
        AssertProperty(auditEvents, "EntityId", required: true, maxLength: 100);
        AssertProperty(auditEvents, "Result", required: true, maxLength: 40);
        AssertProperty(auditEvents, "CorrelationId", required: true, maxLength: 100);
        AssertProperty(auditEvents, "Reason", required: false, maxLength: 500);
        AssertProperty(auditEvents, "DetailsReference", required: false, maxLength: 500);
        AssertIndex(auditEvents, unique: false, nameof(Domain.Entities.AuditEvent.IngenioId), nameof(Domain.Entities.AuditEvent.OccurredAtUtc));

        var idempotencyRecords = AssertTable(model, typeof(Domain.Entities.IdempotencyRecord), "IdempotencyRecords");
        AssertProperty(idempotencyRecords, "Scope", required: true, maxLength: 120);
        AssertProperty(idempotencyRecords, "ActorIdentity", required: true, maxLength: 160);
        AssertProperty(idempotencyRecords, "Operation", required: true, maxLength: 120);
        AssertProperty(idempotencyRecords, "Key", required: true, maxLength: 200);
        AssertProperty(idempotencyRecords, "InputHash", required: true, maxLength: 128);
        AssertProperty(idempotencyRecords, "ResultCode", required: true, maxLength: 50);
        AssertProperty(idempotencyRecords, "ResultReference", required: false, maxLength: 200);
        AssertIndex(
            idempotencyRecords,
            unique: true,
            nameof(Domain.Entities.IdempotencyRecord.Scope),
            nameof(Domain.Entities.IdempotencyRecord.ActorIdentity),
            nameof(Domain.Entities.IdempotencyRecord.Operation),
            nameof(Domain.Entities.IdempotencyRecord.Key));
    }

    [Fact]
    public void Authenticate_context_model_has_nullable_ingenio_and_active_default()
    {
        using var context = new AuthenticateContext(new DbContextOptionsBuilder<AuthenticateContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
        var user = AssertTable(context.Model, typeof(AgroFlowUser), "Usuarios");

        var ingenioId = user.FindProperty(nameof(AgroFlowUser.IngenioId));
        Assert.NotNull(ingenioId);
        Assert.True(ingenioId!.IsNullable);

        var isActive = user.FindProperty(nameof(AgroFlowUser.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive!.IsNullable);
        Assert.Equal(true, isActive.GetDefaultValue());
    }

    [Fact]
    public void Platform_migrations_are_discoverable_and_generate_sql_without_connecting()
    {
        using var context = new Dsw2025TpiContext(new DbContextOptionsBuilder<Dsw2025TpiContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains("20260925174342_AddAgroFlowPlatformPrimitives", migrations);

        var script = context.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE [Ingenios]", script);
        Assert.Contains("CREATE TABLE [AuditEvents]", script);
        Assert.Contains("CREATE TABLE [IdempotencyRecords]", script);
    }

    [Fact]
    public void Master_data_migration_is_discoverable_and_generates_tenant_safe_sql()
    {
        using var context = new Dsw2025TpiContext(new DbContextOptionsBuilder<Dsw2025TpiContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains("20260925202117_AddAgroFlowMasterData", migrations);

        var script = context.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE [Transportistas]", script);
        Assert.Contains("CREATE TABLE [Camiones]", script);
        Assert.Contains("CREATE TABLE [Fincas]", script);
        Assert.Contains("CREATE TABLE [TransportistasCamiones]", script);
        Assert.Contains("NormalizedDni", script);
        Assert.Contains("NormalizedWhatsApp", script);
        Assert.Contains("NormalizedPlate", script);
        Assert.Contains("[IsActive] = 1", script);
        Assert.Contains("FK_Transportistas_Ingenios_IngenioId", script);
        Assert.Contains("FK_Fincas_Ingenios_IngenioId", script);
        Assert.Contains("FK_TransportistasCamiones_Camiones_IngenioId_CamionId", script);
    }

    [Fact]
    public void Authenticate_migrations_are_discoverable_and_generate_sql_without_connecting()
    {
        using var context = new AuthenticateContext(new DbContextOptionsBuilder<AuthenticateContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains("20260925174331_AddAgroFlowIdentityContext", migrations);

        var script = context.GetService<IMigrator>().GenerateScript();
        Assert.Contains("ALTER TABLE [Usuarios]", script);
        Assert.Contains("IngenioId", script);
        Assert.Contains("IsActive", script);
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType AssertTable(
        Microsoft.EntityFrameworkCore.Metadata.IModel model,
        Type entityType,
        string tableName)
    {
        var entity = Assert.Single(model.GetEntityTypes(), candidate => candidate.ClrType == entityType);
        Assert.Equal(tableName, entity.GetTableName());
        return entity;
    }

    private static void AssertProperty(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        string propertyName,
        bool required,
        int? maxLength = null)
    {
        var property = entity.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(!required, property!.IsNullable);
        if (maxLength.HasValue)
        {
            Assert.Equal(maxLength, property.GetMaxLength());
        }
    }

    private static void AssertIndex(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        bool unique,
        params string[] propertyNames)
    {
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        Assert.Equal(unique, index.IsUnique);
    }
}
