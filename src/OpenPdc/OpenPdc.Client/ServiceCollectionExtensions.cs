using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace OpenPdc.Client;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddOpenPdcClient(
        this IServiceCollection services,
        Action<OpenPdcClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OpenPdcClientOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException("OpenPdcClientOptions.BaseUrl must be set.");

        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));

        return services.AddHttpClient<IOpenPdcClient, OpenPdcClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenPdc.Client/1.0");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        });
    }
}
