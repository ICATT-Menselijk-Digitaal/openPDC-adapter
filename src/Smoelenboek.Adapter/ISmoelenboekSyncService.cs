namespace Smoelenboek.Adapter;

public interface ISmoelenboekSyncService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
