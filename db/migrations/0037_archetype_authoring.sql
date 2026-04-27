-- archetype authoring (Stage 11.1 — TECH-2459 + TECH-2461).
--
-- Per DEC-A46 archetype authoring + version pinning + migration hint:
--   * `entity_version.migration_hint_json` (jsonb, nullable) — declares per-field
--     upgrade rules from the previous version's params shape to this row's
--     `params_json`. NULL = no hint required (first version, or pure-additive
--     change with no removed fields). Validator computes "required" from diff;
--     column nullability does not gate publish.
--   * Index on `(kind, retired_at)` for archetype list query — `kind` enum
--     already enumerates `archetype` per migration 0021. `kind_tag` sub-kind
--     classifier lives in `entity_version.params_json.kind_tag` (no schema
--     change required; convention only).
--
-- Hint shape (TECH-2461):
--   {
--     "rename":  [{ "from": "old_slug", "to": "new_slug" }],
--     "default": [{ "slug": "new_slug", "value": <literal> }],
--     "drop":    [{ "slug": "old_slug" }]
--   }
--
-- @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 + TECH-2461 §Plan Digests

BEGIN;

ALTER TABLE entity_version
  ADD COLUMN IF NOT EXISTS migration_hint_json jsonb;

COMMENT ON COLUMN entity_version.migration_hint_json IS
  'Per-field upgrade rules from previous version params shape to this row params_json. NULL = no hint required. Shape: {rename:[{from,to}], default:[{slug,value}], drop:[{slug}]}.';

CREATE INDEX IF NOT EXISTS catalog_entity_kind_retired_idx
  ON catalog_entity (kind, retired_at);

COMMENT ON COLUMN catalog_entity.kind IS
  'Top-level kind tag. For archetype rows, sub-kind lives in entity_version.params_json.kind_tag (sprite|asset|button|panel|audio|pool|token).';

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE entity_version DROP COLUMN IF EXISTS migration_hint_json;
--   DROP INDEX IF EXISTS catalog_entity_kind_retired_idx;
