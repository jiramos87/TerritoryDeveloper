-- 0043_catalog_ref_edge.sql
--
-- asset-pipeline Stage 14.1 / TECH-3001.
-- DEC-A37 + DEC-A42 — canonical materialized cross-entity ref graph.
--
-- `catalog_ref_edge` rows are populated by the publish hook
-- (`web/lib/refs/edge-builder.ts` — TECH-3002) on every entity publish,
-- and consumed by Stage 14.4 References tab + Stage 14.4 ripple upgrade
-- + dangling-ref lint (TECH-3003).
--
-- Edge roles enumerated (8 total per spec §2.1):
--   panel.token        — panel slot binding pointing at a token entity
--   button.sprite      — button sprite slot pointing at a sprite entity
--   asset.sprite       — asset sprite slot pointing at a sprite entity
--   pool.asset         — pool member pointing at an asset entity
--   archetype.asset    — archetype param pointing at an asset entity
--   archetype.sprite   — archetype param pointing at a sprite entity
--   archetype.token    — archetype param pointing at a token entity
--   archetype.audio    — archetype param pointing at an audio entity
--
-- Slot 0042 was claimed by `0042_master_plan_change_log_unique.sql`
-- (DB Lifecycle Extensions Stage 1 / TECH-2974); locking on 0043.
--
-- Soft FK to `catalog_entity` only — `src_kind`/`dst_kind` are text
-- discriminators, no SQL FK. Matches existing `panel_child` /
-- `button_detail` pattern (DEC-A23 lenient runtime: rows may reference
-- retired entities).
--
-- Idempotent: IF NOT EXISTS on table + indexes; re-applying is a no-op.
-- No data seeds.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_ref_edge (
  src_kind         text        NOT NULL,
  src_id           bigint      NOT NULL,
  src_version_id   bigint      NOT NULL,
  dst_kind         text        NOT NULL,
  dst_id           bigint      NOT NULL,
  dst_version_id   bigint      NOT NULL,
  edge_role        text        NOT NULL,
  created_at       timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE catalog_ref_edge IS
  'Materialized cross-entity ref graph (DEC-A37 + DEC-A42, asset-pipeline Stage 14.1). Populated on publish; consumed by References tab + ripple + dangling-ref lint.';
COMMENT ON COLUMN catalog_ref_edge.src_kind IS
  'Source entity kind (sprite|asset|button|panel|pool|token|archetype|audio). Soft FK — no SQL constraint.';
COMMENT ON COLUMN catalog_ref_edge.src_id IS
  'Source entity id. Soft FK to catalog_entity.id — no SQL constraint (DEC-A23 lenient runtime).';
COMMENT ON COLUMN catalog_ref_edge.src_version_id IS
  'Source entity_version.id at edge-build time.';
COMMENT ON COLUMN catalog_ref_edge.dst_kind IS
  'Target entity kind. Soft FK — no SQL constraint.';
COMMENT ON COLUMN catalog_ref_edge.dst_id IS
  'Target entity id. Soft FK to catalog_entity.id.';
COMMENT ON COLUMN catalog_ref_edge.dst_version_id IS
  'Target entity_version.id resolved via catalog_entity.current_published_version_id at edge-build time.';
COMMENT ON COLUMN catalog_ref_edge.edge_role IS
  'Edge role discriminator. One of 8 enumerated values: panel.token, button.sprite, asset.sprite, pool.asset, archetype.asset, archetype.sprite, archetype.token, archetype.audio.';

-- Forward-walk index: "outbound edges of source version".
CREATE INDEX IF NOT EXISTS catalog_ref_edge_src_idx
  ON catalog_ref_edge (src_kind, src_id, src_version_id);

-- Reverse-walk index: "inbound edges of target version" (References tab + ripple).
CREATE INDEX IF NOT EXISTS catalog_ref_edge_dst_idx
  ON catalog_ref_edge (dst_kind, dst_id, dst_version_id);

COMMIT;

-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP INDEX IF EXISTS catalog_ref_edge_dst_idx;
--   DROP INDEX IF EXISTS catalog_ref_edge_src_idx;
--   DROP TABLE IF EXISTS catalog_ref_edge;
--   COMMIT;
