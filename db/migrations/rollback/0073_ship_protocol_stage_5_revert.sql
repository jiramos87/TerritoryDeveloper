-- 0073_ship_protocol_stage_5_revert.sql
--
-- Rollback for 0073_backfill_seeded_digests.sql (TECH-14103).
--
-- Restores the original master_plan_bundle_apply function (pre-UPSERT-gate),
-- drops the ia_tasks.backfilled column, drops ia_master_plans.backfill_version,
-- and drops the backfill_id_seq sequence.
--
-- WARNING: dropping ia_tasks.backfilled discards all backfill tracking data.
-- Only run after confirming no seeded tasks remain or after a full plan restore.

BEGIN;

-- -------------------------------------------------------------------------
-- 1. Restore original master_plan_bundle_apply (pre-UPSERT-gate, Stage 1.0).
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION master_plan_bundle_apply(p_bundle jsonb)
  RETURNS jsonb
  LANGUAGE plpgsql
AS $$
DECLARE
  v_plan_slug        text;
  v_stages_inserted  integer := 0;
  v_tasks_inserted   integer := 0;
  v_stage            jsonb;
  v_task             jsonb;
  v_prefix           text;
  v_seq              text;
  v_task_id          text;
  v_task_body        text;
  v_task_key         text;
BEGIN
  v_plan_slug := p_bundle->'plan'->>'slug';
  IF v_plan_slug IS NULL OR length(v_plan_slug) = 0 THEN
    RAISE EXCEPTION 'master_plan_bundle_apply: bundle.plan.slug is required';
  END IF;

  INSERT INTO ia_master_plans (
    slug, title, description, preamble, parent_plan_slug, version
  ) VALUES (
    v_plan_slug,
    COALESCE(p_bundle->'plan'->>'title', ''),
    p_bundle->'plan'->>'description',
    p_bundle->'plan'->>'preamble',
    p_bundle->'plan'->>'parent_plan_slug',
    COALESCE((p_bundle->'plan'->>'version')::int, 1)
  );

  FOR v_stage IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'stages', '[]'::jsonb)) LOOP
    INSERT INTO ia_stages (
      slug, stage_id, title, objective, exit_criteria, status,
      section_id, carcass_role, visibility_delta
    ) VALUES (
      v_plan_slug,
      v_stage->>'stage_id',
      v_stage->>'title',
      v_stage->>'objective',
      v_stage->>'exit_criteria',
      COALESCE((v_stage->>'status')::stage_status, 'pending'::stage_status),
      v_stage->>'section_id',
      v_stage->>'carcass_role',
      v_stage->>'visibility_delta'
    );
    v_stages_inserted := v_stages_inserted + 1;
  END LOOP;

  FOR v_task IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'tasks', '[]'::jsonb)) LOOP
    v_prefix := UPPER(COALESCE(v_task->>'prefix', 'TECH'));
    v_task_key := v_task->>'task_key';

    v_seq := CASE v_prefix
      WHEN 'TECH'  THEN 'tech_id_seq'
      WHEN 'FEAT'  THEN 'feat_id_seq'
      WHEN 'BUG'   THEN 'bug_id_seq'
      WHEN 'ART'   THEN 'art_id_seq'
      WHEN 'AUDIO' THEN 'audio_id_seq'
      ELSE NULL
    END;

    IF v_seq IS NULL THEN
      RAISE EXCEPTION 'master_plan_bundle_apply: unknown task prefix %', v_prefix;
    END IF;

    EXECUTE format('SELECT %s || ''-'' || nextval(''%s'')', quote_literal(v_prefix), v_seq)
      INTO v_task_id;

    v_task_body := COALESCE(v_task->>'body', '');
    IF v_task_key IS NOT NULL THEN
      v_task_body := format('<!-- task_key: %s -->%s%s',
                             v_task_key, E'\n', v_task_body);
    END IF;

    INSERT INTO ia_tasks (
      task_id, prefix, slug, stage_id, title, status, body, type, priority, notes
    ) VALUES (
      v_task_id, v_prefix, v_plan_slug,
      v_task->>'stage_id',
      COALESCE(v_task->>'title', ''),
      COALESCE((v_task->>'status')::task_status, 'pending'::task_status),
      v_task_body,
      v_task->>'type',
      v_task->>'priority',
      v_task->>'notes'
    );
    v_tasks_inserted := v_tasks_inserted + 1;
  END LOOP;

  RETURN jsonb_build_object(
    'plan_slug',       v_plan_slug,
    'stages_inserted', v_stages_inserted,
    'tasks_inserted',  v_tasks_inserted
  );
END;
$$;

-- -------------------------------------------------------------------------
-- 2. Drop backfilled column from ia_tasks.
-- -------------------------------------------------------------------------
ALTER TABLE ia_tasks
  DROP COLUMN IF EXISTS backfilled;

-- -------------------------------------------------------------------------
-- 3. Drop backfill_version column from ia_master_plans.
-- -------------------------------------------------------------------------
ALTER TABLE ia_master_plans
  DROP COLUMN IF EXISTS backfill_version;

-- -------------------------------------------------------------------------
-- 4. Drop backfill_id_seq sequence.
-- -------------------------------------------------------------------------
DROP SEQUENCE IF EXISTS backfill_id_seq;

COMMIT;
