-- 0063_ui_layout_primitives.sql
--
-- TECH-11925 / game-ui-catalog-bake Stage 1.0 §Plan Digest.
--
-- Extends `panel_detail` with three layout-primitive columns used by the
-- Catalog→Snapshot→Bake loop (Stage 1.0 tracer slice):
--
--   layout        — text+CHECK enum: hstack | vstack | grid | free | modal
--                   (mirrors existing layout_template enum, broader vocabulary
--                    aligned with `Hstack`/`Vstack`/`Grid` partial dispatchers).
--   padding_json  — jsonb, CSS-style 4-edge padding {top,right,bottom,left}.
--   gap_px        — int, default 0 (HorizontalLayoutGroup.spacing).
--
-- Adds a BEFORE-INSERT trigger on `panel_child` rejecting rows whose
-- `params_json` is missing the `kind` discriminator (downstream
-- CatalogBakeHandler dispatches on it; missing value silently no-ops).
--
-- Pure additive — does not alter or drop existing `layout_template` /
-- `slot_name` / `child_kind` columns.
--
-- @see ia/projects/game-ui-catalog-bake/stage-1.0 — TECH-11925 §Plan Digest

BEGIN;

-- ─── panel_detail layout primitives ────────────────────────────────────────

ALTER TABLE panel_detail
  ADD COLUMN IF NOT EXISTS layout       text   NOT NULL DEFAULT 'free'
    CHECK (layout IN ('hstack', 'vstack', 'grid', 'free', 'modal'));

ALTER TABLE panel_detail
  ADD COLUMN IF NOT EXISTS padding_json jsonb  NOT NULL DEFAULT '{"top":0,"right":0,"bottom":0,"left":0}'::jsonb;

ALTER TABLE panel_detail
  ADD COLUMN IF NOT EXISTS gap_px       int    NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS panel_detail_layout_primitive_idx
  ON panel_detail (layout);

-- ─── panel_child params_json sub-shape lint ────────────────────────────────

CREATE OR REPLACE FUNCTION panel_child_params_json_lint()
RETURNS trigger AS $$
BEGIN
  IF NEW.params_json IS NULL OR jsonb_typeof(NEW.params_json) <> 'object' THEN
    RAISE EXCEPTION
      'panel_child.params_json must be a JSON object (got %)',
      coalesce(jsonb_typeof(NEW.params_json), 'null')
      USING ERRCODE = 'check_violation';
  END IF;

  IF NOT (NEW.params_json ? 'kind') THEN
    RAISE EXCEPTION
      'panel_child.params_json missing required discriminator key ''kind'' (panel_entity_id=%, slot_name=%, order_idx=%)',
      NEW.panel_entity_id, NEW.slot_name, NEW.order_idx
      USING ERRCODE = 'check_violation';
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_panel_child_params_json_lint ON panel_child;
CREATE TRIGGER trg_panel_child_params_json_lint
  BEFORE INSERT OR UPDATE ON panel_child
  FOR EACH ROW EXECUTE FUNCTION panel_child_params_json_lint();

COMMIT;

-- Rollback (dev only):
--   DROP TRIGGER IF EXISTS trg_panel_child_params_json_lint ON panel_child;
--   DROP FUNCTION IF EXISTS panel_child_params_json_lint();
--   DROP INDEX IF EXISTS panel_detail_layout_primitive_idx;
--   ALTER TABLE panel_detail
--     DROP COLUMN IF EXISTS gap_px,
--     DROP COLUMN IF EXISTS padding_json,
--     DROP COLUMN IF EXISTS layout;
