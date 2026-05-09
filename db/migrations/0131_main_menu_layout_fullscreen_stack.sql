-- 0131_main_menu_layout_fullscreen_stack.sql
-- Switch main-menu panel layout_template from 'vstack' to 'fullscreen-stack'.
--
-- 'vstack' produces a flat top-to-bottom column for all 10 children → branding
-- labels render mixed in with buttons. Design spec (lines 1188-1200) requires
-- a full-screen surface with zoned slots: title=top, studio=bottom-left,
-- version=bottom-right, buttons=center, back=top-left, content-slot=center.
--
-- Adds the 'fullscreen-stack' template to the panel_detail.layout_template
-- CHECK constraint, then UPDATEs main-menu's panel_detail row.
--
-- Bake handler companion change: UiBakeHandler.MapLayoutTemplate +
-- MapLayoutTemplateToPanelKind learn the new template; child loop honors
-- layout_json.zone routing for fullscreen-stack panels.
--
-- Idempotent: DROP CONSTRAINT IF EXISTS, ADD with extended set; UPDATE WHERE
-- layout_template <> 'fullscreen-stack'.

BEGIN;

ALTER TABLE panel_detail
  DROP CONSTRAINT IF EXISTS panel_detail_layout_template_check;

ALTER TABLE panel_detail
  ADD CONSTRAINT panel_detail_layout_template_check
  CHECK (layout_template = ANY (ARRAY[
    'vstack'::text,
    'hstack'::text,
    'grid'::text,
    'free'::text,
    'fullscreen-stack'::text
  ]));

UPDATE panel_detail pd
SET layout_template = 'fullscreen-stack',
    updated_at      = now()
FROM catalog_entity ce
WHERE ce.id = pd.entity_id
  AND ce.kind = 'panel'
  AND ce.slug = 'main-menu'
  AND pd.layout_template <> 'fullscreen-stack';

DO $$
DECLARE
  v_template text;
BEGIN
  SELECT pd.layout_template INTO v_template
  FROM panel_detail pd
  JOIN catalog_entity ce ON ce.id = pd.entity_id
  WHERE ce.kind='panel' AND ce.slug='main-menu';

  IF v_template <> 'fullscreen-stack' THEN
    RAISE EXCEPTION '0131: main-menu layout_template expected fullscreen-stack, got %', v_template;
  END IF;

  RAISE NOTICE '0131 OK: main-menu layout_template = fullscreen-stack';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   UPDATE panel_detail pd SET layout_template='vstack', updated_at=now()
--   FROM catalog_entity ce WHERE ce.id=pd.entity_id AND ce.kind='panel' AND ce.slug='main-menu';
--   ALTER TABLE panel_detail DROP CONSTRAINT panel_detail_layout_template_check;
--   ALTER TABLE panel_detail ADD CONSTRAINT panel_detail_layout_template_check
--     CHECK (layout_template = ANY (ARRAY['vstack','hstack','grid','free']));
