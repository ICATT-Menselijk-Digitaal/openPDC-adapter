namespace Entra.Client;

public interface IEntraClient
{
    IAsyncEnumerable<T> GetAllAsync<T>(string requestUri, CancellationToken cancellationToken = default);
}
