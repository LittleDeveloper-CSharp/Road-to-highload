#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "$SCRIPT_DIR/common.sh"

CONTAINER="$(resolve_postgres_container)"
PASSWORD="$(resolve_postgres_password "$CONTAINER")"
SCALE="${PGBENCH_SCALE:-1}"

if [[ -z "$PASSWORD" ]]; then
  echo "Could not resolve Postgres password. Set PGBENCH_PASSWORD." >&2
  exit 1
fi

echo "Initializing pgbench schema in db='$PGBENCH_DB' (container=$CONTAINER, scale=$SCALE)"
docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" \
  pgbench -U "$PGBENCH_USER" -h localhost -i -s "$SCALE" "$PGBENCH_DB"

echo "Done. Run ./run.sh"
