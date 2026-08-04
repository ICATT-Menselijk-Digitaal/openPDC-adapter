using OpenObjects.Client.Models;

namespace OpenObjects.Client;

/// <summary>
/// Client for the OpenObjects <c>/api/v2/objects</c> REST API.
/// </summary>
public interface IOpenObjectsClient
{
    /// <summary>
    /// Posts a new object to the OpenObjects API.
    /// </summary>
    /// <param name="request">The object to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created object as returned by the API.</returns>
    Task<ObjectResponse<TData>> PostObjectAsync<TData>(
        CreateObjectRequestBody<TData> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams all objects of the given type from the OpenObjects API.
    /// </summary>
    IAsyncEnumerable<ObjectResponse<TData>> GetAllObjectsByObjectTypeUrlAsync<TData>(
        string objectTypeUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the object with the given UUID.
    /// </summary>
    Task DeleteObjectAsync(Guid uuid, CancellationToken cancellationToken = default);
}
