-- 0112_seed_ui_components.sql
--
-- TECH-24408 / ui-implementation-mvp-rest Stage 4.0
--
-- 1. Creates component_detail table (1:1 with catalog_entity kind='component').
-- 2. Seeds catalog_entity (kind='component') + component_detail rows for
--    hud-bar + toolbar consumer components (hybrid-incremental per Q7).
-- Idempotent: ON CONFLICT DO NOTHING.
--
-- Components sourced from ia/specs/ui-design-system.md §Components (Stage 4 canonical):
--   HudStrip, IconButton, Label, Readout, Toggle, Modal
--
-- component_detail schema:
--   entity_id      bigint PK → catalog_entity
--   role           text   NOT NULL  — visual/interaction role description
--   default_props_json jsonb NOT NULL — prop name→default shape
--   variants_json  jsonb NOT NULL  — array of variant names

BEGIN;

-- ── 0. Extend catalog_entity.kind CHECK to include 'component' ────────────────
-- Existing constraint: sprite|asset|button|panel|pool|token|archetype|audio
-- Adding: component (UI atoms + molecules)

ALTER TABLE catalog_entity DROP CONSTRAINT IF EXISTS catalog_entity_kind_check;
ALTER TABLE catalog_entity ADD CONSTRAINT catalog_entity_kind_check
  CHECK (kind = ANY (ARRAY[
    'sprite','asset','button','panel','pool','token','archetype','audio','component'
  ]));

-- ── 1. component_detail table ─────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS component_detail (
  entity_id          bigint PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  role               text   NOT NULL DEFAULT '',
  default_props_json jsonb  NOT NULL DEFAULT '{}'::jsonb,
  variants_json      jsonb  NOT NULL DEFAULT '[]'::jsonb,
  updated_at         timestamptz NOT NULL DEFAULT now()
);

DROP TRIGGER IF EXISTS trg_component_detail_touch ON component_detail;
CREATE TRIGGER trg_component_detail_touch
  BEFORE UPDATE ON component_detail
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

-- ── 2. catalog_entity rows ────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('component', 'hud-strip',    'HudStrip'),
  ('component', 'icon-button',  'IconButton'),
  ('component', 'ui-label',     'Label'),
  ('component', 'ui-readout',   'Readout'),
  ('component', 'ui-toggle',    'Toggle'),
  ('component', 'ui-modal',     'Modal')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 3. component_detail rows ──────────────────────────────────────────────────

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'Anchored full-width strip with named zones (left/center/right)',
  '{"side":{"type":"enum","values":["top","bottom","left","right"],"default":"bottom"},"h":{"type":"size","default":"size.strip.h"},"bg":{"type":"color","default":"color.bg.cream"},"zones":{"type":"array","default":["left","center","right"]}}'::jsonb,
  '["idle","dimmed"]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='hud-strip'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'Icon-only button with optional label',
  '{"slug":{"type":"string","required":true},"icon":{"type":"string","required":true},"size":{"type":"enum","values":["icon","tall","short"],"default":"icon"},"variant":{"type":"string","default":"amber"},"hotkey":{"type":"string","default":null},"action":{"type":"string","required":true},"tooltip":{"type":"string","default":null}}'::jsonb,
  '["default","hover","pressed","disabled","active"]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='icon-button'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'Static or data-bound text label',
  '{"slug":{"type":"string","required":true},"bind":{"type":"string","default":null},"font":{"type":"enum","values":["display","body","mono"],"default":"body"},"align":{"type":"enum","values":["start","center","end"],"default":"center"}}'::jsonb,
  '[]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='ui-label'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'Live data text with format + cadence',
  '{"slug":{"type":"string","required":true},"bind":{"type":"string","required":true},"format":{"type":"enum","values":["text","currency","percent","integer"],"default":"text"},"cadence":{"type":"enum","values":["frame","tick","event"],"default":"tick"}}'::jsonb,
  '[]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='ui-readout'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'On/off toggle bound to setting',
  '{"slug":{"type":"string","required":true},"bind":{"type":"string","required":true}}'::jsonb,
  '["default","hover","on","disabled"]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='ui-toggle'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO component_detail (entity_id, role, default_props_json, variants_json)
SELECT id,
  'Overlay panel with focus-trap + Esc-close',
  '{"slug":{"type":"string","required":true},"trapFocus":{"type":"bool","default":true},"closeOnEsc":{"type":"bool","default":true}}'::jsonb,
  '["closed","opening","open","closing"]'::jsonb
FROM catalog_entity WHERE kind='component' AND slug='ui-modal'
ON CONFLICT (entity_id) DO NOTHING;

-- ── 4. Sanity assertions ──────────────────────────────────────────────────────

DO $$
DECLARE
  n_entities int;
  n_details  int;
BEGIN
  SELECT COUNT(*) INTO n_entities
    FROM catalog_entity
    WHERE kind = 'component'
      AND slug IN ('hud-strip','icon-button','ui-label','ui-readout','ui-toggle','ui-modal');

  SELECT COUNT(*) INTO n_details
    FROM component_detail cd
    JOIN catalog_entity ce ON ce.id = cd.entity_id
    WHERE ce.kind = 'component'
      AND ce.slug IN ('hud-strip','icon-button','ui-label','ui-readout','ui-toggle','ui-modal');

  IF n_entities < 6 THEN
    RAISE EXCEPTION '0112: expected ≥6 component catalog_entity rows, got %', n_entities;
  END IF;

  IF n_details < 6 THEN
    RAISE EXCEPTION '0112: expected ≥6 component_detail rows, got %', n_details;
  END IF;

  RAISE NOTICE '0112 OK: component entities=% detail rows=%', n_entities, n_details;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM catalog_entity WHERE kind='component'
--     AND slug IN ('hud-strip','icon-button','ui-label','ui-readout','ui-toggle','ui-modal');
--   DROP TABLE IF EXISTS component_detail;
