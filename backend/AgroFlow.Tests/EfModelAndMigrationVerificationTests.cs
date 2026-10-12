using AgroFlow.Data;
using AgroFlow.Data.Appointments;
using AgroFlow.Data.Identity;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

public sealed class EfModelAndMigrationVerificationTests
{
    [Fact]
    public void Live_SQLite_model_matches_appointment_schema_and_ignores_unmigrated_audit_foundation()
    {
        using var context = new AgroFlowDbContext(new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite("Data Source=:memory:").Options);
        var model = context.Model;
        AssertTable(model, typeof(Ingenio), "Ingenios");
        var transportista = AssertTable(model, typeof(Transportista), "Transportistas");
        Assert.NotNull(transportista.FindProperty(nameof(Transportista.NormalizedDni)));
        Assert.NotNull(transportista.FindProperty(nameof(Transportista.NormalizedWhatsApp)));
        var finca = AssertTable(model, typeof(Finca), "Fincas");
        Assert.NotNull(finca.FindProperty(nameof(Finca.NormalizedCode)));
        var association = AssertTable(model, typeof(TransportistaCamion), "TransportistaCamiones");
        Assert.Empty(association.GetForeignKeys());
        Assert.Null(model.FindEntityType(typeof(AuditEvent)));
        Assert.Null(model.FindEntityType(typeof(IdempotencyRecord)));

        Assert.False(context.Database.HasPendingModelChanges());

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains(migrations, migration => migration.Contains("InitialAppointments", StringComparison.Ordinal));
        Assert.DoesNotContain(migrations, migration => migration.Contains("MasterData", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Main_Identity_context_keeps_existing_identity_user_schema_and_does_not_register_preparatory_user()
    {
        using var context = new AuthenticateContext(new DbContextOptionsBuilder<AuthenticateContext>().UseSqlite("Data Source=:memory:").Options);
        var identityUser = AssertTable(context.Model, typeof(IdentityUser), "Usuarios");
        Assert.NotNull(identityUser.FindProperty(nameof(IdentityUser.PasswordHash)));
        Assert.Null(context.Model.FindEntityType(typeof(AgroFlowUser)));
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType AssertTable(
        Microsoft.EntityFrameworkCore.Metadata.IModel model, Type entityType, string tableName)
    {
        var entity = Assert.Single(model.GetEntityTypes(), candidate => candidate.ClrType == entityType);
        Assert.Equal(tableName, entity.GetTableName());
        return entity;
    }
}
