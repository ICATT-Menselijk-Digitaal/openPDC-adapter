using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPdc.Client.Models;

/// <summary>
/// Parses the non-ISO date strings produced by the OpenPDC WordPress endpoint
/// (e.g. <c>"2026-04-29 13:18:38"</c>), while still accepting standard ISO 8601 values.
/// Empty strings and <c>null</c> are returned as <c>null</c>.
/// </summary>
internal sealed class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.fffK",
        "yyyy-MM-dd",
    ];

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token {reader.TokenType} when parsing DateTimeOffset.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var iso))
        {
            return iso;
        }

        if (DateTimeOffset.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exact))
        {
            return exact;
        }

        throw new JsonException($"Unsupported date value '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("O", CultureInfo.InvariantCulture));
    }
}
