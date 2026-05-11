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

    private async Task<PdcItemsResponse> GetItemsAsync(
        int page = 1,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be >= 1.");
        }

        var query = $"?page={page}";
        if (limit is > 0)
        {
            query += $"&per_page={limit.Value}";
        }

        // BaseAddress is expected to end with "/owc/pdc/v1/"; "items/" is relative to it.
        var requestUri = new Uri("items/" + query, UriKind.Relative);

        var response = await _httpClient
            .GetFromJsonAsync<PdcItemsResponse>(requestUri, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return response ?? new PdcItemsResponse();
    }

    public async IAsyncEnumerable<PdcItem> GetAllItemsAsync(
        int pageSize = 50,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be >= 1.");
        }

        var page = 1;
        while (true)
        {
            var response = await GetItemsAsync(page, pageSize, cancellationToken).ConfigureAwait(false);

            foreach (var item in response.Data)
            {
                yield return item;
            }

            if (response.Data.Count == 0 ||
                response.Pagination.TotalPages <= 0 ||
                page >= response.Pagination.TotalPages)
            {
                yield break;
            }

            page++;
        }
    }
}
