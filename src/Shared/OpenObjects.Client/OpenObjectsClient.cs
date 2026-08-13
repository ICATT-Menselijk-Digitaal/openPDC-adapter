using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenObjects.Client.Models;

namespace OpenObjects.Client;

public sealed class OpenObjectsClient : IOpenObjectsClient
{
    // OpenObjects object type schemas declare optional fields with a non-nullable type (e.g. "string", not
    // ["string","null"]) — a present-but-null value still fails their JSON Schema validation, so null fields
    // must be omitted entirely rather than serialized as `null`.
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenObjectsClient> _logger;

    public OpenObjectsClient(HttpClient httpClient, ILogger<OpenObjectsClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ObjectResponse<TData>> PostObjectAsync<TData>(
        CreateObjectRequestBody<TData> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = JsonSerializer.Serialize(request, JsonOptions);
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v2/objects")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };

        var response = await _httpClient
            .SendAsync(message, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"POST {response.RequestMessage?.RequestUri} failed with {(int)response.StatusCode} ({response.ReasonPhrase}).\nResponse body:\n{errorBody}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<ObjectResponse<TData>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("OpenObjects API returned an empty response.");
    }

    public async IAsyncEnumerable<ObjectResponse<TData>> GetAllObjectsByObjectTypeUrlAsync<TData>(
        string objectTypeUrl,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Uri? requestUri = new($"{_httpClient.BaseAddress}api/v2/objects?type={objectTypeUrl}");

        while (requestUri != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("GET {RequestUri}", requestUri.AbsoluteUri);

            var httpResponse = await _httpClient
                .GetAsync(requestUri, cancellationToken)
                .ConfigureAwait(false);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                yield break;

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"GET {requestUri} failed with {(int)httpResponse.StatusCode} ({httpResponse.ReasonPhrase}).\nResponse body:\n{errorBody}");
            }

            var page = await httpResponse.Content
                .ReadFromJsonAsync<ObjectsPage<TData>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (page == null) yield break;

            foreach (var obj in page.Results)
                yield return obj;

            requestUri = page.Next is { Length: > 0 } next
                ? new Uri(next)
                : null;
        }
    }

    public async Task DeleteObjectAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .DeleteAsync($"api/v2/objects/{uuid}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"DELETE api/v2/objects/{uuid} failed with {(int)response.StatusCode} ({response.ReasonPhrase}).\nResponse body:\n{errorBody}");
        }
    }
}
