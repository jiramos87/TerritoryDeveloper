-- 0087_seed_growth_budget_panel.sql
--
-- TECH-16992 / game-ui-catalog-bake Stage 9.9 §Plan Digest.
--
-- Seeds:
--   1. sprite `hud_bar_icon_budget` (32×32 dial/sliders glyph)
--   2. button `hud_bar_btn_budget` (icon=hud_bar_icon_budget, size=md, action=hud_bar_action_budget)
--   3. panel  `growth_budget_panel` (vstack, 16/16/16/16 padding, gap 12, 360×220 right-anchored)
--   4. archetype `slider_row_2` (row_height 40, label_w 110, value_w 48)
--   5. 3 panel_child rows under growth_budget_panel (ord 1=total, 2=zoning, 3=roads)
--   6. 1 panel_child row under hud_bar — BUDGET button at AUTO's prior order_idx;
--      shift AUTO + downstream by +1 so total grows 9→10 (when hud_bar is seeded)
--
-- Schema deviation from spec text (TECH-16992):
--   - spec calls out `anchor_json` on panel_detail — actual schema has `params_json`;
--     anchor data lives in `entity_version.params_json`.
--   - spec calls out `archetype_detail` table — does not exist; archetype params live
--     in `entity_version.params_json`; motion lives in `catalog_entity.motion`.
--
-- Provenance: 'hand' (PNG drop pending; sprite_detail seeded so AssetPostprocessor
-- updates the row idempotently on import).
--
-- @see ia/projects/TECH-16992 §Plan Digest

BEGIN;

-- ─── 1. Sprite — hud_bar_icon_budget ────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('sprite', 'hud_bar_icon_budget', 'Hud Bar Icon Budget')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO sprite_detail (entity_id, assets_path, pixels_per_unit, provenance)
SELECT ce.id, 'Assets/UI/Sprites/hud_bar_icon_budget.png', 100, 'hand'
FROM catalog_entity ce
WHERE ce.kind = 'sprite' AND ce.slug = 'hud_bar_icon_budget'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published', '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'sprite' AND ce.slug = 'hud_bar_icon_budget'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'sprite'
  AND ce.slug = 'hud_bar_icon_budget'
  AND ce.current_published_version_id IS NULL;

-- ─── 2. Button — hud_bar_btn_budget ─────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('button', 'hud_bar_btn_budget', 'Hud Bar Button Budget')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO button_detail (entity_id, sprite_icon_entity_id, size_variant, action_id)
SELECT btn.id, spr.id, 'md', 'hud_bar_action_budget'
FROM catalog_entity btn
JOIN catalog_entity spr
  ON spr.kind = 'sprite' AND spr.slug = 'hud_bar_icon_budget'
WHERE btn.kind = 'button' AND btn.slug = 'hud_bar_btn_budget'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published', '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'button' AND ce.slug = 'hud_bar_btn_budget'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug = 'hud_bar_btn_budget'
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Panel — growth_budget_panel ─────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name, motion)
VALUES (
  'panel',
  'growth_budget_panel',
  'Growth Budget Panel',
  '{"enter":"fade","exit":"fade","hover":"none"}'::jsonb
)
ON CONFLICT (kind, slug) DO UPDATE
  SET display_name = EXCLUDED.display_name,
      motion       = EXCLUDED.motion;

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px)
SELECT
  ce.id,
  'vstack',
  'vstack',
  '{"top":16,"right":16,"bottom":16,"left":16}'::jsonb,
  12
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'growth_budget_panel'
ON CONFLICT (entity_id) DO UPDATE
  SET layout_template = EXCLUDED.layout_template,
      layout          = EXCLUDED.layout,
      padding_json    = EXCLUDED.padding_json,
      gap_px          = EXCLUDED.gap_px;

-- entity_version carries anchor + sizeDelta (panel_detail has no anchor_json column).
INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT
  ce.id, 1, 'published',
  '{"sizeDelta_x":360,"sizeDelta_y":220,"anchorMin":[1,1],"anchorMax":[1,1],"pivot":[1,1]}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'growth_budget_panel'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id AND ev.version_number = 1);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'growth_budget_panel'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. Archetype — slider_row_2 ────────────────────────────────────────────
-- Archetype params live on entity_version.params_json (no archetype_detail table).
-- motion lives on catalog_entity.motion.

INSERT INTO catalog_entity (kind, slug, display_name, motion)
VALUES (
  'archetype',
  'slider_row_2',
  'Slider Row (label + slider + value)',
  '{"enter":"fade","exit":"fade","hover":"tint"}'::jsonb
)
ON CONFLICT (kind, slug) DO UPDATE
  SET display_name = EXCLUDED.display_name,
      motion       = EXCLUDED.motion;

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT
  ce.id, 1, 'published',
  '{"row_height":40,"label_width":110,"value_width":48}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'archetype' AND ce.slug = 'slider_row_2'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id AND ev.version_number = 1);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug = 'slider_row_2'
  AND ce.current_published_version_id IS NULL;

-- ─── 5. panel_child rows under growth_budget_panel (3 sliders) ──────────────
-- ord 1 = total budget %, 2 = zoning weight %, 3 = roads weight %.
-- child_kind='row' (panel_child constraint admits 'row'); child_entity_id → archetype.

INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  pnl.id,
  pnl.current_published_version_id,
  'main',
  rows.ord,
  'row',
  arch.id,
  arch.current_published_version_id,
  jsonb_build_object('kind', 'row', 'archetype', 'slider_row_2', 'role', rows.role, 'ord', rows.ord)
FROM (
  VALUES (1, 'total'), (2, 'zoning'), (3, 'roads')
) AS rows(ord, role)
JOIN catalog_entity pnl ON pnl.kind = 'panel'     AND pnl.slug = 'growth_budget_panel'
JOIN catalog_entity arch ON arch.kind = 'archetype' AND arch.slug = 'slider_row_2'
ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;

-- ─── 6. panel_child row under hud_bar — insert BUDGET left of AUTO ──────────
-- Safe in three states:
--   (a) hud_bar empty (dev DB drift) → insert BUDGET at ord 1.
--   (b) hud_bar seeded with AUTO at known ord N → shift ≥ N by +1, insert BUDGET at N.
--   (c) hud_bar seeded but no AUTO row → insert BUDGET at max(order_idx)+1 (right edge).

DO $$
DECLARE
  v_hud_id     bigint;
  v_hud_ver    bigint;
  v_btn_id     bigint;
  v_btn_ver    bigint;
  v_auto_ord   int;
  v_max_ord    int;
  v_target_ord int;
BEGIN
  SELECT id, current_published_version_id INTO v_hud_id, v_hud_ver
    FROM catalog_entity
    WHERE kind = 'panel' AND slug = 'hud_bar';

  IF v_hud_id IS NULL THEN
    RAISE NOTICE 'Stage 9.9 seed: hud_bar panel missing — skipping panel_child insertion (dev-DB drift; production-fresh DB will have it from migration 0064)';
    RETURN;
  END IF;

  SELECT id, current_published_version_id INTO v_btn_id, v_btn_ver
    FROM catalog_entity
    WHERE kind = 'button' AND slug = 'hud_bar_btn_budget';

  -- Locate AUTO order_idx if present.
  SELECT pc.order_idx INTO v_auto_ord
    FROM panel_child pc
    JOIN catalog_entity btn ON btn.id = pc.child_entity_id
    WHERE pc.panel_entity_id = v_hud_id
      AND btn.slug = 'hud_bar_btn_auto';

  -- Idempotency guard: BUDGET already a child of hud_bar? exit.
  IF EXISTS (
    SELECT 1 FROM panel_child pc
    WHERE pc.panel_entity_id = v_hud_id
      AND pc.child_entity_id = v_btn_id
  ) THEN
    RAISE NOTICE 'Stage 9.9 seed: BUDGET already linked to hud_bar — idempotent skip';
    RETURN;
  END IF;

  IF v_auto_ord IS NOT NULL THEN
    -- Shift AUTO + downstream by +1 in reverse order to preserve unique idx during update.
    UPDATE panel_child
       SET order_idx = order_idx + 1
     WHERE panel_entity_id = v_hud_id
       AND order_idx >= v_auto_ord;
    v_target_ord := v_auto_ord;
  ELSE
    SELECT COALESCE(MAX(order_idx), 0) + 1 INTO v_target_ord
      FROM panel_child WHERE panel_entity_id = v_hud_id;
  END IF;

  INSERT INTO panel_child (
    panel_entity_id, panel_version_id,
    slot_name, order_idx, child_kind,
    child_entity_id, child_version_id,
    params_json
  ) VALUES (
    v_hud_id, v_hud_ver,
    'main', v_target_ord, 'button',
    v_btn_id, v_btn_ver,
    jsonb_build_object('kind', 'button', 'ord', v_target_ord, 'slug', 'hud_bar_btn_budget')
  );

  SELECT MAX(order_idx) INTO v_max_ord FROM panel_child WHERE panel_entity_id = v_hud_id;
  RAISE NOTICE 'Stage 9.9 seed: BUDGET inserted at hud_bar ord=% (max ord now=%)', v_target_ord, v_max_ord;
END;
$$;

-- ─── 7. Sanity NOTICE ───────────────────────────────────────────────────────
DO $$
DECLARE
  n_sprite     int;
  n_button     int;
  n_panel      int;
  n_archetype  int;
  n_gbp_kids   int;
  n_hud_kids   int;
BEGIN
  SELECT COUNT(*) INTO n_sprite    FROM catalog_entity WHERE kind='sprite'    AND slug='hud_bar_icon_budget';
  SELECT COUNT(*) INTO n_button    FROM catalog_entity WHERE kind='button'    AND slug='hud_bar_btn_budget';
  SELECT COUNT(*) INTO n_panel     FROM catalog_entity WHERE kind='panel'     AND slug='growth_budget_panel';
  SELECT COUNT(*) INTO n_archetype FROM catalog_entity WHERE kind='archetype' AND slug='slider_row_2';
  SELECT COUNT(*) INTO n_gbp_kids  FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
   WHERE ce.kind='panel' AND ce.slug='growth_budget_panel';
  SELECT COUNT(*) INTO n_hud_kids  FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
   WHERE ce.kind='panel' AND ce.slug='hud_bar';
  RAISE NOTICE 'Stage 9.9 seed: sprite=% btn=% panel=% archetype=% growth_budget_panel-kids=% hud_bar-kids=%',
    n_sprite, n_button, n_panel, n_archetype, n_gbp_kids, n_hud_kids;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child USING catalog_entity ce
--    WHERE panel_child.panel_entity_id = ce.id
--      AND ce.kind='panel' AND ce.slug IN ('growth_budget_panel','hud_bar')
--      AND (panel_child.child_entity_id IN
--           (SELECT id FROM catalog_entity WHERE slug='hud_bar_btn_budget')
--           OR ce.slug='growth_budget_panel');
--   -- Restore AUTO + downstream ord (manual — track previous shift offset).
--   DELETE FROM panel_detail USING catalog_entity ce
--    WHERE panel_detail.entity_id = ce.id AND ce.slug='growth_budget_panel';
--   DELETE FROM button_detail USING catalog_entity ce
--    WHERE button_detail.entity_id = ce.id AND ce.slug='hud_bar_btn_budget';
--   DELETE FROM sprite_detail USING catalog_entity ce
--    WHERE sprite_detail.entity_id = ce.id AND ce.slug='hud_bar_icon_budget';
--   DELETE FROM entity_version USING catalog_entity ce
--    WHERE entity_version.entity_id = ce.id
--      AND ce.slug IN ('hud_bar_icon_budget','hud_bar_btn_budget','growth_budget_panel','slider_row_2');
--   DELETE FROM catalog_entity
--    WHERE slug IN ('hud_bar_icon_budget','hud_bar_btn_budget','growth_budget_panel','slider_row_2');
