using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Helpers;

/// <summary>
/// Writes every DateTime as RFC 3339 UTC with an explicit 'Z' regardless of Kind;
/// reads any accepted ISO 8601 input and normalizes it to Kind=Utc.
/// </summary>
public sealed class Rfc3339JsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handles 'Z' and ±hh:mm offsets natively
        if (reader.TryGetDateTimeOffset(out var dto))
            return dto.UtcDateTime;

        var raw = reader.GetString();
        return DateTimeHelper.ParseFlexible(raw)
               ?? throw new JsonException($"Invalid date value '{raw}'; expected RFC 3339 UTC (e.g. 2026-07-06T00:00:00Z)");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTimeHelper.ToRfc3339(value));
}

public sealed class NullableRfc3339JsonConverter : JsonConverter<DateTime?>
{
    private static readonly Rfc3339JsonConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : Inner.Read(ref reader, typeof(DateTime), options);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            Inner.Write(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}
