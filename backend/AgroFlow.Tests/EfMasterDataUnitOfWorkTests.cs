using AgroFlow.Data.Appointments;
using AgroFlow.Data.Repositories;
using AgroFlow.Domain.Entities;
using AgroFlow.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

public sealed class EfMasterDataUnitOfWorkTests
{
    [Fact]
    public async Task Ef_association_listing_returns_empty_for_an_ingenio_without_associations()
    {
        using var database = new MigratedSqliteDatabase();
        await using var context = database.CreateContext();
        var ingenio = new Ingenio("Empty Association Ingenio");
        context.Ingenios.Add(ingenio);
        await context.SaveChangesAsync();

        var associations = await new EfMasterDataUnitOfWork(context)
            .ListAssociations(ingenio.Id, active: null, CancellationToken.None);

        Assert.Empty(associations);
    }

    [Fact]
    public async Task Ef_association_listing_filters_by_activity_and_ingenio_in_stable_id_order()
    {
        using var database = new MigratedSqliteDatabase();
        await using var context = database.CreateContext();
        var ingenio = new Ingenio("Association Ingenio");
        var foreignIngenio = new Ingenio("Foreign Association Ingenio");
        context.Ingenios.AddRange(ingenio, foreignIngenio);
        var firstTransportista = new Transportista(ingenio.Id, "First", "20111111111", "+54111111111");
        var secondTransportista = new Transportista(ingenio.Id, "Second", "20222222222", "+54222222222");
        var foreignTransportista = new Transportista(foreignIngenio.Id, "Foreign", "20333333333", "+54333333333");
        var firstCamion = new Camion(ingenio.Id, "AA-001-AA", Camion.FleetTypePropia);
        var secondCamion = new Camion(ingenio.Id, "AA-002-AA", Camion.FleetTypePropia);
        var foreignCamion = new Camion(foreignIngenio.Id, "BB-001-BB", Camion.FleetTypePropia);
        var first = new TransportistaCamion(ingenio.Id, firstTransportista.Id, firstCamion.Id)
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001")
        };
        var second = new TransportistaCamion(ingenio.Id, secondTransportista.Id, secondCamion.Id)
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002")
        };
        var foreign = new TransportistaCamion(foreignIngenio.Id, foreignTransportista.Id, foreignCamion.Id)
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000000")
        };
        second.Inactivate(DateTime.UtcNow);
        context.AddRange(firstTransportista, secondTransportista, foreignTransportista,
            firstCamion, secondCamion, foreignCamion, first, second, foreign);
        await context.SaveChangesAsync();

        var unitOfWork = new EfMasterDataUnitOfWork(context);
        var all = await unitOfWork.ListAssociations(ingenio.Id, active: null, CancellationToken.None);
        var active = await unitOfWork.ListAssociations(ingenio.Id, active: true, CancellationToken.None);
        var inactive = await unitOfWork.ListAssociations(ingenio.Id, active: false, CancellationToken.None);
        var repeated = await unitOfWork.ListAssociations(ingenio.Id, active: null, CancellationToken.None);

        Assert.Equal(new[] { first.Id, second.Id }, all.Select(association => association.Id));
        Assert.Equal(new[] { first.Id }, active.Select(association => association.Id));
        Assert.Equal(new[] { second.Id }, inactive.Select(association => association.Id));
        Assert.Equal(all.Select(association => association.Id), repeated.Select(association => association.Id));
        Assert.DoesNotContain(all, association => association.Id == foreign.Id);
    }

    [Fact]
    public async Task SQLite_unique_constraint_is_translated_to_generic_master_data_conflict()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateContext(connection);
        var ingenio = new Ingenio("Unique Constraint Ingenio");
        context.AddRange(ingenio,
            new Finca(ingenio.Id, "F-01", "First", "North"),
            new Finca(ingenio.Id, "f-01", "Duplicate", "South"));

        var exception = await Assert.ThrowsAsync<MasterDataPersistenceConflictException>(
            () => new EfMasterDataUnitOfWork(context).Commit(CancellationToken.None));

        Assert.Equal("duplicate_identifier", exception.Code);
    }

    [Fact]
    public async Task SQLite_check_constraint_is_not_misclassified_as_uniqueness()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateContext(connection);
        var window = new Ventana(Guid.NewGuid(), DateTimeOffset.Parse("2025-01-01T10:00:00Z"),
            DateTimeOffset.Parse("2025-01-01T10:30:00Z"), capacity: 2);
        context.Add(window);
        await context.SaveChangesAsync();
        context.Entry(window).Property(nameof(Ventana.Occupied)).CurrentValue = 3;

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => new EfMasterDataUnitOfWork(context).Commit(CancellationToken.None));

        Assert.Equal(275, GetSqliteExtendedCode(exception));
        Assert.IsNotType<MasterDataPersistenceConflictException>(exception);
    }

    [Fact]
    public async Task SQLite_foreign_key_constraint_is_not_misclassified_as_uniqueness()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateContext(connection);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE MasterDataProbeParent (Id INTEGER PRIMARY KEY);
            CREATE TABLE MasterDataProbeChild (ParentId INTEGER NOT NULL REFERENCES MasterDataProbeParent(Id));
            CREATE TRIGGER TR_Fincas_ProbeForeignKey BEFORE INSERT ON Fincas
            BEGIN
                INSERT INTO MasterDataProbeChild (ParentId) VALUES (99);
            END;
            """);
        context.Add(new Finca(Guid.NewGuid(), "F-01", "Farm", "North"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => new EfMasterDataUnitOfWork(context).Commit(CancellationToken.None));

        Assert.Equal(787, GetSqliteExtendedCode(exception));
        Assert.IsNotType<MasterDataPersistenceConflictException>(exception);
    }

    [Fact]
    public async Task SQLite_busy_or_locked_failure_remains_a_provider_failure()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"agroflow-master-data-lock-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            DefaultTimeout = 2
        }.ToString();

        try
        {
            await using var lockConnection = new SqliteConnection(connectionString);
            await lockConnection.OpenAsync();
            await using (var journalCommand = lockConnection.CreateCommand())
            {
                journalCommand.CommandText = "PRAGMA journal_mode=DELETE";
                Assert.Equal("delete", await journalCommand.ExecuteScalarAsync());
            }
            await using (var setup = await CreateContext(lockConnection)) { }

            await using var writerConnection = new SqliteConnection(connectionString);
            await writerConnection.OpenAsync();
            var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
                .UseSqlite(writerConnection, sqlite => sqlite.CommandTimeout(2))
                .Options;
            await using var writer = new AgroFlowDbContext(options);
            _ = writer.Model;
            writer.Add(new Finca(Guid.NewGuid(), "F-LOCK", "Locked farm", "North"));

            await using (var lockCommand = lockConnection.CreateCommand())
            {
                lockCommand.CommandTimeout = 2;
                lockCommand.CommandText = "BEGIN EXCLUSIVE";
                await lockCommand.ExecuteNonQueryAsync();
            }

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => new EfMasterDataUnitOfWork(writer).Commit(CancellationToken.None));
            var sqlite = FindInner<SqliteException>(exception);
            Assert.True(sqlite is not null, exception.ToString());
            Assert.Contains(sqlite!.SqliteErrorCode, new[] { 5, 6 });
            Assert.IsNotType<MasterDataPersistenceConflictException>(exception);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-journal" })
            {
                var path = databasePath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    private sealed class MigratedSqliteDatabase : IDisposable
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(), $"agroflow-master-data-{Guid.NewGuid():N}.db");

        public AgroFlowDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
                .UseSqlite($"Data Source={_databasePath};Default Timeout=5;Pooling=False")
                .Options;
            var context = new AgroFlowDbContext(options);
            context.Database.Migrate();
            return context;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                var path = _databasePath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    private static async Task<AgroFlowDbContext> CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>().UseSqlite(connection).Options;
        var context = new AgroFlowDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static int GetSqliteExtendedCode(DbUpdateException exception)
    {
        var sqlite = FindInner<SqliteException>(exception);
        Assert.NotNull(sqlite);
        return sqlite!.SqliteExtendedErrorCode;
    }

    private static TException? FindInner<TException>(Exception exception) where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is TException typed) return typed;
        return null;
    }
}
