-- 0136_master_plan_bundle_apply_proof_block.sql
--
-- Lifecycle skills refactor — Phase 1 / weak-spot #11 + #2.
--
-- Two changes:
--
-- 1. Extend master_plan_bundle_apply to accept stages[].red_stage_proof_block
--    jsonb (4 fields: red_test_anchor, target_kind, proof_artifact_id,
--    proof_status). When stage `body` is absent, render server-side via the
--    new format_stage_body(jsonb) helper — moves stage body composition out
--    of the agent prompt.
--
-- 2. Inside the bundle_apply tx, after the plan version row is inserted/updated,
--    call promote_drift_lint_staged(slug, version) so any pre-staged drift-lint
--    findings flip from 'staged' → 'queued' atomically with the plan write.
--    Crash between agent's staged enqueue and bundle_apply leaves rows in
--    'staged' (drainer skips). On retry, agent re-enqueues with same idempotency
--    key (no-op) then re-runs bundle_apply which flips them.
--
-- Idempotent: CREATE OR REPLACE FUNCTION re-applies cleanly.

BEGIN;

-- Helper: render stage body from a 4-field red_stage_proof_block jsonb.
-- Output mirrors the §Red-Stage Proof block shape used by stage-authoring.
CREATE OR REPLACE FUNCTION format_stage_body(p_proof jsonb)
  RETURNS text
  LANGUAGE plpgsql
  IMMUTABLE
AS $$
DECLARE
  v_anchor   text;
  v_kind     text;
  v_artifact text;
  v_status   text;
BEGIN
  IF p_proof IS NULL OR jsonb_typeof(p_proof) <> 'object' THEN
    RETURN '';
  END IF;

  v_anchor   := COALESCE(p_proof->>'red_test_anchor',  'design_only');
  v_kind     := COALESCE(p_proof->>'target_kind',      'not_applicable');
  v_artifact := COALESCE(p_proof->>'proof_artifact_id', '');
  v_status   := COALESCE(p_proof->>'proof_status',     'pending');

  RETURN format(
    E'## §Red-Stage Proof\n\n- red_test_anchor: %s\n- target_kind: %s\n- proof_artifact_id: %s\n- proof_status: %s\n',
    v_anchor, v_kind, v_artifact, v_status
  );
END;
$$;

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
  v_drift_promoted      integer := 0;
  v_stage               jsonb;
  v_task                jsonb;
  v_prefix              text;
  v_seq                 text;
  v_task_id             text;
  v_task_body           text;
  v_task_key            text;
  v_stage_body          text;
  v_proof_block         jsonb;
BEGIN
  -- 1. Validate slug.
  v_plan_slug := p_bundle->'plan'->>'slug';
  IF v_plan_slug IS NULL OR length(v_plan_slug) = 0 THEN
    RAISE EXCEPTION 'master_plan_bundle_apply: bundle.plan.slug is required';
  END IF;

  v_incoming_version := COALESCE((p_bundle->'plan'->>'version')::int, 1);

  -- 2. Decide apply path. Single-row-per-slug invariant.
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

  -- 3. Apply plan row mutation per path.
  IF v_apply_path = 'insert_new' THEN
    INSERT INTO ia_master_plans (
      slug, title, description, preamble, parent_plan_slug, version, backfilled
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
    DELETE FROM ia_tasks  WHERE slug = v_plan_slug AND backfilled = true;
    DELETE FROM ia_stages WHERE slug = v_plan_slug;

    UPDATE ia_master_plans
       SET title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           backfilled       = false,
           updated_at       = now()
     WHERE slug = v_plan_slug;

  ELSE -- version_bump
    DELETE FROM ia_tasks  WHERE slug = v_plan_slug;
    DELETE FROM ia_stages WHERE slug = v_plan_slug;

    UPDATE ia_master_plans
       SET version          = v_incoming_version,
           title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           updated_at       = now()
     WHERE slug = v_plan_slug;
  END IF;

  -- 3.5. Promote any pre-staged drift-lint findings for this (slug, version).
  --      Crash-safe two-phase commit: agent enqueued findings BEFORE this
  --      bundle_apply call with status='staged'; we now flip them to 'queued'
  --      so the cron drainer picks them up.
  v_drift_promoted := promote_drift_lint_staged(v_plan_slug, v_incoming_version);

  -- 4. Stages. Accept body | red_stage_proof_block | empty fallback.
  --    Mig 0136: red_stage_proof_block jsonb rendered server-side via format_stage_body.
  FOR v_stage IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'stages', '[]'::jsonb)) LOOP
    v_proof_block := v_stage->'red_stage_proof_block';
    v_stage_body := COALESCE(
      v_stage->>'body',
      CASE
        WHEN v_proof_block IS NOT NULL AND jsonb_typeof(v_proof_block) = 'object'
          THEN format_stage_body(v_proof_block)
        ELSE ''
      END
    );

    INSERT INTO ia_stages (
      slug, stage_id, title, objective, exit_criteria, status,
      section_id, carcass_role, visibility_delta, body
    ) VALUES (
      v_plan_slug,
      v_stage->>'stage_id',
      v_stage->>'title',
      v_stage->>'objective',
      v_stage->>'exit_criteria',
      COALESCE((v_stage->>'status')::stage_status, 'pending'::stage_status),
      v_stage->>'section_id',
      v_stage->>'carcass_role',
      v_stage->>'visibility_delta',
      v_stage_body
    );
    v_stages_inserted := v_stages_inserted + 1;
  END LOOP;

  -- 5. Tasks. Dual-key body (body | digest_body | empty fallback).
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

    v_task_body := COALESCE(v_task->>'body', v_task->>'digest_body', '');
    IF v_task_key IS NOT NULL THEN
      v_task_body := format('<!-- task_key: %s -->%s%s', v_task_key, E'\n', v_task_body);
    END IF;

    INSERT INTO ia_tasks (
      task_id, prefix, slug, stage_id, title, status, body,
      type, priority, notes, backfilled
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
    'plan_slug',          v_plan_slug,
    'apply_path',         v_apply_path,
    'plan_version',       v_incoming_version,
    'stages_inserted',    v_stages_inserted,
    'tasks_inserted',     v_tasks_inserted,
    'drift_lint_promoted', v_drift_promoted
  );
END;
$$;

COMMENT ON FUNCTION master_plan_bundle_apply(jsonb) IS
  'Mig 0136: server-side stage body render via format_stage_body(red_stage_proof_block); promote_drift_lint_staged() flips staged→queued in same tx. Mig 0114: version_bump cascade-deletes. Mig 0113: stage body. Three apply paths: insert_new, backfill_replace, version_bump.';

COMMIT;
