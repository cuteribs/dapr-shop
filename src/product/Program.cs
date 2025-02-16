using Dapr.Client;
using Dapr.Extensions.Configuration;
using DaprShop.Common;
using DaprShop.Product;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

var configKeys = LoadKeys(configuration.GetChildren()).ToArray();
var daprClient = new DaprClientBuilder().Build();
configuration.AddDaprConfigurationStore(DaprComponents.ConfigStoreName, configKeys, daprClient, TimeSpan.FromSeconds(5));   // required for dapr Config Store to work

services.Configure<DbOptions>(configuration.GetSection("DbOptions"));
services.AddScoped<Repository>();

var app = builder.Build();

app.MapGet("/", () => "product");

app.MapGet("/list", (Repository repository) => repository.GetList());

app.MapGet("/{id:guid}", (Repository repository, Guid id) => repository.Get(id));

await app.RunAsync();


static IEnumerable<string> LoadKeys(IEnumerable<IConfigurationSection> sections, string parentKey = "")
{
	foreach (var child in sections)
	{
		var key = string.IsNullOrEmpty(parentKey) ? child.Key : $"{parentKey}:{child.Key}";

		if (child.Value != null)
		{
			yield return key;
		}

		foreach(var item in LoadKeys(child.GetChildren(), key))
		{
			yield return item;
		}
	}
}