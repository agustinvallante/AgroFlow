using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data.Appointments;

/// <summary>
/// Inicialización de la persistencia de turnos (migrar + sembrar), separada
/// de <c>Program.cs</c> para poder probarla sin levantar el host completo.
/// </summary>
public static class AppointmentsStartup
{
    /// <summary>
    /// Migra y siembra <paramref name="db"/>. Con <paramref name="failFast"/>
    /// en verdadero (perfil LocalDemo) una falla se propaga y aborta el
    /// arranque antes de que el host empiece a escuchar: así nunca queda un
    /// proceso "aparentemente sano" con /health respondiendo sin haber
    /// inicializado. Con falso, conserva el comportamiento heredado de
    /// registrar el error y continuar.
    /// </summary>
    public static async Task InitializeAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        bool failFast,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.MigrateAsync(cancellationToken);
            await AppointmentsSeeder.SeedAsync(db, timeProvider, cancellationToken);
        }
        catch (Exception ex) when (!failFast)
        {
            // failFast=true: el filtro no aplica, la excepción no se atrapa
            // acá y sigue subiendo sin envolver, abortando el arranque.
            Console.WriteLine(" Error al inicializar la persistencia de turnos (AgroFlowDbContext):");
            Console.WriteLine(ex.ToString());
        }
    }
}
