-- IA dev system DB-primary refactor — step 9.6.5 (runtime_state DB row).
-- Source: docs/ia-dev-db-refactor-implementation.md §Step 9.6.5
--
-- Replaces ia/state/runtime-state.json (per-clone gitignored file +
-- runtime-state-write.sh flock script) with a single-row Postgres table.
-- Schema mirrors the JSON keys verbatim so MCP `runtime_state` tool can
-- swap storage backend without changing its read/write surface.
--
-- Singleton enforced via PRIMARY KEY on a fixed `id = 1` integer.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_runtime_state (
  id smallint PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  last_verify_exit_code integer,
  last_bridge_preflight_exit_code integer,
  queued_test_scenario_id text,
  updated_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO ia_runtime_state (id) VALUES (1)
ON CONFLICT (id) DO NOTHING;

COMMIT;
