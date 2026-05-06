-- TECH-15909: Status cascade SQL trigger — soft-flip stage with audit row
-- When all child tasks flip to 'done', auto-flip parent stage to 'done'
-- and append audit row to ia_master_plan_change_log (kind='status_cascade').
-- Soft-flip per locked decision #8 — existing closeout flow unchanged.

CREATE OR REPLACE FUNCTION trg_status_cascade_fn()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_all_done BOOLEAN;
  v_stage_status TEXT;
  v_stage_title TEXT;
  v_payload JSONB;
BEGIN
  -- Only proceed when a task transitions to 'done'
  IF NEW.status <> 'done' THEN
    RETURN NEW;
  END IF;

  -- Check whether all tasks in this (slug, stage_id) are done
  SELECT bool_and(status = 'done')
    INTO v_all_done
    FROM ia_tasks
   WHERE slug     = NEW.slug
     AND stage_id = NEW.stage_id;

  IF NOT v_all_done THEN
    RETURN NEW;
  END IF;

  -- Read current stage status to avoid double-flip
  SELECT status, title
    INTO v_stage_status, v_stage_title
    FROM ia_stages
   WHERE slug     = NEW.slug
     AND stage_id = NEW.stage_id;

  IF v_stage_status = 'done' THEN
    -- Already done — idempotent, skip
    RETURN NEW;
  END IF;

  -- Soft-flip stage to 'done'
  UPDATE ia_stages
     SET status     = 'done',
         updated_at = NOW()
   WHERE slug     = NEW.slug
     AND stage_id = NEW.stage_id;

  -- Append audit row
  v_payload := jsonb_build_object(
    'trigger',   'soft_flip',
    'stage_id',  NEW.stage_id,
    'slug',      NEW.slug,
    'task_id',   NEW.task_id,
    'prev_stage_status', v_stage_status
  );

  INSERT INTO ia_master_plan_change_log
    (slug, stage_id, kind, body, actor)
  VALUES (
    NEW.slug,
    NEW.stage_id,
    'status_cascade',
    ('Stage ' || NEW.stage_id || ' auto-closed by status_cascade trigger after all tasks done. ' ||
     'Prev status: ' || COALESCE(v_stage_status, 'null') || '. ' ||
     'Last task: ' || NEW.task_id || '.'),
    'trg_status_cascade'
  );

  RETURN NEW;
END;
$$;

-- Drop existing trigger if present (idempotent re-run)
DROP TRIGGER IF EXISTS trg_status_cascade ON ia_tasks;

CREATE TRIGGER trg_status_cascade
AFTER UPDATE OF status ON ia_tasks
FOR EACH ROW
EXECUTE FUNCTION trg_status_cascade_fn();

COMMENT ON FUNCTION trg_status_cascade_fn() IS
  'Auto-flip ia_stages.status → done when all child tasks are done. '
  'Appends audit row to ia_master_plan_change_log with kind=status_cascade. '
  'Idempotent: skips if stage already done. TECH-15909.';

COMMENT ON TRIGGER trg_status_cascade ON ia_tasks IS
  'Fires AFTER UPDATE OF status on every ia_tasks row. '
  'Delegates to trg_status_cascade_fn(). TECH-15909.';

-- Rollback:
--   DROP TRIGGER IF EXISTS trg_status_cascade ON ia_tasks;
--   DROP FUNCTION IF EXISTS trg_status_cascade_fn();
