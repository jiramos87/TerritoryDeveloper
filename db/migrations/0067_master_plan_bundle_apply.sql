-- 0067_master_plan_bundle_apply.sql
--
-- ship-protocol Stage 1.0 / TECH-12630 — atomic bundle-apply Postgres
-- function. Inserts one master plan + N stages + M tasks from a single
-- jsonb payload in one tx; any insert failure raises and rolls the entire
-- bundle back (no partial state ever persists).
--
-- Bundle JSONB shape:
--   {
--     "plan":  {"slug": text, "title": text, "description"?: text, "preamble"?: text,
--               "parent_plan_slug"?: text, "version"?: int},
--     "stages": [{"stage_id": text, "title"?: text, "objective"?: text,
--                 "exit_criteria"?: text, "status"?: stage_status,
--                 "section_id"?: text, "carcass_role"?: text, ...}],
--     "tasks":  [{"task_key": text, "stage_id": text, "prefix": text,
--                 "title": text, "body"?: text, "status"?: task_status, ...}]
--   }
--
-- Return shape: jsonb {plan_slug, stages_inserted, tasks_inserted}.
--
-- Notes:
--   - task_id is generated as `<prefix>-<seq>` from per-prefix sequences
--     (tech_id_seq / feat_id_seq / ...) — same pattern mutateTaskInsert uses.
--   - task_key (e.g. T1.0.1) is human-facing only; stored in body for now to
--     avoid schema churn outside Stage 1.0 scope.
--   - Function is plpgsql LANGUAGE — implicit tx frame on RAISE rolls back.
--   - On duplicate plan slug → unique-constraint pass-through (SQLSTATE 23505).
--   - Idempotent migration: CREATE OR REPLACE FUNCTION re-applies cleanly.

BEGIN;

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
  -- -----------------------------------------------------------------------
  -- 1. Plan row.
  -- -----------------------------------------------------------------------
  v_plan_slug := p_bundle->'plan'->>'slug';
  IF v_plan_slug IS NULL OR length(v_plan_slug) = 0 THEN
    RAISE EXCEPTION 'master_plan_bundle_apply: bundle.plan.slug is required';
  END IF;

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
    COALESCE((p_bundle->'plan'->>'version')::int, 1)
  );

  -- -----------------------------------------------------------------------
  -- 2. Stages.
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
  -- 3. Tasks. task_id minted via per-prefix sequence.
  -- -----------------------------------------------------------------------
  FOR v_task IN SELECT * FROM jsonb_array_elements(COALESCE(p_bundle->'tasks', '[]'::jsonb)) LOOP
    v_prefix := UPPER(COALESCE(v_task->>'prefix', 'TECH'));
    v_task_key := v_task->>'task_key';

    -- Map prefix → sequence name (same canonical mapping as mutations.ts).
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

    -- Compose body — embed task_key in heading so downstream tools can recover
    -- the human-facing T-coordinate without a schema add.
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
      notes
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
      v_task->>'notes'
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
  'ship-protocol Stage 1.0 (TECH-12630): atomic plan+stages+tasks insert from a single jsonb bundle. Returns {plan_slug, stages_inserted, tasks_inserted}. Any constraint failure rolls back the whole bundle.';

COMMIT;
