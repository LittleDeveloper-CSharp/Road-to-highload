namespace Shop.Api.Ordering;

public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    /// <summary>Default Dev: DbAtomic (see fix-checkout-oversell).</summary>
    public StockStrategy StockStrategy { get; set; } = StockStrategy.DbAtomic;
}
