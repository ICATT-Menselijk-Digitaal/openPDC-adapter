using System.Text.Json.Serialization;

namespace Entra.Client.Models;

public sealed class EntraUserSkills
{
    [JsonPropertyName("skills")]
    public IReadOnlyList<string> Skills { get; init; } = [];
}
