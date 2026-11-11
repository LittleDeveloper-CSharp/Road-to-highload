using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Domain.Enums;
using Shop.Api.Ordering.Checkout;

namespace Shop.Api.Ordering.Checkout.Fixes.Optimistic;

/// <summary>
/// EF optimistic concurrency on <see cref="Product.UpdatedAt"/> with retry.
/// Timestamp stamped by <see cref="ProductTimestampInterceptor"/>.
/// </summary>
public sealed class OptimisticCheckoutService(AppDbContext db) : ICheckoutService
{
    private const int MaxAttempts = 64;

    public async Task<(bool Ok, string? Error, Order? Order)> CheckoutAsync(
        Guid orderId, string userId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var (order, error) = await CheckoutSupport.LoadCreatedOrderAsync(db, orderId, userId, tx, ct);
        if (order is null)
            return (false, error, null);
        if (error is not null)
            return (false, error, order);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == order.ProductId, ct);
            if (product is null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Product not found", order);
            }

            if (product.Stock < order.Quantity)
            {
                order.Status = OrderStatus.Rejected;
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return (false, "Insufficient stock", order);
            }

            // Clear a previous failed attempt's payment if any (tracked Added).
            foreach (var payment in db.ChangeTracker.Entries<Payment>()
                         .Where(e => e.State == EntityState.Added && e.Entity.OrderId == order.Id)
                         .ToList())
            {
                payment.State = EntityState.Detached;
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

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return (true, null, order);
            }
            catch (DbUpdateConcurrencyException)
            {
                await db.Entry(product).ReloadAsync(ct);
                order.Status = OrderStatus.Created;
                // Interceptor already stamped UpdatedAt on the failed attempt's CurrentValues;
                // Reload restores DB Stock + UpdatedAt for the next try.
            }
        }

        await tx.RollbackAsync(ct);
        return (false, "Optimistic concurrency: too many retries", order);
    }
}
