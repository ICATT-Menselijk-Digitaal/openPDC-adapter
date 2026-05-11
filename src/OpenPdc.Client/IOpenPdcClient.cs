using OpenPdc.Client.Models;

namespace OpenPdc.Client;

public interface IOpenPdcClient
{
    /// <summary>
    /// Streams every PDC item by transparently following the API's pagination.
    /// </summary>
    /// <param name="pageSize">Page size to request from the upstream API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<PdcItem> GetAllItemsAsync(
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
