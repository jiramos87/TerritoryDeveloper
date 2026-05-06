-- 0075_sprite_catalog.sql
-- Stage 9.6 game-ui-catalog-bake — sprite-catalog DB tier genesis.
-- Creates separate sprite_catalog table (distinct from asset-registry / catalog_entity).
-- Authority: asset-pipeline-standard §Authority (DEC-A25) 2-tier split.
-- TECH-15230

CREATE TABLE IF NOT EXISTS sprite_catalog (
  id            bigserial PRIMARY KEY,
  slug          text        NOT NULL,
  kind          text        NOT NULL DEFAULT 'sprite',
  path          text        NOT NULL,
  tier          text        NOT NULL DEFAULT 'sprite-catalog',
  imported_at   timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_sprite_catalog_kind    CHECK (kind = 'sprite'),
  CONSTRAINT ck_sprite_catalog_tier    CHECK (tier = 'sprite-catalog'),
  CONSTRAINT ck_sprite_catalog_slug_ne CHECK (slug <> ''),
  CONSTRAINT ck_sprite_catalog_path_ne CHECK (path <> ''),
  CONSTRAINT uq_sprite_catalog_path    UNIQUE (path)
);

COMMENT ON TABLE sprite_catalog IS
  'sprite-catalog tier: raw .png sprite assets imported via AssetPostprocessor. '
  'Separate from asset-registry (catalog_entity). '
  'DB-wins authority per asset-pipeline-standard §Authority (DEC-A25). '
  'slug = stem of filename; path = repo-relative asset path; tier fixed = sprite-catalog.';

COMMENT ON COLUMN sprite_catalog.slug IS 'Filename stem without extension (e.g. hud_bar_icon_1).';
COMMENT ON COLUMN sprite_catalog.kind IS 'Fixed: sprite. Constraint-enforced.';
COMMENT ON COLUMN sprite_catalog.path IS 'Repo-relative asset path (e.g. Assets/UI/Sprites/hud_bar_icon_1.png). Unique.';
COMMENT ON COLUMN sprite_catalog.tier IS 'Fixed: sprite-catalog. Constraint-enforced.';
COMMENT ON COLUMN sprite_catalog.imported_at IS 'UTC timestamp of DB row creation (first import or backfill).';
