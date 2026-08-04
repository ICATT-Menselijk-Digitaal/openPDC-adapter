using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

public sealed class ObjectsPage<TData>
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }

    [JsonPropertyName("results")]
    public IReadOnlyList<ObjectResponse<TData>> Results { get; init; } = [];
}
