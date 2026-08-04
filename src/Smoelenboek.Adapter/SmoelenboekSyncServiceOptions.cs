namespace Smoelenboek.Adapter;

public sealed class SmoelenboekSyncServiceOptions
{
    public string MedewerkerObjectTypeUrl { get; set; } = string.Empty;
    public int MedewerkerObjectTypeVersion { get; set; } = 1;

    /// <summary>
    /// Departments are managed manually in OpenObjects by Groep object type — only read from here, never written.
    /// </summary>
    public string GroepObjectTypeUrl { get; set; } = string.Empty;

    public int GroepObjectTypeVersion { get; set; } = 1;
}
