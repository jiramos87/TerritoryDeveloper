-- 0154_hud_bar_redesign_drop_filemenu_relocate_play.sql
-- Stage 13 hotfix follow-on — hud-bar design pivot.
--
-- Pause-menu now owns new-game / save / load. Drop those 3 buttons from
-- hud-bar; relocate play-pause + speed-cycle from Right Col1 Row1 → Left;
-- collapse Right Col1 to budget-only (single row, no sub-grid).
--
-- Idempotent: re-running matches the same final state.

BEGIN;

-- ── 1. Drop new-game / save / load instances from hud-bar ────────────────────
DELETE FROM panel_child pc
USING catalog_entity ce
WHERE pc.panel_entity_id = ce.id
  AND ce.slug = 'hud-bar'
  AND pc.instance_slug IN (
    'hud-bar-new-button',
    'hud-bar-save-button',
    'hud-bar-load-button'
  );

-- ── 2. Relocate play-pause to Left zone ord 0 ────────────────────────────────
UPDATE panel_child pc
SET layout_json = jsonb_build_object('ord', 0, 'zone', 'left'),
    order_idx   = 1
FROM catalog_entity ce
WHERE pc.panel_entity_id = ce.id
  AND ce.slug = 'hud-bar'
  AND pc.instance_slug = 'hud-bar-play-pause-button';

-- ── 3. Relocate speed-cycle to Left zone ord 1 ───────────────────────────────
UPDATE panel_child pc
SET layout_json = jsonb_build_object('ord', 1, 'zone', 'left'),
    order_idx   = 2
FROM catalog_entity ce
WHERE pc.panel_entity_id = ce.id
  AND ce.slug = 'hud-bar'
  AND pc.instance_slug = 'hud-bar-speed-cycle-button';

-- ── 4. Collapse Right Col1 to budget-only (drop sub-grid hint) ───────────────
UPDATE panel_child pc
SET layout_json = jsonb_build_object('col', 1, 'row', 0, 'zone', 'right')
FROM catalog_entity ce
WHERE pc.panel_entity_id = ce.id
  AND ce.slug = 'hud-bar'
  AND pc.instance_slug = 'hud-bar-budget-button';

-- ── 5. Verify post-state ─────────────────────────────────────────────────────
DO $$
DECLARE
  v_total_children int;
  v_left_count     int;
  v_drop_count     int;
BEGIN
  SELECT count(*)::int INTO v_total_children
  FROM panel_child pc JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.slug = 'hud-bar';

  SELECT count(*)::int INTO v_left_count
  FROM panel_child pc JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.slug = 'hud-bar'
    AND pc.layout_json->>'zone' = 'left';

  SELECT count(*)::int INTO v_drop_count
  FROM panel_child pc JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.slug = 'hud-bar'
    AND pc.instance_slug IN (
      'hud-bar-new-button', 'hud-bar-save-button', 'hud-bar-load-button'
    );

  IF v_drop_count <> 0 THEN
    RAISE EXCEPTION '0154: expected 0 dropped buttons, found %', v_drop_count;
  END IF;

  IF v_left_count <> 2 THEN
    RAISE EXCEPTION '0154: expected 2 left-zone children (play-pause + speed), found %', v_left_count;
  END IF;

  RAISE NOTICE '0154 OK: hud-bar now has % children, % in Left zone', v_total_children, v_left_count;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   See migration 0108_seed_hud_bar_panel_v2 for original layout.
