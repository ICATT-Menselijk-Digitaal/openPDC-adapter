using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

public sealed class VerantwoordelijkeOrganisatie
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("owmsIdentifier")]
    public string? OwmsIdentifier { get; init; }

    [JsonPropertyName("owmsEndDate")]
    public DateTimeOffset? OwmsEndDate { get; init; }
}
