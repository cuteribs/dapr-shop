using Dapr;
using Dapr.Client;
using DaprShop.Common.Events;
using DaprShop.Common.Models;
using DaprShop.Common;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddDaprClient();	// inject DaprClient

var app = builder.Build();

app.UseCloudEvents();       // required for PubSub to work
app.MapSubscribeHandler();  // required for PubSub to work

var logger = app.Logger;

app.MapGet("/", () => "web");

app.MapGet("/products", async (DaprClient client) =>
{
	var items = await client.InvokeMethodAsync<ProductItem[]>(HttpMethod.Get, DaprApps.Product, "/list");
	return items;
});

app.MapGet("/products/{id:guid}", async (DaprClient client, Guid id) =>
{
	var items = await client.InvokeMethodAsync<ProductItem>(HttpMethod.Get, DaprApps.Product, $"/{id}");
	return items;
});

app.MapGet("/orders", async(DaprClient client) =>
{
	var cart = await client.InvokeMethodAsync<OrderItem[]>(HttpMethod.Get, DaprApps.Order, "/list");
	return cart;
});

app.MapGet("/orders/{orderId:guid}", async (DaprClient client, Guid orderId) =>
{
	var order = await client.InvokeMethodAsync<OrderItem>(HttpMethod.Get, DaprApps.Order, $"/{orderId}");
	return order;
});

app.MapPost("/orders/{productId:guid}", async (DaprClient client, Guid productId) =>
{
	await client.PublishEventAsync(DaprComponents.PubSubName, "ordercreate", new OrderCreateEvent(productId));
	logger.LogInformation("Send creating order message with product: {ProductId}", productId);
});

app.MapDelete("/orders/{orderId:guid}", async (DaprClient client, Guid orderId) =>
{
	await client.PublishEventAsync(DaprComponents.PubSubName, "ordercancel", new OrderCancelEvent(orderId));
	logger.LogInformation("Send cancelling order by Id {ProductId} from order", orderId);
});

app.MapPost("/sub", [Topic(DaprComponents.PubSubName, "web")] (OrderUpdateEvent @event) =>
{
	app.Logger.LogWarning("Received order update event");
});

await app.RunAsync();
