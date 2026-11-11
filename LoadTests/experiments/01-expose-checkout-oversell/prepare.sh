#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../pgbench/common.sh
source "$SCRIPT_DIR/../../pgbench/common.sh"

CONTAINER="$(resolve_postgres_container)"
PASSWORD="$(resolve_postgres_password "$CONTAINER")"
if [[ -z "$PASSWORD" ]]; then
  echo "Could not resolve Postgres password. Set PGBENCH_PASSWORD." >&2
  exit 1
fi

INITIAL_STOCK="${INITIAL_STOCK:-10}"
echo "prepare: container=$CONTAINER db=$PGBENCH_DB (DELETE Orders/Payments, Stock=$INITIAL_STOCK)"
docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" \
  psql -U "$PGBENCH_USER" -h localhost -d "$PGBENCH_DB" -v ON_ERROR_STOP=1 \
  -v "stock=$INITIAL_STOCK" \
  < "$SCRIPT_DIR/sql/prepare.sql"

echo "prepare: done"
