-- Wipe shop orders for an isolated oversell run (Dev polygon only).
-- Stock comes from psql -v stock=N (default 10 via prepare.sh).
DELETE FROM "Payments";
DELETE FROM "Orders";

UPDATE "Products"
SET "Stock" = :stock
WHERE "Id" = '11111111-1111-1111-1111-111111111111';
