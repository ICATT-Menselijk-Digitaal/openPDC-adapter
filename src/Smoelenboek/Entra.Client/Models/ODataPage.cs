using System.Text.Json.Serialization;

namespace Entra.Client.Models;

public sealed class ODataPage<T>
{
    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; init; }

    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];
}
