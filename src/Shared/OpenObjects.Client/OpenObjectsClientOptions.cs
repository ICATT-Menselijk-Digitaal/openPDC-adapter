namespace OpenObjects.Client;

public sealed class OpenObjectsClientOptions
{
    public const string DefaultBaseUrl = "http://localhost:8000";

    /// <summary>
    /// Base URL of the OpenObjects API. Must end with a trailing slash.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// Token used for the <c>Authorization: Token &lt;value&gt;</c> request header.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
