using Microsoft.Extensions.DependencyInjection;

namespace OpenPdc.Adapter;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenPdcToOpenObjectsSyncService(
        this IServiceCollection services,
        Action<OpenPdcToOpenObjectsSyncOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OpenPdcToOpenObjectsSyncOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddTransient<IOpenPdcToOpenObjectsSyncService, OpenPdcToOpenObjectsSyncService>();

        return services;
    }
}
