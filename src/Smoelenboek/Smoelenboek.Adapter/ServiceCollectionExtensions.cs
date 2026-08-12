using Microsoft.Extensions.DependencyInjection;

namespace Smoelenboek.Adapter;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmoelenboekSyncService(
        this IServiceCollection services,
        Action<SmoelenboekSyncServiceOptions> configure)
    {
        var options = new SmoelenboekSyncServiceOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddTransient<ISmoelenboekSyncService, SmoelenboekSyncService>();
        return services;
    }
}
