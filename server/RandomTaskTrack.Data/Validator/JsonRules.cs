using System.Text.Json;

namespace RandomTaskTrack.Data.Validator;

/// <summary>
/// Shared predicates for the jsonb-backed payload fields. These columns are
/// written straight into `jsonb`, so anything that is not a well-formed JSON
/// object has to be rejected at the edge — Postgres would otherwise reject it
/// mid-transaction with a much less useful message.
/// </summary>
public static class JsonRules
{
    public static bool BeAJsonObjectOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return IsJsonObject(value);
    }

    public static bool BeAJsonObject(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && IsJsonObject(value);
    }

    private static bool IsJsonObject(string value)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
