-- 0125_seed_tool_subtype_picker.sql
-- Wave B1 (TECH-27078) — seed tool-subtype-picker panel catalog rows.
--
-- Conforms to actual panel_detail + panel_child schema (0116_seed_main_menu pattern).
-- 3 child schema rows: arrow-left, arrow-right, card-template (runtime expanded to N cards).
-- host_slots + anchor params stored in params_json.
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ── 1. catalog_entity ─────────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name, tags)
VALUES (
    'panel',
    'tool-subtype-picker',
    'Tool Subtype Picker',
    ARRAY['cityscene', 'toolbar', 'wave-b1']
)
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. panel_detail ───────────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px, params_json)
SELECT
    ce.id,
    'hstack',
    'hstack',
    '{"top":8,"left":8,"right":8,"bottom":8}'::jsonb,
    8,
    '{
        "panel_kind": "hud",
        "anchor": "bottom-left",
        "strip_h_px": 96,
        "card_w_px": 80,
        "card_h_px": 80,
        "max_strip_w_px": 1200,
        "hidden_default": true,
        "open_trigger": "toolbar.tool-select",
        "close_triggers": ["key.escape","toolbar.tool-deselect"]
    }'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'tool-subtype-picker'
ON CONFLICT (entity_id) DO UPDATE
    SET params_json = EXCLUDED.params_json,
        updated_at  = now();

-- ── 3. entity_version + publish ───────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
    ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
    '{"migration":"0125_seed_tool_subtype_picker","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'tool-subtype-picker'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'tool-subtype-picker'
  AND ce.current_published_version_id IS NULL;

-- ── 4. panel_child schema rows ────────────────────────────────────────────────
-- 3 children: scroll-left arrow, scroll-right arrow, card-template (expanded at runtime).

DO $$
DECLARE
    v_panel_id bigint;
    v_ver_id   bigint;
BEGIN
    SELECT ce.id INTO v_panel_id FROM catalog_entity ce
    WHERE ce.kind = 'panel' AND ce.slug = 'tool-subtype-picker';
    IF v_panel_id IS NULL THEN RAISE EXCEPTION '0125: tool-subtype-picker entity missing'; END IF;

    SELECT ev.id INTO v_ver_id FROM entity_version ev
    WHERE ev.entity_id = v_panel_id AND ev.version_number = 1;
    IF v_ver_id IS NULL THEN RAISE EXCEPTION '0125: tool-subtype-picker entity_version missing'; END IF;

    DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

    INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
    VALUES
        (v_panel_id, v_ver_id, 'arrow-left',    1, 'button', 'tool-subtype-picker-arrow-left',
         '{"icon":"scroll-left","kind":"illuminated-button","role":"strip-arrow-left","hidden_unless_overflow":true}'::jsonb,
         '{"zone":"left-edge"}'::jsonb),
        (v_panel_id, v_ver_id, 'arrow-right',   2, 'button', 'tool-subtype-picker-arrow-right',
         '{"icon":"scroll-right","kind":"illuminated-button","role":"strip-arrow-right","hidden_unless_overflow":true}'::jsonb,
         '{"zone":"right-edge"}'::jsonb),
        (v_panel_id, v_ver_id, 'card-template', 10, 'panel', 'tool-subtype-picker-card-template',
         '{"kind":"subtype-card","family":"*","subtype":"*","icon":"*","name":"*","cost_text":"*","affordable_bind":"toolSelection.affordable.*"}'::jsonb,
         '{"zone":"cards"}'::jsonb);

    RAISE NOTICE '0125 OK: tool-subtype-picker panel seeded with 3 schema children (panel_id=%)', v_panel_id;
END;
$$;

-- ── 5. sanity assert ──────────────────────────────────────────────────────────

DO $$
DECLARE cnt int;
BEGIN
    SELECT COUNT(*) INTO cnt
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.kind = 'panel' AND ce.slug = 'tool-subtype-picker';

    IF cnt < 3 THEN
        RAISE EXCEPTION '0125: tool-subtype-picker panel_child count expected >= 3, got %', cnt;
    END IF;

    RAISE NOTICE '0125 OK: tool-subtype-picker assertions passed (children=%)', cnt;
END;
$$;

COMMIT;
