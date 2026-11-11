using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Domain.Enums;
using Shop.Api.Ordering.Checkout;

namespace Shop.Api.Ordering.Checkout.Fixes.DbAtomic;

/// <summary>Atomic stock decrement in Postgres; default Dev strategy.</summary>
public sealed class DbAtomicCheckoutService(AppDbContext db) : ICheckoutService
{
    public async Task<(bool Ok, string? Error, Order? Order)> CheckoutAsync(
        Guid orderId, string userId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var (order, error) = await CheckoutSupport.LoadCreatedOrderAsync(db, orderId, userId, tx, ct);
        if (order is null)
            return (false, error, null);
        if (error is not null)
            return (false, error, order);

        var product = await db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == order.ProductId, ct);
        if (product is null)
        {
            await tx.RollbackAsync(ct);
            return (false, "Product not found", order);
        }

        var qty = order.Quantity;
        var productId = product.Id;
        var rows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "Products"
             SET "Stock" = "Stock" - {qty}
             WHERE "Id" = {productId} AND "Stock" >= {qty}
             """,
            ct);

        if (rows == 0)
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
        order.Status = OrderStatus.Completed;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (true, null, order);
    }
}
