-- Prints: completed_count stock
-- Oversell when completed_count > 10 (initial_stock for this experiment).
SELECT
  (SELECT COUNT(*)::int FROM "Orders" WHERE "Status" = 1) AS completed_count,
  (SELECT "Stock" FROM "Products" WHERE "Id" = '11111111-1111-1111-1111-111111111111') AS stock;
