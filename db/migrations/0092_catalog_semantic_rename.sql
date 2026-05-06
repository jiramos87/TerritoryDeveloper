-- 0092_catalog_semantic_rename.sql
-- TECH-17997 (game-ui-catalog-bake Stage 9.11)
-- Rename catalog_entity.slug rows from underscore/numeric form to {purpose}-{kind} convention.
-- Author-curated explicit map — validator lint (TECH-17996) surfaced these rows.
-- Each rename appends a new entity_version row preserving history.
-- entity_id FKs on panel_child / button_detail / sprite_detail unchanged (FKs ref id, not slug).
--
-- Also migrates the slug format check constraint from the old DEC-A24 regex
-- (^[a-z][a-z0-9_]{2,63}$) to the new {purpose}-{kind} hyphen convention regex.

BEGIN;

-- ── Drop old slug format check (allows underscores) ─────────────────────────
ALTER TABLE catalog_entity DROP CONSTRAINT IF EXISTS ck_catalog_entity_slug_format;

-- ── Renames ────────────────────────────────────────────────────────────────

-- id=1  demo_panel (panel) → demo-panel
UPDATE catalog_entity SET slug = 'demo-panel' WHERE slug = 'demo_panel';

-- id=2  subtype_picker (panel) → subtype-picker
UPDATE catalog_entity SET slug = 'subtype-picker' WHERE slug = 'subtype_picker';

-- id=3  picker_tile_72 (archetype) → picker-tile  (strip numeric ordinal)
UPDATE catalog_entity SET slug = 'picker-tile' WHERE slug = 'picker_tile_72';

-- id=10 hud_bar_icon_budget (sprite) → hud-bar-budget-icon
UPDATE catalog_entity SET slug = 'hud-bar-budget-icon' WHERE slug = 'hud_bar_icon_budget';

-- id=11 hud_bar_btn_budget (button) → hud-bar-budget-button
UPDATE catalog_entity SET slug = 'hud-bar-budget-button' WHERE slug = 'hud_bar_btn_budget';

-- id=12 growth_budget_panel (panel) → growth-budget-panel
UPDATE catalog_entity SET slug = 'growth-budget-panel' WHERE slug = 'growth_budget_panel';

-- id=13 slider_row_2 (archetype) → slider-row  (strip numeric ordinal)
UPDATE catalog_entity SET slug = 'slider-row' WHERE slug = 'slider_row_2';

-- ── Add new slug format check (hyphens, {purpose}-{kind} shape) ─────────────
ALTER TABLE catalog_entity
  ADD CONSTRAINT ck_catalog_entity_slug_format
  CHECK (slug ~ '^[a-z][a-z0-9]+(-[a-z0-9]+)*$');

-- ── entity_version history rows ─────────────────────────────────────────────
-- Append version rows to record the rename event (migration_hint_json carries provenance).

-- id=1 demo-panel: no prior version rows → first version (1)
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  1, 1, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "demo_panel", "new_slug": "demo-panel"}'
);

-- id=2 subtype-picker: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  2, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "subtype_picker", "new_slug": "subtype-picker"}'
);

-- id=3 picker-tile: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  3, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "picker_tile_72", "new_slug": "picker-tile"}'
);

-- id=10 hud-bar-budget-icon: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  10, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "hud_bar_icon_budget", "new_slug": "hud-bar-budget-icon"}'
);

-- id=11 hud-bar-budget-button: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  11, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "hud_bar_btn_budget", "new_slug": "hud-bar-budget-button"}'
);

-- id=12 growth-budget-panel: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  12, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "growth_budget_panel", "new_slug": "growth-budget-panel"}'
);

-- id=13 slider-row: prior version 1 → new version 2
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
VALUES (
  13, 2, 'published', '{}', '{}',
  '{"migration": "0092_catalog_semantic_rename", "event": "slug_rename", "old_slug": "slider_row_2", "new_slug": "slider-row"}'
);

-- ── Post-rename verification ─────────────────────────────────────────────────

-- Assert: zero rows with parenthesised-numeric pattern remain
DO $$
BEGIN
  IF (SELECT COUNT(*) FROM catalog_entity WHERE slug ~ '\(\d+\)$') > 0 THEN
    RAISE EXCEPTION '0092: trailing (N) pattern still present after rename';
  END IF;
END $$;

-- Assert: zero rows with underscore remain
DO $$
BEGIN
  IF (SELECT COUNT(*) FROM catalog_entity WHERE slug LIKE '%\_%') > 0 THEN
    RAISE EXCEPTION '0092: underscore slug still present after rename';
  END IF;
END $$;

COMMIT;
