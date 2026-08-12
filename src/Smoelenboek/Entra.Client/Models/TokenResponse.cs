using System.Text.Json.Serialization;

namespace Entra.Client.Models;

internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
