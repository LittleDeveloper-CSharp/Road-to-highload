# screenshots rerun 2026-11-09T00:23:51Z

Параметры прогона: INITIAL_STOCK=100 PARALLEL=100
shop-api: http://127.0.0.1:11872
before = idle после prepare(stock=100), до PARALLEL=100
after = сразу после / во время того же прогона

| strategy | oversell? | wall_s | cpu_avg | cpu_max | rss_avg_kb | rss_max_kb | completed | stock | checkout_ok | checkout_5xx | notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Naive | no | 4.354 | 0.00 | 0.00 | 103934 | 104096 | 100 | 98 | 0 | 0 | prom_increase_3m |
| AppLock | no | 5.963 | 0.00 | 0.00 | 52964 | 56544 | 100 | 0 | 83 | 0 | prom_increase_3m |
| DbAtomic | no | 4.116 | 0.00 | 0.00 | 48939 | 48960 | 100 | 0 | 101 | 0 | prom_increase_3m |
| DbForUpdate | no | 1.736 | 0.00 | 0.00 | 45728 | 45728 | 100 | 0 | 101 | 0 | prom_increase_3m |
