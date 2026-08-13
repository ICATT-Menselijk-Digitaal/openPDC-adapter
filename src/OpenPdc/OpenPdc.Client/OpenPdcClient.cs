using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenPdc.Client.Models;

namespace OpenPdc.Client;

public sealed class OpenPdcClient : IOpenPdcClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new FlexibleDateTimeOffsetConverter() },
    };

    private readonly HttpClient _httpClient;

    public OpenPdcClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public string BaseUrl => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;

    private async Task<(IReadOnlyList<PdcItem> Items, int TotalPages)> GetItemsAsync(
        string contentType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var requestUri = $"{contentType}?per_page={pageSize}&_fields=id,internal_memo,title,content,modified,link,excerpt&page={page}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<PdcItem>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? [];

        var totalPages = 1;
        if (response.Headers.TryGetValues("X-WP-TotalPages", out var headerValues) &&
            int.TryParse(headerValues.FirstOrDefault(), out var tp))
        {
            totalPages = tp;
        }

        return (items, totalPages);
    }

    public async IAsyncEnumerable<PdcItem> GetAllItemsAsync(
        string contentType,
        int pageSize = 50,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be >= 1.");

        var page = 1;
        while (true)
        {
            var (items, totalPages) = await GetItemsAsync(contentType, page, pageSize, cancellationToken).ConfigureAwait(false);

            foreach (var item in items)
                yield return item;

            if (items.Count == 0 || page >= totalPages)
                yield break;

            page++;
        }
    }
}
