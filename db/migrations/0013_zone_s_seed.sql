-- Zone S reference rows (ids 0–6) for State Service subtypes (TECH-616 / T1.1.5).
-- Aligned with Assets/Resources/Economy/zone-sub-types.json (displayName + cost scale).

BEGIN;

-- Stable asset PKs 0..6; application matches ZoneSubTypeRegistry subTypeId.
INSERT INTO catalog_asset (id, category, slug, display_name, status, footprint_w, footprint_h, has_button, updated_at)
VALUES
  (0, 'zone_s', 'police',          'Police',         'published', 1, 1, true, now()),
  (1, 'zone_s', 'fire',            'Fire',            'published', 1, 1, true, now()),
  (2, 'zone_s', 'education',      'Education',       'published', 1, 1, true, now()),
  (3, 'zone_s', 'health',          'Health',          'published', 1, 1, true, now()),
  (4, 'zone_s', 'parks',           'Parks',           'published', 1, 1, true, now()),
  (5, 'zone_s', 'public_housing',  'Public Housing',  'published', 1, 1, true, now()),
  (6, 'zone_s', 'public_offices',  'Public Offices',  'published', 1, 1, true, now())
ON CONFLICT (id) DO NOTHING;

-- Economy: JSON baseCost / monthlyUpkeep are whole sim currency units; store cents = units * 100.
INSERT INTO catalog_economy (asset_id, base_cost_cents, monthly_upkeep_cents, demolition_refund_pct, construction_ticks, budget_envelope_id, cost_catalog_row_id)
VALUES
  (0, 50000,  5000,  0, 0, NULL, NULL),
  (1, 60000,  6000,  0, 0, NULL, NULL),
  (2, 80000,  8000,  0, 0, NULL, NULL),
  (3, 100000, 10000, 0, 0, NULL, NULL),
  (4, 30000,  3000,  0, 0, NULL, NULL),
  (5, 70000,  7000,  0, 0, NULL, NULL),
  (6, 90000,  9000,  0, 0, NULL, NULL)
ON CONFLICT (asset_id) DO NOTHING;

SELECT setval(
  pg_get_serial_sequence('catalog_asset', 'id'),
  GREATEST((SELECT COALESCE(MAX(id), 0) FROM catalog_asset), 1)
);

COMMIT;
