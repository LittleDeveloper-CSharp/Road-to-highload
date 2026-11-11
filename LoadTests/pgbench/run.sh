#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "$SCRIPT_DIR/common.sh"

CONTAINER="$(resolve_postgres_container)"
PASSWORD="$(resolve_postgres_password "$CONTAINER")"
CLIENTS="${PGBENCH_CLIENTS:-4}"
JOBS="${PGBENCH_JOBS:-2}"
DURATION="${PGBENCH_DURATION:-15}"

if [[ -z "$PASSWORD" ]]; then
  echo "Could not resolve Postgres password. Set PGBENCH_PASSWORD." >&2
  exit 1
fi

echo "pgbench: clients=$CLIENTS jobs=$JOBS duration=${DURATION}s db=$PGBENCH_DB"

if [[ -n "${PGBENCH_SCRIPT:-}" ]]; then
  echo "Custom script: $PGBENCH_SCRIPT"
  docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" sh -c \
    "cat > /tmp/pgbench-custom.sql && pgbench -U \"$PGBENCH_USER\" -h localhost -c $CLIENTS -j $JOBS -T $DURATION -f /tmp/pgbench-custom.sql \"$PGBENCH_DB\"" \
    < "$PGBENCH_SCRIPT"
else
  docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" \
    pgbench -U "$PGBENCH_USER" -h localhost -c "$CLIENTS" -j "$JOBS" -T "$DURATION" "$PGBENCH_DB"
fi
