namespace DaprShop.Common.Models;

public class ProductItem
{
	public Guid Id { get; set; }
	public required string Name { get; set; }
	public decimal Price { get; set; }
}
