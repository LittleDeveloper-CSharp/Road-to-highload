# 01 — expose checkout oversell

Цель: поймать **oversell** на naive checkout (`Completed > 10` при `initial_stock=10`). Фикс BL здесь не делается.

**Внимание:** `prepare.sh` делает `DELETE` всех `Orders`/`Payments` в `appdb` (Dev-полигон).

## Пререквизиты

1. AppHost up (`shop-api` + Postgres).
2. `LOADTEST_BASE_URL` = HTTP URL `shop-api` из Aspire Dashboard.

## Прогон

```bash
cd src/LoadTests/experiments/01-expose-checkout-oversell
chmod +x *.sh

./prepare.sh

LOADTEST_BASE_URL=http://localhost:<port> ./run.sh
# optional: PARALLEL=40 ./run.sh

./assert-oversell.sh
# exit 0  → oversell найден (успех expose)
# exit 1  → гонка не проявилась — повтори run или увеличь PARALLEL
```

Система запуска нагрузки: **bash `xargs -P`** (15 параллельных цепочек login→order→checkout). NBomber inject намеренно не используем — слабее для «одновременного старта».

## Сигнал

`Completed_count > initial_stock` (по умолчанию `> 10`). Скрипт печатает `completed` и `stock`.

### Пример успешного assert (expose OK)

```
assert: completed=15 stock=9 initial_stock=10
assert: OVERSELL detected (Completed > initial_stock) — expose signal OK
```

`PARALLEL=15`, `LOADTEST_BASE_URL=http://localhost:5040`. Цифры от прогона к прогону плавают; важно `completed > 10`.
