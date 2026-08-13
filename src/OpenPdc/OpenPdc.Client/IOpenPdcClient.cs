using OpenPdc.Client.Models;

namespace OpenPdc.Client;

public interface IOpenPdcClient
{
    string BaseUrl { get; }

    /// <summary>
    /// Streams every PDC item from the given endpoint by transparently following the API's pagination.
    /// </summary>
    /// <param name="contentType">WordPress content type, e.g. "product", "pages", "publication".</param>
    /// <param name="pageSize">Page size to request from the upstream API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<PdcItem> GetAllItemsAsync(
        string contentType,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
