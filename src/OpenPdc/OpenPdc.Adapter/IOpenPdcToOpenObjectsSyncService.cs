namespace OpenPdc.Adapter;

public interface IOpenPdcToOpenObjectsSyncService
{
    /// <summary>
    /// Streams all items from OpenPDC and posts each one to OpenObjects.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);
}
