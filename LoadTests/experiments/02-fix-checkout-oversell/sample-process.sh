#!/usr/bin/env bash
# Sample Shop.Api %CPU and RSS until STOP file appears or timeout.
# Usage: sample-process.sh <out.csv> [interval_sec]
# Stop:  touch "$(dirname out)/.sample-stop"  OR kill this PID
set -euo pipefail

OUT="${1:?usage: sample-process.sh <out.csv> [interval]}"
INTERVAL="${2:-0.5}"
STOP_FILE="$(dirname "$OUT")/.sample-stop"
rm -f "$STOP_FILE"

PID="$(pgrep -f '[S]hop.Api' | head -1 || true)"
if [[ -z "${PID}" ]]; then
  echo "sample-process: Shop.Api PID not found" >&2
  exit 1
fi

echo "timestamp_epoch,cpu_pct,rss_kb" > "$OUT"
echo "sample-process: pid=$PID out=$OUT interval=$INTERVAL"

while [[ ! -f "$STOP_FILE" ]]; do
  # ps: %cpu and rss (KB) — macOS and Linux
  LINE="$(ps -p "$PID" -o %cpu=,rss= 2>/dev/null || true)"
  if [[ -z "$LINE" ]]; then
    echo "sample-process: process exited" >&2
    break
  fi
  # shellcheck disable=SC2086
  set -- $LINE
  CPU="${1:-0}"
  RSS="${2:-0}"
  echo "$(date +%s),$CPU,$RSS" >> "$OUT"
  sleep "$INTERVAL"
done

rm -f "$STOP_FILE"

# Summarize avg/max
python3 - "$OUT" <<'PY'
import sys, csv
path = sys.argv[1]
rows = list(csv.DictReader(open(path)))
if not rows:
    print("sample-process: summary empty")
    sys.exit(0)
cpus = [float(r["cpu_pct"]) for r in rows]
rss = [float(r["rss_kb"]) for r in rows]
print(f"sample-process: n={len(rows)} cpu_avg={sum(cpus)/len(cpus):.2f} cpu_max={max(cpus):.2f} rss_avg_kb={sum(rss)/len(rss):.0f} rss_max_kb={max(rss):.0f}")
# also write sidecar summary for compare.sh
open(path + ".summary", "w").write(
    f"n={len(rows)} cpu_avg={sum(cpus)/len(cpus):.2f} cpu_max={max(cpus):.2f} rss_avg_kb={sum(rss)/len(rss):.0f} rss_max_kb={max(rss):.0f}\n"
)
PY
