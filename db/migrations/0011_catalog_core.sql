-- Grid asset catalog — core tables (TECH-612 / T1.1.1).
-- Source: docs/grid-asset-visual-registry-exploration.md §8.1
-- updated_at: application-owned on UPDATE (DEFAULT now() on insert); no DB trigger in MVP.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_asset (
  id              bigserial PRIMARY KEY,
  category        text        NOT NULL,
  slug            text        NOT NULL,
  display_name    text        NOT NULL,
  status          text        NOT NULL CHECK (status IN ('draft', 'published', 'retired')),
  replaced_by     bigint      REFERENCES catalog_asset (id) ON DELETE SET NULL,
  footprint_w     int         NOT NULL DEFAULT 1,
  footprint_h     int         NOT NULL DEFAULT 1,
  placement_mode  text,
  unlocks_after   text,
  has_button      boolean     NOT NULL DEFAULT true,
  updated_at      timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_catalog_asset_category_slug UNIQUE (category, slug)
);

CREATE TABLE IF NOT EXISTS catalog_sprite (
  id                         bigserial PRIMARY KEY,
  path                       text        NOT NULL,
  ppu                        int         NOT NULL DEFAULT 100,
  pivot_x                    real        NOT NULL DEFAULT 0.5,
  pivot_y                    real        NOT NULL DEFAULT 0.5,
  provenance                 text        NOT NULL CHECK (provenance IN ('hand', 'generator')),
  generator_archetype_id     text,
  generator_build_fingerprint text,
  art_revision               int         NOT NULL DEFAULT 0
);

-- One sprite binding per slot per asset; soft-retire: assets usually not hard-deleted.
CREATE TABLE IF NOT EXISTS catalog_asset_sprite (
  asset_id  bigint NOT NULL REFERENCES catalog_asset (id) ON DELETE RESTRICT,
  sprite_id bigint NOT NULL REFERENCES catalog_sprite (id) ON DELETE RESTRICT,
  slot      text   NOT NULL CHECK (slot IN (
    'world', 'button_target', 'button_pressed', 'button_disabled', 'button_hover'
  )),
  PRIMARY KEY (asset_id, slot)
);

CREATE TABLE IF NOT EXISTS catalog_economy (
  asset_id                 bigint  PRIMARY KEY
    REFERENCES catalog_asset (id) ON DELETE RESTRICT,
  base_cost_cents          bigint  NOT NULL,
  monthly_upkeep_cents     bigint  NOT NULL,
  demolition_refund_pct    int     NOT NULL DEFAULT 0
    CHECK (demolition_refund_pct >= 0 AND demolition_refund_pct <= 100),
  construction_ticks       int     NOT NULL DEFAULT 0,
  budget_envelope_id       int,
  cost_catalog_row_id      bigint
);

COMMIT;
