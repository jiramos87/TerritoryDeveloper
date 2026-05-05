-- 0071_seed_grid_and_free_panels.sql
--
-- FEAT-57 / game-ui-catalog-bake Stage 5 §Plan Digest.
--
-- Seeds the first grid-layout + free-layout consumers baked from catalog:
--   - `subtype_picker_rcis` panel (grid, 4 cols × 3 rows, 12 button children)
--   - `resource_info_popup`  panel (free, 1 sprite + 2 text children, 240×120)
--   - `tooltip`              panel (free, 1 text child, 200×40)
--
-- Schema extensions (additive):
--   (a) panel_child_child_kind_check — DROP/ADD enum extended with 'text' for
--       free-panel text children (popup labels + tooltip body). Pattern mirrors
--       0068 ('row' add).
--
-- params_json keys (NEW for Stage 5):
--   panel_detail.params_json grid:
--     {grid_cols, cell_w_px, cell_h_px, spacing_x_px, spacing_y_px}
--   panel_detail.params_json free:
--     {width_px, height_px}
--   panel_child.params_json free:
--     {kind, ord, x_px, y_px, w_px, h_px, text?}
--
-- Slugs use snake_case per ck_catalog_entity_slug_format (^[a-z][a-z0-9_]{2,63}$).
-- Single-shot INSERT (no ON CONFLICT); idempotency via fresh-DB-only migration
-- convention matching 0068 precedent.
--
-- §Open Questions:
--   Q1 — sprite catalog gap. FEAT-57 plan-digest assumed `building_subtype_*`
--   sprite slugs exist; current catalog only carries hud_bar_icon_1..9. Picker
--   icons (12 children) seed with empty sprite_ref via panel_child.sprite_ref
--   resolved at bake time → ResolveSprite(null) → null Image.sprite. Popup
--   sprite child references `hud_bar_icon_1` (entity_id 39) as placeholder
--   pending dedicated icon catalog entries. Tracker: TECH-11939 §Open
--   Questions captures the closeout to swap once `building_subtype_*` sprites
--   land in catalog (post-Stage 5).
--
-- @see ia/projects/game-ui-catalog-bake/stage-5 — FEAT-57 §Plan Digest

BEGIN;

-- ─── 0. Schema extensions (additive) ─────────────────────────────────────────

ALTER TABLE panel_child
  DROP CONSTRAINT IF EXISTS panel_child_child_kind_check;

ALTER TABLE panel_child
  ADD  CONSTRAINT panel_child_child_kind_check
       CHECK (child_kind = ANY (ARRAY[
         'button'::text,
         'panel'::text,
         'label'::text,
         'spacer'::text,
         'audio'::text,
         'sprite'::text,
         'label_inline'::text,
         'row'::text,
         'text'::text
       ]));

-- ─── 1. Subtype picker icon entities (12) ────────────────────────────────────
-- Reuse 'button' kind (admin-scope) — bake reads panel_child.kind='button'.
-- Slugs: subtype_picker_{rcis}_{n} where rcis ∈ {r,c,i,s} (Residential/
-- Commercial/Industrial/Special), n ∈ {1,2,3}.

INSERT INTO catalog_entity (kind, slug, display_name) VALUES
  ('button', 'subtype_picker_r_1', 'Subtype Picker — Residential 1'),
  ('button', 'subtype_picker_r_2', 'Subtype Picker — Residential 2'),
  ('button', 'subtype_picker_r_3', 'Subtype Picker — Residential 3'),
  ('button', 'subtype_picker_c_1', 'Subtype Picker — Commercial 1'),
  ('button', 'subtype_picker_c_2', 'Subtype Picker — Commercial 2'),
  ('button', 'subtype_picker_c_3', 'Subtype Picker — Commercial 3'),
  ('button', 'subtype_picker_i_1', 'Subtype Picker — Industrial 1'),
  ('button', 'subtype_picker_i_2', 'Subtype Picker — Industrial 2'),
  ('button', 'subtype_picker_i_3', 'Subtype Picker — Industrial 3'),
  ('button', 'subtype_picker_s_1', 'Subtype Picker — Special 1'),
  ('button', 'subtype_picker_s_2', 'Subtype Picker — Special 2'),
  ('button', 'subtype_picker_s_3', 'Subtype Picker — Special 3');

INSERT INTO button_detail (entity_id, size_variant, action_id)
SELECT id, 'sm', 'subtype_pick_' || REPLACE(slug, 'subtype_picker_', '')
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'subtype_picker_%';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'subtype_picker_%';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug LIKE 'subtype_picker_%';

-- ─── 2. Free-panel text-child placeholder entities (3) ───────────────────────
-- Tooltip (1 text) + Resource info popup (2 text) = 3 placeholder entities.
-- Reuse 'button' kind (admin-scope) per 0068 precedent — panel_child.kind='text'
-- drives bake behavior (BakeFree emits UnityEngine.UI.Text per spec).

INSERT INTO catalog_entity (kind, slug, display_name) VALUES
  ('button', 'free_text_resource_info_label', 'Free Text — Resource Info Label'),
  ('button', 'free_text_resource_info_value', 'Free Text — Resource Info Value'),
  ('button', 'free_text_tooltip_body',        'Free Text — Tooltip Body');

INSERT INTO button_detail (entity_id, size_variant, action_id)
SELECT id, 'sm', ''
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'free_text_%';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'free_text_%';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug LIKE 'free_text_%';

-- ─── 3. subtype_picker_rcis panel (grid, 12 buttons) ─────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'subtype_picker_rcis', 'Subtype Picker — RCIS Grid');

INSERT INTO panel_detail (entity_id, layout, padding_json, gap_px, params_json)
SELECT id,
       'grid',
       '{"top":4,"right":4,"bottom":4,"left":4}'::jsonb,
       4,
       '{"grid_cols":4,"cell_w_px":72,"cell_h_px":72,"spacing_x_px":4,"spacing_y_px":4}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'subtype_picker_rcis';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'subtype_picker_rcis';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'subtype_picker_rcis';

INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  panel.id,
  panel.current_published_version_id,
  'main',
  picks.ord_idx,
  'button',
  child.id,
  child.current_published_version_id,
  jsonb_build_object('kind', 'button', 'ord', picks.ord_idx)
FROM catalog_entity panel
JOIN (VALUES
  ( 1, 'subtype_picker_r_1'),
  ( 2, 'subtype_picker_r_2'),
  ( 3, 'subtype_picker_r_3'),
  ( 4, 'subtype_picker_c_1'),
  ( 5, 'subtype_picker_c_2'),
  ( 6, 'subtype_picker_c_3'),
  ( 7, 'subtype_picker_i_1'),
  ( 8, 'subtype_picker_i_2'),
  ( 9, 'subtype_picker_i_3'),
  (10, 'subtype_picker_s_1'),
  (11, 'subtype_picker_s_2'),
  (12, 'subtype_picker_s_3')
) AS picks(ord_idx, slug) ON true
JOIN catalog_entity child
  ON child.kind = 'button' AND child.slug = picks.slug
WHERE panel.kind = 'panel' AND panel.slug = 'subtype_picker_rcis';

-- ─── 4. resource_info_popup panel (free, 1 sprite + 2 text) ──────────────────
-- Layout: 240×120 frame. Icon at (8,8,32,32). Label at (48,8,184,24).
-- Value at (48,40,184,72).

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'resource_info_popup', 'Resource Info Popup');

INSERT INTO panel_detail (entity_id, layout, padding_json, gap_px, params_json)
SELECT id,
       'free',
       '{"top":0,"right":0,"bottom":0,"left":0}'::jsonb,
       0,
       '{"width_px":240,"height_px":120}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'resource_info_popup';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'resource_info_popup';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'resource_info_popup';

-- Sprite child references hud_bar_icon_1 placeholder (Q1 above).
INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  panel.id,
  panel.current_published_version_id,
  'main',
  1,
  'sprite',
  sprite.id,
  sprite.current_published_version_id,
  '{"kind":"sprite","ord":1,"x_px":8,"y_px":8,"w_px":32,"h_px":32}'::jsonb
FROM catalog_entity panel
JOIN catalog_entity sprite
  ON sprite.kind = 'sprite' AND sprite.slug = 'hud_bar_icon_1'
WHERE panel.kind = 'panel' AND panel.slug = 'resource_info_popup';

INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  panel.id,
  panel.current_published_version_id,
  'main',
  texts.ord_idx,
  'text',
  child.id,
  child.current_published_version_id,
  texts.params_json
FROM catalog_entity panel
JOIN (VALUES
  (2, 'free_text_resource_info_label', '{"kind":"text","ord":2,"x_px":48,"y_px":8,"w_px":184,"h_px":24,"text":"Resource"}'::jsonb),
  (3, 'free_text_resource_info_value', '{"kind":"text","ord":3,"x_px":48,"y_px":40,"w_px":184,"h_px":72,"text":"0/0"}'::jsonb)
) AS texts(ord_idx, slug, params_json) ON true
JOIN catalog_entity child
  ON child.kind = 'button' AND child.slug = texts.slug
WHERE panel.kind = 'panel' AND panel.slug = 'resource_info_popup';

-- ─── 5. tooltip panel (free, 1 text) ─────────────────────────────────────────
-- Layout: 200×40 frame. Body at (8,8,184,24).

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'tooltip', 'Tooltip');

INSERT INTO panel_detail (entity_id, layout, padding_json, gap_px, params_json)
SELECT id,
       'free',
       '{"top":0,"right":0,"bottom":0,"left":0}'::jsonb,
       0,
       '{"width_px":200,"height_px":40}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'tooltip';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'tooltip';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'tooltip';

INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  panel.id,
  panel.current_published_version_id,
  'main',
  1,
  'text',
  child.id,
  child.current_published_version_id,
  '{"kind":"text","ord":1,"x_px":8,"y_px":8,"w_px":184,"h_px":24,"text":"Tooltip"}'::jsonb
FROM catalog_entity panel
JOIN catalog_entity child
  ON child.kind = 'button' AND child.slug = 'free_text_tooltip_body'
WHERE panel.kind = 'panel' AND panel.slug = 'tooltip';

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child
--     USING catalog_entity panel
--     WHERE panel_child.panel_entity_id = panel.id
--       AND panel.kind = 'panel'
--       AND panel.slug IN ('subtype_picker_rcis', 'resource_info_popup', 'tooltip');
--   DELETE FROM panel_detail
--     USING catalog_entity panel
--     WHERE panel_detail.entity_id = panel.id
--       AND panel.kind = 'panel'
--       AND panel.slug IN ('subtype_picker_rcis', 'resource_info_popup', 'tooltip');
--   DELETE FROM button_detail
--     USING catalog_entity ce
--     WHERE button_detail.entity_id = ce.id
--       AND ce.kind = 'button'
--       AND (ce.slug LIKE 'subtype_picker_%' OR ce.slug LIKE 'free_text_%');
--   DELETE FROM entity_version
--     USING catalog_entity ce
--     WHERE entity_version.entity_id = ce.id
--       AND ((ce.kind = 'panel'  AND ce.slug IN ('subtype_picker_rcis', 'resource_info_popup', 'tooltip'))
--         OR (ce.kind = 'button' AND (ce.slug LIKE 'subtype_picker_%' OR ce.slug LIKE 'free_text_%')));
--   DELETE FROM catalog_entity
--     WHERE (kind = 'panel'  AND slug IN ('subtype_picker_rcis', 'resource_info_popup', 'tooltip'))
--        OR (kind = 'button' AND (slug LIKE 'subtype_picker_%' OR slug LIKE 'free_text_%'));
