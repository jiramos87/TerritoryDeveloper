-- 0064_seed_hud_bar_panel.sql
--
-- TECH-11925 / game-ui-catalog-bake Stage 1.0 §Plan Digest.
--
-- Tracer-stage seed: one `hud-bar` panel + 9 child buttons + 9 icon sprites,
-- all bound through the catalog spine (catalog_entity + entity_version +
-- *_detail). Deterministic slugs; published versions; ord 1..9 left-to-right.
--
-- Layout primitive (added by 0063): hstack, gap_px=8, padding 4/4/4/4.
--
-- Pure additive — uses ON CONFLICT DO NOTHING so re-applying is a no-op
-- against an already-seeded database.
--
-- @see ia/projects/game-ui-catalog-bake/stage-1.0 — TECH-11925 §Plan Digest

BEGIN;

-- ─── 1. Sprite entities (9 icon sprites) ───────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
SELECT 'sprite', 'hud_bar_icon_' || gs::text, 'Hud Bar Icon ' || gs::text
FROM generate_series(1, 9) AS gs
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO sprite_detail (entity_id, assets_path, pixels_per_unit, provenance)
SELECT
  ce.id,
  'Assets/UI/Sprites/' || ce.slug || '.png',
  100,
  'hand'
FROM catalog_entity ce
WHERE ce.kind = 'sprite'
  AND ce.slug LIKE 'hud_bar_icon_%'
ON CONFLICT (entity_id) DO NOTHING;

-- Pin a published version per sprite.
INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published', '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'sprite'
  AND ce.slug LIKE 'hud_bar_icon_%'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'sprite'
  AND ce.slug LIKE 'hud_bar_icon_%'
  AND ce.current_published_version_id IS NULL;

-- ─── 2. Button entities (9 hud-bar buttons) ────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
SELECT 'button', 'hud_bar_btn_' || gs::text, 'Hud Bar Button ' || gs::text
FROM generate_series(1, 9) AS gs
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO button_detail (entity_id, sprite_icon_entity_id, size_variant, action_id)
SELECT
  btn.id,
  spr.id,
  'md',
  'hud_bar_action_' || regexp_replace(btn.slug, '^hud_bar_btn_', '')
FROM catalog_entity btn
JOIN catalog_entity spr
  ON spr.kind = 'sprite'
 AND spr.slug = 'hud_bar_icon_' || regexp_replace(btn.slug, '^hud_bar_btn_', '')
WHERE btn.kind = 'button'
  AND btn.slug LIKE 'hud_bar_btn_%'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published', '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'button'
  AND ce.slug LIKE 'hud_bar_btn_%'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug LIKE 'hud_bar_btn_%'
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Hud-bar panel entity ───────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'hud_bar', 'Hud Bar')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px)
SELECT
  ce.id,
  'hstack',
  'hstack',
  '{"top":4,"right":4,"bottom":4,"left":4}'::jsonb,
  8
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'hud_bar'
ON CONFLICT (entity_id) DO UPDATE
  SET layout       = EXCLUDED.layout,
      padding_json = EXCLUDED.padding_json,
      gap_px       = EXCLUDED.gap_px;

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published', '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel'
  AND ce.slug = 'hud_bar'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'hud_bar'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. 9 panel_child rows (ord 1..9, kind=button) ─────────────────────────

INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  hud.id,
  hud.current_published_version_id,
  'main',
  gs.n,
  'button',
  btn.id,
  btn.current_published_version_id,
  jsonb_build_object('kind', 'button', 'ord', gs.n)
FROM (SELECT generate_series(1, 9) AS n) gs
JOIN catalog_entity hud ON hud.kind = 'panel' AND hud.slug = 'hud_bar'
JOIN catalog_entity btn ON btn.kind = 'button' AND btn.slug = 'hud_bar_btn_' || gs.n::text
ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child
--     USING catalog_entity hud
--     WHERE panel_child.panel_entity_id = hud.id
--       AND hud.kind = 'panel' AND hud.slug = 'hud_bar';
--   DELETE FROM panel_detail
--     USING catalog_entity hud
--     WHERE panel_detail.entity_id = hud.id
--       AND hud.kind = 'panel' AND hud.slug = 'hud_bar';
--   DELETE FROM button_detail
--     USING catalog_entity btn
--     WHERE button_detail.entity_id = btn.id
--       AND btn.kind = 'button' AND btn.slug LIKE 'hud_bar_btn_%';
--   DELETE FROM sprite_detail
--     USING catalog_entity spr
--     WHERE sprite_detail.entity_id = spr.id
--       AND spr.kind = 'sprite' AND spr.slug LIKE 'hud_bar_icon_%';
--   DELETE FROM entity_version
--     USING catalog_entity ce
--     WHERE entity_version.entity_id = ce.id
--       AND ce.kind IN ('panel', 'button', 'sprite')
--       AND (ce.slug = 'hud_bar' OR ce.slug LIKE 'hud_bar_btn_%' OR ce.slug LIKE 'hud_bar_icon_%');
--   DELETE FROM catalog_entity
--     WHERE (kind = 'panel'  AND slug = 'hud_bar')
--        OR (kind = 'button' AND slug LIKE 'hud_bar_btn_%')
--        OR (kind = 'sprite' AND slug LIKE 'hud_bar_icon_%');
