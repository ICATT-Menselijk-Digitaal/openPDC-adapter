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

    // Schema types these as "null" and lists them as required: the key must be present
    // with a literal JSON null, not omitted — override the client's omit-null default.
    [JsonPropertyName("productValtOnder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public object? ProductValtOnder { get; init; } = null;

    [JsonPropertyName("locaties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
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
