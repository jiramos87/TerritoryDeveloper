-- 0145_seed_hud_widgets_bundle.sql
-- Stage 9.0 T9.0.1 (TECH-27097)
-- Seed 3 HUD panel catalog rows + panel_detail + panel_child (~14 children total):
--   info-panel          — right-edge dock, header + field-list + demolish confirm-button
--   map-panel           — bottom-right minimap-canvas + header layer toggles
--   notifications-toast — top-right transient toast stack
--
-- Extends panel_detail.layout_template CHECK with: right-edge-dock, bottom-right-dock, top-right-toast
-- Extends panel_child.child_kind CHECK with: field-list, minimap-canvas, toggle-row, toast-stack, toast-card
-- Idempotent via ON CONFLICT DO NOTHING.

BEGIN;

-- ── 1. Extend layout_template CHECK ──────────────────────────────────────────

ALTER TABLE panel_detail
  DROP CONSTRAINT IF EXISTS panel_detail_layout_template_check;

ALTER TABLE panel_detail
  ADD CONSTRAINT panel_detail_layout_template_check
  CHECK (layout_template = ANY (ARRAY[
    'vstack'::text,
    'hstack'::text,
    'grid'::text,
    'free'::text,
    'fullscreen-stack'::text,
    'modal-card'::text,
    'right-edge-dock'::text,
    'bottom-right-dock'::text,
    'top-right-toast'::text
  ]));

-- ── 2. Extend panel_child.child_kind CHECK ────────────────────────────────────

ALTER TABLE panel_child
  DROP CONSTRAINT IF EXISTS panel_child_child_kind_check;

ALTER TABLE panel_child
  ADD CONSTRAINT panel_child_child_kind_check
  CHECK (child_kind = ANY (ARRAY[
    'button'::text,
    'panel'::text,
    'label'::text,
    'spacer'::text,
    'audio'::text,
    'sprite'::text,
    'label_inline'::text,
    'row'::text,
    'text'::text,
    'confirm-button'::text,
    'view-slot'::text,
    'tab-strip'::text,
    'range-tabs'::text,
    'chart'::text,
    'stacked-bar-row'::text,
    'service-row'::text,
    'section-header'::text,
    'slider-row-numeric'::text,
    'expense-row'::text,
    'readout-block'::text,
    -- Wave B5 HUD widget archetypes (TECH-27097)
    'field-list'::text,
    'minimap-canvas'::text,
    'toggle-row'::text,
    'toast-stack'::text,
    'toast-card'::text
  ]));

-- ── 3. info-panel ─────────────────────────────────────────────────────────────

INSERT INTO catalog_entity (slug, kind, display_name, created_at, updated_at)
VALUES ('info-panel', 'panel', 'Info Panel', now(), now())
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, modal)
SELECT ce.id, 'right-edge-dock', false
FROM catalog_entity ce
WHERE ce.slug = 'info-panel' AND ce.kind = 'panel'
ON CONFLICT (entity_id) DO NOTHING;

-- slot_name + order_idx unique per panel; instance_slug nullable annotation only
INSERT INTO panel_child (panel_entity_id, child_kind, order_idx, slot_name, instance_slug, params_json)
SELECT ce.id, child_kind, order_idx, slot_name, instance_slug, jsonb_build_object('kind', child_kind) || params_json::jsonb
FROM catalog_entity ce,
  (VALUES
    ('label',          0, 'header',         'info-panel-header',         '{"text":"Cell Info","bind":"info.header"}'),
    ('field-list',     1, 'field-list',     'info-panel-field-list',     '{"bind":"info.fields","rows":6}'),
    ('label',          2, 'type-label',     'info-panel-type-label',     '{"bind":"info.type","label":"Type"}'),
    ('label',          3, 'zone-label',     'info-panel-zone-label',     '{"bind":"info.zone","label":"Zone"}'),
    ('label',          4, 'pop-label',      'info-panel-pop-label',      '{"bind":"info.population","label":"Population"}'),
    ('label',          5, 'value-label',    'info-panel-value-label',    '{"bind":"info.value","label":"Land Value"}'),
    ('label',          6, 'owner-label',    'info-panel-owner-label',    '{"bind":"info.owner","label":"Owner"}'),
    ('label',          7, 'coord-label',    'info-panel-coord-label',    '{"bind":"info.coord","label":"Grid"}'),
    ('confirm-button', 8, 'demolish-btn',   'info-panel-demolish-button','{"label":"Demolish","action":"info.demolish","confirm_text":"Demolish this cell?"}')
  ) AS t(child_kind, order_idx, slot_name, instance_slug, params_json)
WHERE ce.slug = 'info-panel' AND ce.kind = 'panel'
ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;

-- ── 4. map-panel ──────────────────────────────────────────────────────────────

INSERT INTO catalog_entity (slug, kind, display_name, created_at, updated_at)
VALUES ('map-panel', 'panel', 'Map Panel', now(), now())
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, modal)
SELECT ce.id, 'bottom-right-dock', false
FROM catalog_entity ce
WHERE ce.slug = 'map-panel' AND ce.kind = 'panel'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO panel_child (panel_entity_id, child_kind, order_idx, slot_name, instance_slug, params_json)
SELECT ce.id, child_kind, order_idx, slot_name, instance_slug, jsonb_build_object('kind', child_kind) || params_json::jsonb
FROM catalog_entity ce,
  (VALUES
    ('label',          0, 'header',         'map-panel-header',         '{"text":"Map","bind":"map.header"}'),
    ('minimap-canvas', 1, 'minimap',        'map-panel-minimap',        '{"width":360,"height":324,"bind":"map.canvas"}'),
    ('toggle-row',     2, 'toggle-streets', 'map-panel-toggle-streets', '{"label":"Streets","action":"minimap.layer.set","layer":"Streets"}'),
    ('toggle-row',     3, 'toggle-zones',   'map-panel-toggle-zones',   '{"label":"Zones","action":"minimap.layer.set","layer":"Zones"}'),
    ('toggle-row',     4, 'toggle-forests', 'map-panel-toggle-forests', '{"label":"Forests","action":"minimap.layer.set","layer":"Forests"}')
  ) AS t(child_kind, order_idx, slot_name, instance_slug, params_json)
WHERE ce.slug = 'map-panel' AND ce.kind = 'panel'
ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;

-- ── 5. notifications-toast ────────────────────────────────────────────────────

INSERT INTO catalog_entity (slug, kind, display_name, created_at, updated_at)
VALUES ('notifications-toast', 'panel', 'Notifications Toast', now(), now())
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, modal)
SELECT ce.id, 'top-right-toast', false
FROM catalog_entity ce
WHERE ce.slug = 'notifications-toast' AND ce.kind = 'panel'
ON CONFLICT (entity_id) DO NOTHING;

-- toast-stack container + 5 standard + 3 milestone sticky toast-cards
INSERT INTO panel_child (panel_entity_id, child_kind, order_idx, slot_name, instance_slug, params_json)
SELECT ce.id, child_kind, order_idx, slot_name, instance_slug, jsonb_build_object('kind', child_kind) || params_json::jsonb
FROM catalog_entity ce,
  (VALUES
    ('toast-stack', 0, 'toast-stack',      'notifications-toast-stack',       '{"max_visible":5,"bind":"toast.stack"}'),
    ('toast-card',  1, 'toast-card-0',     'notifications-toast-card-0',      '{"bind":"toast.card.0","sticky":false}'),
    ('toast-card',  2, 'toast-card-1',     'notifications-toast-card-1',      '{"bind":"toast.card.1","sticky":false}'),
    ('toast-card',  3, 'toast-card-2',     'notifications-toast-card-2',      '{"bind":"toast.card.2","sticky":false}'),
    ('toast-card',  4, 'toast-card-3',     'notifications-toast-card-3',      '{"bind":"toast.card.3","sticky":false}'),
    ('toast-card',  5, 'toast-card-4',     'notifications-toast-card-4',      '{"bind":"toast.card.4","sticky":false}'),
    ('toast-card',  6, 'toast-milestone-0','notifications-toast-milestone-0', '{"bind":"toast.milestone.0","sticky":true,"variant":"milestone"}'),
    ('toast-card',  7, 'toast-milestone-1','notifications-toast-milestone-1', '{"bind":"toast.milestone.1","sticky":true,"variant":"milestone"}'),
    ('toast-card',  8, 'toast-milestone-2','notifications-toast-milestone-2', '{"bind":"toast.milestone.2","sticky":true,"variant":"milestone"}')
  ) AS t(child_kind, order_idx, slot_name, instance_slug, params_json)
WHERE ce.slug = 'notifications-toast' AND ce.kind = 'panel'
ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;

-- ── 6. Verify ─────────────────────────────────────────────────────────────────

DO $$
DECLARE
  v_count int;
BEGIN
  SELECT count(*)::int INTO v_count
  FROM catalog_entity
  WHERE slug IN ('info-panel', 'map-panel', 'notifications-toast') AND kind = 'panel';

  IF v_count <> 3 THEN
    RAISE EXCEPTION '0145: expected 3 panel rows, got %', v_count;
  END IF;

  SELECT count(*)::int INTO v_count
  FROM panel_detail pd
  JOIN catalog_entity ce ON ce.id = pd.entity_id
  WHERE ce.slug IN ('info-panel', 'map-panel', 'notifications-toast');

  IF v_count <> 3 THEN
    RAISE EXCEPTION '0145: expected 3 panel_detail rows, got %', v_count;
  END IF;

  SELECT count(*)::int INTO v_count
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.slug IN ('info-panel', 'map-panel', 'notifications-toast');

  IF v_count < 20 THEN
    RAISE EXCEPTION '0145: expected >=20 panel_child rows, got %', v_count;
  END IF;

  RAISE NOTICE '0145 OK: 3 panels + 3 panel_detail + % panel_child rows seeded', v_count;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child pc USING catalog_entity ce WHERE ce.id=pc.panel_entity_id AND ce.slug IN ('info-panel','map-panel','notifications-toast');
--   DELETE FROM panel_detail pd USING catalog_entity ce WHERE ce.id=pd.entity_id AND ce.slug IN ('info-panel','map-panel','notifications-toast');
--   DELETE FROM catalog_entity WHERE slug IN ('info-panel','map-panel','notifications-toast');
