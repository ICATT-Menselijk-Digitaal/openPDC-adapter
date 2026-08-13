using System.Text.Json.Serialization;

namespace OpenObjects.Client.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Taal
{
    [JsonStringEnumMemberName("nl")]
    Nl,

    [JsonStringEnumMemberName("en")]
    En,
}
