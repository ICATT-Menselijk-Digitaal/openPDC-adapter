using System.Reflection;
using Entra.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenObjects.Client;
using Smoelenboek.Adapter;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables()
    .Build();

static string Require(IConfiguration cfg, string key) =>
    cfg[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"'{key}' is required. Set it via user secrets (dotnet user-secrets set \"{key}\" \"...\") or as an environment variable.");

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().AddConfiguration(config.GetSection("Logging")));

services.AddEntraClient(o =>
{
    o.TenantId     = Require(config, "Entra:TenantId");
    o.ClientId     = Require(config, "Entra:ClientId");
    o.ClientSecret = Require(config, "Entra:ClientSecret");
    o.UsersFilter  = config["Entra:UsersFilter"] ?? string.Empty;
});

services.AddOpenObjectsClient(o =>
{
    o.BaseUrl = config["OpenObjects:BaseUrl"] ?? OpenObjectsClientOptions.DefaultBaseUrl;
    o.Token   = Require(config, "OpenObjects:Token");
});

services.AddSmoelenboekSyncService(o =>
{
    o.MedewerkerObjectTypeUrl     = Require(config, "OpenObjects:MedewerkerObjectTypeUrl");
    o.MedewerkerObjectTypeVersion = int.Parse(config["OpenObjects:MedewerkerObjectTypeVersion"] ?? "1");
    o.AfdelingObjectTypeUrl    = Require(config, "OpenObjects:AfdelingObjectTypeUrl");
    o.AfdelingObjectTypeVersion = int.Parse(config["OpenObjects:AfdelingObjectTypeVersion"] ?? "1");
});

await using var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<ISmoelenboekSyncService>>();
var syncService = provider.GetRequiredService<ISmoelenboekSyncService>();

logger.LogInformation("Starting Smoelenboek sync from Entra to OpenObjects...");
await syncService.RunAsync();
