-- 0077_ia_master_plans_backfilled_and_upsert.sql
--
-- Bug fix for migration 0073 (TECH-14103 backfill_seeded_digests):
--   1. Migration 0073 added `ia_tasks.backfilled` and `ia_master_plans.backfill_version`
--      but FORGOT `ia_master_plans.backfilled` boolean column. The bundle-apply
--      function references it via `bool_or(backfilled)` → 42703.
--   2. The original UPSERT path called DELETE + INSERT on the plan row, which
--      contradicts the slug-keyed primary key (`btree(slug)` — single row per
--      slug, 13 FKs cascade). Version bumps via INSERT new row are impossible.
--
-- Fix:
--   - ADD COLUMN IF NOT EXISTS backfilled boolean (default false) on ia_master_plans.
--   - CREATE OR REPLACE master_plan_bundle_apply with three explicit apply paths:
--       (a) No row at slug → plain INSERT.
--       (b) Backfilled placeholder at same version → wipe children + UPDATE plan in-place.
--       (c) version > existing → UPDATE plan row in-place + INSERT new stages/tasks
--           alongside existing ones (composite PK on stages/tasks supports this).
--       (d) Otherwise → RAISE EXCEPTION (duplicate apply / stale version).
--
-- Idempotent: re-running this migration produces zero schema diff.

BEGIN;

-- -------------------------------------------------------------------------
-- 1. Missing column from 0073.
-- -------------------------------------------------------------------------
ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS backfilled boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN ia_master_plans.backfilled IS
  'true = plan row was seeded by backfill classifier; gates UPSERT path in master_plan_bundle_apply. Cleared on first non-backfilled apply.';

-- -------------------------------------------------------------------------
-- 2. Replace master_plan_bundle_apply with single-row-per-slug semantics.
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION master_plan_bundle_apply(p_bundle jsonb)
  RETURNS jsonb
  LANGUAGE plpgsql
AS $$
DECLARE
  v_plan_slug           text;
  v_incoming_version    integer;
  v_existing_version    integer;
  v_existing_backfilled boolean;
  v_apply_path          text;
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
  -- 2. Decide apply path. Single-row-per-slug invariant — never INSERT a
  --    second row at the same slug.
  -- -----------------------------------------------------------------------
  SELECT version, backfilled
    INTO v_existing_version, v_existing_backfilled
    FROM ia_master_plans
   WHERE slug = v_plan_slug;

  IF NOT FOUND THEN
    v_apply_path := 'insert_new';
  ELSIF v_existing_backfilled AND v_incoming_version = v_existing_version THEN
    v_apply_path := 'backfill_replace';
  ELSIF v_incoming_version > v_existing_version THEN
    v_apply_path := 'version_bump';
  ELSE
    RAISE EXCEPTION
      'master_plan_bundle_apply: cannot apply slug=% incoming_version=% existing_version=% backfilled=% (must be > existing or backfilled placeholder)',
      v_plan_slug, v_incoming_version, v_existing_version, v_existing_backfilled;
  END IF;

  -- -----------------------------------------------------------------------
  -- 3. Apply plan row mutation per path.
  -- -----------------------------------------------------------------------
  IF v_apply_path = 'insert_new' THEN
    INSERT INTO ia_master_plans (
      slug,
      title,
      description,
      preamble,
      parent_plan_slug,
      version,
      backfilled
    ) VALUES (
      v_plan_slug,
      COALESCE(p_bundle->'plan'->>'title', ''),
      p_bundle->'plan'->>'description',
      p_bundle->'plan'->>'preamble',
      p_bundle->'plan'->>'parent_plan_slug',
      v_incoming_version,
      COALESCE((p_bundle->'plan'->>'backfilled')::boolean, false)
    );

  ELSIF v_apply_path = 'backfill_replace' THEN
    -- Wipe seeded children, then UPDATE plan row in-place (preserve PK + FKs).
    DELETE FROM ia_tasks
      WHERE slug = v_plan_slug
        AND backfilled = true;
    DELETE FROM ia_stages
      WHERE slug = v_plan_slug;

    UPDATE ia_master_plans
       SET title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           backfilled       = false,
           updated_at       = now()
     WHERE slug = v_plan_slug;

  ELSE -- version_bump
    -- Bump plan row in-place. Existing stages/tasks (lower stage_ids) survive;
    -- new stages/tasks land alongside via composite PK on (slug, stage_id) /
    -- task_id. Caller is responsible for not colliding existing stage ids.
    UPDATE ia_master_plans
       SET version          = v_incoming_version,
           title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           updated_at       = now()
     WHERE slug = v_plan_slug;
  END IF;

  -- -----------------------------------------------------------------------
  -- 4. Stages — INSERT each. Composite PK (slug, stage_id) handles
  --    coexistence with prior stages on version_bump path.
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
    'apply_path',       v_apply_path,
    'plan_version',     v_incoming_version,
    'stages_inserted',  v_stages_inserted,
    'tasks_inserted',   v_tasks_inserted
  );
END;
$$;

COMMENT ON FUNCTION master_plan_bundle_apply(jsonb) IS
  'Bug-fix for 0073 (mig 0077): single-row-per-slug invariant. Three apply paths — insert_new (no row), backfill_replace (wipe + UPDATE in-place), version_bump (UPDATE plan + INSERT new stages/tasks alongside existing). Duplicate same-version non-backfilled raises. Returns {plan_slug, apply_path, plan_version, stages_inserted, tasks_inserted}.';

COMMIT;
