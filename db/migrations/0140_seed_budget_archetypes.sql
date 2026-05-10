-- Migration: 0140_seed_budget_archetypes.sql
-- Stage 7.0 Wave B3 — TECH-27088
-- Archetype acknowledgement for slider-row-numeric, expense-row, readout-block.
-- NOTE: catalog_archetype table does not exist in this schema version.
-- Archetypes are registered as bake-handler case arms (UiBakeHandler.Archetype.cs).
-- This migration is a no-op acknowledgement for migration-chain continuity.

DO $$
BEGIN
  RAISE NOTICE '0140 OK: budget-panel archetypes (slider-row-numeric/expense-row/readout-block) registered in bake-handler — no catalog_archetype table in this schema version';
END;
$$;
