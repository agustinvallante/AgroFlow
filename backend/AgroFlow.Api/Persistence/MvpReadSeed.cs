using AgroFlow.Api.Authorization;
using AgroFlow.Data.Appointments;
using AgroFlow.Data.Identity;
using AgroFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Api.Persistence;

internal static class MvpReadSeed
{
    private const string SeedKey = "mvp-appointment-reads:v1";

    public static async Task ApplyAsync(IServiceProvider services, string password)
    {
        var db = services.GetRequiredService<AgroFlowDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await db.SeedRecords.AnyAsync(record => record.Key == SeedKey))
            return;

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<AgroFlowUser>>();
        foreach (var role in AgroFlowRoles.ApprovedInternalLogin)
        {
            if (!await roles.RoleExistsAsync(role))
                RequireSuccess(await roles.CreateAsync(new IdentityRole(role)));
        }

        var day = new DateTimeOffset(2026, 7, 15, 9, 30, 0, TimeSpan.FromHours(-3));
        var ingenioA = AddAppointment(db, "A", day, "20303030301", "+5493815550171", "AH401AB", TurnoEstado.Cancelado);
        var ingenioB = AddAppointment(db, "B", day, "20303030302", "+5493815550172", "AH402AB", TurnoEstado.Finalizado);
        db.SeedRecords.Add(new SeedRecord(SeedKey, ingenioA.Id, day.UtcDateTime));
        await db.SaveChangesAsync();

        var accounts = new[]
        {
            ("operador-mvp-a", AgroFlowRoles.Operador, ingenioA.Id),
            ("supervisor-mvp-a", AgroFlowRoles.Supervisor, ingenioA.Id),
            ("operador-mvp-b", AgroFlowRoles.Operador, ingenioB.Id)
        };
        foreach (var (name, role, ingenioId) in accounts)
        {
            var user = new AgroFlowUser { UserName = name, IngenioId = ingenioId };
            RequireSuccess(await users.CreateAsync(user, password));
            RequireSuccess(await users.AddToRoleAsync(user, role));
        }

        await transaction.CommitAsync();
    }

    private static Ingenio AddAppointment(AgroFlowDbContext db, string label, DateTimeOffset start,
        string dni, string phone, string plate, TurnoEstado status)
    {
        var ingenio = new Ingenio($"Ingenio de prueba {label}", "America/Argentina/Tucuman");
        var carrier = new Transportista(ingenio.Id, $"Transportista de prueba {label}", dni, phone);
        var truck = new Camion(ingenio.Id, plate, Camion.FleetTypePropia);
        var farm = new Finca(ingenio.Id, $"MVP-PRUEBA-{label}", $"Finca de prueba {label}", "Ubicación ficticia");
        var window = new Ventana(ingenio.Id, start, start.AddMinutes(30), capacity: 2);
        var appointment = new Turno(ingenio.Id, carrier.Id, truck.Id, farm.Id, window.Id, start.AddHours(-3), 28.5, start.AddHours(-2));
        appointment.SetStatus(status);
        db.AddRange(ingenio, carrier, truck, farm, window, appointment);
        return ingenio;
    }

    private static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudieron crear las cuentas ficticias: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
    }
}
