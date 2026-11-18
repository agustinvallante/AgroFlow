namespace AgroFlow.Tests;

/// <summary>
/// Las pruebas que arrancan la API apuntan la precarga privada a una ruta
/// inexistente: así nunca leen el archivo real del equipo, aunque exista en
/// config/local-demo de quien corre las pruebas.
/// </summary>
internal static class TestTeamFile
{
    public static readonly string Absent =
        Path.Combine(Path.GetTempPath(), $"agroflow-sin-precarga-{Guid.NewGuid():N}.json");
}
