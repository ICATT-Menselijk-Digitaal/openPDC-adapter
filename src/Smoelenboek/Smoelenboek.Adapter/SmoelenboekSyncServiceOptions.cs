namespace Smoelenboek.Adapter;

public sealed class SmoelenboekSyncServiceOptions
{
    public string MedewerkerObjectTypeUrl { get; set; } = string.Empty;
    public int MedewerkerObjectTypeVersion { get; set; } = 1;

    /// <summary>
    /// Afdeling objects are managed manually in OpenObjects — only read from here, never written.
    /// </summary>
    public string AfdelingObjectTypeUrl { get; set; } = string.Empty;

    public int AfdelingObjectTypeVersion { get; set; } = 1;

}
