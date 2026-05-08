-- 0111_seed_ui_tokens.sql
--
-- TECH-24407 / ui-implementation-mvp-rest Stage 4.0
--
-- Seeds catalog_entity (kind='token') + token_detail rows for
-- hud-bar + toolbar consumer tokens (hybrid-incremental per Q7).
-- Idempotent: ON CONFLICT DO NOTHING.
--
-- Tokens sourced from ia/specs/ui-design-system.md §Tokens (Stage 4 canonical).
--
-- Token slugs (20 rows):
--   Color (6): color-bg-cream, color-bg-cream-pressed, color-border-tan,
--              color-icon-indigo, color-text-dark, color-alert-red
--   Spacing (14): size-icon, size-button-tall, size-button-short, size-strip-h,
--                 size-panel-card, gap-tight, gap-default, gap-loose, pad-button,
--                 layer-world, layer-hud, layer-toast, layer-modal, layer-overlay
--
-- Note: z.* design tokens map to layer-* slugs (slug format constraint requires
--       each segment ≥2 chars; "z" alone fails; layer-{name} preserves semantics).
--
-- token_kind mapping:
--   color.* tokens  → token_kind='color'
--   size.* / gap.* / pad.* / z.* tokens → token_kind='spacing'

BEGIN;

-- ── 1. catalog_entity rows ────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  -- Color tokens
  ('token', 'color-bg-cream',         'Color BG Cream'),
  ('token', 'color-bg-cream-pressed', 'Color BG Cream Pressed'),
  ('token', 'color-border-tan',       'Color Border Tan'),
  ('token', 'color-icon-indigo',      'Color Icon Indigo'),
  ('token', 'color-text-dark',        'Color Text Dark'),
  ('token', 'color-alert-red',        'Color Alert Red'),
  -- Spacing/size tokens
  ('token', 'size-icon',              'Size Icon'),
  ('token', 'size-button-tall',       'Size Button Tall'),
  ('token', 'size-button-short',      'Size Button Short'),
  ('token', 'size-strip-h',           'Size Strip Height'),
  ('token', 'size-panel-card',        'Size Panel Card'),
  ('token', 'gap-tight',              'Gap Tight'),
  ('token', 'gap-default',            'Gap Default'),
  ('token', 'gap-loose',              'Gap Loose'),
  ('token', 'pad-button',             'Pad Button'),
  ('token', 'layer-world',             'Layer World'),
  ('token', 'layer-hud',              'Layer HUD'),
  ('token', 'layer-toast',            'Layer Toast'),
  ('token', 'layer-modal',            'Layer Modal'),
  ('token', 'layer-overlay',          'Layer Overlay')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. token_detail rows ──────────────────────────────────────────────────────

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#f5e6c8"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-bg-cream'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#d9c79c"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-bg-cream-pressed'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#a37b3a"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-border-tan'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#4a3aff"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-icon-indigo'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#1a1a1a"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-text-dark'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'color'::text,   '{"value":"#c53030"}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='color-alert-red'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":64}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='size-icon'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":72}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='size-button-tall'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":48}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='size-button-short'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":80}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='size-strip-h'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":320}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='size-panel-card'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":4}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='gap-tight'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":8}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='gap-default'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":16}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='gap-loose'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":[4,8,4,8]}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='pad-button'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":0}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='layer-world'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":10}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='layer-hud'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":20}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='layer-toast'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":30}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='layer-modal'
ON CONFLICT (entity_id) DO NOTHING;

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT id, 'spacing'::text, '{"value":40}'::jsonb
  FROM catalog_entity WHERE kind='token' AND slug='layer-overlay'
ON CONFLICT (entity_id) DO NOTHING;

-- ── 3. Sanity assertions ──────────────────────────────────────────────────────

DO $$
DECLARE
  n_entities int;
  n_details  int;
BEGIN
  SELECT COUNT(*) INTO n_entities
    FROM catalog_entity
    WHERE kind = 'token'
      AND slug IN (
        'color-bg-cream','color-bg-cream-pressed','color-border-tan',
        'color-icon-indigo','color-text-dark','color-alert-red',
        'size-icon','size-button-tall','size-button-short','size-strip-h','size-panel-card',
        'gap-tight','gap-default','gap-loose','pad-button',
        'z-world','z-hud','z-toast','z-modal','z-overlay'
      );

  SELECT COUNT(*) INTO n_details
    FROM token_detail td
    JOIN catalog_entity ce ON ce.id = td.entity_id
    WHERE ce.kind = 'token'
      AND ce.slug IN (
        'color-bg-cream','color-bg-cream-pressed','color-border-tan',
        'color-icon-indigo','color-text-dark','color-alert-red',
        'size-icon','size-button-tall','size-button-short','size-strip-h','size-panel-card',
        'gap-tight','gap-default','gap-loose','pad-button',
        'layer-world','layer-hud','layer-toast','layer-modal','layer-overlay'
      );

  IF n_details < 20 THEN
    RAISE EXCEPTION '0111: expected ≥20 token_detail rows, got %', n_details;
  END IF;

  RAISE NOTICE '0111 OK: token entities=% detail rows=%', n_entities, n_details;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM catalog_entity WHERE kind='token' AND slug IN (
--     'color-bg-cream','color-bg-cream-pressed','color-border-tan',
--     'color-icon-indigo','color-text-dark','color-alert-red',
--     'size-icon','size-button-tall','size-button-short','size-strip-h','size-panel-card',
--     'gap-tight','gap-default','gap-loose','pad-button',
--     'layer-world','layer-hud','layer-toast','layer-modal','layer-overlay'
--   );
