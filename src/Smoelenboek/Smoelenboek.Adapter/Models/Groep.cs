namespace Smoelenboek.Adapter.Models;

public sealed class Groep
{
    public required string Identificatie { get; init; }

    public required string Naam { get; init; }

    public string? Email { get; init; }
}
