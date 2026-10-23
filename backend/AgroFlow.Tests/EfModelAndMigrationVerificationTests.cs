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
    public void Live_SQLite_model_matches_migrated_appointment_master_data_and_channel_schema()
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
        AssertTable(model, typeof(AuditEvent), "AuditEvents");
        AssertTable(model, typeof(IdempotencyRecord), "IdempotencyRecords");
        AssertTable(model, typeof(ChannelInstance), "ChannelInstances");
        AssertTable(model, typeof(Conversation), "Conversations");
        AssertTable(model, typeof(ConversationMessage), "ConversationMessages");
        AssertTable(model, typeof(MessageProcessing), "MessageProcessings");
        AssertTable(model, typeof(SeedRecord), "SeedRecords");

        Assert.False(context.Database.HasPendingModelChanges());

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains(migrations, migration => migration.Contains("InitialAppointments", StringComparison.Ordinal));
        Assert.EndsWith("_AddMasterDataFoundationTables", migrations[^3], StringComparison.Ordinal);
        Assert.EndsWith("_AddWhatsAppConversations", migrations[^2], StringComparison.Ordinal);
        Assert.EndsWith("_AddSeedRecords", migrations[^1], StringComparison.Ordinal);
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
