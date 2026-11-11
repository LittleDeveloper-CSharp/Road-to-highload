using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Ordering.Checkout.Naive;

namespace Shop.Api.Ordering.Checkout.Fixes.AppLock;

/// <summary>In-process serialization per product. Safe on one instance only.</summary>
public sealed class AppLockCheckoutService(AppDbContext db, NaiveCheckoutService naive) : ICheckoutService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> ProductLocks = new();

    public async Task<(bool Ok, string? Error, Order? Order)> CheckoutAsync(
        Guid orderId, string userId, CancellationToken ct)
    {
        var productId = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => (Guid?)o.ProductId)
            .FirstOrDefaultAsync(ct);

        if (productId is null)
            return (false, "Order not found", null);

        var gate = ProductLocks.GetOrAdd(productId.Value, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            return await naive.CheckoutAsync(orderId, userId, ct);
        }
        finally
        {
            gate.Release();
        }
    }
}
