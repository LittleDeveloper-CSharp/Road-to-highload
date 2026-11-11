#!/usr/bin/env bash
# Exit 0 when Completed <= initial_stock (fix holds). Exit 1 on oversell.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../pgbench/common.sh
source "$SCRIPT_DIR/../../pgbench/common.sh"

INITIAL_STOCK="${INITIAL_STOCK:-10}"

CONTAINER="$(resolve_postgres_container)"
PASSWORD="$(resolve_postgres_password "$CONTAINER")"
if [[ -z "$PASSWORD" ]]; then
  echo "Could not resolve Postgres password. Set PGBENCH_PASSWORD." >&2
  exit 1
fi

OUT="$(docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" \
  psql -U "$PGBENCH_USER" -h localhost -d "$PGBENCH_DB" -t -A -F ' ' \
  < "$SCRIPT_DIR/../01-expose-checkout-oversell/sql/assert.sql")"

# shellcheck disable=SC2086
set -- $OUT
COMPLETED="${1:-0}"
STOCK="${2:-}"

echo "assert-no-oversell: completed=$COMPLETED stock=$STOCK initial_stock=$INITIAL_STOCK"

if [[ -z "$STOCK" ]]; then
  echo "assert-no-oversell: FAIL — product row missing" >&2
  exit 1
fi

if (( COMPLETED > INITIAL_STOCK )); then
  echo "assert-no-oversell: FAIL — oversell (Completed > initial_stock)" >&2
  exit 1
fi

echo "assert-no-oversell: OK (Completed <= initial_stock)"
exit 0
