-- 0080_seed_subtype_picker_panel_and_archetype.sql
-- Stage 9.7 game-ui-catalog-bake — seed subtype-picker panel + picker-tile-72 archetype.
-- Also seeds 16 sprite_catalog rows for picker icons (R/C/I × {light,medium,heavy} + S 0..6).
-- Requires 0079 (motion.hover enum extension) for tint value.
-- TECH-15890

BEGIN;

-- ─── 1. subtype-picker panel entity (catalog_entity kind=panel) ──────────────
-- Slug uses underscores (ck_catalog_entity_slug_format: ^[a-z][a-z0-9_]{2,63}$).

INSERT INTO catalog_entity (kind, slug, display_name, motion)
VALUES (
  'panel',
  'subtype_picker',
  'Subtype Picker',
  '{"enter":"fade","exit":"fade","hover":"none"}'::jsonb
)
ON CONFLICT (kind, slug) DO UPDATE
  SET display_name = EXCLUDED.display_name,
      motion       = EXCLUDED.motion;

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px)
SELECT
  ce.id,
  'hstack',
  'hstack',
  '{"top":8,"right":10,"bottom":8,"left":10}'::jsonb,
  8
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'subtype_picker'
ON CONFLICT (entity_id) DO UPDATE
  SET layout_template = EXCLUDED.layout_template,
      layout          = EXCLUDED.layout,
      padding_json    = EXCLUDED.padding_json,
      gap_px          = EXCLUDED.gap_px;

-- entity_version for subtype_picker panel.
INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published',
       '{"sizeDelta_x":0,"sizeDelta_y":88,"anchorMin":[0.5,0],"anchorMax":[0.5,0],"pivot":[0.5,0]}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'subtype_picker'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id AND ev.version_number = 1);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'subtype_picker'
  AND ce.current_published_version_id IS NULL;

-- ─── 2. picker-tile-72 archetype entity (catalog_entity kind=archetype) ──────
-- motion.hover = "tint" per TECH-15892 (admitted by migration 0079).

INSERT INTO catalog_entity (kind, slug, display_name, motion)
VALUES (
  'archetype',
  'picker_tile_72',
  'Picker Tile 72',
  '{"enter":"fade","exit":"fade","hover":"tint"}'::jsonb
)
ON CONFLICT (kind, slug) DO UPDATE
  SET display_name = EXCLUDED.display_name,
      motion       = EXCLUDED.motion;

-- entity_version for picker-tile-72 archetype.
-- params_json carries tile geometry consumed by SubtypePickerController.
INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT ce.id, 1, 'published',
       '{"tileWidth":72,"tileHeight":72,"iconOffsetMin":[6,18],"iconOffsetMax":[-6,-6],"captionHeight":12}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'archetype' AND ce.slug = 'picker_tile_72'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id AND ev.version_number = 1);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug = 'picker_tile_72'
  AND ce.current_published_version_id IS NULL;

-- ─── 3. sprite_catalog rows — 16 picker icon sprites ─────────────────────────
-- R/C/I × {light,medium,heavy} = 9 rows + S 0..6 = 7 rows = 16 total.
-- sprite_catalog uses slug = filename stem (hyphens OK — no ck_catalog_entity_slug_format here).
-- Path prefix: Assets/UI/Sprites/Picker/.

INSERT INTO sprite_catalog (slug, path) VALUES
  ('picker-residential-light-icon-72',   'Assets/UI/Sprites/Picker/picker-residential-light-icon-72.png'),
  ('picker-residential-medium-icon-72',  'Assets/UI/Sprites/Picker/picker-residential-medium-icon-72.png'),
  ('picker-residential-heavy-icon-72',   'Assets/UI/Sprites/Picker/picker-residential-heavy-icon-72.png'),
  ('picker-commercial-light-icon-72',    'Assets/UI/Sprites/Picker/picker-commercial-light-icon-72.png'),
  ('picker-commercial-medium-icon-72',   'Assets/UI/Sprites/Picker/picker-commercial-medium-icon-72.png'),
  ('picker-commercial-heavy-icon-72',    'Assets/UI/Sprites/Picker/picker-commercial-heavy-icon-72.png'),
  ('picker-industrial-light-icon-72',    'Assets/UI/Sprites/Picker/picker-industrial-light-icon-72.png'),
  ('picker-industrial-medium-icon-72',   'Assets/UI/Sprites/Picker/picker-industrial-medium-icon-72.png'),
  ('picker-industrial-heavy-icon-72',    'Assets/UI/Sprites/Picker/picker-industrial-heavy-icon-72.png'),
  ('picker-state-0-icon-72',             'Assets/UI/Sprites/Picker/picker-state-0-icon-72.png'),
  ('picker-state-1-icon-72',             'Assets/UI/Sprites/Picker/picker-state-1-icon-72.png'),
  ('picker-state-2-icon-72',             'Assets/UI/Sprites/Picker/picker-state-2-icon-72.png'),
  ('picker-state-3-icon-72',             'Assets/UI/Sprites/Picker/picker-state-3-icon-72.png'),
  ('picker-state-4-icon-72',             'Assets/UI/Sprites/Picker/picker-state-4-icon-72.png'),
  ('picker-state-5-icon-72',             'Assets/UI/Sprites/Picker/picker-state-5-icon-72.png'),
  ('picker-state-6-icon-72',             'Assets/UI/Sprites/Picker/picker-state-6-icon-72.png')
ON CONFLICT (path) DO UPDATE
  SET slug = EXCLUDED.slug;

-- ─── 4. Sanity NOTICE: post-insert counts ────────────────────────────────────
DO $$
DECLARE
  n_panel     int;
  n_archetype int;
  n_sprites   int;
BEGIN
  SELECT COUNT(*) INTO n_panel     FROM catalog_entity WHERE kind = 'panel'     AND slug = 'subtype_picker';
  SELECT COUNT(*) INTO n_archetype FROM catalog_entity WHERE kind = 'archetype' AND slug = 'picker_tile_72';
  SELECT COUNT(*) INTO n_sprites   FROM sprite_catalog  WHERE slug LIKE 'picker-%';
  RAISE NOTICE 'Stage 9.7 seed: panel=% archetype=% picker-sprites=%', n_panel, n_archetype, n_sprites;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM sprite_catalog WHERE slug LIKE 'picker-%';
--   DELETE FROM entity_version USING catalog_entity ce WHERE entity_version.entity_id = ce.id
--     AND ce.kind IN ('panel','archetype') AND ce.slug IN ('subtype_picker','picker_tile_72');
--   DELETE FROM panel_detail USING catalog_entity ce WHERE panel_detail.entity_id = ce.id
--     AND ce.kind = 'panel' AND ce.slug = 'subtype_picker';
--   DELETE FROM catalog_entity WHERE (kind='panel' AND slug='subtype_picker')
--     OR (kind='archetype' AND slug='picker_tile_72');
