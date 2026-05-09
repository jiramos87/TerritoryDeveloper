-- 0122_seed_form_tokens.sql
-- Wave A2 (TECH-27072) — 5 ui_token rows for form + settings UI.
--
-- Tokens:
--   color-bg-selected       — selected card/chip fill color
--   color-border-selected   — selected card/chip border color
--   color-text-dark         — dark text on light surfaces
--   size-text-section-header— section header text size
--   color-text-muted        — muted/secondary text color
--
-- Idempotent: ON CONFLICT DO NOTHING / DO UPDATE throughout.

BEGIN;

-- ─── 1. catalog_entity rows ──────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('token', 'color-bg-selected',        'Color BG Selected'),
  ('token', 'color-border-selected',    'Color Border Selected'),
  ('token', 'color-text-dark',          'Color Text Dark'),
  ('token', 'size-text-section-header', 'Size Text Section Header'),
  ('token', 'color-text-muted',         'Color Text Muted')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. token_detail rows ────────────────────────────────────────────────────

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT ce.id, m.token_kind, m.value_json::jsonb
FROM (VALUES
  ('color-bg-selected',        'color',      '{"hex":"#2A5EBF","opacity":1.0}'),
  ('color-border-selected',    'color',      '{"hex":"#5A8EEF","opacity":1.0}'),
  ('color-text-dark',          'color',      '{"hex":"#0A0A12","opacity":1.0}'),
  ('size-text-section-header', 'type-scale', '{"pt":16,"weight":"bold"}'),
  ('color-text-muted',         'color',      '{"hex":"#8888AA","opacity":1.0}')
) AS m(slug, token_kind, value_json)
JOIN catalog_entity ce ON ce.kind = 'token' AND ce.slug = m.slug
ON CONFLICT (entity_id) DO UPDATE
  SET value_json = EXCLUDED.value_json,
      updated_at = now();

-- ─── 3. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0122_seed_form_tokens","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'token'
  AND ce.slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'token'
  AND ce.slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted')
  AND ce.current_published_version_id IS NULL;

-- ─── 4. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_tokens int;
BEGIN
  SELECT COUNT(*) INTO n_tokens
  FROM catalog_entity ce
  JOIN token_detail td ON td.entity_id = ce.id
  WHERE ce.kind = 'token'
    AND ce.slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted');

  IF n_tokens <> 5 THEN
    RAISE EXCEPTION '0122: expected 5 token rows with token_detail, got %', n_tokens;
  END IF;

  RAISE NOTICE '0122 OK: 5 form/settings tokens seeded';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted'));
--   DELETE FROM token_detail WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted'));
--   DELETE FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-selected','color-border-selected','color-text-dark','size-text-section-header','color-text-muted');
