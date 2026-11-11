#!/usr/bin/env bash
# 15 parallel order→checkout chains (bash xargs -P). Chosen over NBomber inject so
# starts are truly concurrent — better chance to hit lost-update on Stock.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

BASE="${LOADTEST_BASE_URL:?Set LOADTEST_BASE_URL to shop-api HTTP URL (Aspire dashboard)}"
EMAIL="${LOADTEST_EMAIL:-demo@local.test}"
PASSWORD="${LOADTEST_PASSWORD:-Demo123!}"
PRODUCT_ID="${LOADTEST_PRODUCT_ID:-11111111-1111-1111-1111-111111111111}"
PARALLEL="${PARALLEL:-15}"

LOGIN="$(curl -sS -X POST "$BASE/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")"
TOKEN="$(printf '%s' "$LOGIN" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")"

export LOADTEST_BASE_URL="$BASE"
export LOADTEST_TOKEN="$TOKEN"
export LOADTEST_PRODUCT_ID="$PRODUCT_ID"

echo "run: base=$BASE parallel=$PARALLEL product=$PRODUCT_ID"
# seq | xargs -P N -I{} runs up to N workers at once
seq 1 "$PARALLEL" | xargs -P "$PARALLEL" -I{} "$SCRIPT_DIR/worker.sh"
echo "run: done"
