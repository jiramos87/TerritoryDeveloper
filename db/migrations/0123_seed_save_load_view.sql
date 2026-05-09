-- 0123_seed_save_load_view.sql
-- Wave A3 (TECH-27073) — save-load-view panel seed.
--
-- Conforms to actual panel_detail + panel_child schema (0116_seed_main_menu pattern).
-- host_slots + mode bind stored in params_json.
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. catalog_entity ───────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'save-load-view', 'Save / Load View')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. panel_detail ─────────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px, params_json)
SELECT
  ce.id,
  'vstack',
  'vstack',
  '{"top":8,"left":8,"right":8,"bottom":8}'::jsonb,
  8,
  '{"panel_kind":"screen","host_slots":["main-menu-content-slot","pause-menu-content-slot"],"modeBindId":"saveload.mode","defaultMode":"load"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view'
ON CONFLICT (entity_id) DO UPDATE
  SET params_json = EXCLUDED.params_json,
      updated_at  = now();

-- ─── 3. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0123_seed_save_load_view","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'save-load-view'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. panel_child rows ─────────────────────────────────────────────────────
-- 4 children: save-controls-strip, save-list, save-name-input, footer-load-button

DO $$
DECLARE
  v_panel_id bigint;
  v_ver_id   bigint;
BEGIN
  SELECT ce.id INTO v_panel_id FROM catalog_entity ce WHERE ce.kind='panel' AND ce.slug='save-load-view';
  IF v_panel_id IS NULL THEN RAISE EXCEPTION '0123: save-load-view entity missing'; END IF;
  SELECT ev.id INTO v_ver_id FROM entity_version ev WHERE ev.entity_id=v_panel_id AND ev.version_number=1;
  IF v_ver_id IS NULL THEN RAISE EXCEPTION '0123: save-load-view entity_version missing'; END IF;

  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES
    (v_panel_id, v_ver_id, 'save-controls-strip', 1, 'panel', 'save-controls-strip',
     '{"kind":"save-controls-strip","bindId":"saveload.mode"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'save-list',           2, 'panel', 'save-list',
     '{"kind":"save-list","listBindId":"saveload.list","selectedSlotBindId":"saveload.selectedSlot","trashAction":"saveload.delete","selectAction":"saveload.selectSlot"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'save-name-input',     3, 'panel', 'save-name-input',
     '{"kind":"text-input","bind":"saveload.saveName","placeholder":"City-YYYY-MM-DD-HHmm"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'footer-load-button',  4, 'button', 'footer-load-button',
     '{"kind":"themed-button","label":"Load","action":"saveload.load","disabledBindId":"saveload.loadDisabled"}'::jsonb, '{}'::jsonb);

  RAISE NOTICE '0123 OK: save-load-view panel seeded with 4 children (panel_id=%)', v_panel_id;
END;
$$;

-- ─── 5. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_children int;
BEGIN
  SELECT COUNT(*) INTO n_children
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view';

  IF n_children <> 4 THEN
    RAISE EXCEPTION '0123: expected 4 panel_child rows for save-load-view, got %', n_children;
  END IF;

  RAISE NOTICE '0123 OK: save-load-view panel seeded (4 children)';
END;
$$;

COMMIT;
