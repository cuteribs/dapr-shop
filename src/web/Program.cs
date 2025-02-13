using Dapr;
using Dapr.Client;
using DaprShop.Order.Events;
using DaprShop.Order.Models;
using DaprShop.Product.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddDaprClient();

var app = builder.Build();

app.UseCloudEvents();       // required for PubSub to work
app.MapSubscribeHandler();  // required for PubSub to work

var logger = app.Logger;

app.MapGet("/", () => "web");

app.MapGet("/products", async (DaprClient client) =>
{
	var items = await client.InvokeMethodAsync<ProductItem[]>(HttpMethod.Get, "product", "/list");
	return items;
});

app.MapGet("/products/{id:guid}", async (DaprClient client, Guid id) =>
{
	var items = await client.InvokeMethodAsync<ProductItem>(HttpMethod.Get, "product", $"/{id}");
	return items;
});

app.MapGet("/orders", async(DaprClient client) =>
{
	var cart = await client.InvokeMethodAsync<OrderItem[]>(HttpMethod.Get, "order", "/list");
	return cart;
});

app.MapGet("/orders/{id:guid}", async (DaprClient client, Guid orderId) =>
{
	var order = await client.InvokeMethodAsync<OrderItem>(HttpMethod.Get, "order", $"/{orderId}");
	return order;
});

app.MapPost("/orders/{productId:guid}", async (DaprClient client, Guid productId) =>
{
	await client.PublishEventAsync("pubsub", "ordercreate", new OrderCreateEvent(productId));
	logger.LogInformation("Send creating order message with product: {ProductId}", productId);
});

app.MapDelete("/orders/{orderId:guid}", async (DaprClient client, Guid orderId) =>
{
	await client.PublishEventAsync("pubsub", "ordercancel", new OrderCancelEvent(orderId));
	logger.LogInformation("Send cancelling order by Id {ProductId} from order", orderId);
});

app.MapPost("/sub", [Topic("pubsub", "web")] (OrderUpdateEvent @event) =>
{
	app.Logger.LogWarning("Received order update event");
});

await app.RunAsync();
