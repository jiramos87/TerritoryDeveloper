-- Catalog spine schema (DEC-A8 / DEC-A38). Pure additive — does NOT mutate or
-- read legacy `catalog_*` tables. Backfill happens in 0022; legacy drop in 0023.
--
-- Spine model:
--   catalog_entity      — kind-tagged identity row (asset, sprite, pool, …)
--   entity_version      — versioned params per entity (current_published_version_id)
--   {kind}_detail       — kind-scoped 1:1 attribute tables, PK = entity_id
--   pool_member         — entity-id-keyed M:N (replaces catalog_pool_member)
--
-- Legacy id preservation: detail tables carry `legacy_*_id` UNIQUE columns.
-- Unity `ZoneSubTypeRegistry.subTypeId` reads `catalog_asset.id` (Zone S 0..6
-- seeded by 0013_zone_s_seed.sql); `catalog_asset_compat` view in 0023 will
-- expose these as `id` so consumer code keeps reading same numeric ids.
--
-- Slug regex: ^[a-z][a-z0-9_]{2,63}$  — enforced by CHECK + validate:catalog-spine.

BEGIN;

-- ─── catalog_entity ────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS catalog_entity (
  id                            bigserial PRIMARY KEY,
  kind                          text        NOT NULL CHECK (kind IN (
    'sprite', 'asset', 'button', 'panel', 'pool', 'token', 'archetype', 'audio'
  )),
  slug                          text        NOT NULL,
  display_name                  text        NOT NULL,
  tags                          text[]      NOT NULL DEFAULT '{}',
  current_published_version_id  bigint,                       -- FK added below (cycle break)
  slug_frozen_at                timestamptz,
  retired_at                    timestamptz,
  retired_by_user_id            bigint,                       -- FK once users land (Stage 2)
  retired_reason                text,
  replaced_by_entity_id         bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  lint_overrides_json           jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at                    timestamptz NOT NULL DEFAULT now(),
  updated_at                    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_catalog_entity_kind_slug UNIQUE (kind, slug),
  CONSTRAINT ck_catalog_entity_slug_format CHECK (slug ~ '^[a-z][a-z0-9_]{2,63}$')
);

CREATE INDEX IF NOT EXISTS catalog_entity_kind_idx
  ON catalog_entity (kind);
CREATE INDEX IF NOT EXISTS catalog_entity_retired_idx
  ON catalog_entity (retired_at)
  WHERE retired_at IS NOT NULL;
CREATE INDEX IF NOT EXISTS catalog_entity_replaced_by_idx
  ON catalog_entity (replaced_by_entity_id)
  WHERE replaced_by_entity_id IS NOT NULL;

-- ─── entity_version ────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS entity_version (
  id                    bigserial PRIMARY KEY,
  entity_id             bigint      NOT NULL REFERENCES catalog_entity (id) ON DELETE CASCADE,
  version_number        int         NOT NULL CHECK (version_number >= 1),
  status                text        NOT NULL CHECK (status IN ('draft', 'published')),
  archetype_version_id  bigint      REFERENCES entity_version (id) ON DELETE SET NULL,
  params_json           jsonb       NOT NULL DEFAULT '{}'::jsonb,
  parent_version_id     bigint      REFERENCES entity_version (id) ON DELETE SET NULL,
  source_run_id         uuid,                                     -- DEC-A41
  source_variant_idx    int,
  lint_overrides_json   jsonb       NOT NULL DEFAULT '{}'::jsonb,
  manual_pin            boolean     NOT NULL DEFAULT false,
  created_at            timestamptz NOT NULL DEFAULT now(),
  updated_at            timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_entity_version_number UNIQUE (entity_id, version_number)
);

CREATE INDEX IF NOT EXISTS entity_version_entity_idx
  ON entity_version (entity_id);
CREATE INDEX IF NOT EXISTS entity_version_status_idx
  ON entity_version (status);

-- Resolve catalog_entity.current_published_version_id FK now that entity_version exists.
ALTER TABLE catalog_entity
  ADD CONSTRAINT fk_catalog_entity_current_version
  FOREIGN KEY (current_published_version_id) REFERENCES entity_version (id)
  ON DELETE SET NULL DEFERRABLE INITIALLY DEFERRED;

-- ─── sprite_detail ─────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS sprite_detail (
  entity_id           bigint  PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  legacy_sprite_id    bigint  UNIQUE,                              -- DEC-A8 transition
  source_uri          text,                                        -- gen://run_id/idx | asset://...
  assets_path         text,                                        -- Assets/Sprites/Generated/...
  pixels_per_unit     int     NOT NULL DEFAULT 100,
  pivot_x             real    NOT NULL DEFAULT 0.5,
  pivot_y             real    NOT NULL DEFAULT 0.5,
  provenance          text    NOT NULL CHECK (provenance IN ('hand', 'generator')),
  source_run_id       uuid,
  source_variant_idx  int,
  build_fingerprint   text,
  palette_hash        text
);

CREATE INDEX IF NOT EXISTS sprite_detail_provenance_idx
  ON sprite_detail (provenance);
CREATE INDEX IF NOT EXISTS sprite_detail_build_fingerprint_idx
  ON sprite_detail (build_fingerprint)
  WHERE build_fingerprint IS NOT NULL;

-- ─── asset_detail ──────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS asset_detail (
  entity_id                          bigint  PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  legacy_asset_id                    bigint  UNIQUE,                  -- DEC-A8: ZoneSubTypeRegistry.subTypeId
  category                           text    NOT NULL,
  footprint_w                        int     NOT NULL DEFAULT 1,
  footprint_h                        int     NOT NULL DEFAULT 1,
  placement_mode                     text,
  unlocks_after                      text,
  has_button                         boolean NOT NULL DEFAULT true,
  world_sprite_entity_id             bigint  REFERENCES catalog_entity (id) ON DELETE SET NULL,
  button_target_sprite_entity_id     bigint  REFERENCES catalog_entity (id) ON DELETE SET NULL,
  button_pressed_sprite_entity_id    bigint  REFERENCES catalog_entity (id) ON DELETE SET NULL,
  button_disabled_sprite_entity_id   bigint  REFERENCES catalog_entity (id) ON DELETE SET NULL,
  button_hover_sprite_entity_id      bigint  REFERENCES catalog_entity (id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS asset_detail_category_idx
  ON asset_detail (category);
CREATE INDEX IF NOT EXISTS asset_detail_world_sprite_idx
  ON asset_detail (world_sprite_entity_id)
  WHERE world_sprite_entity_id IS NOT NULL;

-- ─── economy_detail ────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS economy_detail (
  entity_id              bigint  PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  base_cost_cents        bigint  NOT NULL,
  monthly_upkeep_cents   bigint  NOT NULL,
  demolition_refund_pct  int     NOT NULL DEFAULT 0
    CHECK (demolition_refund_pct >= 0 AND demolition_refund_pct <= 100),
  construction_ticks     int     NOT NULL DEFAULT 0,
  budget_envelope_id     int,
  cost_catalog_row_id    bigint
);

-- ─── pool_detail ───────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS pool_detail (
  entity_id        bigint PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  legacy_pool_id   bigint UNIQUE,                                  -- DEC-A8 transition
  primary_subtype  text,                                            -- legacy.owner_subtype
  owner_category   text                                             -- legacy.owner_category
);

CREATE INDEX IF NOT EXISTS pool_detail_owner_category_idx
  ON pool_detail (owner_category)
  WHERE owner_category IS NOT NULL;

-- ─── pool_member ───────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS pool_member (
  pool_entity_id   bigint NOT NULL REFERENCES catalog_entity (id) ON DELETE CASCADE,
  asset_entity_id  bigint NOT NULL REFERENCES catalog_entity (id) ON DELETE RESTRICT,
  weight           int    NOT NULL DEFAULT 1 CHECK (weight > 0),
  PRIMARY KEY (pool_entity_id, asset_entity_id)
);

CREATE INDEX IF NOT EXISTS pool_member_asset_idx
  ON pool_member (asset_entity_id);

-- ─── updated_at trigger (DEC-A38) ──────────────────────────────────────────

CREATE OR REPLACE FUNCTION catalog_touch_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_catalog_entity_touch ON catalog_entity;
CREATE TRIGGER trg_catalog_entity_touch
  BEFORE UPDATE ON catalog_entity
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

DROP TRIGGER IF EXISTS trg_entity_version_touch ON entity_version;
CREATE TRIGGER trg_entity_version_touch
  BEFORE UPDATE ON entity_version
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

COMMIT;

-- Rollback: bash tools/scripts/restore-db-snapshot.sh \
--            var/db-snapshots/pre-spine-{date}.dump --confirm
