using AgroFlow.Data.Appointments;

namespace AgroFlow.Api.Persistence;

/// <summary>
/// Arranque de la precarga privada del equipo (sólo LocalDemo y nunca en
/// Production). El archivo se busca en <c>LocalDemo:TeamFile</c> o, por
/// defecto, en <c>config/local-demo/transportistas.private.json</c> de la raíz
/// del repositorio. Si no existe, la demo arranca con los datos ficticios; si
/// existe y es inválido, el arranque falla sin aplicar nada.
/// </summary>
public static class LocalDemoTeamFile
{
    public const string Setting = "LocalDemo:TeamFile";
    private static readonly string[] DefaultRelativePath = ["..", "..", "config", "local-demo", "transportistas.private.json"];

    public static string ResolvePath(string? configured, string contentRootPath) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(Path.Combine([contentRootPath, .. DefaultRelativePath]))
            : Path.GetFullPath(configured, contentRootPath);

    public static async Task ApplyIfPresentAsync(
        AgroFlowDbContext db,
        TimeProvider timeProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(configuration[Setting], environment.ContentRootPath);
        if (!File.Exists(path))
        {
            logger.LogInformation("Sin precarga privada del equipo: se usan los transportistas ficticios.");
            return;
        }

        if (environment.IsProduction())
        {
            // Datos personales del equipo: nunca fuera de un entorno de desarrollo.
            logger.LogWarning("Se ignoró la precarga privada del equipo: no se aplica en Production.");
            return;
        }

        IReadOnlyList<LocalDemoTeamPreload.Entry> entries;
        try
        {
            entries = LocalDemoTeamPreload.Parse(await File.ReadAllTextAsync(path, cancellationToken));
        }
        catch (LocalDemoTeamPreloadException exception)
        {
            throw new InvalidOperationException($"Precarga privada inválida ({Path.GetFileName(path)}): {exception.Message}", exception);
        }

        LocalDemoTeamPreload.Result result;
        try
        {
            result = await LocalDemoTeamPreload.ApplyAsync(db, entries, timeProvider, cancellationToken);
        }
        catch (LocalDemoTeamPreloadException exception)
        {
            throw new InvalidOperationException($"No se aplicó la precarga privada: {exception.Message}", exception);
        }

        logger.LogInformation(
            "Precarga privada del equipo: {Applied} posiciones aplicadas ({Phones}), {Unchanged} sin cambios.",
            result.Applied, string.Join(", ", result.AppliedMaskedPhones), result.Unchanged);
    }
}
