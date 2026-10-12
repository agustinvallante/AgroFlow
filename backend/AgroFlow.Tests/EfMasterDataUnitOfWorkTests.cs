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
