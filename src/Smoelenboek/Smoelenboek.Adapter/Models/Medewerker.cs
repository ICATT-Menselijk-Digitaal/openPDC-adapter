using System.Text.Json.Serialization;

namespace Smoelenboek.Adapter.Models;

public sealed class Medewerker
{
    public required string Identificatie { get; init; }

    public string? Voornaam { get; init; }

    public string? Achternaam { get; init; }

    public string? VolledigeNaam { get; init; }

    public string? Functie { get; init; }

    [JsonPropertyName("telefoonnummers")]
    public IReadOnlyList<TelefoonnummerRef>? Telefoonnummers { get; init; }

    [JsonPropertyName("emails")]
    public IReadOnlyList<EmailRef>? Emails { get; init; }

    [JsonPropertyName("afdelingen")]
    public required IReadOnlyList<AfdelingRef> Afdelingen { get; init; }

    [JsonPropertyName("groepen")]
    public IReadOnlyList<GroepenRef>? Groepen { get; init; }

    [JsonPropertyName("skills")]
    public string? Skills { get; init; }

}

public sealed class TelefoonnummerRef
{
    public required string Telefoonnummer { get; init; }
}

public sealed class EmailRef
{
    public required string Email { get; init; }

    public string? Naam { get; init; }
}

public sealed class AfdelingRef
{

    public required string Afdelingnaam { get; init; }

    public string? AfdelingId { get; init; }
}

public sealed class GroepenRef
{
    public required string Groepsnaam { get; init; }

    public string? GroepsId { get; init; }
}
