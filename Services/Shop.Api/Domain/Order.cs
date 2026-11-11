using Shop.Api.Domain.Enums;

namespace Shop.Api.Domain;

public class Order
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
