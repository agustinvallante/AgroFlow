using Microsoft.Data.Sqlite;

namespace AgroFlow.Api.Persistence;

/// <summary>
/// Resuelve rutas de SQLite sin depender del directorio desde donde se ejecuta
/// dotnet. No traslada, copia ni elimina bases existentes.
/// </summary>
public static class LocalDemoDatabase
{
    public static string ResolveConnectionString(string? connectionString, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:AgroFlowDb para la demo local.");

        var settings = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(settings.DataSource))
            throw new InvalidOperationException("AgroFlowDb debe identificar una base SQLite.");

        if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
            return settings.ToString();

        if (Path.IsPathFullyQualified(settings.DataSource))
            return settings.ToString();

        var root = Path.GetFullPath(contentRootPath);
        var backendDirectory = Path.GetDirectoryName(root);
        // El nombre histórico predeterminado sigue protegido aunque se configure
        // otro destino relativo. También se busca el counterpart de la ruta
        // configurada, normalizada desde la raíz histórica de la API.
        if (backendDirectory is not null)
        {
            var legacyRoot = Path.Combine(backendDirectory, "Dsw2025Tpi.Api");
            var legacyPaths = new HashSet<string>(PathComparer)
            {
                Path.Combine(legacyRoot, "agroflow-demo.db"),
                Path.GetFullPath(settings.DataSource, legacyRoot)
            };
            if (legacyPaths.Any(HasSqliteFiles))
            {
                throw new InvalidOperationException(
                    "Se detectaron archivos SQLite de la demo anterior. Detenga la API, respalde los datos " +
                    "y configure ConnectionStrings__AgroFlowDb con una ruta absoluta explícita y Mode=ReadWrite " +
                    "para reutilizar la base elegida. Consulte docs/development/local-demo-runbook.md. " +
                    "No se creará ni seleccionará otra base automáticamente.");
            }
        }

        settings.DataSource = Path.GetFullPath(settings.DataSource, root);
        return settings.ToString();
    }

    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static bool HasSqliteFiles(string path) => File.Exists(path) || File.Exists(path + "-wal")
        || File.Exists(path + "-shm") || File.Exists(path + "-journal");
}
