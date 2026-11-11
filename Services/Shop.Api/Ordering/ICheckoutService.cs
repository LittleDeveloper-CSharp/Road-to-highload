using Shop.Api.Domain;

namespace Shop.Api.Ordering;

public interface ICheckoutService
{
    Task<(bool Ok, string? Error, Order? Order)> CheckoutAsync(
        Guid orderId, string userId, CancellationToken ct);
}
