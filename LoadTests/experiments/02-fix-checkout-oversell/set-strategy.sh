#!/usr/bin/env bash
# Writes Shop.Api/checkout.strategy.json (reloadOnChange). Values: Naive|AppLock|DbAtomic|DbForUpdate|Optimistic
set -euo pipefail
STRATEGY="${1:?usage: set-strategy.sh <Naive|AppLock|DbAtomic|DbForUpdate|Optimistic>}"
SHOP_API="/Users/pavel/Desktop/Road to highload/src/Services/Shop.Api"
FILE="$SHOP_API/checkout.strategy.json"
cat > "$FILE" <<JSON
{
  "Checkout": {
    "StockStrategy": "$STRATEGY"
  }
}
JSON
echo "set-strategy: wrote $FILE -> $STRATEGY (wait ~2s for IOptionsMonitor reload, or restart shop-api)"
