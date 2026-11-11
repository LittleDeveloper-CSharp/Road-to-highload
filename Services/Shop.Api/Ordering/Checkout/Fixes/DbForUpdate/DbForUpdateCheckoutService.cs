using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Ordering.Checkout;

namespace Shop.Api.Ordering.Checkout.Fixes.DbForUpdate;

/// <summary>Row lock via SELECT FOR UPDATE, then RMW in the same transaction.</summary>
public sealed class DbForUpdateCheckoutService(AppDbContext db) : ICheckoutService
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

        var productId = order.ProductId;
        var product = await db.Products
            .FromSqlInterpolated(
                $"""SELECT * FROM "Products" WHERE "Id" = {productId} FOR UPDATE""")
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            await tx.RollbackAsync(ct);
            return (false, "Product not found", order);
        }

        return await CheckoutSupport.CompleteRmwAsync(db, order, product, tx, ct);
    }
}
