using System.Net;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Persistence;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AgroFlow.Tests;

public sealed class LocalDemoDatabaseTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"agroflow-upgrade-{Guid.NewGuid():N}");
    private string ApiDirectory => Path.Combine(_root, "backend", "AgroFlow.Api");
    // Sólo reproduce la ubicación de origen, no un proyecto activo.
    private string LegacyDatabase => Path.Combine(_root, "backend", "Dsw2025Tpi.Api", "agroflow-demo.db");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-28T07:00:00-03:00");

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Fresh_checkout_resolves_relative_database_against_api_content_root()
    {
        var resolved = new SqliteConnectionStringBuilder(LocalDemoDatabase.ResolveConnectionString(
            "Data Source=agroflow-demo.db;Default Timeout=5", ApiDirectory));

        Assert.Equal(Path.Combine(ApiDirectory, "agroflow-demo.db"), resolved.DataSource);
        Assert.Equal(5, resolved.DefaultTimeout);
        Assert.False(File.Exists(resolved.DataSource));
    }

    [Theory]
    [InlineData("")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void Legacy_files_abort_relative_startup_without_creating_another_database(string suffix)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyDatabase)!);
        File.WriteAllText(LegacyDatabase + suffix, "guard-test-only");

        var error = Assert.Throws<InvalidOperationException>(() => LocalDemoDatabase.ResolveConnectionString(
            "Data Source=agroflow-demo.db", ApiDirectory));

        Assert.Contains("ruta absoluta", error.Message);
        Assert.False(File.Exists(Path.Combine(ApiDirectory, "agroflow-demo.db")));
        Assert.Equal("guard-test-only", File.ReadAllText(LegacyDatabase + suffix));
    }

    [Fact]
    public void Two_database_locations_require_an_explicit_selection()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyDatabase)!);
        Directory.CreateDirectory(ApiDirectory);
        File.WriteAllText(LegacyDatabase, "legacy-test-only");
        var newDatabase = Path.Combine(ApiDirectory, "agroflow-demo.db");
        File.WriteAllText(newDatabase, "new-test-only");

        Assert.Throws<InvalidOperationException>(() => LocalDemoDatabase.ResolveConnectionString(
            "Data Source=agroflow-demo.db", ApiDirectory));
        var explicitSelection = new SqliteConnectionStringBuilder(LocalDemoDatabase.ResolveConnectionString(
            new SqliteConnectionStringBuilder { DataSource = newDatabase, Mode = SqliteOpenMode.ReadWrite }.ToString(),
            ApiDirectory));

        Assert.Equal(newDatabase, explicitSelection.DataSource);
        Assert.Equal("legacy-test-only", File.ReadAllText(LegacyDatabase));
        Assert.Equal("new-test-only", File.ReadAllText(newDatabase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Default Timeout=5")]
    public void Missing_database_configuration_is_rejected(string? connectionString)
    {
        Assert.Throws<InvalidOperationException>(() =>
            LocalDemoDatabase.ResolveConnectionString(connectionString, ApiDirectory));
    }

    [Fact]
    public async Task ReadWrite_selection_rejects_a_missing_file_instead_of_creating_it()
    {
        Directory.CreateDirectory(ApiDirectory);
        var path = Path.Combine(ApiDirectory, "missing.db");
        var resolved = LocalDemoDatabase.ResolveConnectionString(
            new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite }.ToString(),
            ApiDirectory);
        await using var connection = new SqliteConnection(resolved);

        await Assert.ThrowsAsync<SqliteException>(() => connection.OpenAsync());
        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("DELETE")]
    [InlineData("WAL")]
    public async Task Existing_demo_survives_api_rename_migration_seed_and_two_host_restarts(string journalMode)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyDatabase)!);
        Directory.CreateDirectory(ApiDirectory);
        var clock = new FixedClock();
        Guid appointmentId;
        Guid windowId;
        string[] originalMigrations;
        Guid[] originalWindows;
        Guid[] originalCarriers;
        var options = new DbContextOptionsBuilder<AgroFlowDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = LegacyDatabase, Pooling = false }.ToString())
            .Options;

        // Esquema anterior al último ajuste de capacidad, ubicado donde la
        // demo previa al renombre almacenaba el archivo ignorado por Git.
        await using (var db = new AgroFlowDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync("20260928031441_AddVentanaStartAtUtc");
            await db.Database.ExecuteSqlRawAsync(journalMode == "WAL"
                ? "PRAGMA journal_mode=WAL;" : "PRAGMA journal_mode=DELETE;");
            await AppointmentsSeeder.SeedAsync(db, clock);
            await db.Ventanas.ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Capacity, 1));
            var window = (await db.Ventanas.ToListAsync()).OrderBy(v => v.StartAt).First();
            windowId = window.Id;
            var carrier = await db.Transportistas.SingleAsync(t => t.WhatsApp == "+5493815550101");
            var truck = await db.Camiones.SingleAsync(t => t.NormalizedPlate == "AF123BC");
            var farm = await db.Fincas.SingleAsync(t => t.NormalizedCode == "FINCA-NORTE");
            var appointment = new Turno(window.IngenioId, carrier.Id, truck.Id, farm.Id, windowId,
                Now.AddHours(-2), 20, Now);
            appointment.SetStatus(TurnoEstado.EnCamino);
            db.Turnos.Add(appointment);
            await db.Ventanas.Where(v => v.Id == windowId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Occupied, 1));
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
            originalMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            originalWindows = await db.Ventanas.Select(v => v.Id).ToArrayAsync();
            originalCarriers = await db.Transportistas.Select(t => t.Id).ToArrayAsync();
        }

        // Reutilización explícita de la misma ubicación, sin mover archivos
        // ni arriesgar que una ruta mal escrita genere una base vacía.
        var selectedConnection = new SqliteConnectionStringBuilder
        {
            DataSource = LegacyDatabase, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 5
        }.ToString();
        for (var restart = 0; restart < 2; restart++)
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(ApiDirectory);
                builder.UseEnvironment("Development");
                builder.UseSetting("LocalDemo:Enabled", "true");
                builder.UseSetting("ConnectionStrings:AgroFlowDb", selectedConnection);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(clock);
                });
            });
            using var client = factory.CreateClient();
            using var health = await client.GetAsync("/health");
            using var detail = await client.GetAsync($"/api/v1/appointments/{appointmentId}");
            using var list = await client.GetAsync("/api/v1/appointments?date=2026-09-28");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var document = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal(appointmentId, document.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("EN_CAMINO", document.RootElement.GetProperty("status").GetString());
            using var listDocument = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            Assert.Equal(appointmentId, Assert.Single(listDocument.RootElement.EnumerateArray()).GetProperty("id").GetGuid());
            Assert.False(File.Exists(Path.Combine(ApiDirectory, "agroflow-demo.db")));
        }

        await using var verify = new AgroFlowDbContext(options);
        Assert.Equal(1, await verify.Turnos.CountAsync());
        Assert.Equal(1, await verify.Ingenios.CountAsync());
        Assert.Equal(2, await verify.Camiones.CountAsync());
        Assert.Equal(2, await verify.Fincas.CountAsync());
        Assert.Equal(2, await verify.Asociaciones.CountAsync());
        Assert.Equal(originalWindows.Order(), (await verify.Ventanas.Select(v => v.Id).ToArrayAsync()).Order());
        Assert.Equal(originalCarriers.Order(), (await verify.Transportistas.Select(t => t.Id).ToArrayAsync()).Order());
        var migrations = (await verify.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(originalMigrations, migrations.Take(originalMigrations.Length));
        Assert.Equal(4, migrations.Length);
        var finalWindow = await verify.Ventanas.SingleAsync(v => v.Id == windowId);
        Assert.Equal(2, finalWindow.Capacity);
        Assert.Equal(1, finalWindow.Occupied);
        Assert.Equal(TurnoEstado.EnCamino, (await verify.Turnos.SingleAsync()).Status);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
