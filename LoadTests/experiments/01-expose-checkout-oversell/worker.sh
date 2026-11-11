#!/usr/bin/env bash
# One chain: POST /orders → POST /orders/{id}/checkout
set -euo pipefail

BASE="${LOADTEST_BASE_URL:?LOADTEST_BASE_URL required}"
TOKEN="${LOADTEST_TOKEN:?LOADTEST_TOKEN required}"
PRODUCT_ID="${LOADTEST_PRODUCT_ID:-11111111-1111-1111-1111-111111111111}"

ORDER_JSON="$(curl -sS -X POST "$BASE/orders" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"productId\":\"$PRODUCT_ID\",\"quantity\":1}")"

OID="$(printf '%s' "$ORDER_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])" 2>/dev/null || true)"
if [[ -z "$OID" ]]; then
  echo "worker: create order failed: $ORDER_JSON" >&2
  exit 0
fi

curl -sS -o /dev/null -w "%{http_code}" -X POST "$BASE/orders/$OID/checkout" \
  -H "Authorization: Bearer $TOKEN" >/dev/null || true
