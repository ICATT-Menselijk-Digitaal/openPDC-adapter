using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

public sealed class Vertaling
{
    [JsonPropertyName("taal")]
    public Taal Taal { get; init; }

    [JsonPropertyName("titel")]
    public string? Titel { get; init; }

    [JsonPropertyName("tekst")]
    public string? Tekst { get; init; }

    [JsonPropertyName("datumWijziging")]
    public DateTimeOffset? DatumWijziging { get; init; }

    [JsonPropertyName("deskMemo")]
    public string? DeskMemo { get; init; }
}
