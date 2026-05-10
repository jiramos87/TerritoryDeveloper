-- Migration: 0143_seed_modal_card_archetype.sql
-- Stage 8.0 Wave B4 — TECH-27093
-- modal-card archetype record.
-- No catalog_archetype table in this schema version;
-- archetype is registered via layout_template extension (done in 0142).
-- This migration validates modal-card is usable and records the archetype
-- in panel_detail CHECK constraint (constraint already extended by 0142).

-- No-op if 0142 already applied the constraint extension.
-- Idempotent: safe to re-run.

DO $$
DECLARE
  v_modal_card_in_check bool;
BEGIN
  SELECT true INTO v_modal_card_in_check
  FROM information_schema.check_constraints
  WHERE constraint_schema = 'public'
    AND check_clause LIKE '%modal-card%';

  IF v_modal_card_in_check IS NOT TRUE THEN
    RAISE EXCEPTION '0143: modal-card not found in panel_detail layout_template CHECK — run 0142 first';
  END IF;

  RAISE NOTICE '0143 OK: modal-card archetype acknowledged in layout_template constraint';
END;
$$;
