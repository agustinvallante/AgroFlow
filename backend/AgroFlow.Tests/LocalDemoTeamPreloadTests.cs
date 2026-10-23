using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Security;
using AgroFlow.Data.Appointments;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// Cinco posiciones de la demo y precarga privada del equipo, con SQLite
/// temporal migrada. Todos los nombres y números son sintéticos (rango 555);
/// ningún dato real del equipo participa de estas pruebas.
/// </summary>
public sealed class LocalDemoTeamPreloadTests : IDisposable
{
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private static readonly string[] SeedIds = ["demo-uno", "demo-dos", "demo-tres", "demo-cuatro", "demo-cinco"];
    private static readonly string[] Plates = ["AF123BC", "AF456DE", "AH301AB", "AH302AB", "AH303AB"];

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-team-preload-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task A_new_database_gets_five_drivers_each_with_an_active_truck()
    {
        await StartAsync();

        await using var db = CreateContext();
        Assert.Equal(5, await db.Transportistas.CountAsync());
        Assert.Equal(5, await db.Camiones.CountAsync());
        Assert.Equal(5, await db.Asociaciones.CountAsync(a => a.IsActive));
        foreach (var position in AppointmentsSeeder.DemoPositions)
        {
            var driver = await db.Transportistas.SingleAsync(t => t.NormalizedDni == position.Dni);
            var truck = await db.Camiones.SingleAsync(c => c.NormalizedPlate == position.Plate);
            Assert.True(driver.IsActive && truck.IsActive);
            Assert.True(await db.Asociaciones.AnyAsync(a => a.TransportistaId == driver.Id && a.CamionId == truck.Id && a.IsActive));
        }
    }

    [Fact]
    public async Task An_existing_database_with_two_positions_gains_the_other_three_without_losing_data()
    {
        Guid unoId, dosId, turnoId;
        await using (var db = CreateContext())
        {
            // Base creada por la versión anterior del seed: sólo dos posiciones, con sus marcas y un turno.
            await db.Database.MigrateAsync();
            var ingenio = new Ingenio("Ingenio Demo");
            var uno = new Transportista(ingenio.Id, "Transportista Demo Uno", "20111111111", "+5493815550101");
            var dos = new Transportista(ingenio.Id, "Transportista Demo Dos", "20222222222", "+5493815550102");
            var camionUno = new Camion(ingenio.Id, "AF123BC", Camion.FleetTypePropia);
            var camionDos = new Camion(ingenio.Id, "AF456DE", Camion.FleetTypePropia);
            var finca = new Finca(ingenio.Id, "FINCA-NORTE", "Finca Norte", "Ruta 9, km 12");
            var ventana = new Ventana(ingenio.Id, new DateTimeOffset(2030, 1, 1, 11, 0, 0, TimeSpan.FromHours(-3)),
                new DateTimeOffset(2030, 1, 1, 11, 30, 0, TimeSpan.FromHours(-3)), 2);
            var turno = new Turno(ingenio.Id, uno.Id, camionUno.Id, finca.Id, ventana.Id, Clock.GetUtcNow().AddHours(-2), 20, Clock.GetUtcNow());
            db.AddRange(ingenio, uno, dos, camionUno, camionDos, finca, ventana, turno,
                new TransportistaCamion(ingenio.Id, uno.Id, camionUno.Id), new TransportistaCamion(ingenio.Id, dos.Id, camionDos.Id));
            var now = Clock.GetUtcNow().UtcDateTime;
            db.SeedRecords.AddRange(new SeedRecord("transportista:demo-uno", uno.Id, now), new SeedRecord("transportista:demo-dos", dos.Id, now),
                new SeedRecord("camion:demo-uno", camionUno.Id, now), new SeedRecord("camion:demo-dos", camionDos.Id, now));
            await db.SaveChangesAsync();
            (unoId, dosId, turnoId) = (uno.Id, dos.Id, turno.Id);
        }

        await StartAsync();
        await StartAsync();

        await using var verify = CreateContext();
        Assert.Equal(5, await verify.Transportistas.CountAsync());
        Assert.Equal(5, await verify.Asociaciones.CountAsync(a => a.IsActive));
        Assert.True(await verify.Transportistas.AnyAsync(t => t.Id == unoId));
        Assert.True(await verify.Transportistas.AnyAsync(t => t.Id == dosId));
        Assert.True(await verify.Turnos.AnyAsync(t => t.Id == turnoId));
        Assert.Equal(17, await verify.SeedRecords.CountAsync());
    }

    [Fact]
    public async Task The_private_file_assigns_names_and_phones_but_keeps_dni_trucks_and_associations()
    {
        await StartAsync();

        var result = await ApplyAsync(TeamFile());

        Assert.Equal(5, result.Applied);
        Assert.DoesNotContain(result.AppliedMaskedPhones, masked => masked.Contains("5493815550141"));
        await using var db = CreateContext();
        Assert.Equal(5, await db.Transportistas.CountAsync());
        for (var i = 0; i < 5; i++)
        {
            var position = AppointmentsSeeder.DemoPositions[i];
            var driver = await db.Transportistas.SingleAsync(t => t.NormalizedDni == position.Dni);
            Assert.Equal($"[SINTÉTICO] Integrante {i + 1}", driver.Name);
            Assert.Equal($"+549381555014{i + 1}", driver.WhatsApp);
            var truck = await db.Camiones.SingleAsync(c => c.NormalizedPlate == Plates[i]);
            Assert.True(await db.Asociaciones.AnyAsync(a => a.TransportistaId == driver.Id && a.CamionId == truck.Id && a.IsActive));
        }
    }

    [Fact]
    public async Task Applying_the_same_file_again_changes_nothing_and_keeps_later_api_edits()
    {
        await StartAsync();
        await ApplyAsync(TeamFile());
        await using (var db = CreateContext())
        {
            // Edición posterior hecha desde la API.
            var tres = await db.Transportistas.SingleAsync(t => t.NormalizedDni == "20313131313");
            tres.UpdateContactDetails("Editado por la API", tres.DNI, "+5493815550199");
            await db.SaveChangesAsync();
        }

        await StartAsync();
        var again = await ApplyAsync(TeamFile());

        Assert.Equal(0, again.Applied);
        Assert.Equal(5, again.Unchanged);
        await using var verify = CreateContext();
        var after = await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20313131313");
        Assert.Equal("Editado por la API", after.Name);
        Assert.Equal("+5493815550199", after.WhatsApp);
        Assert.Equal(5, await verify.Transportistas.CountAsync());
    }

    [Fact]
    public async Task Changing_one_entry_reapplies_only_that_position()
    {
        await StartAsync();
        await ApplyAsync(TeamFile());
        await using (var db = CreateContext())
        {
            var uno = await db.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111");
            uno.UpdateContactDetails("Editado por la API", uno.DNI, "+5493815550198");
            await db.SaveChangesAsync();
        }

        var result = await ApplyAsync(TeamFile(overrides: new() { ["demo-dos"] = ("[SINTÉTICO] Nuevo integrante", "+5493815550152") }));

        Assert.Equal(1, result.Applied);
        await using var verify = CreateContext();
        Assert.Equal("+5493815550152", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20222222222")).WhatsApp);
        Assert.Equal("+5493815550198", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111")).WhatsApp);
    }

    [Fact]
    public async Task Positions_can_swap_numbers_without_violating_uniqueness()
    {
        await StartAsync();
        await ApplyAsync(TeamFile());

        var result = await ApplyAsync(TeamFile(overrides: new()
        {
            ["demo-uno"] = ("[SINTÉTICO] Integrante 1", "+5493815550142"),
            ["demo-dos"] = ("[SINTÉTICO] Integrante 2", "+5493815550141"),
        }));

        Assert.Equal(2, result.Applied);
        await using var verify = CreateContext();
        Assert.Equal("+5493815550142", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111")).WhatsApp);
        Assert.Equal("+5493815550141", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20222222222")).WhatsApp);
    }

    [Fact]
    public async Task A_number_owned_by_a_sixth_driver_rejects_the_whole_file_without_partial_changes()
    {
        await StartAsync();
        await using (var db = CreateContext())
        {
            var ingenio = await db.Ingenios.SingleAsync();
            db.Transportistas.Add(new Transportista(ingenio.Id, "Sexto por la API", "20666000166", "+5493815550145"));
            await db.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<LocalDemoTeamPreloadException>(() => ApplyAsync(TeamFile()));

        Assert.DoesNotContain("5493815550145", exception.Message);
        await using var verify = CreateContext();
        Assert.Equal("+5493815550101", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20111111111")).WhatsApp);
        Assert.Equal("Sexto por la API", (await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20666000166")).Name);
        Assert.Equal(0, await verify.LocalDemoTeamAssignments.CountAsync());
    }

    [Fact]
    public async Task A_sixth_driver_registered_later_is_never_modified_by_the_preload()
    {
        await StartAsync();
        await ApplyAsync(TeamFile());
        await using (var db = CreateContext())
        {
            var ingenio = await db.Ingenios.SingleAsync();
            db.Transportistas.Add(new Transportista(ingenio.Id, "Sexto por la API", "20666000166", "+5493815550166"));
            await db.SaveChangesAsync();
        }

        await StartAsync();
        await ApplyAsync(TeamFile());

        await using var verify = CreateContext();
        Assert.Equal(6, await verify.Transportistas.CountAsync());
        var sexto = await verify.Transportistas.SingleAsync(t => t.NormalizedDni == "20666000166");
        Assert.Equal(("Sexto por la API", "+5493815550166"), (sexto.Name, sexto.WhatsApp));
    }

    [Theory]
    [InlineData("no es json", "JSON")]
    [InlineData("""{"version":2,"transportistas":[]}""", "version")]
    [InlineData("""{"version":1,"transportistas":[],"extra":1}""", "no admitida")]
    [InlineData("""{"version":1,"transportistas":[]}""", "entre 1 y 5")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-seis","nombre":"X","whatsApp":"+5493815550141"}]}""", "desconocido")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"+5493815550141"},{"seedId":"demo-uno","nombre":"Y","whatsApp":"+5493815550142"}]}""", "repetido")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"+5493815550141"},{"seedId":"demo-dos","nombre":"Y","whatsApp":"+5493815550141"}]}""", "repite")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"5493815550141"}]}""", "E.164")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"+54 9 381 555 0141"}]}""", "E.164")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"  ","whatsApp":"+5493815550141"}]}""", "nombre")]
    [InlineData("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"+5493815550141","dni":"1"}]}""", "no admitida")]
    public void Invalid_files_are_rejected_without_echoing_phones(string json, string expected)
    {
        var exception = Assert.Throws<LocalDemoTeamPreloadException>(() => LocalDemoTeamPreload.Parse(json));

        Assert.Contains(expected, exception.Message);
        Assert.DoesNotContain("381555014", exception.Message);
    }

    [Fact]
    public void Masked_phones_never_show_the_full_number()
    {
        Assert.Equal("+54•••••••0141", LocalDemoTeamPreload.MaskPhone("5493815550141"));
    }

    [Fact]
    public async Task The_real_host_applies_the_file_and_each_position_can_request_an_appointment_across_restarts()
    {
        var file = WriteTeamFile(TeamFile());
        using (var factory = CreateFactory(file))
        {
            var client = factory.CreateClient();
            var drivers = await Drivers(client);
            Assert.Equal(5, drivers.Count);
            Assert.All(drivers, d => Assert.True(d.GetProperty("canRequestAppointments").GetBoolean()));

            for (var i = 0; i < 5; i++)
            {
                using var appointment = await client.PostAsJsonAsync("/api/v1/appointments", new
                {
                    carrierPhone = $"+549381555014{i + 1}",
                    truckPlate = Plates[i],
                    farmCode = "FINCA-NORTE",
                    cutAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    estimatedLoadTons = 20 + i
                });
                Assert.Equal(HttpStatusCode.Created, appointment.StatusCode);
            }

            // Un sexto transportista por la API queda fuera de la precarga.
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/drivers")
            {
                Content = JsonContent.Create(new { name = "Sexto por la API", dni = "20666000166", whatsApp = "+5493815550166" })
            };
            request.Headers.Add(ApiKeySchemes.DashboardHeader, DashboardKey);
            request.Headers.Add("Idempotency-Key", "fixture-sixth-1");
            using var sixth = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, sixth.StatusCode);
        }

        using (var restarted = CreateFactory(file))
        {
            var drivers = await Drivers(restarted.CreateClient());
            Assert.Equal(6, drivers.Count);
            for (var i = 0; i < 5; i++)
            {
                Assert.Contains(drivers, d => d.GetProperty("whatsApp").GetString() == $"+549381555014{i + 1}"
                    && d.GetProperty("activeTruckPlates")[0].GetString() == Plates[i]);
            }
            Assert.Contains(drivers, d => d.GetProperty("whatsApp").GetString() == "+5493815550166");
        }
    }

    [Fact]
    public void The_real_host_refuses_to_start_with_an_invalid_file_and_applies_nothing()
    {
        var file = WriteTeamFile("""{"version":1,"transportistas":[{"seedId":"demo-uno","nombre":"X","whatsApp":"381555"}]}""");

        var exception = Assert.ThrowsAny<Exception>(() => CreateFactory(file).CreateClient());

        Assert.Contains("E.164", exception.GetBaseException().Message);
    }

    [Fact]
    public async Task The_real_host_ignores_the_file_in_production()
    {
        await StartAsync();
        var file = WriteTeamFile(TeamFile());
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("LocalDemo:Enabled", "true");
            builder.UseSetting("ConnectionStrings:AgroFlowDb", $"Data Source={_dbPath};Default Timeout=5");
            builder.UseSetting("LocalDemo:TeamFile", file);
        });

        using var health = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        await using var verify = CreateContext();
        Assert.Equal(0, await verify.LocalDemoTeamAssignments.CountAsync());
        Assert.False(await verify.Transportistas.AnyAsync(t => t.NormalizedWhatsApp == "5493815550141"));
    }

    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";
    private readonly List<string> _files = [];

    private static string TeamFile(Dictionary<string, (string Name, string WhatsApp)>? overrides = null)
    {
        var entries = SeedIds.Select((seedId, i) =>
        {
            var (name, whatsApp) = overrides?.GetValueOrDefault(seedId) is { Name: not null } value
                ? value
                : ($"[SINTÉTICO] Integrante {i + 1}", $"+549381555014{i + 1}");
            return new { seedId, nombre = name, whatsApp };
        });
        return JsonSerializer.Serialize(new { version = 1, transportistas = entries });
    }

    private string WriteTeamFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"agroflow-team-{Guid.NewGuid():N}.private.json");
        File.WriteAllText(path, json);
        _files.Add(path);
        return path;
    }

    private async Task<LocalDemoTeamPreload.Result> ApplyAsync(string json)
    {
        await using var db = CreateContext();
        return await LocalDemoTeamPreload.ApplyAsync(db, LocalDemoTeamPreload.Parse(json), Clock);
    }

    private async Task StartAsync()
    {
        await using var db = CreateContext();
        await AppointmentsStartup.InitializeAsync(db, Clock, "fixture-instance-A");
    }

    private WebApplicationFactory<Program> CreateFactory(string teamFile) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("LocalDemo:Enabled", "true");
        builder.UseSetting("ConnectionStrings:AgroFlowDb", $"Data Source={_dbPath};Default Timeout=5");
        builder.UseSetting("LocalDemo:TeamFile", teamFile);
        builder.UseSetting(ApiKeySchemes.ChannelKeySetting, ChannelKey);
        builder.UseSetting(ApiKeySchemes.DashboardKeySetting, DashboardKey);
    });

    private static async Task<List<JsonElement>> Drivers(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/drivers");
        request.Headers.Add(ApiKeySchemes.DashboardHeader, DashboardKey);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
    }

    private AgroFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<AgroFlowDbContext>()
        .UseSqlite($"Data Source={_dbPath};Default Timeout=5")
        .Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in _files.Concat(new[] { "", "-wal", "-shm", "-journal" }.Select(suffix => _dbPath + suffix)))
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
