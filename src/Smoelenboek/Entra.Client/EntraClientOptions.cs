namespace Entra.Client;

public sealed class EntraClientOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// $filter expression applied to the /users query, e.g.
    /// <c>endsWith(userPrincipalName,'@example.nl') and not(department eq null)</c>.
    /// Customer-specific, so it's configured per deployment rather than hardcoded.
    /// </summary>
    public string UsersFilter { get; set; } = string.Empty;
}
