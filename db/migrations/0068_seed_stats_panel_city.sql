-- 0068_seed_stats_panel_city.sql
--
-- FEAT-56 / game-ui-catalog-bake Stage 4 §Plan Digest.
--
-- Seeds the city-scope Stats panel as the first vstack-layout consumer baked
-- from catalog. Inserts:
--   - `stats_panel_city` panel entity + panel_detail (layout='vstack',
--     gap_px=4, padding_json={top,right,bottom,left=8}, params_json={scroll:true})
--   - 4 tab-button entities + button_detail rows
--   - 16 stat-row entities (Money=4, People=4, Land=3, Infrastructure=5)
--   - 4 panel_child rows (ord 1..4) for tab buttons (kind='button')
--   - 16 panel_child rows (ord 5..20) for stat rows (kind='row')
--   - published entity_version + current_published_version_id for every
--     catalog_entity row written here.
--
-- Adds the `params_json jsonb` column to panel_detail (ALTER TABLE IF NOT
-- EXISTS) used by BakeVstack to gate ScrollRect wrapping. Pure-additive,
-- mirrors the 0063 ALTER TABLE pattern that introduced layout/padding_json/gap_px.
--
-- Slugs use snake_case per ck_catalog_entity_slug_format (^[a-z][a-z0-9_]{2,63}$).
-- Single-shot INSERT (no ON CONFLICT); idempotency via fresh-DB-only migration
-- convention matching 0065_seed_first_modal.sql precedent.
--
-- @see ia/projects/game-ui-catalog-bake/stage-4 — FEAT-56 §Plan Digest

BEGIN;

-- ─── 0. Schema extensions (additive) ─────────────────────────────────────────
--
-- (a) panel_detail.params_json (jsonb, default '{}') — gates BakeVstack ScrollRect
--     wrap; mirrors 0063 ALTER TABLE pattern that introduced layout/padding_json.
-- (b) panel_child_child_kind_check — extend enum with 'row' discriminator for
--     vstack stat-row children (label/icon/value/vu/delta tuple per ui §3.6 D4).
--     DROP/ADD pattern (idempotent: existing rows all carry pre-existing kinds).

ALTER TABLE panel_detail
  ADD COLUMN IF NOT EXISTS params_json jsonb NOT NULL DEFAULT '{}'::jsonb;

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
         'row'::text
       ]));

-- ─── 1. Tab-button entities (4) ──────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name) VALUES
  ('button', 'stats_tab_money',          'Stats Tab — Money'),
  ('button', 'stats_tab_people',         'Stats Tab — People'),
  ('button', 'stats_tab_land',           'Stats Tab — Land'),
  ('button', 'stats_tab_infrastructure', 'Stats Tab — Infrastructure');

INSERT INTO button_detail (entity_id, size_variant, action_id)
SELECT id, 'sm', 'stats_tab_select_' || REPLACE(slug, 'stats_tab_', '')
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'stats_tab_%';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'stats_tab_%';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug LIKE 'stats_tab_%';

-- ─── 2. Stat-row entities (16) ───────────────────────────────────────────────
-- Money=4, People=4, Land=3, Infrastructure=5; deterministic snake_case slugs
-- per stats_row_{tab}_{n} pattern.

INSERT INTO catalog_entity (kind, slug, display_name) VALUES
  ('button', 'stats_row_money_1',          'Stats Row — Money 1'),
  ('button', 'stats_row_money_2',          'Stats Row — Money 2'),
  ('button', 'stats_row_money_3',          'Stats Row — Money 3'),
  ('button', 'stats_row_money_4',          'Stats Row — Money 4'),
  ('button', 'stats_row_people_1',         'Stats Row — People 1'),
  ('button', 'stats_row_people_2',         'Stats Row — People 2'),
  ('button', 'stats_row_people_3',         'Stats Row — People 3'),
  ('button', 'stats_row_people_4',         'Stats Row — People 4'),
  ('button', 'stats_row_land_1',           'Stats Row — Land 1'),
  ('button', 'stats_row_land_2',           'Stats Row — Land 2'),
  ('button', 'stats_row_land_3',           'Stats Row — Land 3'),
  ('button', 'stats_row_infrastructure_1', 'Stats Row — Infrastructure 1'),
  ('button', 'stats_row_infrastructure_2', 'Stats Row — Infrastructure 2'),
  ('button', 'stats_row_infrastructure_3', 'Stats Row — Infrastructure 3'),
  ('button', 'stats_row_infrastructure_4', 'Stats Row — Infrastructure 4'),
  ('button', 'stats_row_infrastructure_5', 'Stats Row — Infrastructure 5');

-- Note: stat-row entities reuse 'button' kind because catalog_entity.kind
-- enum is constrained; the panel_child.kind='row' discriminator drives bake
-- behavior, while catalog_entity.kind is admin-scope.
-- (kept additive: no kind enum change in this migration.)

INSERT INTO button_detail (entity_id, size_variant, action_id)
SELECT id, 'sm', ''
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'stats_row_%';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'button' AND slug LIKE 'stats_row_%';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug LIKE 'stats_row_%';

-- ─── 3. Stats panel entity ───────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'stats_panel_city', 'Stats Panel — City');

INSERT INTO panel_detail (entity_id, layout, padding_json, gap_px, params_json)
SELECT id,
       'vstack',
       '{"top":8,"right":8,"bottom":8,"left":8}'::jsonb,
       4,
       '{"scroll":true}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'stats_panel_city';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'stats_panel_city';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'stats_panel_city';

-- ─── 4. Panel children — 4 tab buttons (ord 1..4) ────────────────────────────

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
  ord_idx,
  'button',
  tab.id,
  tab.current_published_version_id,
  jsonb_build_object('kind', 'button', 'ord', ord_idx)
FROM catalog_entity panel
JOIN (VALUES
  (1, 'stats_tab_money'),
  (2, 'stats_tab_people'),
  (3, 'stats_tab_land'),
  (4, 'stats_tab_infrastructure')
) AS tabs(ord_idx, slug) ON true
JOIN catalog_entity tab
  ON tab.kind = 'button' AND tab.slug = tabs.slug
WHERE panel.kind = 'panel' AND panel.slug = 'stats_panel_city';

-- ─── 5. Panel children — 16 stat rows (ord 5..20) ────────────────────────────

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
  rows.ord_idx,
  'row',
  child.id,
  child.current_published_version_id,
  rows.params_json
FROM catalog_entity panel
JOIN (VALUES
  ( 5, 'stats_row_money_1',          '{"kind":"row","label":"Treasury","value":"0","vu":"$","delta":"+0"}'::jsonb),
  ( 6, 'stats_row_money_2',          '{"kind":"row","label":"Income","value":"0","vu":"$/mo","delta":"+0"}'::jsonb),
  ( 7, 'stats_row_money_3',          '{"kind":"row","label":"Expenses","value":"0","vu":"$/mo","delta":"-0"}'::jsonb),
  ( 8, 'stats_row_money_4',          '{"kind":"row","label":"Bond debt","value":"0","vu":"$","delta":"+0"}'::jsonb),
  ( 9, 'stats_row_people_1',         '{"kind":"row","label":"Population","value":"0","vu":"","delta":"+0"}'::jsonb),
  (10, 'stats_row_people_2',         '{"kind":"row","label":"Workers","value":"0","vu":"","delta":"+0"}'::jsonb),
  (11, 'stats_row_people_3',         '{"kind":"row","label":"Happiness","value":"0","vu":"%","delta":"+0"}'::jsonb),
  (12, 'stats_row_people_4',         '{"kind":"row","label":"Migration","value":"0","vu":"/mo","delta":"+0"}'::jsonb),
  (13, 'stats_row_land_1',           '{"kind":"row","label":"Total cells","value":"0","vu":"","delta":"+0"}'::jsonb),
  (14, 'stats_row_land_2',           '{"kind":"row","label":"Zoned","value":"0","vu":"","delta":"+0"}'::jsonb),
  (15, 'stats_row_land_3',           '{"kind":"row","label":"Reserved","value":"0","vu":"","delta":"+0"}'::jsonb),
  (16, 'stats_row_infrastructure_1', '{"kind":"row","label":"Roads","value":"0","vu":"km","delta":"+0"}'::jsonb),
  (17, 'stats_row_infrastructure_2', '{"kind":"row","label":"Power","value":"0","vu":"MW","delta":"+0"}'::jsonb),
  (18, 'stats_row_infrastructure_3', '{"kind":"row","label":"Water","value":"0","vu":"m3","delta":"+0"}'::jsonb),
  (19, 'stats_row_infrastructure_4', '{"kind":"row","label":"Pollution","value":"0","vu":"%","delta":"+0"}'::jsonb),
  (20, 'stats_row_infrastructure_5', '{"kind":"row","label":"Coverage","value":"0","vu":"%","delta":"+0"}'::jsonb)
) AS rows(ord_idx, slug, params_json) ON true
JOIN catalog_entity child
  ON child.kind = 'button' AND child.slug = rows.slug
WHERE panel.kind = 'panel' AND panel.slug = 'stats_panel_city';

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE panel_detail DROP COLUMN IF EXISTS params_json;
--   DELETE FROM panel_child
--     USING catalog_entity panel
--     WHERE panel_child.panel_entity_id = panel.id
--       AND panel.kind = 'panel' AND panel.slug = 'stats_panel_city';
--   DELETE FROM panel_detail
--     USING catalog_entity panel
--     WHERE panel_detail.entity_id = panel.id
--       AND panel.kind = 'panel' AND panel.slug = 'stats_panel_city';
--   DELETE FROM button_detail
--     USING catalog_entity ce
--     WHERE button_detail.entity_id = ce.id
--       AND ce.kind = 'button' AND (ce.slug LIKE 'stats_tab_%' OR ce.slug LIKE 'stats_row_%');
--   DELETE FROM entity_version
--     USING catalog_entity ce
--     WHERE entity_version.entity_id = ce.id
--       AND ((ce.kind = 'panel'  AND ce.slug = 'stats_panel_city')
--         OR (ce.kind = 'button' AND (ce.slug LIKE 'stats_tab_%' OR ce.slug LIKE 'stats_row_%')));
--   DELETE FROM catalog_entity
--     WHERE (kind = 'panel'  AND slug = 'stats_panel_city')
--        OR (kind = 'button' AND (slug LIKE 'stats_tab_%' OR slug LIKE 'stats_row_%'));
