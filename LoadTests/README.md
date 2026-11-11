# Load tests

Инструменты после поднятого Aspire AppHost:

| Инструмент | Что меряет | Где |
|---|---|---|
| **pgbench** | PostgreSQL (TPC-B / custom SQL) | `pgbench/` |
| **NBomber** | HTTP API smoke: login → order → checkout | `NBomber/` |
| **experiments** | эволюция highload (expose/fix полигоны) | `experiments/` |

### Experiment 01 — checkout oversell (expose)

Каталог: [`experiments/01-expose-checkout-oversell/`](experiments/01-expose-checkout-oversell/).  
Цепочка: `prepare.sh` → `run.sh` → `assert-oversell.sh` (успех expose = `Completed > 10`).

### Experiment 02 — fix checkout oversell (compare)

Каталог: [`experiments/02-fix-checkout-oversell/`](experiments/02-fix-checkout-oversell/).  
Режимы `Naive` / `AppLock` / `DbAtomic` / `DbForUpdate` / `Optimistic`: `compare.sh` → таблица в `results.md` (нет oversell на фиксах; naive — да). Default API: `DbAtomic`.

## Пререквизиты

1. Docker (для Postgres контейнера Aspire).
2. .NET 10 SDK + Aspire CLI (см. корневой README).
3. Запущенный AppHost:

```bash
cd ..
dotnet run --project AppHost
```

4. URL `shop-api` из Aspire Dashboard (ресурс `shop-api` → endpoint, обычно `http://localhost:<port>`).

### Сид-учётка (Dev)

Создаётся при старте `Shop.Api` в Development:

| Поле | Значение |
|---|---|
| Email | `demo@local.test` |
| Password | `Demo123!` |
| Product Id | `11111111-1111-1111-1111-111111111111` |
| Начальный stock | `100` |

JWT SigningKey для Dev — в `Shop.Api/appsettings.Development.json` (или env `Jwt__SigningKey` / user-secrets). Не коммитьте прод-секреты.

## pgbench

`pgbench` берётся **из образа Postgres** через `docker exec` — локально ставить клиент не обязательно. Таблицы `pgbench_`* не пересекаются с EF-схемой магазина.

Дефолты smoke: `SCALE=1`, `CLIENTS=4`, `DURATION=15`.

```bash
cd LoadTests/pgbench
./init.sh
./run.sh

# кастомный SQL
PGBENCH_SCRIPT=./scripts/simple-select.sql ./run.sh
```

Переменные: `PGBENCH_CONTAINER`, `PGBENCH_DB` (default `appdb`), `PGBENCH_USER`, `PGBENCH_PASSWORD` (по умолчанию читается из `POSTGRES_PASSWORD` контейнера Aspire), `PGBENCH_SCALE`, `PGBENCH_CLIENTS`, `PGBENCH_JOBS`, `PGBENCH_DURATION`.

Если контейнер не находится автоматически — скопируйте id из `docker ps` и задайте `PGBENCH_CONTAINER=<id>`.

## NBomber

Сценарий `checkout_smoke`: `POST /auth/login` → `POST /orders` → `POST /orders/{id}/checkout`.

```bash
cd LoadTests/NBomber
LOADTEST_BASE_URL=http://localhost:<shop-api-port> \
LOADTEST_RATE=2 \
LOADTEST_DURATION_SEC=15 \
dotnet run
```

Опционально: `LOADTEST_EMAIL`, `LOADTEST_PASSWORD`, `LOADTEST_PRODUCT_ID`.

Без `LOADTEST_BASE_URL` сценарий падает на httpbin.

Отчёты пишутся в `NBomber/reports/`.

## Ручной happy-path (проверка API)

```bash
BASE=http://localhost:<shop-api-port>

# health анонимно
curl -sS "$BASE/health"

# login
TOKEN=$(curl -sS -X POST "$BASE/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@local.test","password":"Demo123!"}' \
  | jq -r .access_token)

# products
curl -sS "$BASE/products" -H "Authorization: Bearer $TOKEN"

# order + checkout
ORDER=$(curl -sS -X POST "$BASE/orders" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"productId":"11111111-1111-1111-1111-111111111111","quantity":1}')
OID=$(echo "$ORDER" | jq -r .id)
curl -sS -X POST "$BASE/orders/$OID/checkout" -H "Authorization: Bearer $TOKEN"
```
