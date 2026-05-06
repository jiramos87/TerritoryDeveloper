-- 0078_master_plan_bundle_apply_digest_body_alias.sql
--
-- Defensive patch on master_plan_bundle_apply (mig 0077): accept both
-- `task.body` (canonical, matches test fixture + ia_tasks.body column +
-- task_spec_body MCP) and `task.digest_body` (alias used by ship-plan
-- SKILL §Phase 7 in earlier drafts).
--
-- Why: the SKILL emitted `digest_body`, the function only read `body` →
-- silent drop of the entire §Plan Digest body for every task in the bundle.
-- 9 tasks shipped with empty bodies on game-ui-catalog-bake v2 before the
-- mismatch was caught. Adding COALESCE prevents recurrence on future
-- field-name drift.
--
-- Idempotent: CREATE OR REPLACE FUNCTION — re-run produces zero schema diff.

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
    UPDATE ia_master_plans
       SET version          = v_incoming_version,
           title            = COALESCE(p_bundle->'plan'->>'title', title),
           description      = COALESCE(p_bundle->'plan'->>'description', description),
           preamble         = COALESCE(p_bundle->'plan'->>'preamble', preamble),
           parent_plan_slug = COALESCE(p_bundle->'plan'->>'parent_plan_slug', parent_plan_slug),
           updated_at       = now()
     WHERE slug = v_plan_slug;
  END IF;

  -- 4. Stages.
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
  'Mig 0078: dual-key body resolution (body|digest_body) — defensive against ship-plan SKILL field-name drift. Three apply paths: insert_new, backfill_replace, version_bump. Returns {plan_slug, apply_path, plan_version, stages_inserted, tasks_inserted}.';

COMMIT;
