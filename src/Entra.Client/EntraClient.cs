using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Entra.Client.Models;

namespace Entra.Client;

public sealed class EntraClient(
    HttpClient httpClient,
    IHttpClientFactory httpClientFactory,
    EntraClientOptions options) : IEntraClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly EntraClientOptions _options = options;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // Fetches users matching the configured EntraClientOptions.UsersFilter (customer-specific — e.g.
    // restricting to a domain and requiring a department), selecting just the name/contact/department
    // fields the Medewerker sync needs. UsersFilter is optional: Graph treats an empty $filter as no
    // filter at all, so leaving it unset fetches every user in the tenant, unfiltered.
    public async IAsyncEnumerable<EntraUser> GetAllUsersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await SetBearerTokenAsync(cancellationToken).ConfigureAwait(false);

        // endsWith()/not() filters need Graph's advanced query support: ConsistencyLevel: eventual + $count=true.
        if (!_httpClient.DefaultRequestHeaders.Contains("ConsistencyLevel"))
        {
            _httpClient.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
        }
        
        var select = "displayName,userPrincipalName,department,givenName,surname,mail,businessPhones";
        string? nextLink = $"users?$select={select}&$filter={Uri.EscapeDataString(_options.UsersFilter)}&$count=true&$top=999";

        while (nextLink != null)
        {
            var page = await FetchPageAsync<EntraUser>(nextLink, cancellationToken).ConfigureAwait(false);
            foreach (var user in page.Value)
                yield return user;
            nextLink = page.NextLink;
        }
    }

    private async Task<ODataPage<T>> FetchPageAsync<T>(string requestUri, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(requestUri, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new HttpRequestException(
                $"GET {requestUri} failed with {(int)response.StatusCode} ({response.ReasonPhrase}).\nResponse body:\n{body}");
        }

        return await response.Content
            .ReadFromJsonAsync<ODataPage<T>>(JsonOptions, ct)
            .ConfigureAwait(false) ?? new ODataPage<T>();
    }

    private async Task<string> GetAccessToken(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry - TimeSpan.FromMinutes(5))
                return _cachedToken;

            using var client = _httpClientFactory.CreateClient();
            var tokenUrl = $"https://login.microsoftonline.com/{_options.TenantId}/oauth2/v2.0/token";
            using var form = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
            ]);

            using var response = await client.PostAsync(tokenUrl, form, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Entra token retrieval failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
            }

            var tokenResp = await response.Content
                .ReadFromJsonAsync<TokenResponse>(JsonOptions, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Token response was null.");

            _cachedToken = tokenResp.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResp.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task SetBearerTokenAsync(CancellationToken ct)
    {
        var token = await GetAccessToken(ct).ConfigureAwait(false);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

}
