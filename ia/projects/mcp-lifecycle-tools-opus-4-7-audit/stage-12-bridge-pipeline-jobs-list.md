### Stage 12 — Mutations + Authorship + Bridge + Journal Lifecycle / Bridge Pipeline + Jobs List


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement `unity_bridge_pipeline` hybrid tool (sync ≤30s, auto-async above ceiling) and `unity_bridge_jobs_list` query surface. Wire timeout auto-attach in existing `unity_bridge_command`.

**Exit:**

- `unity_bridge_pipeline([enter_play_mode, get_compilation_status, exit_play_mode])` completes in <30s → `{ results, lease_released: true, elapsed_ms }`.
- Same pipeline >30s → `{ job_id, status: "running", poll_with: "unity_bridge_jobs_list" }`.
- Timeout on kind 2 of 3 → `{ ok: false, error: { code: "timeout", details: { completed_kinds, last_output_preview, command_id } } }`.
- `unity_bridge_jobs_list` queries `agent_bridge_job` table; `db_unconfigured` → graceful envelope error.
- Tests green.
- Phase 1 — Pipeline + jobs-list tools.
- Phase 2 — Timeout auto-attach + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | unity_bridge_pipeline | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/unity-bridge-pipeline.ts` via `wrapTool`: accept `commands: CommandKind[]` + optional `caller_agent`; acquire lease internally (calls `unity_bridge_lease` acquire); execute kinds sequentially with `UNITY_BRIDGE_PIPELINE_CEILING_MS` wall-clock budget; on completion ≤ ceiling → release lease, return `{ results, lease_released: true, elapsed_ms }`; on ceiling exceeded → detach to async job, return `{ job_id, status: "running", current_kind, poll_with, lease_held_by: caller_agent }`. |
| T12.2 | unity_bridge_jobs_list | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/unity-bridge-jobs-list.ts` via `wrapTool`: `filter?: { status?, caller_agent?, since? }`; query `agent_bridge_job` Postgres table; `db_unconfigured` → `{ ok: false, error: { code: "db_unconfigured" } }`; return `{ jobs: [{job_id, caller_agent, started_at, status, last_output_preview}] }` filtered by provided params; empty result → `{ jobs: [] }`, `ok: true`. |
| T12.3 | Timeout auto-attach | _pending_ | _pending_ | Extend `unity-bridge-command.ts` timeout error path: before `wrapTool` surfaces the `timeout` error, inject `details: { command_id, last_output_preview, completed_kinds: string[] }` — where `completed_kinds` = list of kinds that completed before timeout; `last_output_preview` = last N chars of bridge job output column. Update snapshot test in `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`. |
| T12.4 | Bridge + jobs tests | _pending_ | _pending_ | Tests for `unity_bridge_pipeline`: sync-complete path (3 mock kinds < 30s ceiling); async-convert path (> 30s ceiling mock → `{ job_id }`); timeout on kind 2 → `error.details.completed_kinds` contains completed kinds only. Tests for `unity_bridge_jobs_list`: filter by `status: "running"`; empty result; `db_unconfigured`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
