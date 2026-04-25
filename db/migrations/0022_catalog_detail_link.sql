-- Catalog spine backfill (DEC-A8). Reads legacy `catalog_*` tables, populates
-- `catalog_entity` + `entity_version` + `*_detail` + `pool_member`. Legacy
-- tables are NOT mutated. Drop happens in 0023.
--
-- Idempotency: applied once per `schema_migrations` row. Re-running this file
-- with psql -f against an already-migrated DB will fail at the first INSERT
-- via the (kind, slug) UNIQUE constraint — that is the desired behavior.
--
-- Slug rules:
--   - assets/pools: copy `lower(slug)` (Stage 0 audit confirmed regex compliance).
--   - sprites: synthesize `sprite_{legacy_id}_{base}` where base = sanitized
--     `generator_archetype_id` or 'hand'. Length capped to fit slug regex.

BEGIN;

-- ─── Lookup tables — legacy id → spine entity_id ──────────────────────────

CREATE TEMP TABLE _legacy_asset_map (
  legacy_id bigint PRIMARY KEY,
  entity_id bigint NOT NULL UNIQUE
) ON COMMIT DROP;

CREATE TEMP TABLE _legacy_sprite_map (
  legacy_id bigint PRIMARY KEY,
  entity_id bigint NOT NULL UNIQUE,
  slug      text   NOT NULL UNIQUE
) ON COMMIT DROP;

CREATE TEMP TABLE _legacy_pool_map (
  legacy_id bigint PRIMARY KEY,
  entity_id bigint NOT NULL UNIQUE
) ON COMMIT DROP;

-- ─── Step 1: catalog_asset → catalog_entity (kind=asset) + map ────────────

WITH src AS (
  SELECT
    id           AS legacy_id,
    lower(slug)  AS slug,
    display_name,
    status
  FROM catalog_asset
),
inserted AS (
  INSERT INTO catalog_entity (kind, slug, display_name, retired_at)
  SELECT
    'asset',
    slug,
    display_name,
    CASE WHEN status = 'retired' THEN now() ELSE NULL END
  FROM src
  RETURNING id AS entity_id, slug
)
INSERT INTO _legacy_asset_map (legacy_id, entity_id)
SELECT s.legacy_id, i.entity_id
FROM src s
JOIN inserted i USING (slug);

-- ─── Step 2: asset_detail rows ─────────────────────────────────────────────

INSERT INTO asset_detail (
  entity_id, legacy_asset_id, category,
  footprint_w, footprint_h, placement_mode, unlocks_after, has_button
)
SELECT
  m.entity_id,
  a.id,
  a.category,
  a.footprint_w, a.footprint_h, a.placement_mode, a.unlocks_after, a.has_button
FROM catalog_asset a
JOIN _legacy_asset_map m ON m.legacy_id = a.id;

-- ─── Step 3: asset entity_version v1 (published) + current pointer ────────

WITH inserted AS (
  INSERT INTO entity_version (entity_id, version_number, status, params_json)
  SELECT m.entity_id, 1, 'published', '{}'::jsonb
  FROM _legacy_asset_map m
  RETURNING id AS version_id, entity_id
)
UPDATE catalog_entity ce
   SET current_published_version_id = i.version_id
  FROM inserted i
 WHERE ce.id = i.entity_id;

-- ─── Step 4: catalog_asset.replaced_by → catalog_entity.replaced_by_entity_id ─

UPDATE catalog_entity ce
   SET replaced_by_entity_id = m_target.entity_id
  FROM catalog_asset a
  JOIN _legacy_asset_map m_src    ON m_src.legacy_id    = a.id
  JOIN _legacy_asset_map m_target ON m_target.legacy_id = a.replaced_by
 WHERE ce.id = m_src.entity_id
   AND a.replaced_by IS NOT NULL;

-- ─── Step 5: catalog_economy → economy_detail ─────────────────────────────

INSERT INTO economy_detail (
  entity_id, base_cost_cents, monthly_upkeep_cents, demolition_refund_pct,
  construction_ticks, budget_envelope_id, cost_catalog_row_id
)
SELECT
  m.entity_id,
  e.base_cost_cents, e.monthly_upkeep_cents, e.demolition_refund_pct,
  e.construction_ticks, e.budget_envelope_id, e.cost_catalog_row_id
FROM catalog_economy e
JOIN _legacy_asset_map m ON m.legacy_id = e.asset_id;

-- ─── Step 6: catalog_sprite → catalog_entity (kind=sprite) + map ──────────

WITH src AS (
  SELECT
    s.id AS legacy_id,
    s.path,
    s.ppu, s.pivot_x, s.pivot_y, s.provenance,
    s.generator_archetype_id, s.generator_build_fingerprint,
    -- Synthesize slug: 'sprite_{id}_{sanitized_archetype_or_hand}', cap base at 30 chars.
    'sprite_' || s.id::text || '_' || left(
      regexp_replace(
        lower(coalesce(s.generator_archetype_id, 'hand')),
        '[^a-z0-9_]', '_', 'g'
      ),
      30
    ) AS slug,
    -- display_name: filename basename (final segment of path)
    coalesce(
      nullif(regexp_replace(s.path, '^.*/', ''), ''),
      'sprite_' || s.id::text
    ) AS display_name
  FROM catalog_sprite s
),
inserted AS (
  INSERT INTO catalog_entity (kind, slug, display_name)
  SELECT 'sprite', slug, display_name
  FROM src
  RETURNING id AS entity_id, slug
)
INSERT INTO _legacy_sprite_map (legacy_id, entity_id, slug)
SELECT s.legacy_id, i.entity_id, s.slug
FROM src s
JOIN inserted i USING (slug);

-- ─── Step 7: sprite_detail rows ───────────────────────────────────────────

INSERT INTO sprite_detail (
  entity_id, legacy_sprite_id,
  source_uri, assets_path, pixels_per_unit, pivot_x, pivot_y,
  provenance, build_fingerprint
)
SELECT
  m.entity_id,
  s.id,
  NULL,                             -- source_uri unknown for legacy rows (DEC-A41 introduced post-spine)
  s.path,
  s.ppu, s.pivot_x, s.pivot_y,
  s.provenance,
  s.generator_build_fingerprint
FROM catalog_sprite s
JOIN _legacy_sprite_map m ON m.legacy_id = s.id;

-- ─── Step 8: sprite entity_version v1 (published) + current pointer ──────

WITH inserted AS (
  INSERT INTO entity_version (entity_id, version_number, status, params_json)
  SELECT m.entity_id, 1, 'published', '{}'::jsonb
  FROM _legacy_sprite_map m
  RETURNING id AS version_id, entity_id
)
UPDATE catalog_entity ce
   SET current_published_version_id = i.version_id
  FROM inserted i
 WHERE ce.id = i.entity_id;

-- ─── Step 9: catalog_asset_sprite → asset_detail.{slot}_sprite_entity_id ──

UPDATE asset_detail ad
   SET world_sprite_entity_id            = sm.entity_id
  FROM catalog_asset_sprite cas
  JOIN _legacy_sprite_map sm ON sm.legacy_id = cas.sprite_id
  JOIN _legacy_asset_map  am ON am.legacy_id = cas.asset_id
 WHERE ad.entity_id = am.entity_id
   AND cas.slot     = 'world';

UPDATE asset_detail ad
   SET button_target_sprite_entity_id    = sm.entity_id
  FROM catalog_asset_sprite cas
  JOIN _legacy_sprite_map sm ON sm.legacy_id = cas.sprite_id
  JOIN _legacy_asset_map  am ON am.legacy_id = cas.asset_id
 WHERE ad.entity_id = am.entity_id
   AND cas.slot     = 'button_target';

UPDATE asset_detail ad
   SET button_pressed_sprite_entity_id   = sm.entity_id
  FROM catalog_asset_sprite cas
  JOIN _legacy_sprite_map sm ON sm.legacy_id = cas.sprite_id
  JOIN _legacy_asset_map  am ON am.legacy_id = cas.asset_id
 WHERE ad.entity_id = am.entity_id
   AND cas.slot     = 'button_pressed';

UPDATE asset_detail ad
   SET button_disabled_sprite_entity_id  = sm.entity_id
  FROM catalog_asset_sprite cas
  JOIN _legacy_sprite_map sm ON sm.legacy_id = cas.sprite_id
  JOIN _legacy_asset_map  am ON am.legacy_id = cas.asset_id
 WHERE ad.entity_id = am.entity_id
   AND cas.slot     = 'button_disabled';

UPDATE asset_detail ad
   SET button_hover_sprite_entity_id     = sm.entity_id
  FROM catalog_asset_sprite cas
  JOIN _legacy_sprite_map sm ON sm.legacy_id = cas.sprite_id
  JOIN _legacy_asset_map  am ON am.legacy_id = cas.asset_id
 WHERE ad.entity_id = am.entity_id
   AND cas.slot     = 'button_hover';

-- ─── Step 10: catalog_spawn_pool → catalog_entity (kind=pool) + map ──────

WITH src AS (
  SELECT
    id          AS legacy_id,
    lower(slug) AS slug,
    owner_category,
    owner_subtype
  FROM catalog_spawn_pool
),
inserted AS (
  INSERT INTO catalog_entity (kind, slug, display_name)
  SELECT 'pool', slug, slug
  FROM src
  RETURNING id AS entity_id, slug
)
INSERT INTO _legacy_pool_map (legacy_id, entity_id)
SELECT s.legacy_id, i.entity_id
FROM src s
JOIN inserted i USING (slug);

-- ─── Step 11: pool_detail rows ────────────────────────────────────────────

INSERT INTO pool_detail (
  entity_id, legacy_pool_id, primary_subtype, owner_category
)
SELECT
  m.entity_id,
  p.id,
  p.owner_subtype,
  p.owner_category
FROM catalog_spawn_pool p
JOIN _legacy_pool_map m ON m.legacy_id = p.id;

-- ─── Step 12: pool entity_version v1 (published) + current pointer ──────

WITH inserted AS (
  INSERT INTO entity_version (entity_id, version_number, status, params_json)
  SELECT m.entity_id, 1, 'published', '{}'::jsonb
  FROM _legacy_pool_map m
  RETURNING id AS version_id, entity_id
)
UPDATE catalog_entity ce
   SET current_published_version_id = i.version_id
  FROM inserted i
 WHERE ce.id = i.entity_id;

-- ─── Step 13: catalog_pool_member → pool_member ──────────────────────────

INSERT INTO pool_member (pool_entity_id, asset_entity_id, weight)
SELECT
  pm_pool.entity_id,
  pm_asset.entity_id,
  cpm.weight
FROM catalog_pool_member cpm
JOIN _legacy_pool_map  pm_pool  ON pm_pool.legacy_id  = cpm.pool_id
JOIN _legacy_asset_map pm_asset ON pm_asset.legacy_id = cpm.asset_id;

-- ─── Step 14: checksum gate ──────────────────────────────────────────────

DO $$
DECLARE
  legacy_assets   bigint;
  spine_assets    bigint;
  legacy_sprites  bigint;
  spine_sprites   bigint;
  legacy_pools    bigint;
  spine_pools     bigint;
  legacy_economy  bigint;
  spine_economy   bigint;
  legacy_members  bigint;
  spine_members   bigint;
  legacy_slots    bigint;
  spine_slots     bigint;
BEGIN
  SELECT count(*) INTO legacy_assets  FROM catalog_asset;
  SELECT count(*) INTO spine_assets   FROM asset_detail;
  IF legacy_assets <> spine_assets THEN
    RAISE EXCEPTION 'asset checksum mismatch: legacy=% spine=%', legacy_assets, spine_assets;
  END IF;

  SELECT count(*) INTO legacy_sprites FROM catalog_sprite;
  SELECT count(*) INTO spine_sprites  FROM sprite_detail;
  IF legacy_sprites <> spine_sprites THEN
    RAISE EXCEPTION 'sprite checksum mismatch: legacy=% spine=%', legacy_sprites, spine_sprites;
  END IF;

  SELECT count(*) INTO legacy_pools   FROM catalog_spawn_pool;
  SELECT count(*) INTO spine_pools    FROM pool_detail;
  IF legacy_pools <> spine_pools THEN
    RAISE EXCEPTION 'pool checksum mismatch: legacy=% spine=%', legacy_pools, spine_pools;
  END IF;

  SELECT count(*) INTO legacy_economy FROM catalog_economy;
  SELECT count(*) INTO spine_economy  FROM economy_detail;
  IF legacy_economy <> spine_economy THEN
    RAISE EXCEPTION 'economy checksum mismatch: legacy=% spine=%', legacy_economy, spine_economy;
  END IF;

  SELECT count(*) INTO legacy_members FROM catalog_pool_member;
  SELECT count(*) INTO spine_members  FROM pool_member;
  IF legacy_members <> spine_members THEN
    RAISE EXCEPTION 'pool_member checksum mismatch: legacy=% spine=%', legacy_members, spine_members;
  END IF;

  -- Slot bindings: count non-null sprite refs across all 5 slot columns vs legacy rows.
  SELECT count(*) INTO legacy_slots FROM catalog_asset_sprite;
  SELECT
    count(world_sprite_entity_id)
    + count(button_target_sprite_entity_id)
    + count(button_pressed_sprite_entity_id)
    + count(button_disabled_sprite_entity_id)
    + count(button_hover_sprite_entity_id)
    INTO spine_slots
  FROM asset_detail;
  IF legacy_slots <> spine_slots THEN
    RAISE EXCEPTION 'sprite-slot checksum mismatch: legacy=% spine=%', legacy_slots, spine_slots;
  END IF;

  RAISE NOTICE 'spine backfill OK: assets=% sprites=% pools=% economy=% members=% slots=%',
    spine_assets, spine_sprites, spine_pools, spine_economy, spine_members, spine_slots;
END $$;

-- ─── Step 15: catalog_asset_compat view (DEC-A8 transition) ──────────────
-- Exposes spine asset rows under the legacy `catalog_asset` shape, keyed by
-- `legacy_asset_id` so Unity `ZoneSubTypeRegistry.subTypeId` (Zone S 0..6)
-- keeps reading the same numeric ids during cutover. View drops in 0023
-- together with legacy tables.

CREATE OR REPLACE VIEW catalog_asset_compat AS
SELECT
  ad.legacy_asset_id    AS id,
  ad.category           AS category,
  ce.slug               AS slug,
  ce.display_name       AS display_name,
  CASE
    WHEN ce.retired_at IS NOT NULL                   THEN 'retired'
    WHEN ce.current_published_version_id IS NOT NULL THEN 'published'
    ELSE 'draft'
  END                   AS status,
  (SELECT ad2.legacy_asset_id
     FROM asset_detail ad2
    WHERE ad2.entity_id = ce.replaced_by_entity_id) AS replaced_by,
  ad.footprint_w,
  ad.footprint_h,
  ad.placement_mode,
  ad.unlocks_after,
  ad.has_button,
  ce.updated_at
FROM catalog_entity ce
JOIN asset_detail   ad ON ad.entity_id = ce.id
WHERE ce.kind = 'asset';

COMMIT;

-- Rollback: bash tools/scripts/restore-db-snapshot.sh \
--            var/db-snapshots/pre-spine-{date}.dump --confirm
