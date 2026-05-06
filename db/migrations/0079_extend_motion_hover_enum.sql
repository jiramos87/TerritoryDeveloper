-- 0079_extend_motion_hover_enum.sql
-- Stage 9.7 game-ui-catalog-bake — extend motion.hover allowed values.
-- Replaces ck_catalog_entity_motion_keys to admit tint|glow|scale in addition to
-- the existing fade|slide|none. Archetype rows (picker-tile-72) carry hover=tint.
-- TECH-15892

BEGIN;

-- Drop old constraint that only admitted fade|slide|none for all motion keys.
ALTER TABLE catalog_entity
  DROP CONSTRAINT IF EXISTS ck_catalog_entity_motion_keys;

-- New constraint: enter/exit stay {fade,slide,none}; hover expands to {fade,slide,none,tint,glow,scale}.
-- Rationale: enter/exit are animation curves; hover is a pointer-interaction variant.
-- tint/glow/scale are UI hover behaviours irrelevant to enter/exit.
ALTER TABLE catalog_entity
  ADD CONSTRAINT ck_catalog_entity_motion_keys
  CHECK (
    motion ? 'enter'
    AND motion ? 'exit'
    AND motion ? 'hover'
    AND motion->>'enter' = ANY(ARRAY['fade','slide','none'])
    AND motion->>'exit'  = ANY(ARRAY['fade','slide','none'])
    AND motion->>'hover' = ANY(ARRAY['fade','slide','none','tint','glow','scale'])
  );

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE catalog_entity DROP CONSTRAINT IF EXISTS ck_catalog_entity_motion_keys;
--   ALTER TABLE catalog_entity ADD CONSTRAINT ck_catalog_entity_motion_keys CHECK (
--     motion ? 'enter' AND motion ? 'exit' AND motion ? 'hover'
--     AND motion->>'enter' = ANY(ARRAY['fade','slide','none'])
--     AND motion->>'exit'  = ANY(ARRAY['fade','slide','none'])
--     AND motion->>'hover' = ANY(ARRAY['fade','slide','none'])
--   );
