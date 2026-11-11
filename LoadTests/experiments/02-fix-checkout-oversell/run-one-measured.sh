#!/usr/bin/env bash
# One strategy: prepare → sample → run PARALLEL → assert → append results-screenshots-rerun.md
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STRATEGY="${1:?usage: run-one-measured.sh <Naive|AppLock|DbAtomic|DbForUpdate>}"
PARALLEL="${PARALLEL:-100}"
INITIAL_STOCK="${INITIAL_STOCK:-100}"
LOADTEST_BASE_URL="${LOADTEST_BASE_URL:?Set LOADTEST_BASE_URL}"
RESULTS_MD="${RESULTS_MD:-$SCRIPT_DIR/results-screenshots-rerun.md}"
RESULTS_DIR="$SCRIPT_DIR/results"
mkdir -p "$RESULTS_DIR"
PROM_PORT="${PROM_PORT:-}"

"$SCRIPT_DIR/set-strategy.sh" "$STRATEGY"
sleep 2

INITIAL_STOCK="$INITIAL_STOCK" "$SCRIPT_DIR/prepare.sh"

CSV="$RESULTS_DIR/sample-rerun-$STRATEGY.csv"
rm -f "$RESULTS_DIR/.sample-stop"
"$SCRIPT_DIR/sample-process.sh" "$CSV" 0.5 &
SAMPLE_PID=$!

START="$(python3 -c 'import time; print(time.time())')"
PARALLEL="$PARALLEL" LOADTEST_BASE_URL="$LOADTEST_BASE_URL" "$SCRIPT_DIR/run.sh" || true
END="$(python3 -c 'import time; print(time.time())')"
WALL="$(python3 -c "print(f'{float('$END')-float('$START'):.3f}')")"

touch "$RESULTS_DIR/.sample-stop"
wait "$SAMPLE_PID" || true

OVERSELL="no"
if INITIAL_STOCK="$INITIAL_STOCK" "$SCRIPT_DIR/assert-no-oversell.sh"; then
  OVERSELL="no"
else
  OVERSELL="yes"
fi

# shellcheck source=../../pgbench/common.sh
source "$SCRIPT_DIR/../../pgbench/common.sh"
CONTAINER="$(resolve_postgres_container)"
PASSWORD="$(resolve_postgres_password "$CONTAINER")"
OUT="$(docker exec -e "PGPASSWORD=$PASSWORD" -i "$CONTAINER" \
  psql -U "$PGBENCH_USER" -h localhost -d "$PGBENCH_DB" -t -A -F ' ' \
  < "$SCRIPT_DIR/../01-expose-checkout-oversell/sql/assert.sql")"
# shellcheck disable=SC2086
set -- $OUT
COMPLETED="${1:-?}"
STOCK="${2:-?}"

SUMMARY="$(cat "$CSV.summary" 2>/dev/null || echo 'n=0 cpu_avg=? cpu_max=? rss_avg_kb=? rss_max_kb=?')"
CPU_AVG="$(echo "$SUMMARY" | sed -n 's/.*cpu_avg=\([^ ]*\).*/\1/p')"
CPU_MAX="$(echo "$SUMMARY" | sed -n 's/.*cpu_max=\([^ ]*\).*/\1/p')"
RSS_AVG="$(echo "$SUMMARY" | sed -n 's/.*rss_avg_kb=\([^ ]*\).*/\1/p')"
RSS_MAX="$(echo "$SUMMARY" | sed -n 's/.*rss_max_kb=\([^ ]*\).*/\1/p')"

CHECKOUT_OK="?"
CHECKOUT_5XX="?"
NOTES=""
if [[ -n "$PROM_PORT" ]]; then
  Q_OK='sum(increase(http_server_request_duration_seconds_count{http_route="orders/{id:guid}/checkout",http_response_status_code="200"}[3m]))'
  Q_5XX='sum(increase(http_server_request_duration_seconds_count{http_route="orders/{id:guid}/checkout",http_response_status_code=~"5.."}[3m]))'
  CHECKOUT_OK="$(curl -sS --get "http://127.0.0.1:$PROM_PORT/api/v1/query" --data-urlencode "query=$Q_OK" \
    | python3 -c 'import sys,json; r=json.load(sys.stdin)["data"]["result"]; print(round(float(r[0]["value"][1])) if r else 0)' 2>/dev/null || echo '?')"
  CHECKOUT_5XX="$(curl -sS --get "http://127.0.0.1:$PROM_PORT/api/v1/query" --data-urlencode "query=$Q_5XX" \
    | python3 -c 'import sys,json; r=json.load(sys.stdin)["data"]["result"]; print(round(float(r[0]["value"][1])) if r else 0)' 2>/dev/null || echo '?')"
  NOTES="prom_increase_3m"
fi

echo "| $STRATEGY | $OVERSELL | $WALL | $CPU_AVG | $CPU_MAX | $RSS_AVG | $RSS_MAX | $COMPLETED | $STOCK | $CHECKOUT_OK | $CHECKOUT_5XX | $NOTES |" >> "$RESULTS_MD"
echo "run-one-measured: $STRATEGY oversell=$OVERSELL wall=$WALL completed=$COMPLETED stock=$STOCK checkout_ok=$CHECKOUT_OK checkout_5xx=$CHECKOUT_5XX"
