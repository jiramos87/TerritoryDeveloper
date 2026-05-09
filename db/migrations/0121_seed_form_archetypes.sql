-- 0121_seed_form_archetypes.sql
-- Wave A2 (TECH-27069) — 7 new archetype catalog rows for form + settings controls.
--
-- Archetypes:
--   card-picker    — 3-N card grid with selected-bind
--   chip-picker    — compact horizontal chips
--   text-input     — label + placeholder + value-bind + optional reroll-action
--   toggle-row     — label + toggle + value-bind
--   slider-row     — label + slider + value-bind + min/max/step + optional linearToDecibel
--   dropdown-row   — label + dropdown + options + value-bind
--   section-header — text + size token
--
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. archetype catalog_entity rows ────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('archetype', 'card-picker',    'Card Picker'),
  ('archetype', 'chip-picker',    'Chip Picker'),
  ('archetype', 'text-input',     'Text Input'),
  ('archetype', 'toggle-row',     'Toggle Row'),
  ('archetype', 'slider-row',     'Slider Row'),
  ('archetype', 'dropdown-row',   'Dropdown Row'),
  ('archetype', 'section-header', 'Section Header')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published',
  CASE ce.slug
    WHEN 'card-picker' THEN
      '{"bind":"<required>","value":"<required>","label":"<required>","description":""}'::jsonb
    WHEN 'chip-picker' THEN
      '{"bind":"<required>","value":"<required>","label":"<required>"}'::jsonb
    WHEN 'text-input' THEN
      '{"bind":"<required>","placeholder":"","reroll_action":null}'::jsonb
    WHEN 'toggle-row' THEN
      '{"bind":"<required>","label":"<required>"}'::jsonb
    WHEN 'slider-row' THEN
      '{"bind":"<required>","label":"<required>","min":0,"max":1,"step":0.01,"linearToDecibel":false}'::jsonb
    WHEN 'dropdown-row' THEN
      '{"bind":"<required>","label":"<required>","options_action":"<required>"}'::jsonb
    WHEN 'section-header' THEN
      '{"label":"<required>","size_token":"size-text-section-header"}'::jsonb
  END,
  '{}'::jsonb,
  '{"migration":"0121_seed_form_archetypes","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'archetype'
  AND ce.slug IN ('card-picker','chip-picker','text-input','toggle-row','slider-row','dropdown-row','section-header')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug IN ('card-picker','chip-picker','text-input','toggle-row','slider-row','dropdown-row','section-header')
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_archetypes int;
BEGIN
  SELECT COUNT(*) INTO n_archetypes
  FROM catalog_entity ce
  JOIN entity_version ev ON ev.entity_id = ce.id AND ev.status = 'published'
  WHERE ce.kind = 'archetype'
    AND ce.slug IN ('card-picker','chip-picker','text-input','toggle-row','slider-row','dropdown-row','section-header');

  IF n_archetypes <> 7 THEN
    RAISE EXCEPTION '0121: expected 7 published archetype rows, got %', n_archetypes;
  END IF;

  RAISE NOTICE '0121 OK: 7 form archetypes seeded (card-picker, chip-picker, text-input, toggle-row, slider-row, dropdown-row, section-header)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='archetype' AND slug IN ('card-picker','chip-picker','text-input','toggle-row','slider-row','dropdown-row','section-header'));
--   DELETE FROM catalog_entity WHERE kind='archetype' AND slug IN ('card-picker','chip-picker','text-input','toggle-row','slider-row','dropdown-row','section-header');
