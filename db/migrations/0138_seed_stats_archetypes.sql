-- Migration: 0138_seed_stats_archetypes.sql
-- Stage 6.0 Wave B2 — TECH-27083
-- Archetype acknowledgement for tab-strip, chart, range-tabs, stacked-bar-row, service-row.
-- NOTE: catalog_archetype table does not exist in this schema version.
-- Archetypes are registered as bake-handler case arms (UiBakeHandler.Archetype.cs).
-- This migration is a no-op acknowledgement for migration-chain continuity.

DO $$
BEGIN
  RAISE NOTICE '0138 OK: stats-panel archetypes (tab-strip/chart/range-tabs/stacked-bar-row/service-row) registered in bake-handler — no catalog_archetype table in this schema version';
END;
$$;
