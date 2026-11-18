using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgroFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroFlow.Data.Appointments;

/// <summary>Error de la precarga privada. Nunca incluye nombres ni teléfonos.</summary>
public sealed class LocalDemoTeamPreloadException(string message) : Exception(message);

/// <summary>
/// Precarga privada del equipo para LocalDemo: asigna nombre y WhatsApp
/// autorizados a las posiciones ficticias del seed (<see cref="AppointmentsSeeder.DemoPositions"/>),
/// que conservan su DNI, camión y asociación. El archivo vive fuera de Git.
///
/// Control de cambios: por posición se guarda la huella de la entrada aplicada
/// (<see cref="LocalDemoTeamAssignment"/>). Una entrada sin cambios no vuelve a
/// aplicarse, así las ediciones posteriores hechas desde la API prevalecen; si
/// el equipo cambia esa entrada, se aplica de nuevo sólo esa posición. Todo se
/// valida antes de escribir y se aplica en una única transacción.
/// </summary>
public static class LocalDemoTeamPreload
{
    public const int SupportedVersion = 1;
    public const int MaxNameLength = 150;

    private static readonly Regex InternationalPhone = new(
        @"\A\+[1-9][0-9]{7,14}\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> RootKeys = new(["version", "transportistas"], StringComparer.Ordinal);
    private static readonly HashSet<string> EntryKeys = new(["seedId", "nombre", "whatsApp"], StringComparer.Ordinal);

    public sealed record Entry(string SeedId, string Name, string WhatsApp)
    {
        public string NormalizedWhatsApp => Transportista.NormalizeWhatsApp(WhatsApp);
    }

    public sealed record Result(int Applied, int Unchanged, IReadOnlyList<string> AppliedMaskedPhones);

    /// <summary>Lee y valida el archivo completo; ante cualquier error no devuelve nada aplicable.</summary>
    public static IReadOnlyList<Entry> Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new LocalDemoTeamPreloadException("El archivo de precarga no es JSON válido.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new LocalDemoTeamPreloadException("El archivo de precarga debe ser un objeto JSON.");
            RejectUnknownKeys(root, RootKeys, "la raíz");
            if (!root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out var number) || number != SupportedVersion)
                throw new LocalDemoTeamPreloadException($"\"version\" debe ser {SupportedVersion}.");
            if (!root.TryGetProperty("transportistas", out var items) || items.ValueKind != JsonValueKind.Array)
                throw new LocalDemoTeamPreloadException("\"transportistas\" debe ser una lista.");

            var known = AppointmentsSeeder.DemoPositions.Select(p => p.SeedId).ToHashSet(StringComparer.Ordinal);
            var count = items.GetArrayLength();
            if (count is < 1 || count > known.Count)
                throw new LocalDemoTeamPreloadException($"\"transportistas\" debe tener entre 1 y {known.Count} entradas.");

            var entries = new List<Entry>();
            var seedIds = new HashSet<string>(StringComparer.Ordinal);
            var phones = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var item in items.EnumerateArray())
            {
                var where = $"la entrada {index + 1}";
                if (item.ValueKind != JsonValueKind.Object)
                    throw new LocalDemoTeamPreloadException($"{where} debe ser un objeto.");
                RejectUnknownKeys(item, EntryKeys, where);

                var seedId = ReadString(item, "seedId", where);
                if (!known.Contains(seedId))
                    throw new LocalDemoTeamPreloadException(
                        $"{where}: \"seedId\" desconocido. Valores admitidos: {string.Join(", ", known)}.");
                if (!seedIds.Add(seedId))
                    throw new LocalDemoTeamPreloadException($"{where}: \"{seedId}\" está repetido.");

                var name = ReadString(item, "nombre", where).Trim();
                if (name.Length is 0 || name.Length > MaxNameLength)
                    throw new LocalDemoTeamPreloadException($"{where}: \"nombre\" debe tener entre 1 y {MaxNameLength} caracteres.");

                var whatsApp = ReadString(item, "whatsApp", where).Trim();
                if (!InternationalPhone.IsMatch(whatsApp))
                    throw new LocalDemoTeamPreloadException(
                        $"{where}: \"whatsApp\" debe estar en formato E.164 (+ código de país y número, sin espacios).");

                var entry = new Entry(seedId, name, whatsApp);
                if (!phones.Add(entry.NormalizedWhatsApp))
                    throw new LocalDemoTeamPreloadException($"{where}: el WhatsApp repite el de otra entrada.");
                entries.Add(entry);
                index++;
            }

            return entries;
        }
    }

    public static async Task<Result> ApplyAsync(
        AgroFlowDbContext db, IReadOnlyList<Entry> entries, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // 1. Resolver cada posición por su clave estable del seed.
        var plans = new List<(Entry Entry, Transportista Transportista, LocalDemoTeamAssignment? Assignment, string Fingerprint)>();
        foreach (var entry in entries)
        {
            var key = $"transportista:{entry.SeedId}";
            var record = await db.SeedRecords.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
            var transportista = record is null
                ? null
                : await db.Transportistas.FirstOrDefaultAsync(t => t.Id == record.EntityId, cancellationToken);
            if (transportista is null)
                throw new LocalDemoTeamPreloadException(
                    $"La posición \"{entry.SeedId}\" no existe en la base: el seed no pudo crearla (revisar conflictos de DNI o WhatsApp).");
            var assignment = await db.LocalDemoTeamAssignments.FirstOrDefaultAsync(a => a.SeedId == entry.SeedId, cancellationToken);
            plans.Add((entry, transportista, assignment, Fingerprint(entry)));
        }

        // 2. Sólo cambian las entradas nuevas o modificadas en el archivo.
        var changes = plans.Where(p => p.Assignment?.Fingerprint != p.Fingerprint).ToList();
        if (changes.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new Result(0, plans.Count, []);
        }

        // 3. Ningún número puede pertenecer a un transportista que no se modifica.
        var changingIds = changes.Select(c => c.Transportista.Id).ToHashSet();
        var newPhones = changes.Select(c => c.Entry.NormalizedWhatsApp).ToArray();
        var conflict = await db.Transportistas.AsNoTracking()
            .AnyAsync(t => newPhones.Contains(t.NormalizedWhatsApp) && !changingIds.Contains(t.Id), cancellationToken);
        if (conflict)
            throw new LocalDemoTeamPreloadException(
                "Un WhatsApp del archivo ya pertenece a otro transportista (otra posición o un alta hecha por la API). No se aplicó ningún cambio.");

        // 4. Dos fases dentro de la transacción: valores temporales únicos y
        // luego los definitivos, para que un intercambio de números entre
        // posiciones no viole el índice único en un paso intermedio.
        for (var i = 0; i < changes.Count; i++)
        {
            var t = changes[i].Transportista;
            t.UpdateContactDetails(t.Name, t.DNI, $"+0000000000{i:D4}");
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var (entry, transportista, assignment, fingerprint) in changes)
        {
            transportista.UpdateContactDetails(entry.Name, transportista.DNI, entry.WhatsApp);
            if (assignment is null)
                db.LocalDemoTeamAssignments.Add(new LocalDemoTeamAssignment(entry.SeedId, transportista.Id, fingerprint, now));
            else
                assignment.Reapply(fingerprint, now);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new Result(changes.Count, plans.Count - changes.Count,
            changes.Select(c => MaskPhone(c.Entry.NormalizedWhatsApp)).ToArray());
    }

    /// <summary>Para logs: "+54•••••••0101". Nunca el número completo.</summary>
    public static string MaskPhone(string digits) =>
        digits.Length <= 6 ? "+•••" : $"+{digits[..2]}{new string('•', digits.Length - 6)}{digits[^4..]}";

    private static string Fingerprint(Entry entry) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{entry.SeedId}\n{entry.Name}\n{entry.NormalizedWhatsApp}")))
            .ToLowerInvariant();

    private static void RejectUnknownKeys(JsonElement element, IReadOnlySet<string> allowed, string where)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
                throw new LocalDemoTeamPreloadException($"{where}: propiedad no admitida \"{property.Name}\".");
            if (!seen.Add(property.Name))
                throw new LocalDemoTeamPreloadException($"{where}: propiedad repetida \"{property.Name}\".");
        }
    }

    private static string ReadString(JsonElement item, string property, string where) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new LocalDemoTeamPreloadException($"{where}: falta \"{property}\" como texto.");
}
