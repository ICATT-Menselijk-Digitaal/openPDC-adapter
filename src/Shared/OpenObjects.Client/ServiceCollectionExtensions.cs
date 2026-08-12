using Microsoft.Extensions.DependencyInjection;

namespace OpenObjects.Client;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddOpenObjectsClient(
        this IServiceCollection services,
        Action<OpenObjectsClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OpenObjectsClientOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException("OpenObjectsClientOptions.BaseUrl must be set.");
        }

        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";

        return services.AddHttpClient<IOpenObjectsClient, OpenObjectsClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.Add("Accept-Crs", "EPSG:4326");
            client.DefaultRequestHeaders.Add("Content-Crs", "EPSG:4326");

            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Token {options.Token}");
            }
        });
    }
}
