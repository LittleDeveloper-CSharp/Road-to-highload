namespace Shop.Api.Ordering;

public enum StockStrategy
{
    Naive,
    AppLock,
    DbAtomic,
    DbForUpdate,
    Optimistic
}
