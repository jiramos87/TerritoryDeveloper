-- 0110_seed_toolbar_panel.sql
--
-- Pay scene-edit debt — toolbar root rect goes into DB
-- (Track A.1, `docs/ui-bake-pipeline-rollout-plan.md`).
--
-- Background:
--   `Assets/Scenes/CityScene.unity` carries hand-authored
--   PrefabInstance overrides on the toolbar root RectTransform
--   (commit 0e9060a2) — violates DB-first invariant
--   (`docs/ui-bake-pipeline-rollout-plan.md` § Track A).
--
-- Migration scope:
--   1. Insert `catalog_entity (kind=panel, slug=toolbar)` if missing.
--   2. Insert `panel_detail` row keyed by entity_id with the rebake-7
--      visual rect_json — left-edge full-height stretch, 220 px wide,
--      top inset 152 + bottom inset 200 (size_delta.y = -352).
--   3. Publish `entity_version=1` so snapshot exporter picks up
--      the row.
--
-- Out of scope (Track A only owns root rect — children stay
-- hand-authored on `Assets/UI/Prefabs/Generated/toolbar.prefab`
-- until later track):
--   - panel_child rows for the 11 tools — deferred.
--   - Sprite/button entities for tools — deferred.
--
-- Idempotent: ON CONFLICT DO NOTHING + UPDATE-no-op when row already
-- carries the seeded shape.

BEGIN;

-- ─── 1. catalog_entity row ─────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'toolbar', 'Toolbar')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. panel_detail row + rect_json seed ──────────────────────────────────

INSERT INTO panel_detail (
  entity_id,
  layout_template,
  layout,
  padding_json,
  gap_px,
  rect_json
)
SELECT
  ce.id,
  'vstack',
  'vstack',
  '{"top":4,"left":4,"right":4,"bottom":4}'::jsonb,
  4,
  jsonb_build_object(
    'anchor_min',        jsonb_build_array(0,    0),
    'anchor_max',        jsonb_build_array(0,    1),
    'pivot',             jsonb_build_array(0,    0.5),
    'anchored_position', jsonb_build_array(12,   24),
    'size_delta',        jsonb_build_array(220,  -352)
  )
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'toolbar'
ON CONFLICT (entity_id) DO UPDATE
  SET rect_json  = EXCLUDED.rect_json,
      updated_at = now()
  WHERE panel_detail.rect_json IS NULL
     OR panel_detail.rect_json = '{}'::jsonb;

-- ─── 3. entity_version v1 + publish ────────────────────────────────────────

INSERT INTO entity_version (
  entity_id, version_number, status,
  params_json, lint_overrides_json, migration_hint_json
)
SELECT
  ce.id, 1, 'published',
  '{}'::jsonb, '{}'::jsonb,
  '{"migration": "0110_seed_toolbar_panel", "event": "initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel'
  AND ce.slug = 'toolbar'
  AND NOT EXISTS (
    SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id
  );

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'toolbar'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. Sanity assertions ──────────────────────────────────────────────────

DO $$
DECLARE
  v_panel_id   bigint;
  v_pub_id     bigint;
  v_rect       jsonb;
BEGIN
  SELECT ce.id, ce.current_published_version_id
    INTO v_panel_id, v_pub_id
  FROM catalog_entity ce
  WHERE ce.kind = 'panel' AND ce.slug = 'toolbar';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0110: toolbar entity missing';
  END IF;
  IF v_pub_id IS NULL THEN
    RAISE EXCEPTION '0110: toolbar current_published_version_id NULL';
  END IF;

  SELECT pd.rect_json INTO v_rect
  FROM panel_detail pd
  WHERE pd.entity_id = v_panel_id;

  IF v_rect IS NULL OR v_rect = '{}'::jsonb THEN
    RAISE EXCEPTION '0110: toolbar rect_json failed to seed';
  END IF;

  IF (v_rect->'size_delta'->>1)::numeric <> -352 THEN
    RAISE EXCEPTION
      '0110: toolbar size_delta.y expected -352, got %',
      v_rect->'size_delta'->>1;
  END IF;

  IF (v_rect->'size_delta'->>0)::numeric <> 220 THEN
    RAISE EXCEPTION
      '0110: toolbar size_delta.x expected 220, got %',
      v_rect->'size_delta'->>0;
  END IF;

  RAISE NOTICE '0110 OK: toolbar seeded (panel_id=% ver=% rect=%)',
    v_panel_id, v_pub_id, v_rect;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version
--     WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='toolbar');
--   DELETE FROM panel_detail
--     WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='toolbar');
--   DELETE FROM catalog_entity WHERE kind='panel' AND slug='toolbar';
