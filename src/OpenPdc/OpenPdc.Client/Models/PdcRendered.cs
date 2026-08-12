using System.Text.Json.Serialization;

namespace OpenPdc.Client.Models;

public sealed class PdcRendered
{
    [JsonPropertyName("rendered")]
    public string? Rendered { get; init; }
}
