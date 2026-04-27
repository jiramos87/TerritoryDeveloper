-- Panel detail + panel_child (TECH-1887 / Stage 8.1).
--
-- Per DEC-A7 panel_detail schema + DEC-A27 slot composition:
--   panel_detail   — 1:1 with catalog_entity (kind=panel); archetype binding,
--                    background sprite + token slots, layout template, modal flag.
--   panel_child    — child rows keyed by (panel_entity_id, slot_name, order_idx);
--                    child_kind ∈ DEC-A27 vocabulary; child_entity_id NULL allowed
--                    for spacer/label_inline (DEC-A27 row schema).
--
-- App-level validators (web/lib/catalog/panel-child-validators.ts) enforce:
--   - child_kind ∈ slot.accepts[]      (DEC-A27)
--   - slot child count ∈ [min, max]    (DEC-A27)
--   - no panel cycle (BFS on child panels)
--
-- @see ia/projects/asset-pipeline/stage-8.1 — TECH-1887 §Plan Digest

BEGIN;

-- ─── panel_detail ──────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS panel_detail (
  entity_id                       bigint PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  archetype_entity_id             bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  background_sprite_entity_id     bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  palette_entity_id               bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  frame_style_entity_id           bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  layout_template                 text   NOT NULL DEFAULT 'vstack'
    CHECK (layout_template IN ('vstack', 'hstack', 'grid', 'free')),
  modal                           boolean NOT NULL DEFAULT false,
  updated_at                      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS panel_detail_archetype_idx
  ON panel_detail (archetype_entity_id)
  WHERE archetype_entity_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS panel_detail_layout_idx
  ON panel_detail (layout_template);

DROP TRIGGER IF EXISTS trg_panel_detail_touch ON panel_detail;
CREATE TRIGGER trg_panel_detail_touch
  BEFORE UPDATE ON panel_detail
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

-- ─── panel_child ───────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS panel_child (
  id                  bigserial PRIMARY KEY,
  panel_entity_id     bigint NOT NULL REFERENCES catalog_entity (id) ON DELETE CASCADE,
  panel_version_id    bigint REFERENCES entity_version (id) ON DELETE SET NULL,
  slot_name           text   NOT NULL,
  order_idx           int    NOT NULL CHECK (order_idx >= 0),
  child_kind          text   NOT NULL CHECK (child_kind IN (
    'button', 'panel', 'label', 'spacer', 'audio', 'sprite', 'label_inline'
  )),
  child_entity_id     bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  child_version_id    bigint REFERENCES entity_version (id) ON DELETE SET NULL,
  params_json         jsonb  NOT NULL DEFAULT '{}'::jsonb,
  created_at          timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_panel_child_slot_order UNIQUE (panel_entity_id, slot_name, order_idx)
);

CREATE INDEX IF NOT EXISTS panel_child_panel_idx
  ON panel_child (panel_entity_id);
CREATE INDEX IF NOT EXISTS panel_child_child_idx
  ON panel_child (child_entity_id)
  WHERE child_entity_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS panel_child_kind_idx
  ON panel_child (child_kind);

COMMIT;

-- Rollback (dev only):
--   DROP TABLE IF EXISTS panel_child;
--   DROP TABLE IF EXISTS panel_detail;
