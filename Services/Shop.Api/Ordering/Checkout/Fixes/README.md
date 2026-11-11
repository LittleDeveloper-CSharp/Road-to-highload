# Fixes: oversell на конкурентном checkout

Проблема: наивный RMW даёт `Completed > initial_stock` под параллельными checkout.

Здесь варианты **узкого** фикса списания остатка (один инстанс, одна БД). Выбор реализации — `Checkout:StockStrategy` / env `Checkout__StockStrategy` (см. DI в `CheckoutServiceCollectionExtensions`).

| Стратегия | Папка / класс | Идея | Ограничение |
|---|---|---|---|
| `AppLock` | `AppLock/` | `SemaphoreSlim` по `productId` + naive RMW | Только один процесс `shop-api` |
| `DbAtomic` | `DbAtomic/` | `UPDATE … WHERE Stock >= qty` + affected rows | Default Dev |
| `DbForUpdate` | `DbForUpdate/` | `FromSql` + `SELECT … FOR UPDATE`, затем RMW | Лок на всю tx checkout |
| `Optimistic` | `Optimistic/` | EF concurrency token `UpdatedAt` + retry | Штамп `UpdatedAt` только при этой стратегии (`ProductTimestampInterceptor` + `Checkout:StockStrategy`) |

Полигон сравнения: `src/LoadTests/experiments/02-fix-checkout-oversell/`.
