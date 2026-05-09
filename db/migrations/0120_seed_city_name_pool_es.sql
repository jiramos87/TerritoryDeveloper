-- 0120_seed_city_name_pool_es.sql
-- Wave A2 (TECH-27068) — city-name pool (ES).
--
-- DEFERRED: catalog_entity_kind_check enum does not include 'string-pool'.
-- City name generation handled at runtime via CityNameGenerator (C# hardcoded list).
-- This migration is a no-op placeholder to keep migration numbering intact.

BEGIN;

DO $$ BEGIN
  RAISE NOTICE '0120 SKIP: string-pool kind not in catalog_entity enum — city names deferred to future schema extension';
END $$;

COMMIT;
