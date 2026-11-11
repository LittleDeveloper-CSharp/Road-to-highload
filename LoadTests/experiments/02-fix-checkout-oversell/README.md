# 02 — fix checkout oversell (compare strategies)

Сравнивает режимы списания stock на том же полигоне, что expose (`01`).

**Внимание:** `prepare` делает `DELETE` всех `Orders`/`Payments` в `appdb`.

## Стратегии

| Значение | Класс | Смысл |
|---|---|---|
| `Naive` | `Ordering/Checkout/Naive/` | read → check → `Stock -= qty` (ожидаем oversell) |
| `AppLock` | `Ordering/Checkout/Fixes/AppLock/` | `SemaphoreSlim` по productId + RMW (только 1 инстанс) |
| `DbAtomic` | `Ordering/Checkout/Fixes/DbAtomic/` | `UPDATE … WHERE Stock >= qty` (**default Dev**) |
| `DbForUpdate` | `Ordering/Checkout/Fixes/DbForUpdate/` | `SELECT … FOR UPDATE` + RMW |
| `Optimistic` | `Ordering/Checkout/Fixes/Optimistic/` | EF `UpdatedAt` concurrency token + retry (`ProductTimestampInterceptor`) |

DI регает все реализации; `ICheckoutService` выбирается по `Checkout:StockStrategy` на каждый scope.  
Переключение: `./set-strategy.sh DbAtomic` → `checkout.strategy.json` (или env `Checkout__StockStrategy`). Описание фиксов — `Ordering/Checkout/Fixes/README.md`.

CPU/RAM: CSV-сэмпл процесса + OTEL Runtime в Aspire Dashboard.

## Прогон одной стратегии

```bash
cd src/LoadTests/experiments/02-fix-checkout-oversell
./set-strategy.sh Naive   # или AppLock / DbAtomic / DbForUpdate / Optimistic
# подожди ~3s или restart shop-api

./prepare.sh
LOADTEST_BASE_URL=http://localhost:<port> ./run.sh
./assert-no-oversell.sh   # exit 0 = нет oversell (успех фикса)
./assert-oversell.sh      # exit 0 = oversell (успех expose / регрессия Naive)
```

## Сравнение всех четырёх

```bash
LOADTEST_BASE_URL=http://localhost:<port> PARALLEL=15 ./compare.sh
# тяжелее:
# INITIAL_STOCK=100 PARALLEL=300 RESULTS_MD=./results-stock100-p300.md ./compare.sh
# → results.md (+ CSV в results/)
```

`INITIAL_STOCK` задаёт и SQL prepare, и порог assert. При PARALLEL≫100 может понадобиться поднять `max_connections` у Postgres (в прогоне 100/300 — до 400).

## Результаты

См. [`results.md`](results.md) после прогона `compare.sh`.

Пример прогона (`PARALLEL=15`, `initial_stock=10`):

| strategy | oversell? | wall_s | cpu_avg | cpu_max | rss_avg_kb | rss_max_kb | completed | stock |
|---|---|---|---|---|---|---|---|---|
| Naive | yes | 0.941 | 18.15 | 36.30 | 191104 | 195904 | 15 | 9 |
| AppLock | no | 0.678 | 18.60 | 37.00 | 225864 | 227744 | 10 | 0 |
| DbAtomic | no | 0.526 | 25.85 | 50.80 | 241440 | 243520 | 10 | 0 |
| DbForUpdate | no | 0.605 | 10.30 | 20.60 | 248824 | 250896 | 10 | 0 |

Default Dev после compare снова `DbAtomic`. CPU/RSS — относительные сэмплы `ps` за короткое окно; для живого разбора — OTEL Runtime в Aspire Dashboard.
