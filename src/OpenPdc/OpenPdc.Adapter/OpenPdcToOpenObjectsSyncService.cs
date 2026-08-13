using Microsoft.Extensions.Logging;
using OpenObjects.Client;
using OpenObjects.Client.Models;
using OpenPdc.Client;
using OpenPdc.Client.Models;

namespace OpenPdc.Adapter;

public sealed class OpenPdcToOpenObjectsSyncService(
    IOpenPdcClient pdcClient,
    IOpenObjectsClient objectsClient,
    OpenPdcToOpenObjectsSyncOptions options,
    ILogger<OpenPdcToOpenObjectsSyncService> logger) : IOpenPdcToOpenObjectsSyncService
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var products     = await CollectItemsAsync("product",     cancellationToken);
        var pages        = await CollectItemsAsync("pages",       cancellationToken);
        var publications = await CollectItemsAsync("publication", cancellationToken);

        List<PdcRequest> allPdcItems = [.. products, .. pages, .. publications];
        logger.LogInformation("Collected {Count} PDC item(s) in total.", allPdcItems.Count);

        Dictionary<long, ObjectResponse<ObjectData>> existingByItemId;
        try
        {
            existingByItemId = await BuildExistingLookupAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to retrieve existing OpenObjects records. Aborting sync.");
            return;
        }

        var processedCount = await UpsertItemsAsync(allPdcItems, existingByItemId, cancellationToken);
        var deletedCount   = await DeleteOrphansAsync(allPdcItems, existingByItemId, cancellationToken);
        logger.LogInformation("Done. Processed {ProcessedCount} item(s), deleted {DeletedCount} orphan(s).", processedCount, deletedCount);
    }

    private async Task<List<PdcRequest>> CollectItemsAsync(string contentType, CancellationToken cancellationToken)
    {
        var requests = new List<PdcRequest>();
        await foreach (var item in pdcClient.GetAllItemsAsync(contentType, cancellationToken: cancellationToken))
            requests.Add(new PdcRequest(item.Id, MapToRequest(item, contentType)));
        return requests;
    }

    // Fetches all existing OpenObjects records, deletes any duplicates, and returns a lookup by PDC item id.
    private async Task<Dictionary<long, ObjectResponse<ObjectData>>> BuildExistingLookupAsync(CancellationToken cancellationToken)
    {
        var objectsByItemId = new Dictionary<long, List<ObjectResponse<ObjectData>>>();
        await foreach (var obj in objectsClient.GetAllObjectsByObjectTypeUrlAsync<ObjectData>(options.ObjectTypeUrl, cancellationToken))
        {
            var dataUrl = obj.Record?.Data?.Url;
            if (dataUrl is null)
                continue;

            var rawItemId = dataUrl.TrimEnd('/').Split('/')[^1];
            if (!long.TryParse(rawItemId, out var itemId))
                continue;

            if (!objectsByItemId.TryGetValue(itemId, out var list))
                objectsByItemId[itemId] = list = [];
            list.Add(obj);
        }

        var existingByItemId = new Dictionary<long, ObjectResponse<ObjectData>>();
        foreach (var (itemId, objects) in objectsByItemId)
        {
            existingByItemId[itemId] = objects[0];
            for (var i = 1; i < objects.Count; i++)
            {
                var duplicate = objects[i];
                try
                {
                    await objectsClient.DeleteObjectAsync(duplicate.Uuid, cancellationToken);
                    logger.LogWarning("Deleted duplicate object {Uuid} for PDC item {ItemId}.", duplicate.Uuid, itemId);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Failed to delete duplicate object {Uuid} for PDC item {ItemId}.", duplicate.Uuid, itemId);
                }
            }
        }
        return existingByItemId;
    }

    private async Task<int> UpsertItemsAsync(
        IReadOnlyList<PdcRequest> requests,
        Dictionary<long, ObjectResponse<ObjectData>> existingByItemId,
        CancellationToken cancellationToken)
    {
        var processedCount = 0;
        foreach (var (itemId, request) in requests)
        {
            try
            {
                if (existingByItemId.TryGetValue(itemId, out var existing))
                    await objectsClient.DeleteObjectAsync(existing.Uuid, cancellationToken);
                await objectsClient.PostObjectAsync(request, cancellationToken);
                processedCount++;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to process PDC item {Id} ({Done}/{Total} processed before failure). Continuing sync.", itemId, processedCount, requests.Count);
            }
        }
        return processedCount;
    }

    private async Task<int> DeleteOrphansAsync(
        IReadOnlyList<PdcRequest> requests,
        Dictionary<long, ObjectResponse<ObjectData>> existingByItemId,
        CancellationToken cancellationToken)
    {
        var pdcIds = requests.Select(r => r.ItemId).ToHashSet();
        var orphans = existingByItemId.Where(kvp => !pdcIds.Contains(kvp.Key)).ToList();
        var deletedCount = 0;
        foreach (var (itemId, obj) in orphans)
        {
            try
            {
                await objectsClient.DeleteObjectAsync(obj.Uuid, cancellationToken);
                deletedCount++;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to delete orphaned object {Uuid} (PDC item {ItemId}), {Deleted}/{Total} deleted before failure. Continuing orphan check.", obj.Uuid, itemId, deletedCount, orphans.Count);
            }
        }
        return deletedCount;
    }

    private CreateObjectRequestBody<ObjectData> MapToRequest(PdcItem item, string contentType) =>
        new()
        {
            Type = $"{options.ObjectTypeUrl}",
            Record = new ObjectRecord<ObjectData>
            {
                TypeVersion = options.ObjectTypeVersion,
                StartAt     = DateOnly.FromDateTime(DateTime.UtcNow),
                Data = new ObjectData
                {
                    Url             = $"{pdcClient.BaseUrl}/{contentType}/{item.Id}",
                    // made up per-item UUID — Elasticsearch deduplicates by UUID, so each item needs a unique one
                    Uuid            = $"00000000-0000-0000-0000-{item.Id:D12}",
                    UpnUri          = "unknown",
                    PublicatieDatum = item.Modified is { } dto ? DateOnly.FromDateTime(dto.UtcDateTime) : null,
                    ProductAanwezig = true,
                    Doelgroep       = "eu-burger",
                    VerantwoordelijkeOrganisatie = new VerantwoordelijkeOrganisatie
                    {
                        Url = pdcClient.BaseUrl,
                        OwmsIdentifier = pdcClient.BaseUrl,
                        OwmsEndDate = new DateTime(3000, 1, 1),
                    },
                    Vertalingen = [
                        new Vertaling
                        {
                            Taal           = Taal.Nl,
                            Titel          = BuildTitel(item.Title?.Rendered, contentType),
                            Tekst          = BuildTekst(item.Content?.Rendered, item.Excerpt?.Rendered, item.Link),
                            DatumWijziging = item.Modified,
                            DeskMemo       = item.InternalMemo ?? string.Empty,
                        }
                    ],
                    BeschikbareTalen = ["nl"],
                },
            },
        };

    private static string? BuildTitel(string? titel, string contentType) => contentType switch
    {
        "publication" => $"Publicatie: {titel}",
        "pages"       => $"Website: {titel}",
        _             => titel,
    };

    private static string? BuildTekst(string? content, string? excerpt, string? link)
    {
        var tekst = string.IsNullOrEmpty(content) ? excerpt : content;
        return string.IsNullOrEmpty(link)
            ? tekst
            : $"{tekst}<hr><a href='{link}' target='_blank'>Bron</a>";
    }

    private readonly record struct PdcRequest(long ItemId, CreateObjectRequestBody<ObjectData> Request);
}
