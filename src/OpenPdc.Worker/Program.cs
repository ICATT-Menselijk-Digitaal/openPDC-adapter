using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenObjects.Client;
using OpenPdc.Adapter;
using OpenPdc.Client;

Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

static string Require(IConfiguration cfg, string key) =>
    cfg[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"'{key}' is required. Set it in .env or as an environment variable.");

var services = new ServiceCollection();

services.AddLogging(b => b.AddConsole().AddConfiguration(config.GetSection("Logging")));

services.AddOpenPdcClient(o =>
{
    o.BaseUrl = Require(config, "OpenPdc:BaseUrl");
});

services.AddOpenObjectsClient(o =>
{
    o.BaseUrl = config["OpenObjects:BaseUrl"] ?? OpenObjectsClientOptions.DefaultBaseUrl;
    o.Token   = Require(config, "OpenObjects:Token");
});

services.AddOpenPdcToOpenObjectsSyncService(o =>
{
    o.ObjectTypeUrl     = Require(config, "OpenObjects:ObjectTypeUrl");
    o.ObjectTypeVersion = int.Parse(config["OpenObjects:ObjectTypeVersion"] ?? "1");
    o.PdcItemBaseUrl    = Require(config, "OpenPdc:ItemBaseUrl");
    o.OwmsUrl           = Require(config, "OpenObjects:OwmsUrl");
    o.OwmsIdentifier    = Require(config, "OpenObjects:OwmsIdentifier");
    o.OwmsEndDate       = DateTimeOffset.Parse(config["OpenObjects:OwmsEndDate"] ?? "2099-12-31T23:59:59Z");
});

await using var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<IOpenPdcToOpenObjectsSyncService>>();
var syncService = provider.GetRequiredService<IOpenPdcToOpenObjectsSyncService>();

logger.LogInformation("Starting synchronization of openPDC items to OpenObjects...");
await syncService.RunAsync();
