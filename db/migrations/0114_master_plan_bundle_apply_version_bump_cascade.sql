-- 0114_master_plan_bundle_apply_version_bump_cascade.sql
--
-- Patch master_plan_bundle_apply (mig 0113) version_bump path to cascade-delete
-- existing stages + tasks before re-INSERT. Without this, a re-dispatched
-- /ship-plan with bumped target_version collides on ia_stages_pkey (slug, stage_id)
-- because v1's stages remain and the function unconditionally INSERTs the new
-- stage rows. Mirrors the working backfill_replace pattern (lines 81-95 of mig 0113).
--
-- Behavior change: version_bump now retires old task ids + stage rows under the
-- same slug. Fresh task ids minted on re-INSERT. Old task_id rows are removed —
-- if external references to retired task_ids exist (audit logs, commits), they
-- become dangling but are not FK-enforced (ia_task_commits records sha by
-- task_id text, no FK).
--
-- Schema diff: zero. Function-only patch.
-- Idempotent: CREATE OR REPLACE FUNCTION re-applies cleanly.

BEGIN;

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
    -- Mig 0114: cascade-delete prior version's stages + tasks before re-INSERT.
    -- Mirrors backfill_replace pattern. Prevents ia_stages_pkey collision when
    -- /ship-plan re-dispatches with bumped target_version.
    DELETE FROM ia_tasks
      WHERE slug = v_plan_slug;
    DELETE FROM ia_stages
      WHERE slug = v_plan_slug;

    UPDATE ia_master_plans
       SET version          = v_incoming_version,
           title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           updated_at       = now()
     WHERE slug = v_plan_slug;
  END IF;

  -- 4. Stages. Accept `body` field (mig 0113); falls back to '' default.
  FOR v_stage IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'stages', '[]'::jsonb)) LOOP
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
      COALESCE(v_stage->>'body', '')
    );
    v_stages_inserted := v_stages_inserted + 1;
  END LOOP;

  -- 5. Tasks. Accept BOTH `body` (canonical) and `digest_body` (SKILL alias).
  --    COALESCE prefers `body`; falls back to `digest_body` so SKILL drift
  --    cannot silently drop a §Plan Digest payload again.
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

    -- Dual-key body resolution: body | digest_body | empty fallback.
    v_task_body := COALESCE(
      v_task->>'body',
      v_task->>'digest_body',
      ''
    );
    IF v_task_key IS NOT NULL THEN
      v_task_body := format('<!-- task_key: %s -->%s%s',
                             v_task_key, E'\n', v_task_body);
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
    'plan_slug',        v_plan_slug,
    'apply_path',       v_apply_path,
    'plan_version',     v_incoming_version,
    'stages_inserted',  v_stages_inserted,
    'tasks_inserted',   v_tasks_inserted
  );
END;
$$;

COMMENT ON FUNCTION master_plan_bundle_apply(jsonb) IS
  'Mig 0114: version_bump cascade-deletes prior stages+tasks under slug before re-INSERT (parity with backfill_replace). Mig 0113: stage body field accepted; dual-key task body (body|digest_body). Three apply paths: insert_new, backfill_replace, version_bump.';

COMMIT;
