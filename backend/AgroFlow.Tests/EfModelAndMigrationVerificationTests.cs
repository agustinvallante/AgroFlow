using AgroFlow.Data.Appointments;
using AgroFlow.Data.Identity;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
        AssertTable(model, typeof(LocalDemoTeamAssignment), "LocalDemoTeamAssignments");
        var turno = AssertTable(model, typeof(Turno), "Turnos");
        var incident = AssertTable(model, typeof(IncidenteRuta), "IncidentesRuta");
        var openIncidentIndex = Assert.Single(incident.GetIndexes(), index => index.IsUnique);
        Assert.Equal("ResolvedAtUtc IS NULL", openIncidentIndex.GetFilter());

        Assert.False(context.Database.HasPendingModelChanges());

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Contains(migrations, migration => migration.Contains("InitialAppointments", StringComparison.Ordinal));
        Assert.EndsWith("_AddMasterDataFoundationTables", migrations[^7], StringComparison.Ordinal);
        Assert.EndsWith("_AddWhatsAppConversations", migrations[^6], StringComparison.Ordinal);
        Assert.EndsWith("_AddSeedRecords", migrations[^5], StringComparison.Ordinal);
        Assert.EndsWith("_AddLocalDemoTeamAssignments", migrations[^4], StringComparison.Ordinal);
        Assert.EndsWith("_AddMvpIdentityAndAppointmentReadIndex", migrations[^3], StringComparison.Ordinal);
        Assert.EndsWith("_AddRouteIncidents", migrations[^2], StringComparison.Ordinal);
        Assert.EndsWith("_AddAppointmentDeparture", migrations[^1], StringComparison.Ordinal);

        var departure = turno.FindProperty(nameof(Turno.DepartedAtUtc));
        Assert.NotNull(departure);
        Assert.True(departure.IsNullable);
    }

    [Fact]
    public async Task Route_incident_migration_only_adds_a_table_and_keeps_existing_appointments()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-incident-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
                .UseSqlite($"Data Source={dbPath};Pooling=False").Options;
            Guid turnoId;
            await using (var before = new AgroFlowDbContext(options))
            {
                var migrator = before.GetService<IMigrator>();
                await migrator.MigrateAsync(MigrationNamed(before, "_AddMvpIdentityAndAppointmentReadIndex"));
                await AppointmentsSeeder.SeedAsync(before, TimeProvider.System);
                // Written as a legacy row, not through the store: the store already reads the new schema.
                var carrier = await before.Transportistas.FirstAsync();
                var truck = await before.Camiones.FirstAsync();
                var farm = await before.Fincas.FirstAsync();
                var window = await before.Ventanas.FirstAsync();
                var turno = new Turno(carrier.IngenioId, carrier.Id, truck.Id, farm.Id, window.Id,
                    DateTimeOffset.UtcNow, 25.5, DateTimeOffset.UtcNow);
                await LegacyTurnoRows.InsertAsync(before, turno);
                turnoId = turno.Id;
            }

            await using var after = new AgroFlowDbContext(options);
            var turnosBefore = await after.Turnos.CountAsync();
            await after.Database.MigrateAsync();

            Assert.Empty(await after.Database.GetPendingMigrationsAsync());
            Assert.Equal(turnosBefore, await after.Turnos.CountAsync());
            Assert.Equal(TurnoEstado.Asignado, (await after.Turnos.SingleAsync(t => t.Id == turnoId)).Status);
            Assert.Empty(await after.IncidentesRuta.ToListAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
                File.Delete(dbPath + suffix);
        }
    }

    [Fact]
    public async Task Departure_migration_only_adds_a_nullable_column_and_keeps_appointments_and_incidents()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-departure-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
                .UseSqlite($"Data Source={dbPath};Pooling=False").Options;
            Guid enCaminoId;
            Guid assignedId;
            await using (var before = new AgroFlowDbContext(options))
            {
                await before.GetService<IMigrator>().MigrateAsync(MigrationNamed(before, "_AddRouteIncidents"));
                await AppointmentsSeeder.SeedAsync(before, TimeProvider.System);
                var carriers = await before.Transportistas.OrderBy(t => t.Id).Take(2).ToListAsync();
                var trucks = await before.Camiones.OrderBy(c => c.Id).Take(2).ToListAsync();
                var farm = await before.Fincas.FirstAsync();
                var window = await before.Ventanas.FirstAsync();
                var enCamino = new Turno(carriers[0].IngenioId, carriers[0].Id, trucks[0].Id, farm.Id, window.Id,
                    DateTimeOffset.UtcNow, 25.5, DateTimeOffset.UtcNow);
                enCamino.SetStatus(TurnoEstado.EnCamino);
                var assigned = new Turno(carriers[1].IngenioId, carriers[1].Id, trucks[1].Id, farm.Id, window.Id,
                    DateTimeOffset.UtcNow, 20, DateTimeOffset.UtcNow);
                await LegacyTurnoRows.InsertAsync(before, enCamino);
                await LegacyTurnoRows.InsertAsync(before, assigned);
                var reported = DateTime.UtcNow.AddMinutes(-30);
                before.IncidentesRuta.Add(new IncidenteRuta(enCamino.IngenioId, enCamino.Id,
                    IncidenteRutaTipo.Pinchazo, true, 30, "Rueda", reported));
                await before.SaveChangesAsync();
                await before.IncidentesRuta.ExecuteUpdateAsync(setters =>
                    setters.SetProperty(i => i.ResolvedAtUtc, reported.AddMinutes(10)));
                enCaminoId = enCamino.Id;
                assignedId = assigned.Id;
            }

            await using var after = new AgroFlowDbContext(options);
            var turnosBefore = await after.Turnos.CountAsync();
            await after.Database.MigrateAsync();

            Assert.Empty(await after.Database.GetPendingMigrationsAsync());
            Assert.Equal(turnosBefore, await after.Turnos.CountAsync());
            var migratedEnCamino = await after.Turnos.SingleAsync(t => t.Id == enCaminoId);
            Assert.Equal(TurnoEstado.EnCamino, migratedEnCamino.Status);
            // Appointments already EN_CAMINO before the migration have no known departure.
            Assert.Null(migratedEnCamino.DepartedAtUtc);
            Assert.Null((await after.Turnos.SingleAsync(t => t.Id == assignedId)).DepartedAtUtc);
            var incident = Assert.Single(await after.IncidentesRuta.ToListAsync());
            Assert.Equal(enCaminoId, incident.TurnoId);
            Assert.NotNull(incident.ResolvedAtUtc);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
                File.Delete(dbPath + suffix);
        }
    }

    private static string MigrationNamed(AgroFlowDbContext context, string suffix) =>
        context.Database.GetMigrations().Single(migration => migration.EndsWith(suffix, StringComparison.Ordinal));

    [Fact]
    public void Sqlite_identity_references_ingenio_and_keeps_password_hash()
    {
        using var context = new AgroFlowDbContext(new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite("Data Source=:memory:").Options);
        var identityUser = AssertTable(context.Model, typeof(AgroFlowUser), "Usuarios");
        Assert.NotNull(identityUser.FindProperty(nameof(IdentityUser.PasswordHash)));
        Assert.Contains(identityUser.GetForeignKeys(), key =>
            key.IsRequired && key.PrincipalEntityType.ClrType == typeof(Ingenio));
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType AssertTable(
        Microsoft.EntityFrameworkCore.Metadata.IModel model, Type entityType, string tableName)
    {
        var entity = Assert.Single(model.GetEntityTypes(), candidate => candidate.ClrType == entityType);
        Assert.Equal(tableName, entity.GetTableName());
        return entity;
    }
}
