using Dapr.Actors;
using Dapr.Actors.Runtime;
using DaprShop.Order.Models;

namespace DaprShop.Order.Actors;

public interface IOrderActor : IActor
{
	Task CancelOrder(Guid orderId);
	Task CreateOrder(OrderItem order);
	Task<OrderItem> GetOrder();
}

public class OrderActor : Actor, IOrderActor
{
	private static readonly string StateName = "order";

	public OrderActor(ActorHost host) : base(host) { }

	public async Task CreateOrder(OrderItem order)
	{
		await StateManager.SetStateAsync(StateName, order);
	}

	public async Task CancelOrder(Guid orderId)
	{
		await StateManager.TryRemoveStateAsync(StateName);
	}

	public async Task<OrderItem> GetOrder()
	{
		return await StateManager.GetStateAsync<OrderItem>(StateName);
	}
}