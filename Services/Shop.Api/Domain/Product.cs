namespace Shop.Api.Domain;

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int Stock { get; set; }
    public decimal Price { get; set; }

    /// <summary>EF concurrency token; stamped only under <c>StockStrategy.Optimistic</c>.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
