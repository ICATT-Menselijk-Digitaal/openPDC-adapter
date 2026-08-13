using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

public sealed class ObjectData
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("upnUri")]
    public string? UpnUri { get; init; }

    [JsonPropertyName("publicatieDatum")]
    public DateOnly? PublicatieDatum { get; init; }

    [JsonPropertyName("productAanwezig")]
    public bool? ProductAanwezig { get; init; }

    [JsonPropertyName("productValtOnder")]
    public object? ProductValtOnder { get; init; } = null;

    [JsonPropertyName("locaties")]
    public object? Locaties { get; init; } = null;

    [JsonPropertyName("verantwoordelijkeOrganisatie")]
    public VerantwoordelijkeOrganisatie? VerantwoordelijkeOrganisatie { get; init; }

    [JsonPropertyName("doelgroep")]
    public string? Doelgroep { get; init; }

    [JsonPropertyName("vertalingen")]
    public IReadOnlyList<Vertaling> Vertalingen { get; init; } = [];

    [JsonPropertyName("beschikbareTalen")]
    public IReadOnlyList<string> BeschikbareTalen { get; init; } = [];
}
