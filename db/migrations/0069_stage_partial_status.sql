-- 0069_stage_partial_status.sql
--
-- ship-protocol Stage 3 / TECH-12639 — extend `stage_status` enum with
-- `'partial'`. Enables ship-cycle (Sonnet 4.6 stage-atomic batch) fix-forward
-- resume on mixed-success batches: when a single batch inference closes some
-- tasks of a stage but not all, the stage lands `status='partial'` so resume
-- can re-enter at the first non-done task without flipping the stage to
-- `done` prematurely.
--
-- Treated as NON-TERMINAL by downstream queries (`stage_render`, closeout,
-- `master_plan_health`) — equivalent to `in_progress` for the purposes of
-- "stage is open for re-entry", but distinguishable in render glyph + health
-- attention bands.
--
-- Idempotent: ADD VALUE IF NOT EXISTS — re-run produces zero schema diff.
-- No row migration required (additive enum extension).

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_enum e
    JOIN pg_type t ON t.oid = e.enumtypid
    WHERE t.typname = 'stage_status'
      AND e.enumlabel = 'partial'
  ) THEN
    ALTER TYPE stage_status ADD VALUE 'partial';
  END IF;
END
$$;
