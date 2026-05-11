-- 0155_pause_menu_content_slot.sql
-- Stage 13 hotfix follow-on — add view-slot child to pause-menu so
-- settings-view / save-load-view can mount inside it.
--
-- Adapter (PauseMenuDataAdapter) resolves a slot Transform via
-- SlotAnchorResolver.ResolveByPanel(...), which walks descendants
-- for a child named "{panel}-content-slot" (or suffix fallback).
-- Before this migration pause-menu had only buttons + a title label,
-- so resolver returned null and Settings/Save/Load click handlers
-- silently no-op'd at MountSubView.
--
-- This migration adds the missing view-slot child, mirroring the
-- main-menu-content-slot pattern, so settings-view + save-load-view
-- prefabs mount inside pause-menu instead of the empty MainMenu host.
--
-- Idempotent: re-running matches the same final state.

BEGIN;

-- ── 1. Insert pause-menu-content-slot child (idempotent) ────────────────────
INSERT INTO panel_child (
  panel_entity_id,
  slot_name,
  instance_slug,
  child_kind,
  child_entity_id,
  order_idx,
  layout_json,
  params_json
)
SELECT
  ce.id                                                                AS panel_entity_id,
  'content-slot'                                                       AS slot_name,
  'pause-menu-content-slot'                                            AS instance_slug,
  'view-slot'                                                          AS child_kind,
  NULL                                                                 AS child_entity_id,
  100                                                                  AS order_idx,
  jsonb_build_object('zone', 'center')                                 AS layout_json,
  jsonb_build_object(
    'kind',      'view-slot',
    'views',     jsonb_build_array('root', 'settings', 'save', 'load'),
    'default',   'root',
    'slot_bind', 'pause.contentScreen'
  )                                                                    AS params_json
FROM catalog_entity ce
WHERE ce.slug = 'pause-menu'
  AND NOT EXISTS (
    SELECT 1
    FROM panel_child pc2
    WHERE pc2.panel_entity_id = ce.id
      AND pc2.instance_slug   = 'pause-menu-content-slot'
  );

-- ── 2. Verify post-state ─────────────────────────────────────────────────────
DO $$
DECLARE
  v_slot_count int;
BEGIN
  SELECT count(*)::int INTO v_slot_count
  FROM panel_child pc JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
  WHERE ce.slug = 'pause-menu'
    AND pc.instance_slug = 'pause-menu-content-slot';

  IF v_slot_count <> 1 THEN
    RAISE EXCEPTION '0155: expected exactly 1 pause-menu-content-slot row, found %', v_slot_count;
  END IF;

  RAISE NOTICE '0155 OK: pause-menu-content-slot present';
END;
$$;

COMMIT;
