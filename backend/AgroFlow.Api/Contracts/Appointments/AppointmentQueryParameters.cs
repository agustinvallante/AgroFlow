namespace AgroFlow.Api.Contracts.Appointments;

internal static class AppointmentQueryParameters
{
    public static string? ReadSingle(IQueryCollection query, string key, Dictionary<string, string[]> errors)
    {
        if (!query.TryGetValue(key, out var values))
            return null;

        if (values.Count == 1 && !string.IsNullOrWhiteSpace(values[0]))
            return values[0];

        errors[key] = ["Debe indicarse exactamente un valor no vacío."];
        return null;
    }
}
