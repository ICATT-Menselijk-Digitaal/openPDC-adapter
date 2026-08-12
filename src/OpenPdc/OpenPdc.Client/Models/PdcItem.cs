using System.Text.Json.Serialization;

namespace OpenPdc.Client.Models;

public sealed class PdcItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public PdcRendered? Title { get; init; }

    [JsonPropertyName("content")]
    public PdcRendered? Content { get; init; }

    [JsonPropertyName("excerpt")]
    public PdcRendered? Excerpt { get; init; }

    [JsonPropertyName("internal_memo")]
    public string? InternalMemo { get; init; }

    [JsonPropertyName("modified")]
    public DateTimeOffset? Modified { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }
}
