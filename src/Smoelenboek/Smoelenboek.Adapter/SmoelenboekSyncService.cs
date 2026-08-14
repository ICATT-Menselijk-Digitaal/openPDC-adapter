using Entra.Client;
using Entra.Client.Models;
using Microsoft.Extensions.Logging;
using OpenObjects.Client;
using OpenObjects.Client.Models;
using Smoelenboek.Adapter.Models;

namespace Smoelenboek.Adapter;

public sealed class SmoelenboekSyncService(
    IEntraClient entraClient,
    IOpenObjectsClient objectsClient,
    EntraClientOptions entraOptions,
    SmoelenboekSyncServiceOptions options,
    ILogger<SmoelenboekSyncService> logger) : ISmoelenboekSyncService
{
    // Selects fields the Medewerker sync needs. UsersFilter is
    // customer-specific (e.g. restricting to a domain and requiring a department) and optional: Graph
    // treats an empty $filter as no filter at all, so leaving it unset fetches every user.
    private const string UsersSelect =
        "displayName,userPrincipalName,department,givenName,surname,mail,businessPhones,jobTitle,accountEnabled";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching users from Entra...");
        var usersRequestUri =
            $"users?$select={UsersSelect}&$filter={Uri.EscapeDataString(entraOptions.UsersFilter)}&$count=true&$top=999";
        var users = new List<EntraUser>();
        await foreach (var user in entraClient.GetAllAsync<EntraUser>(usersRequestUri, cancellationToken))
        {
            users.Add(user);
        }
        logger.LogInformation("Fetched {Count} user(s).", users.Count);

        var afdelingenByNaam = await LoadAfdelingenAsync(cancellationToken);

        await SyncMedewerkersAsync(users, afdelingenByNaam, cancellationToken);

        logger.LogInformation("Smoelenboek sync completed.");
    }

    // Afdeling objects are entered manually in OpenObjects, not synced from Entra — only read here.
    // The Entra user's Department is matched by name against these Afdeling objects (see
    // BuildAfdelingRefs) to resolve the afdelingId that goes on each medewerker's afdelingen reference.
    private async Task<Dictionary<string, Afdeling>> LoadAfdelingenAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, Afdeling>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await foreach (var obj in objectsClient.GetAllObjectsByObjectTypeUrlAsync<Afdeling>(options.AfdelingObjectTypeUrl, ct))
            {
                var data = obj.Record?.Data;
                if (data is null)
                {
                    continue;
                } 

                if (!result.TryAdd(data.Naam, data))
                {
                    logger.LogWarning("Duplicate afdeling with naam '{Naam}' found; keeping the first match.", data.Naam);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to load afdelingen from OpenObjects.");
        }
        return result;
    }

    private async Task SyncMedewerkersAsync(
        List<EntraUser> users,
        Dictionary<string, Afdeling> afdelingenByNaam,
        CancellationToken ct)
    {
        var existingMedewerkers = await LoadExistingMedewerkersByIdentificatieAsync(ct);

        logger.LogWarning(
            "Existing medewerkers in OpenObjects: {Count}. Total users in Entra (including disabled which will not be synced): {Count}.",
            existingMedewerkers.Count, users.Count);

        var syncedMedewerkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            var isSharedMailbox = user.JobTitle == "Shared Mailbox";
            if (!isSharedMailbox && !user.AccountEnabled)
            {
                continue;
            }

            var identificatie = user.UserPrincipalName;
            syncedMedewerkers.Add(identificatie);

            var phones = CollectPhones(user);
            var emails = user.Mail is { Length: > 0 }
                ? (IReadOnlyList<EmailRef>)[new EmailRef { Email = user.Mail, Naam = user.DisplayName }]
                : null;


            var afdelingen = BuildAfdelingRefs(user, afdelingenByNaam);
            var skills = await FetchSkillsAsync(user.UserPrincipalName, ct);

            var data = new Medewerker
            {
                Identificatie   = identificatie,
                Voornaam        = isSharedMailbox ? user.DisplayName : user.GivenName,
                Achternaam      = isSharedMailbox ? "Shared mailbox" : user.Surname,
                VolledigeNaam   = user.DisplayName,
                Telefoonnummers = phones.Count > 0 ? phones : null,
                Emails          = emails,
                Afdelingen      = afdelingen,
                Functie         = user.JobTitle,
                Skills          = skills
            };
            var createMedewerkerRequest = BuildCreateMedewerkerRequest(data);
            await UpsertMedewerkerAsync(identificatie, createMedewerkerRequest, existingMedewerkers, ct);
        }

        await DeleteOrphanMedewerkersAsync(syncedMedewerkers, existingMedewerkers, ct);
    }

    private IReadOnlyList<AfdelingRef> BuildAfdelingRefs(EntraUser user, Dictionary<string, Afdeling> afdelingenByNaam)
    {
        if (user.Department is not { Length: > 0 } department)
        {
            return [];
        }

        if (afdelingenByNaam.TryGetValue(department, out var afdeling))
        {
            return [new AfdelingRef { Afdelingnaam = department, AfdelingId = afdeling.Identificatie }];
        }

        logger.LogWarning(
            "No afdeling found in OpenObjects matching name '{Department}' for user '{User}'. Setting with only the name.",
            department, user.UserPrincipalName);
        return [new AfdelingRef { Afdelingnaam = department }];
    }

    private async Task<string?> FetchSkillsAsync(string userPrincipalName, CancellationToken ct)
    {
        var requestUri = $"users/{Uri.EscapeDataString(userPrincipalName)}?$select=skills";
        try
        {
            var result = await entraClient.GetAsync<EntraUserSkills>(requestUri, ct);
            var skills = result?.Skills ?? [];
            return skills.Count > 0 ? string.Join(", ", skills) : null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch skills for user '{User}'.", userPrincipalName);
            return null;
        }
    }

    private async Task<Dictionary<string, ObjectResponse<Medewerker>>> LoadExistingMedewerkersByIdentificatieAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, ObjectResponse<Medewerker>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await foreach (var obj in objectsClient.GetAllObjectsByObjectTypeUrlAsync<Medewerker>(options.MedewerkerObjectTypeUrl, ct))
            {
                var id = obj.Record?.Data?.Identificatie;
                if (id is null)
                {
                  continue;  
                } 

                if (!result.TryAdd(id, obj))
                {
                    logger.LogWarning("Duplicate medewerker with identificatie '{Id}' found; deleting {Uuid}.", id, obj.Uuid);
                    try
                    {
                        await objectsClient.DeleteObjectAsync(obj.Uuid, ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        logger.LogError(ex, "Failed to delete duplicate medewerker {Uuid} (identificatie '{Id}').", obj.Uuid, id);
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to load existing medewerkers.");
        }
        return result;
    }

    private async Task UpsertMedewerkerAsync(
        string identificatie,
        CreateObjectRequestBody<Medewerker> request,
        Dictionary<string, ObjectResponse<Medewerker>> existing,
        CancellationToken ct)
    {
        try
        {
            if (existing.TryGetValue(identificatie, out var obj))
                await objectsClient.DeleteObjectAsync(obj.Uuid, ct);
            await objectsClient.PostObjectAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to process medewerker '{Id}'.", identificatie);
        }
    }

    private async Task DeleteOrphanMedewerkersAsync(
        HashSet<string> synced,
        Dictionary<string, ObjectResponse<Medewerker>> existing,
        CancellationToken ct)
    {
        var orphans = existing.Where(kvp => !synced.Contains(kvp.Key)).ToList();
        foreach (var (id, obj) in orphans)
        {
            try
            {
                await objectsClient.DeleteObjectAsync(obj.Uuid, ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to delete orphaned medewerker '{Id}' ({Uuid}).", id, obj.Uuid);
            }
        }
        if (orphans.Count > 0)
            logger.LogInformation("Deleted {Count} orphaned medewerker object(s).", orphans.Count);
    }

    private CreateObjectRequestBody<Medewerker> BuildCreateMedewerkerRequest(Medewerker data) =>
        new()
        {
            Type   = options.MedewerkerObjectTypeUrl,
            Record = new ObjectRecord<Medewerker>
            {
                TypeVersion = options.MedewerkerObjectTypeVersion,
                StartAt     = DateOnly.FromDateTime(DateTime.UtcNow),
                Data        = data,
            },
        };

    private static IReadOnlyList<TelefoonnummerRef> CollectPhones(EntraUser user)
    {
        var phones = new List<TelefoonnummerRef>();
        foreach (var phone in user.BusinessPhones)
        {
            if (phone is { Length: > 0 })
            {
                phones.Add(new TelefoonnummerRef { Telefoonnummer = phone });
            }
        }

        return phones;
    }
}
