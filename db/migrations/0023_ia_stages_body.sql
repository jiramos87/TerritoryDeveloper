-- IA dev system DB-primary refactor — Stage body blob.
--
-- Adds a `body` text blob column to `ia_stages`, mirroring the existing
-- patterns on `ia_master_plans.preamble` and `ia_tasks.body`. Holds the
-- full canonical Stage block markdown per `docs/MASTER-PLAN-STRUCTURE.md`:
-- Notes / Backlog state / Art / Relevant surfaces / 5-col Task table /
-- §Stage File Plan / §Plan Fix / §Stage Audit / §Stage Closeout Plan.
--
-- Why: the FS-to-DB refactor (0020 + 9.6.x) preserved structured fields
-- (title / objective / exit_criteria / status) but dropped the freeform
-- body sections. Plan-authoring skills (master-plan-new, stage-decompose,
-- master-plan-extend) cannot persist their full output without it.
--
-- Companion MCP tool: `stage_body_write({slug, stage_id, body})` — mirrors
-- `master_plan_preamble_write`. Renderer `renderStageBlock()` prefers the
-- body blob when non-empty; falls back to the structured-field synthesis
-- shape for legacy rows.

BEGIN;

ALTER TABLE ia_stages
  ADD COLUMN IF NOT EXISTS body text NOT NULL DEFAULT '';

COMMIT;
