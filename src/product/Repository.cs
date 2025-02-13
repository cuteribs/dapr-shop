using DaprShop.Product.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace DaprShop.Product;

public class Repository
{
	private readonly DbOptions _options;

	public Repository(IOptions<DbOptions> options)
	{
		_options = options.Value;
	}

	public async Task<IEnumerable<ProductItem>> GetList()
	{
		using var connection = await this.OpenConnection();
		var command = connection.CreateCommand();
		command.CommandText = "SELECT * FROM Item";

		using var reader = await command.ExecuteReaderAsync();
		var items = new List<ProductItem>();

		while (await reader.ReadAsync())
		{
			var item = new ProductItem
			{
				Id = Guid.Parse(Convert.ToString(reader["Id"])!),
				Name = Convert.ToString(reader["Name"])!,
				Price = Convert.ToDecimal(reader["Price"])
			};
			items.Add(item);
		}

		return items;
	}

	public async Task<ProductItem?> Get(Guid id)
	{
		using var connection = await this.OpenConnection();
		var command = connection.CreateCommand();
		command.CommandText = "SELECT * FROM Item WHERE Id = @Id";
		command.Parameters.AddWithValue("@Id", id.ToString());

		using var reader = await command.ExecuteReaderAsync();

		if (await reader.ReadAsync())
		{
			var item = new ProductItem
			{
				Id = Guid.Parse(Convert.ToString(reader["Id"])!),
				Name = Convert.ToString(reader["Name"])!,
				Price = Convert.ToDecimal(reader["Price"])
			};
			return item;
		}

		return null;
	}

	private async Task<SqliteConnection> OpenConnection()
	{
		var connection = new SqliteConnection(_options.ConnectionString);
		Console.WriteLine("Data Source: {0}", connection.DataSource);
		await connection.OpenAsync();
		return connection;
	}
}

public class DbOptions
{
	public required string ConnectionString { get; set; }
}