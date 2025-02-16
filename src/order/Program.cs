using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using DaprShop.Common;
using DaprShop.Common.Events;
using DaprShop.Common.Models;
using DaprShop.Order.Actors;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddDaprClient();
services.AddActors(o =>
{
	o.Actors.RegisterActor<OrderActor>();  // required for Actor to work
});

var app = builder.Build();

app.MapActorsHandlers();    // required for Actor to work

app.UseCloudEvents();       // required for PubSub to work
app.MapSubscribeHandler();  // required for PubSub to work

app.MapGet("/", () => "order");

app.MapGet("/list", async (DaprClient client) =>
{
	var orders = await client.GetStateAsync<IEnumerable<OrderItem>>(DaprComponents.StateStoreName, "orders") ?? [];
	return orders;
});

app.MapGet("/{id:guid}", async (Guid id) =>
{
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(id.ToString()), nameof(OrderActor));
	return await actor.GetOrder();
});

app.MapPost("/ordercreate", [Topic(DaprComponents.PubSubName, "ordercreate")] async (DaprClient client, OrderCreateEvent @event) =>
{
	app.Logger.LogInformation("OrderCreateEvent: {ProductId}", @event.ProductId);
	var order = new OrderItem
	{
		Id = Guid.NewGuid(),
		ProductId = @event.ProductId,
	};
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(order.Id.ToString()), nameof(OrderActor));
	await actor.CreateOrder(order);
	var orders = await client.GetStateAsync<IList<OrderItem>>(DaprComponents.StateStoreName, "orders") ?? [];
	orders.Add(order);
	await client.SaveStateAsync(DaprComponents.StateStoreName, "orders", orders);
	await client.PublishEventAsync(DaprComponents.PubSubName, "web", new OrderUpdateEvent());
});

app.MapPost("/ordercancel", [Topic(DaprComponents.PubSubName, "ordercancel")] async (DaprClient client, OrderCancelEvent @event) =>
{
	app.Logger.LogInformation("OrderCancelEvent: {OrderId}", @event.OrderId);
	var orderId = @event.OrderId;
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(orderId.ToString()), nameof(OrderActor));
	await actor.CancelOrder(orderId);
	var orders = await client.GetStateAsync<IList<OrderItem>>(DaprComponents.StateStoreName, "orders") ?? [];

	var order = orders.FirstOrDefault(x => x.Id == orderId);

	if (order != null) orders.Remove(order);

	await client.SaveStateAsync(DaprComponents.StateStoreName, "orders", orders);
	await client.PublishEventAsync(DaprComponents.PubSubName, "web", new OrderUpdateEvent());
});

await app.RunAsync();

