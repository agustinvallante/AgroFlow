using System.Globalization;
using System.Text;

namespace AgroFlow.Application.MasterData;

/// <summary>
/// Resuelve cómo nombra una persona a una finca ("Finca Norte", "norte",
/// "FINCA-NORTE") sin que el canal mantenga una tabla propia de códigos.
/// Compara palabras completas, sin mayúsculas, tildes ni separadores, y descarta
/// la palabra genérica "finca" y los artículos. Cada palabra buscada debe coincidir con una
/// palabra del código o del nombre: no hay coincidencias parciales ni por
/// similitud, para no proponer una finca que la persona no nombró. Varias
/// coincidencias se devuelven todas: decidir entre ellas le corresponde a quien
/// consulta, que debe pedir aclaración.
/// </summary>
public static class FarmSearch
{
    public const int MaxLength = 100;

    // Se descartan de la búsqueda y del catálogo por igual, así que "la finca
    // del norte" equivale a "Norte" sin volver ambiguo ningún nombre.
    private static readonly HashSet<string> GenericWords =
        new(["finca", "fincas", "la", "las", "el", "los", "de", "del"], StringComparer.Ordinal);

    public static IReadOnlyList<MasterDataItem> Filter(IReadOnlyList<MasterDataItem> farms, string? search)
    {
        if (search is null)
            return farms;

        var wanted = Words(search);
        // Sin palabras distintivas ("finca") la búsqueda no elige ninguna:
        // devuelve todas para que el consumidor pida aclaración.
        if (wanted.Count == 0)
            return farms;

        return farms
            .Where(farm =>
            {
                var words = Words(farm.Code ?? string.Empty);
                words.UnionWith(Words(farm.Name));
                return wanted.IsSubsetOf(words);
            })
            .ToArray();
    }

    private static HashSet<string> Words(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !GenericWords.Contains(word))
            .ToHashSet(StringComparer.Ordinal);
    }
}
