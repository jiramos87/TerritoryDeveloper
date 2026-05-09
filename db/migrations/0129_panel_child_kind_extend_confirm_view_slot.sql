-- 0129_panel_child_kind_extend_confirm_view_slot.sql
-- Extend panel_child.child_kind CHECK constraint to allow design-spec kinds
-- 'confirm-button' and 'view-slot' (per docs/ui-element-definitions.md
-- main-menu definition, lines 1239-1248).
--
-- Without this, 0130 main-menu re-seed cannot land — quit confirm-button + content
-- view-slot rows would violate the existing CHECK (button|panel|label|spacer|audio|
-- sprite|label_inline|row|text).
--
-- Idempotent: ALTER ... DROP CONSTRAINT IF EXISTS, then re-add expanded set.

BEGIN;

ALTER TABLE panel_child
  DROP CONSTRAINT IF EXISTS panel_child_child_kind_check;

ALTER TABLE panel_child
  ADD CONSTRAINT panel_child_child_kind_check
  CHECK (child_kind = ANY (ARRAY[
    'button'::text,
    'panel'::text,
    'label'::text,
    'spacer'::text,
    'audio'::text,
    'sprite'::text,
    'label_inline'::text,
    'row'::text,
    'text'::text,
    'confirm-button'::text,
    'view-slot'::text
  ]));

DO $$
BEGIN
  RAISE NOTICE '0129 OK: panel_child_child_kind_check extended with confirm-button + view-slot';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE panel_child DROP CONSTRAINT panel_child_child_kind_check;
--   ALTER TABLE panel_child ADD CONSTRAINT panel_child_child_kind_check
--     CHECK (child_kind = ANY (ARRAY['button','panel','label','spacer','audio','sprite','label_inline','row','text']));
