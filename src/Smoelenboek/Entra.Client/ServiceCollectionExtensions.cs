using Microsoft.Extensions.DependencyInjection;

namespace Entra.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraClient(
        this IServiceCollection services,
        Action<EntraClientOptions> configure)
    {
        var options = new EntraClientOptions();
        configure(options);
        services.AddSingleton(options);

        services.AddHttpClient<IEntraClient, EntraClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}
