# Road to highload

Код к серии статей про highload на практике: .NET 10 + Aspire + PostgreSQL.

Идея: простой магазин (заказ + списание остатка), потом нагрузка, потом смотрим, где ломается, и чиним узко.

## Структура

```
AppHost/           # Aspire: Postgres, Shop.Api, Grafana/Prometheus/OTel
ServiceDefaults/   # общие defaults (OTEL и т.д.)
Services/Shop.Api/ # монолит Ordering / Inventory / Payment (локальный)
LoadTests/         # pgbench, NBomber, эксперименты oversell
```

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev/get-started/install-cli/)
- Docker

## Запуск

```bash
dotnet build RoadToHighload.sln
dotnet run --project AppHost
```

В Aspire Dashboard появятся Postgres (`appdb`), `shop-api`, Grafana и остальной observability-стек.

### Глава 1. Checkout Strategies

Списание остатка: `Checkout:StockStrategy` / env `Checkout__StockStrategy` / `set-strategy.sh`.

| Стратегия | Идея |
|---|---|
| `Naive` | RMW через EF |
| `AppLock` | `SemaphoreSlim` по `productId` |
| `DbAtomic` | `UPDATE … WHERE Stock >= qty` (default) |
| `DbForUpdate` | `SELECT … FOR UPDATE` + RMW |
| `Optimistic` | concurrency token + retry |

Подробнее: [`Services/Shop.Api/Ordering/Checkout/Fixes/README.md`](Services/Shop.Api/Ordering/Checkout/Fixes/README.md).

### Нагрузка

См. [`LoadTests/README.md`](LoadTests/README.md).

```bash
cd LoadTests/experiments/02-fix-checkout-oversell
./set-strategy.sh Naive   # или AppLock / DbAtomic / DbForUpdate / Optimistic
INITIAL_STOCK=100 ./prepare.sh
PARALLEL=100 ./run.sh
INITIAL_STOCK=100 ./assert-no-oversell.sh   # для Naive часто упадёт
```

Сравнение стратегий: `./compare.sh` (пишет `results.md`).

### Сид Dev

| | |
|---|---|
| Email | `demo@local.test` |
| Password | `Passw0rd!` |
