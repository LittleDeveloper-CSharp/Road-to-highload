using Microsoft.Extensions.Options;
using Shop.Api.Ordering.Checkout.Fixes.AppLock;
using Shop.Api.Ordering.Checkout.Fixes.DbAtomic;
using Shop.Api.Ordering.Checkout.Fixes.DbForUpdate;
using Shop.Api.Ordering.Checkout.Fixes.Optimistic;
using Shop.Api.Ordering.Checkout.Naive;

namespace Shop.Api.Ordering.Checkout;

public static class CheckoutServiceCollectionExtensions
{
    /// <summary>
    /// Registers all checkout variants; <see cref="ICheckoutService"/> resolves the one
    /// selected by <c>Checkout:StockStrategy</c> (env / checkout.strategy.json) per scope.
    /// </summary>
    public static IServiceCollection AddCheckoutStrategies(this IServiceCollection services)
    {
        services.AddScoped<NaiveCheckoutService>();
        services.AddScoped<AppLockCheckoutService>();
        services.AddScoped<DbAtomicCheckoutService>();
        services.AddScoped<DbForUpdateCheckoutService>();
        services.AddScoped<OptimisticCheckoutService>();

        services.AddScoped<ICheckoutService>(sp =>
        {
            var strategy = sp.GetRequiredService<IOptionsMonitor<CheckoutOptions>>()
                .CurrentValue.StockStrategy;

            return strategy switch
            {
                StockStrategy.Naive => sp.GetRequiredService<NaiveCheckoutService>(),
                StockStrategy.AppLock => sp.GetRequiredService<AppLockCheckoutService>(),
                StockStrategy.DbAtomic => sp.GetRequiredService<DbAtomicCheckoutService>(),
                StockStrategy.DbForUpdate => sp.GetRequiredService<DbForUpdateCheckoutService>(),
                StockStrategy.Optimistic => sp.GetRequiredService<OptimisticCheckoutService>(),
                _ => throw new InvalidOperationException($"Unknown Checkout:StockStrategy '{strategy}'")
            };
        });

        return services;
    }
}
