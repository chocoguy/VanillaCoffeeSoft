using System.Globalization;

namespace Api.Helpers;

/// <summary>
/// Single source of truth for the canonical date format used everywhere:
/// RFC 3339 / ISO 8601 in UTC with an explicit 'Z' (e.g. "2026-07-06T00:00:00Z").
/// </summary>
public static class DateTimeHelper
{
    public const string Rfc3339Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private const DateTimeStyles UtcStyles =
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Accepted input formats: the canonical format itself (with or without fraction),
    /// MAL partial dates ("yyyy", "yyyy-MM", "yyyy-MM-dd"), and legacy
    /// Microsoft.Data.Sqlite text ("yyyy-MM-dd HH:mm:ss[.fffffff]").
    /// </summary>
    private static readonly string[] AcceptedFormats =
    {
        Rfc3339Format,
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd",
        "yyyy-MM",
        "yyyy",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF"
    };

    /// <summary>Utc → as-is; Local → converted; Unspecified → assumed to already be UTC.</summary>
    public static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static string ToRfc3339(DateTime value) =>
        NormalizeToUtc(value).ToString(Rfc3339Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses canonical, MAL partial ("yyyy", "yyyy-MM", "yyyy-MM-dd"), and legacy
    /// database values. Missing parts default to Jan/1st/midnight. Always returns
    /// Kind=Utc; null if the input is empty or unparseable.
    /// </summary>
    public static DateTime? ParseFlexible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParseExact(value, AcceptedFormats,
                CultureInfo.InvariantCulture, UtcStyles, out var exact))
        {
            return exact;
        }

        // Last resort: framework parsing (handles explicit offsets like +00:00)
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, UtcStyles, out var fallback)
            ? fallback
            : null;
    }
}
