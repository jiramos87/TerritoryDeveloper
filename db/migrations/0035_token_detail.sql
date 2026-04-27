-- token_detail (TECH-2092 / Stage 10.1).
--
-- Per DEC-A44 token authoring + ripple semantics:
--   token_detail — 1:1 with catalog_entity (kind=token); kind-discriminated
--                  per-row value JSON for the 5 token kinds (color / type-scale /
--                  motion / spacing / semantic). Semantic kind aliases another
--                  token via FK; non-semantic rows MUST NOT carry a target.
--
-- Storage:
--   token_kind text + CHECK — matches button_detail.size_variant precedent
--                             (no new ENUM type; ALTER TYPE migrations avoided
--                             per DEC-A44 schema-forward).
--   value_json jsonb        — same shape as enable_predicate_json (button_detail)
--                             + params_json (panel_child); per-kind shape
--                             validated in API write path via Zod schemas at
--                             web/lib/catalog/token-detail-schema.ts.
--
-- Cycle prevention:
--   App-level (web/lib/tokens/semantic-cycle-check.ts) DFS pre-write — DEC-A44
--   "no cycles" rule. SQL recursive CTE on every insert is heavier; cycle
--   check belongs in TECH-2093 / TECH-2092 API layer.
--
-- entity_kind = 'token' enforcement: app-level on insert (matches Stage 8.1
-- button/panel detail enforcement; no SQL trigger).
--
-- @see ia/projects/asset-pipeline/stage-10.1 — TECH-2092 §Plan Digest

BEGIN;

CREATE TABLE IF NOT EXISTS token_detail (
  entity_id                       bigint PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  token_kind                      text   NOT NULL
    CHECK (token_kind IN ('color', 'type-scale', 'motion', 'spacing', 'semantic')),
  value_json                      jsonb  NOT NULL DEFAULT '{}'::jsonb,
  semantic_target_entity_id       bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  updated_at                      timestamptz NOT NULL DEFAULT now(),
  -- Semantic rows MUST set the target; non-semantic rows MUST NOT.
  CONSTRAINT token_detail_semantic_target_xor
    CHECK ((token_kind = 'semantic') = (semantic_target_entity_id IS NOT NULL))
);

CREATE INDEX IF NOT EXISTS token_detail_kind_idx
  ON token_detail (token_kind);

CREATE INDEX IF NOT EXISTS token_detail_semantic_target_idx
  ON token_detail (semantic_target_entity_id)
  WHERE semantic_target_entity_id IS NOT NULL;

DROP TRIGGER IF EXISTS trg_token_detail_touch ON token_detail;
CREATE TRIGGER trg_token_detail_touch
  BEFORE UPDATE ON token_detail
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

COMMIT;

-- Rollback (dev only): DROP TABLE IF EXISTS token_detail;
