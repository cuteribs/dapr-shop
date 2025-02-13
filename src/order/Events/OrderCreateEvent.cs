using DaprShop.Order.Models;

namespace DaprShop.Order.Events;

public record OrderCreateEvent(Guid ProductId);
