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
    SmoelenboekSyncServiceOptions options,
    ILogger<SmoelenboekSyncService> logger) : ISmoelenboekSyncService
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching users from Entra...");
        var users = new List<EntraUser>();
        await foreach (var user in entraClient.GetAllUsersAsync(cancellationToken))
        {
            users.Add(user);
        }
        logger.LogInformation("Fetched {Count} user(s).", users.Count);

        var groepenByNaam = await LoadGroepenByNaamAsync(cancellationToken);

        await SyncMedewerkersAsync(users, groepenByNaam, cancellationToken);

        logger.LogInformation("Smoelenboek sync completed.");
    }

    // Groep objects are entered manually in OpenObjects, not synced from Entra — only read here.
    // The Entra user's Department is matched by name against these Groep objects (see
    // BuildGroepRefs) to resolve the groepsId that goes on each medewerker's groepen reference.
    private async Task<Dictionary<string, Groep>> LoadGroepenByNaamAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, Groep>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await foreach (var obj in objectsClient.GetAllObjectsByObjectTypeUrlAsync<Groep>(options.GroepObjectTypeUrl, ct))
            {
                var data = obj.Record?.Data;
                if (data is null)
                {
                    continue;
                } 

                if (!result.TryAdd(data.Naam, data))
                {
                    logger.LogWarning("Duplicate groep with naam '{Naam}' found; keeping the first match.", data.Naam);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to load groepen from OpenObjects.");
        }
        return result;
    }

    private async Task SyncMedewerkersAsync(
        List<EntraUser> users,
        Dictionary<string, Groep> groepenByNaam,
        CancellationToken ct)
    {
        var existingMedewerkers = await LoadExistingMedewerkersByIdentificatieAsync(ct);

        var syncedMedewerkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            var identificatie = user.UserPrincipalName;
            syncedMedewerkers.Add(identificatie);

            var phones = CollectPhones(user);
            var emails = user.Mail is { Length: > 0 }
                ? (IReadOnlyList<EmailRef>)[new EmailRef { Email = user.Mail, Naam = user.DisplayName }]
                : null;


            var groepen = BuildGroepRefs(user, groepenByNaam);

            var data = new Medewerker
            {
                Identificatie   = identificatie,
                Voornaam        = user.GivenName,
                Achternaam      = user.Surname,
                VolledigeNaam   = user.DisplayName,
                Telefoonnummers = phones.Count > 0 ? phones : null,
                Emails          = emails,
                // Replace this with a reference to a single
                // manually-created Afdeling meant to contain all the Groepen objects.
                Afdelingen      = [new AfdelingRef { Afdelingnaam = user.Department ?? string.Empty }],
                Groepen         = groepen,
            };
            var createMedewerkerRequest = BuildCreateMedewerkerRequest(data);
            await UpsertMedewerkerAsync(identificatie, createMedewerkerRequest, existingMedewerkers, ct);
        }

        await DeleteOrphanMedewerkersAsync(syncedMedewerkers, existingMedewerkers, ct);
    }

    private IReadOnlyList<GroepenRef> BuildGroepRefs(EntraUser user, Dictionary<string, Groep> groepenByNaam)
    {
        if (user.Department is not { Length: > 0 } department)
        {
            return [];
        }
        
        if (groepenByNaam.TryGetValue(department, out var groep))
        {
            return [new GroepenRef { Groepsnaam = department, GroepsId = groep.Identificatie }];
        }

        logger.LogWarning(
            "No groep found in OpenObjects matching name '{Department}' for user '{User}'. Setting with only the name.",
            department, user.UserPrincipalName);
        return [new GroepenRef { Groepsnaam = department }];
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
                    logger.LogWarning("Duplicate medewerker with identificatie '{Id}' found; skipping {Uuid}.", id, obj.Uuid);
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
