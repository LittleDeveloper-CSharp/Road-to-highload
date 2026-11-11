| strategy | oversell? | wall_s | cpu_avg | cpu_max | rss_avg_kb | rss_max_kb | completed | stock |
|---|---|---|---|---|---|---|---|---|
| Naive | yes | 0.941 | 18.15 | 36.30 | 191104 | 195904 | 15 | 9 |
| AppLock | no | 0.678 | 18.60 | 37.00 | 225864 | 227744 | 10 | 0 |
| DbAtomic | no | 0.526 | 25.85 | 50.80 | 241440 | 243520 | 10 | 0 |
| DbForUpdate | no | 0.605 | 10.30 | 20.60 | 248824 | 250896 | 10 | 0 |
