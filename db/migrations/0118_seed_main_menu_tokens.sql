-- 0118_seed_main_menu_tokens.sql
-- Wave A1 (TECH-27067) — seed 2 UI design tokens for main-menu panel.
--
-- Tokens:
--   color-bg-menu            — fullscreen panel background color (deep navy-black #0A0A12)
--   size-text-title-display  — title display text size (48pt)
--
-- Slug format: hyphens only (catalog_entity slug constraint forbids dots).
-- Stored as catalog_entity(kind=token) + token_detail rows.
-- Idempotent: ON CONFLICT DO NOTHING / DO UPDATE throughout.

BEGIN;

-- ─── 1. catalog_entity rows ──────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('token', 'color-bg-menu',           'Color BG Menu'),
  ('token', 'size-text-title-display', 'Size Text Title Display')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. token_detail rows ────────────────────────────────────────────────────

INSERT INTO token_detail (entity_id, token_kind, value_json)
SELECT ce.id, m.token_kind, m.value_json::jsonb
FROM (VALUES
  ('color-bg-menu',           'color',      '{"hex":"#0A0A12","opacity":1.0}'),
  ('size-text-title-display', 'type-scale', '{"pt":48,"weight":"bold"}')
) AS m(slug, token_kind, value_json)
JOIN catalog_entity ce ON ce.kind = 'token' AND ce.slug = m.slug
ON CONFLICT (entity_id) DO UPDATE
  SET value_json = EXCLUDED.value_json,
      updated_at = now();

-- ─── 3. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0118_seed_main_menu_tokens","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'token'
  AND ce.slug IN ('color-bg-menu', 'size-text-title-display')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'token'
  AND ce.slug IN ('color-bg-menu', 'size-text-title-display')
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
    AND ce.slug IN ('color-bg-menu', 'size-text-title-display');

  IF n_tokens <> 2 THEN
    RAISE EXCEPTION '0118: expected 2 token rows with token_detail, got %', n_tokens;
  END IF;

  RAISE NOTICE '0118 OK: color-bg-menu + size-text-title-display tokens seeded';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-menu','size-text-title-display'));
--   DELETE FROM token_detail WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-menu','size-text-title-display'));
--   DELETE FROM catalog_entity WHERE kind='token' AND slug IN ('color-bg-menu','size-text-title-display');
