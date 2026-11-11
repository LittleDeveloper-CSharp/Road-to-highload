#!/usr/bin/env bash
set -euo pipefail

# Resolve Aspire/Docker Postgres container that hosts appdb.
# Override: PGBENCH_CONTAINER=<docker container name/id>
resolve_postgres_container() {
  if [[ -n "${PGBENCH_CONTAINER:-}" ]]; then
    echo "$PGBENCH_CONTAINER"
    return
  fi

  local id
  id="$(docker ps --filter "name=postgres" --format '{{.ID}}' | head -n 1 || true)"
  if [[ -z "$id" ]]; then
    id="$(docker ps --filter "ancestor=postgres" --format '{{.ID}}' | head -n 1 || true)"
  fi
  if [[ -z "$id" ]]; then
    echo "Postgres container not found. Start AppHost first: dotnet run --project AppHost" >&2
    echo "Or set PGBENCH_CONTAINER=<id>." >&2
    exit 1
  fi
  echo "$id"
}

# Aspire generates POSTGRES_PASSWORD on the container; override with PGBENCH_PASSWORD.
resolve_postgres_password() {
  local container="$1"
  if [[ -n "${PGBENCH_PASSWORD:-}" ]]; then
    echo "$PGBENCH_PASSWORD"
    return
  fi
  docker inspect "$container" --format '{{range .Config.Env}}{{println .}}{{end}}' \
    | sed -n 's/^POSTGRES_PASSWORD=//p' \
    | head -n 1
}

# Aspire default DB name from AppHost: appdb
PGBENCH_DB="${PGBENCH_DB:-appdb}"
PGBENCH_USER="${PGBENCH_USER:-postgres}"
