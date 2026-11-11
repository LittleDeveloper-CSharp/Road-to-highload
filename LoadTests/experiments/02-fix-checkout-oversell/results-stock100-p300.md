# compare PARALLEL=300 INITIAL_STOCK=100

| strategy | oversell? | wall_s | cpu_avg | cpu_max | rss_avg_kb | rss_max_kb | completed | stock |
|---|---|---|---|---|---|---|---|---|
| Naive | yes | 12.099 | 66.35 | 148.80 | 248034 | 287936 | 300 | 99 |
| AppLock | no | 9.682 | 52.66 | 161.70 | 309992 | 337488 | 100 | 0 |
| DbAtomic | no | 8.567 | 29.27 | 73.40 | 383607 | 418032 | 100 | 0 |
| DbForUpdate | no | 10.069 | 30.21 | 91.60 | 454514 | 492224 | 100 | 0 |
