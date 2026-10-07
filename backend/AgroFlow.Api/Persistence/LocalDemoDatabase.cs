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
        // Nombre histórico usado únicamente para detectar datos ignorados por
        // Git antes del renombre. No es un proyecto ni un namespace activo.
        if (backendDirectory is not null)
        {
            var legacyPath = Path.Combine(backendDirectory, "Dsw2025Tpi.Api", "agroflow-demo.db");
            if (File.Exists(legacyPath) || File.Exists(legacyPath + "-wal")
                || File.Exists(legacyPath + "-shm") || File.Exists(legacyPath + "-journal"))
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
}
