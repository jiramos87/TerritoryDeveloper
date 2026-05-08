-- 0107_seed_hud_bar_sprites_v2.sql
--
-- hud-bar bake-test v2 — sprite catalog backfill.
--
-- Inserts 9 sprite catalog_entity rows + sprite_detail rows that map bare
-- icon slugs (used by locked-def `params_json.icon`) onto existing disk
-- assets under `Assets/Sprites/Buttons/`. The bake handler resolves icon
-- slugs via pure disk lookup (`{slug}-target.png` then variant scan
-- `{slug}-button-*-target.png`), so the assets_path field's primary role
-- is to drive the snapshot-export `sprite_ref` column, not the bake itself.
--
-- 9 sprite slugs:
--   new-game     → Assets/Sprites/Buttons/new-game-button-target.png
--   save-game    → Assets/Sprites/Buttons/save-game-button-target.png
--   load-game    → Assets/Sprites/Buttons/load-game-button-target.png
--   zoom-in      → Assets/Sprites/Buttons/zoom-in-button-1-64-target.png
--   zoom-out     → Assets/Sprites/Buttons/zoom-out-button-1-64-target.png
--   pause        → Assets/Sprites/Buttons/pause-button-1-64-target.png
--   stats        → Assets/Sprites/Buttons/stats-button-64-target.png
--   empty        → Assets/Sprites/Buttons/empty-button-64-target.png   (placeholder)
--   long         → Assets/Sprites/Buttons/long-button-256-64-target.png (placeholder)
--
-- Pure additive — ON CONFLICT DO NOTHING on every insert; safe to re-run.

BEGIN;

-- ── 1. catalog_entity rows (9 sprites) ──────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('sprite', 'new-game',  'New Game Button Sprite'),
  ('sprite', 'save-game', 'Save Game Button Sprite'),
  ('sprite', 'load-game', 'Load Game Button Sprite'),
  ('sprite', 'zoom-in',   'Zoom In Button Sprite'),
  ('sprite', 'zoom-out',  'Zoom Out Button Sprite'),
  ('sprite', 'pause',     'Pause Button Sprite'),
  ('sprite', 'stats',     'Stats Button Sprite'),
  ('sprite', 'empty',     'Empty Square Button Placeholder'),
  ('sprite', 'long',      'Long Rectangular Button Placeholder')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. sprite_detail rows ───────────────────────────────────────────────────

INSERT INTO sprite_detail (entity_id, assets_path, pixels_per_unit, provenance)
SELECT ce.id, m.assets_path, 100, 'hand'
FROM (VALUES
  ('new-game',  'Assets/Sprites/Buttons/new-game-button-target.png'),
  ('save-game', 'Assets/Sprites/Buttons/save-game-button-target.png'),
  ('load-game', 'Assets/Sprites/Buttons/load-game-button-target.png'),
  ('zoom-in',   'Assets/Sprites/Buttons/zoom-in-button-1-64-target.png'),
  ('zoom-out',  'Assets/Sprites/Buttons/zoom-out-button-1-64-target.png'),
  ('pause',     'Assets/Sprites/Buttons/pause-button-1-64-target.png'),
  ('stats',     'Assets/Sprites/Buttons/stats-button-64-target.png'),
  ('empty',     'Assets/Sprites/Buttons/empty-button-64-target.png'),
  ('long',      'Assets/Sprites/Buttons/long-button-256-64-target.png')
) AS m(slug, assets_path)
JOIN catalog_entity ce ON ce.kind = 'sprite' AND ce.slug = m.slug
ON CONFLICT (entity_id) DO NOTHING;

-- ── 3. entity_version (one published row per sprite) ────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration": "0107_seed_hud_bar_sprites_v2", "event": "initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'sprite'
  AND ce.slug IN ('new-game','save-game','load-game','zoom-in','zoom-out','pause','stats','empty','long')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

-- ── 4. publish (current_published_version_id) ───────────────────────────────

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'sprite'
  AND ce.slug IN ('new-game','save-game','load-game','zoom-in','zoom-out','pause','stats','empty','long')
  AND ce.current_published_version_id IS NULL;

-- ── 5. Sanity assertion ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_sprites int;
  n_published int;
BEGIN
  SELECT COUNT(*) INTO n_sprites
    FROM catalog_entity
    WHERE kind = 'sprite'
      AND slug IN ('new-game','save-game','load-game','zoom-in','zoom-out','pause','stats','empty','long');

  SELECT COUNT(*) INTO n_published
    FROM catalog_entity
    WHERE kind = 'sprite'
      AND slug IN ('new-game','save-game','load-game','zoom-in','zoom-out','pause','stats','empty','long')
      AND current_published_version_id IS NOT NULL;

  IF n_sprites <> 9 THEN
    RAISE EXCEPTION '0107: expected 9 hud-bar v2 sprite entities, got %', n_sprites;
  END IF;

  IF n_published <> 9 THEN
    RAISE EXCEPTION '0107: expected 9 published sprite entities, got %', n_published;
  END IF;

  RAISE NOTICE '0107 OK: hud-bar v2 sprites seeded (n=% published=%)', n_sprites, n_published;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version
--     USING catalog_entity ce
--     WHERE entity_version.entity_id = ce.id
--       AND ce.kind = 'sprite'
--       AND ce.slug IN ('new-game','save-game','load-game','zoom-in','zoom-out',
--                       'pause','stats','empty','long');
--   DELETE FROM sprite_detail
--     USING catalog_entity ce
--     WHERE sprite_detail.entity_id = ce.id
--       AND ce.kind = 'sprite'
--       AND ce.slug IN ('new-game','save-game','load-game','zoom-in','zoom-out',
--                       'pause','stats','empty','long');
--   DELETE FROM catalog_entity
--     WHERE kind = 'sprite'
--       AND slug IN ('new-game','save-game','load-game','zoom-in','zoom-out',
--                    'pause','stats','empty','long');
