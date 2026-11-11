using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Domain.Enums;

namespace Shop.Api.Ordering;

public class OrderService(AppDbContext db)
{
    public async Task<(Order? Order, CreateOrderError Error)> CreateAsync(
        string userId,
        Guid productId,
        int quantity,
        CancellationToken ct)
    {
        if (quantity <= 0)
            return (null, CreateOrderError.InvalidQuantity);

        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
            return (null, CreateOrderError.ProductNotFound);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = product.Id,
            Quantity = quantity,
            Status = OrderStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return (order, CreateOrderError.None);
    }
}
