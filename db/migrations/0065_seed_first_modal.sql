-- 0065_seed_first_modal.sql
--
-- FEAT-55 / game-ui-catalog-bake Stage 3 §Plan Digest.
--
-- Seeds the Settings dialog as the first modal-layout consumer baked from
-- catalog. Inserts:
--   - `settings_modal_close_btn` button entity + button_detail (self-contained)
--   - `settings_modal` panel entity + panel_detail (layout='modal')
--   - 3 panel_child rows: title bar (label), body content (label), close button (button)
--
-- Note: slugs use snake_case (underscore) per catalog_entity.ck_catalog_entity_slug_format
-- constraint (`^[a-z][a-z0-9_]{2,63}$`). §Plan Digest used kebab-case in prose
-- but the DB schema forbids dashes — precedent: hud_bar (not hud-bar) in 0064.
--
-- Single-shot INSERT (no ON CONFLICT); idempotency via fresh-DB-only migration
-- convention matching sibling seeds (0064_seed_hud_bar_panel.sql).
--
-- @see ia/projects/game-ui-catalog-bake/stage-3 — FEAT-55 §Plan Digest

BEGIN;

-- ─── 1. Close-button entity + detail ──────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('button', 'settings_modal_close_btn', 'Settings Modal Close Button');

INSERT INTO button_detail (entity_id, size_variant, action_id)
SELECT id, 'sm', 'settings_modal_close'
FROM catalog_entity
WHERE kind = 'button' AND slug = 'settings_modal_close_btn';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'button' AND slug = 'settings_modal_close_btn';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'button'
  AND ce.slug = 'settings_modal_close_btn';

-- ─── 2. Settings modal panel entity ───────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'settings_modal', 'Settings Modal');

INSERT INTO panel_detail (entity_id, layout, padding_json, gap_px)
SELECT id, 'modal', '{"top":24,"right":24,"bottom":24,"left":24}'::jsonb, 8
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'settings_modal';

INSERT INTO entity_version (entity_id, version_number, status, params_json)
SELECT id, 1, 'published', '{}'::jsonb
FROM catalog_entity
WHERE kind = 'panel' AND slug = 'settings_modal';

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'settings_modal';

-- ─── 3. Three panel_child rows (ord 1=title label, 2=body label, 3=close button) ─

-- Child 1: title bar (label)
INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  modal.id,
  modal.current_published_version_id,
  'main',
  1,
  'label',
  NULL,
  NULL,
  '{"kind":"label","text":"Settings"}'::jsonb
FROM catalog_entity modal
WHERE modal.kind = 'panel' AND modal.slug = 'settings_modal';

-- Child 2: body content (label)
INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  modal.id,
  modal.current_published_version_id,
  'main',
  2,
  'label',
  NULL,
  NULL,
  '{"kind":"label","text":"Configure your game settings here."}'::jsonb
FROM catalog_entity modal
WHERE modal.kind = 'panel' AND modal.slug = 'settings_modal';

-- Child 3: close button (button, references settings_modal_close_btn)
INSERT INTO panel_child (
  panel_entity_id, panel_version_id,
  slot_name, order_idx, child_kind,
  child_entity_id, child_version_id,
  params_json
)
SELECT
  modal.id,
  modal.current_published_version_id,
  'main',
  3,
  'button',
  btn.id,
  btn.current_published_version_id,
  '{"kind":"button","ord":3}'::jsonb
FROM catalog_entity modal
JOIN catalog_entity btn
  ON btn.kind = 'button' AND btn.slug = 'settings_modal_close_btn'
WHERE modal.kind = 'panel' AND modal.slug = 'settings_modal';

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child
--     USING catalog_entity modal
--     WHERE panel_child.panel_entity_id = modal.id
--       AND modal.kind = 'panel' AND modal.slug = 'settings_modal';
--   DELETE FROM panel_detail
--     USING catalog_entity modal
--     WHERE panel_detail.entity_id = modal.id
--       AND modal.kind = 'panel' AND modal.slug = 'settings_modal';
--   DELETE FROM entity_version
--     USING catalog_entity ce
--     WHERE entity_version.entity_id = ce.id
--       AND ((ce.kind = 'panel' AND ce.slug = 'settings_modal')
--         OR (ce.kind = 'button' AND ce.slug = 'settings_modal_close_btn'));
--   DELETE FROM button_detail
--     USING catalog_entity btn
--     WHERE button_detail.entity_id = btn.id
--       AND btn.kind = 'button' AND btn.slug = 'settings_modal_close_btn';
--   DELETE FROM catalog_entity
--     WHERE (kind = 'panel'  AND slug = 'settings_modal')
--        OR (kind = 'button' AND slug = 'settings_modal_close_btn');
