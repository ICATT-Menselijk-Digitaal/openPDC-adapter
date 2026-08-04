using Entra.Client.Models;

namespace Entra.Client;

public interface IEntraClient
{
    IAsyncEnumerable<EntraUser> GetAllUsersAsync(CancellationToken cancellationToken = default);
}
