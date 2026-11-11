using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Domain.Enums;

namespace Shop.Api.Ordering.Checkout;

/// <summary>Shared order load + naive RMW complete used by Naive / AppLock / DbForUpdate.</summary>
internal static class CheckoutSupport
{
    public static async Task<(Order? Order, string? Error)> LoadCreatedOrderAsync(
        AppDbContext db, Guid orderId, string userId, IDbContextTransaction tx, CancellationToken ct)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order is null)
            return (null, "Order not found");

        if (order.Status != OrderStatus.Created)
        {
            await tx.RollbackAsync(ct);
            return (order, $"Order is already {order.Status}");
        }

        return (order, null);
    }

    public static async Task<(bool Ok, string? Error, Order Order)> CompleteRmwAsync(
        AppDbContext db, Order order, Product product, IDbContextTransaction tx, CancellationToken ct)
    {
        if (product.Stock < order.Quantity)
        {
            order.Status = OrderStatus.Rejected;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (false, "Insufficient stock", order);
        }

        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = product.Price * order.Quantity,
            Status = PaymentStatus.Captured
        });

        product.Stock -= order.Quantity;
        order.Status = OrderStatus.Completed;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (true, null, order);
    }
}
