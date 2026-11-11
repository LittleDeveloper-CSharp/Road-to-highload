using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Shop.Api.Domain;
using Shop.Api.Ordering;

namespace Shop.Api.Data;

/// <summary>
/// Stamps <see cref="Product.UpdatedAt"/> only when <see cref="StockStrategy.Optimistic"/>.
/// Without a stamp other strategies keep a stable token → classic lost-update on Stock still works.
/// Retries stay in <c>OptimisticCheckoutService</c> (need order/payment domain context).
/// </summary>
public sealed class ProductTimestampInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampProducts(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampProducts(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampProducts(DbContext? context)
    {
        if (context is null)
            return;

        var strategy = context.GetService<IOptionsMonitor<CheckoutOptions>>()
            .CurrentValue.StockStrategy;
        if (strategy != StockStrategy.Optimistic)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<Product>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
