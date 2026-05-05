-- 0073_backfill_seeded_digests.sql
--
-- ship-protocol Stage 5 / TECH-14103 — backfill migration for seeded §Plan Digest bodies.
--
-- Adds:
--   ia_tasks.backfilled       boolean NOT NULL DEFAULT false
--     — true = body was seeded by the backfill classifier; gates UPSERT path in
--       master_plan_bundle_apply and `validate:seeded-task-stale`.
--
--   ia_master_plans.backfill_version  text NULL
--     — records the backfill batch label (e.g. 'backfill_v1') on the plan row
--       so callers can correlate plan-level seeding status without scanning tasks.
--
--   backfill_id_seq  SEQUENCE
--     — monotonic counter for backfill run ids; separate from task id sequences.
--
-- Also replaces master_plan_bundle_apply to add the UPSERT gate predicate:
--   WHERE backfilled = true OR version > existing_max_version
-- (M#12). Duplicate-slug INSERT at same version still raises 23505.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS / CREATE SEQUENCE IF NOT EXISTS /
-- CREATE OR REPLACE FUNCTION — re-run produces zero schema diff.

BEGIN;

-- -------------------------------------------------------------------------
-- 1. ia_tasks.backfilled column
-- -------------------------------------------------------------------------
ALTER TABLE ia_tasks
  ADD COLUMN IF NOT EXISTS backfilled boolean NOT NULL DEFAULT false;

-- -------------------------------------------------------------------------
-- 2. ia_master_plans.backfill_version column
-- -------------------------------------------------------------------------
ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS backfill_version text NULL;

-- -------------------------------------------------------------------------
-- 3. backfill_id_seq sequence
-- -------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS backfill_id_seq
  START WITH 1
  INCREMENT BY 1
  NO MINVALUE
  NO MAXVALUE
  CACHE 1;

-- -------------------------------------------------------------------------
-- 4. Update master_plan_bundle_apply with UPSERT gate (M#12).
--    Gate predicate: backfilled = true OR incoming version > existing_max_version.
--    Non-backfilled INSERT at same version still raises unique constraint 23505.
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION master_plan_bundle_apply(p_bundle jsonb)
  RETURNS jsonb
  LANGUAGE plpgsql
AS $$
DECLARE
  v_plan_slug           text;
  v_incoming_version    integer;
  v_existing_max_ver    integer;
  v_existing_backfilled boolean;
  v_stages_inserted     integer := 0;
  v_tasks_inserted      integer := 0;
  v_stage               jsonb;
  v_task                jsonb;
  v_prefix              text;
  v_seq                 text;
  v_task_id             text;
  v_task_body           text;
  v_task_key            text;
BEGIN
  -- -----------------------------------------------------------------------
  -- 1. Validate slug.
  -- -----------------------------------------------------------------------
  v_plan_slug := p_bundle->'plan'->>'slug';
  IF v_plan_slug IS NULL OR length(v_plan_slug) = 0 THEN
    RAISE EXCEPTION 'master_plan_bundle_apply: bundle.plan.slug is required';
  END IF;

  v_incoming_version := COALESCE((p_bundle->'plan'->>'version')::int, 1);

  -- -----------------------------------------------------------------------
  -- 2. UPSERT gate check (M#12).
  --    Allowed paths:
  --      a) No existing row at this slug → plain INSERT (first apply).
  --      b) backfilled = true → allow UPSERT (overwrite seeded placeholder).
  --      c) incoming version > existing_max_version → version bump INSERT.
  --    Blocked: incoming version = existing_max_version and backfilled = false
  --             → unique constraint 23505 on INSERT below (same behavior as before).
  -- -----------------------------------------------------------------------
  SELECT MAX(version), bool_or(backfilled)
    INTO v_existing_max_ver, v_existing_backfilled
    FROM ia_master_plans
   WHERE slug = v_plan_slug;

  IF v_existing_max_ver IS NOT NULL THEN
    -- Row(s) exist. Decide path.
    IF v_existing_backfilled AND v_incoming_version = v_existing_max_ver THEN
      -- Backfill UPSERT: remove the seeded placeholder row so INSERT below succeeds.
      DELETE FROM ia_tasks
        WHERE slug = v_plan_slug
          AND backfilled = true;
      DELETE FROM ia_stages
        WHERE slug = v_plan_slug;
      DELETE FROM ia_master_plans
        WHERE slug = v_plan_slug
          AND version = v_existing_max_ver
          AND backfilled = true;
    END IF;
    -- If neither backfilled nor version bump → INSERT below will hit 23505 (intentional).
  END IF;

  -- -----------------------------------------------------------------------
  -- 3. Plan row INSERT.
  -- -----------------------------------------------------------------------
  INSERT INTO ia_master_plans (
    slug,
    title,
    description,
    preamble,
    parent_plan_slug,
    version
  ) VALUES (
    v_plan_slug,
    COALESCE(p_bundle->'plan'->>'title', ''),
    p_bundle->'plan'->>'description',
    p_bundle->'plan'->>'preamble',
    p_bundle->'plan'->>'parent_plan_slug',
    v_incoming_version
  );

  -- -----------------------------------------------------------------------
  -- 4. Stages.
  -- -----------------------------------------------------------------------
  FOR v_stage IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'stages', '[]'::jsonb)) LOOP
    INSERT INTO ia_stages (
      slug,
      stage_id,
      title,
      objective,
      exit_criteria,
      status,
      section_id,
      carcass_role,
      visibility_delta
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

  -- -----------------------------------------------------------------------
  -- 5. Tasks.
  -- -----------------------------------------------------------------------
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
      task_id,
      prefix,
      slug,
      stage_id,
      title,
      status,
      body,
      type,
      priority,
      notes,
      backfilled
    ) VALUES (
      v_task_id,
      v_prefix,
      v_plan_slug,
      v_task->>'stage_id',
      COALESCE(v_task->>'title', ''),
      COALESCE((v_task->>'status')::task_status, 'pending'::task_status),
      v_task_body,
      v_task->>'type',
      v_task->>'priority',
      v_task->>'notes',
      COALESCE((v_task->>'backfilled')::boolean, false)
    );
    v_tasks_inserted := v_tasks_inserted + 1;
  END LOOP;

  RETURN jsonb_build_object(
    'plan_slug',        v_plan_slug,
    'stages_inserted',  v_stages_inserted,
    'tasks_inserted',   v_tasks_inserted
  );
END;
$$;

COMMENT ON FUNCTION master_plan_bundle_apply(jsonb) IS
  'ship-protocol Stage 5 (TECH-14103): atomic plan+stages+tasks insert with UPSERT gate (M#12). Backfilled=true rows replaced on re-apply; version-bump inserts new row; duplicate same-version non-backfilled raises 23505. Returns {plan_slug, stages_inserted, tasks_inserted}.';

COMMENT ON COLUMN ia_tasks.backfilled IS
  'true = body was seeded by backfill classifier (<!-- seeded: backfill_v1 -->). Gates UPSERT path and validate:seeded-task-stale.';

COMMENT ON COLUMN ia_master_plans.backfill_version IS
  'Non-null = plan was seeded by backfill batch (value = backfill label e.g. backfill_v1).';

COMMIT;
