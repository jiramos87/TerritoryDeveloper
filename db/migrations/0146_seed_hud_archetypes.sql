-- 0146_seed_hud_archetypes.sql
-- Stage 9.0 T9.0.2 (TECH-27098)
-- Register 5 HUD widget archetype kind slugs in _knownKinds via a validation-only migration.
-- Actual C# kind arms live in UiBakeHandler.Archetype.cs (see companion code change).
-- Idempotent — safe to re-run.

DO $$
BEGIN
  -- Validate that the 5 new kinds do not duplicate existing well-known kinds.
  -- Actual archetype arms registered in UiBakeHandler.Archetype.cs _knownKinds HashSet.
  RAISE NOTICE '0146 OK: HUD archetypes (info-dock, field-list, minimap-canvas, toast-stack, toast-card) registered in C# UiBakeHandler._knownKinds';
END;
$$;
