-- 0046_ia_recipe_runs.sql
--
-- DEC-A19 Phase B (recipe-runner MVP).
--
-- ia_recipe_runs — per-step audit + resume cursor for recipe runs orchestrated
-- by tools/recipe-engine. One row per step execution. Captures input/output
-- hashes (sha256 truncated to 16 hex) so reruns can be diffed without storing
-- raw payloads. Failure rows carry `error_code`; resume reads the latest
-- non-`ok` row per (run_id, recipe_slug) when re-entering a run.
--
-- Phase B writers: tools/recipe-engine/src/audit.ts (DB when DATABASE_URL set,
-- JSONL fallback under ia/state/recipe-runs/{run_id}/audit.jsonl otherwise).
--
-- No FK to ia_master_plans / ia_stages / ia_tasks — recipes can target any
-- surface (or none); link through `recipe_slug` + step args instead.

CREATE TABLE IF NOT EXISTS ia_recipe_runs (
    id              BIGSERIAL PRIMARY KEY,
    run_id          TEXT NOT NULL,
    recipe_slug     TEXT NOT NULL,
    step_id         TEXT NOT NULL,
    parent_path     TEXT NOT NULL DEFAULT '',
    kind            TEXT NOT NULL,
    status          TEXT NOT NULL,
    input_hash      TEXT NOT NULL,
    output_hash     TEXT NOT NULL,
    started_at      TIMESTAMPTZ NOT NULL,
    finished_at     TIMESTAMPTZ NOT NULL,
    error_code      TEXT,
    CONSTRAINT ia_recipe_runs_kind_chk
        CHECK (kind IN ('mcp', 'bash', 'sql', 'seam', 'gate', 'flow')),
    CONSTRAINT ia_recipe_runs_status_chk
        CHECK (status IN ('ok', 'failed', 'skipped'))
);

CREATE INDEX IF NOT EXISTS ia_recipe_runs_run_id_idx
    ON ia_recipe_runs (run_id, started_at);
CREATE INDEX IF NOT EXISTS ia_recipe_runs_recipe_slug_idx
    ON ia_recipe_runs (recipe_slug, finished_at DESC);
CREATE INDEX IF NOT EXISTS ia_recipe_runs_failures_idx
    ON ia_recipe_runs (run_id, error_code)
    WHERE status = 'failed';
