using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Data.Appointments;

/// <summary>
/// Inicialización de la persistencia de turnos (migrar + sembrar), separada
/// de <c>Program.cs</c> para poder probarla sin levantar el host completo.
/// </summary>
public static class AppointmentsStartup
{
    /// <summary>
    /// Migra y siembra <paramref name="db"/>. Una falla se propaga y aborta
    /// el arranque antes de que el host empiece a escuchar.
    /// </summary>
    public static async Task InitializeAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await AppointmentsSeeder.SeedAsync(db, timeProvider, cancellationToken);
    }
}
