using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

public sealed class CreateObjectRequestBody<TData>
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("record")]
    public required ObjectRecord<TData> Record { get; init; }
}
