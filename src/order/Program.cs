using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using DaprShop.Order.Actors;
using DaprShop.Order.Events;
using DaprShop.Order.Models;

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
	var orders = await client.GetStateAsync<IEnumerable<OrderItem>>("statestore", "orders") ?? [];
	return orders;
});

app.MapGet("/{id:guid}", async (Guid id) =>
{
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(id.ToString()), nameof(OrderActor));
	return await actor.GetOrder();
});

app.MapPost("/ordercreate", [Topic("pubsub", "ordercreate")] async (DaprClient client, OrderCreateEvent @event) =>
{
	var order = new OrderItem
	{
		Id = Guid.NewGuid(),
		ProductId = @event.ProductId,
	};
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(order.Id.ToString()), nameof(OrderActor));
	await actor.CreateOrder(order);
	var orders = await client.GetStateAsync<IList<OrderItem>>("statestore", "orders") ?? [];
	orders.Add(order);
	await client.SaveStateAsync("statestore", "orders", orders);
	await client.PublishEventAsync("pubsub", "web", new OrderUpdateEvent());
});

app.MapPost("/ordercancel", [Topic("pubsub", "ordercancel")] async (DaprClient client, OrderCancelEvent @event) =>
{
	var orderId = @event.OrderId;
	var actor = ActorProxy.Create<IOrderActor>(new ActorId(orderId.ToString()), nameof(OrderActor));
	await actor.CancelOrder(orderId);
	var orders = await client.GetStateAsync<IList<OrderItem>>("statestore", "orders") ?? [];

	var order = orders.FirstOrDefault(x => x.Id == orderId);

	if (order != null) orders.Remove(order);

	await client.SaveStateAsync("statestore", "orders", orders);
	await client.PublishEventAsync("pubsub", "web", new OrderUpdateEvent());
});

//app.MapPost("/{productId:guid}", async (DaprClient client, Guid productId) =>
//{
//	var order = new OrderItem
//	{
//		Id = Guid.NewGuid(),
//		ProductId = productId,
//	};
//	var actor = ActorProxy.Create<IOrderActor>(new ActorId("1"), nameof(OrderActor));
//	await actor.CreateOrder(order);
//	var orders = await client.GetStateAsync<IList<OrderItem>>("statestore", "orders") ?? [];
//	orders.Add(order);
//	await client.SaveStateAsync("statestore", "orders", orders);
//	await client.PublishEventAsync("pubsub", "web", new OrderUpdateEvent());
//	return order;
//});

//app.MapDelete("/{orderId:guid}", async (DaprClient client, Guid orderId) =>
//{
//	var actor = ActorProxy.Create<IOrderActor>(new ActorId(orderId.ToString()), nameof(OrderActor));
//	await actor.CancelOrder(orderId);
//	var orders = await client.GetStateAsync<IList<OrderItem>>("statestore", "orders") ?? [];

//	var order = orders.FirstOrDefault(x => x.Id == orderId);

//	if (order != null) orders.Remove(order);

//	await client.SaveStateAsync("statestore", "orders", orders);
//	await client.PublishEventAsync("pubsub", "web", new OrderUpdateEvent());
//	return order;
//});

await app.RunAsync();

