-- 0044_ia_stages_depends_on.sql
--
-- db-lifecycle-extensions Stage 2 / TECH-3225.
-- Adds `depends_on text[]` column on `ia_stages` with cycle-check trigger.
-- Stage-level dep walks consumed by:
--   - TECH-3226: ia_master_plan_health MV (in_progress detection / sibling
--     collision)
--   - TECH-3228: master_plan_next_actionable MCP tool (Kahn topo walk)
--
-- Dep ref shape: text array of `slug/stage_id` strings (e.g.
-- `'asset-pipeline/13.1'`). Matches existing `arch_surfaces[]` pattern.
-- Default empty array — NULL-tolerant for pre-migration rows.
--
-- Cycle check: BEFORE INSERT/UPDATE row-level trigger walks the directed
-- graph induced by `depends_on` edges. Self-loop OR multi-node cycle
-- raises P0001 with payload `cycle_detected: <path>` (consumable by
-- /stage-decompose error handler per TECH-3230).
--
-- Migration slot 0043 was claimed by `0043_catalog_ref_edge.sql`
-- (asset-pipeline Stage 14.1 / TECH-3001); locking on 0044.
--
-- Idempotent: `IF NOT EXISTS` on column add + `CREATE OR REPLACE FUNCTION`
-- + `DROP TRIGGER IF EXISTS` before `CREATE TRIGGER`. Re-applying is a
-- no-op.

BEGIN;

-- 1. Column ----------------------------------------------------------------

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name   = 'ia_stages'
       AND column_name  = 'depends_on'
  ) THEN
    ALTER TABLE ia_stages
      ADD COLUMN depends_on text[] NOT NULL DEFAULT '{}'::text[];
  END IF;
END
$$;

COMMENT ON COLUMN ia_stages.depends_on IS
  'Stage-level dep edges. Array of `slug/stage_id` strings. Cycle-checked by trigger fn_ia_stages_cycle_check (db-lifecycle-extensions Stage 2 / TECH-3225).';

-- 2. Cycle-check trigger function -----------------------------------------

CREATE OR REPLACE FUNCTION fn_ia_stages_cycle_check()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  -- BFS frontier + visited set, both keyed by `slug/stage_id`.
  frontier text[];
  visited  text[];
  current  text;
  next_set text[];
  dep_ref  text;
  parts    text[];
  dep_slug text;
  dep_sid  text;
  child_deps text[];
  start_key  text;
  cycle_path text[];
BEGIN
  -- Empty deps array: no cycle possible.
  IF NEW.depends_on IS NULL OR cardinality(NEW.depends_on) = 0 THEN
    RETURN NEW;
  END IF;

  start_key := NEW.slug || '/' || NEW.stage_id;

  -- Self-loop check: row references its own (slug, stage_id).
  IF start_key = ANY (NEW.depends_on) THEN
    RAISE EXCEPTION USING
      ERRCODE  = 'P0001',
      MESSAGE  = 'cycle detected: self-loop on ' || start_key,
      DETAIL   = 'cycle_detected: ' || start_key || ' -> ' || start_key;
  END IF;

  -- BFS over depends_on graph starting from NEW row's deps. Cycle iff we
  -- revisit start_key.
  frontier := NEW.depends_on;
  visited  := ARRAY[start_key]::text[];
  cycle_path := ARRAY[start_key]::text[];

  WHILE cardinality(frontier) > 0 LOOP
    next_set := ARRAY[]::text[];
    FOREACH dep_ref IN ARRAY frontier LOOP
      IF dep_ref = start_key THEN
        RAISE EXCEPTION USING
          ERRCODE = 'P0001',
          MESSAGE = 'cycle detected: ' || start_key || ' reachable from its own deps',
          DETAIL  = 'cycle_detected: ' || array_to_string(cycle_path || dep_ref, ' -> ');
      END IF;
      IF dep_ref = ANY (visited) THEN
        CONTINUE;
      END IF;
      visited := visited || dep_ref;

      -- Parse `slug/stage_id`. Skip rows with malformed refs (forward-only
      -- BF; agent layer validates shape).
      parts := string_to_array(dep_ref, '/');
      IF cardinality(parts) < 2 THEN
        CONTINUE;
      END IF;
      dep_slug := parts[1];
      -- Re-join in case stage_id itself contains `/`.
      dep_sid  := array_to_string(parts[2:], '/');

      SELECT depends_on
        INTO child_deps
        FROM ia_stages
       WHERE slug = dep_slug
         AND stage_id = dep_sid;

      IF child_deps IS NOT NULL AND cardinality(child_deps) > 0 THEN
        next_set := next_set || child_deps;
      END IF;
    END LOOP;
    frontier := next_set;
  END LOOP;

  RETURN NEW;
END
$$;

COMMENT ON FUNCTION fn_ia_stages_cycle_check() IS
  'BFS cycle detector for ia_stages.depends_on graph. Raises P0001 on self-loop or multi-node cycle. db-lifecycle-extensions Stage 2 / TECH-3225.';

-- 3. Trigger wiring -------------------------------------------------------

DROP TRIGGER IF EXISTS ia_stages_cycle_check ON ia_stages;

CREATE TRIGGER ia_stages_cycle_check
BEFORE INSERT OR UPDATE OF depends_on ON ia_stages
FOR EACH ROW
EXECUTE FUNCTION fn_ia_stages_cycle_check();

COMMIT;

-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP TRIGGER IF EXISTS ia_stages_cycle_check ON ia_stages;
--   DROP FUNCTION IF EXISTS fn_ia_stages_cycle_check();
--   ALTER TABLE ia_stages DROP COLUMN IF EXISTS depends_on;
--   COMMIT;
