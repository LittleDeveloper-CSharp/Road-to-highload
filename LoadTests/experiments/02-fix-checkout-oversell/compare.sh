#!/usr/bin/env bash
# Compare stock strategies: Naive / AppLock / DbAtomic / DbForUpdate / Optimistic
# Requires: AppHost up, LOADTEST_BASE_URL, and shop-api rebuilt with Checkout strategies.
# Strategy switch: writes Shop.Api/checkout.strategy.json (reloadOnChange). If metrics look wrong, restart shop-api.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="$SCRIPT_DIR/results"
mkdir -p "$RESULTS_DIR"
LOADTEST_BASE_URL="${LOADTEST_BASE_URL:?Set LOADTEST_BASE_URL}"
PARALLEL="${PARALLEL:-15}"
INITIAL_STOCK="${INITIAL_STOCK:-10}"
RELOAD_WAIT="${RELOAD_WAIT:-3}"
RESULTS_MD="${RESULTS_MD:-$SCRIPT_DIR/results.md}"
export INITIAL_STOCK

STRATEGIES=(Naive AppLock DbAtomic DbForUpdate Optimistic)

echo "# compare PARALLEL=$PARALLEL INITIAL_STOCK=$INITIAL_STOCK" > "$RESULTS_MD"
echo "" >> "$RESULTS_MD"
echo "| strategy | oversell? | wall_s | cpu_avg | cpu_max | rss_avg_kb | rss_max_kb | completed | stock |" >> "$RESULTS_MD"
echo "|---|---|---|---|---|---|---|---|---|" >> "$RESULTS_MD"

for S in "${STRATEGIES[@]}"; do
  echo ""
  echo "======== compare: $S (stock=$INITIAL_STOCK parallel=$PARALLEL) ========"
  "$SCRIPT_DIR/set-strategy.sh" "$S"
  sleep "$RELOAD_WAIT"

  INITIAL_STOCK="$INITIAL_STOCK" "$SCRIPT_DIR/prepare.sh"

  CSV="$RESULTS_DIR/sample-$S.csv"
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
  if "$SCRIPT_DIR/assert-no-oversell.sh"; then
    OVERSELL="no"
  else
    OVERSELL="yes"
  fi

  # completed/stock from last assert print — re-query
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
  # parse summary fields
  CPU_AVG="$(echo "$SUMMARY" | sed -n 's/.*cpu_avg=\([^ ]*\).*/\1/p')"
  CPU_MAX="$(echo "$SUMMARY" | sed -n 's/.*cpu_max=\([^ ]*\).*/\1/p')"
  RSS_AVG="$(echo "$SUMMARY" | sed -n 's/.*rss_avg_kb=\([^ ]*\).*/\1/p')"
  RSS_MAX="$(echo "$SUMMARY" | sed -n 's/.*rss_max_kb=\([^ ]*\).*/\1/p')"

  echo "| $S | $OVERSELL | $WALL | $CPU_AVG | $CPU_MAX | $RSS_AVG | $RSS_MAX | $COMPLETED | $STOCK |" >> "$RESULTS_MD"
  echo "compare: $S done oversell=$OVERSELL wall=$WALL completed=$COMPLETED"
done

# restore default
"$SCRIPT_DIR/set-strategy.sh" DbAtomic
echo ""
echo "compare: results written to $RESULTS_MD"
cat "$RESULTS_MD"
