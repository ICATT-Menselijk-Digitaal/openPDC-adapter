using System.Text.Json.Serialization;

namespace Entra.Client.Models;

public sealed class EntraUser
{
    public string UserPrincipalName { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public string? GivenName { get; init; }

    public string? Surname { get; init; }

    public string? Mail { get; init; }

    public IReadOnlyList<string> BusinessPhones { get; init; } = [];

    public string? Department { get; init; }

    public string? JobTitle { get; init; }

    public bool AccountEnabled { get; init; }
}
