### Stage 10 — MCP reverse-lookup tooling / `master_plan_locate` + `master_plan_next_pending`

**Status:** Final

**Backlog state (Stage 4.1):** 5 filed

**Objectives:** Ship the two reverse-lookup tools. `master_plan_locate` reads yaml `parent_plan` + `task_key`, then greps plan for the task row line. `master_plan_next_pending` scans plan task tables + returns the first `_pending_` / Draft row (top-of-table tie-break per S3). Both tools deterministic; both register new in the MCP tool registry; both ship tests against fixture plans.

**Exit:**

- `master_plan_locate` responds `{ plan, step, stage, phase, task_key, row_line, row_raw }` for fixture TECH-283 or any fixture v2 yaml.
- `master_plan_next_pending(plan, stage?)` returns first unfiled / Draft row; deterministic top-of-table; `null` when stage complete.
- Tests cover: locate happy path, locate on yaml-without-`parent_plan` (returns error with reason), next-pending with stage filter, next-pending returning null on fully-filed stage, tie-break determinism (2 pending rows → first wins).
- Both tools registered in `tools/mcp-ia-server/src/index.ts`.
- Phase 1 — `master_plan_locate` implementation + tests.
- Phase 2 — `master_plan_next_pending` implementation + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | Implement `master_plan_locate` | **TECH-413** | Done (archived) | `tools/mcp-ia-server/src/tools/master-plan-locate.ts (new)` — input `{ issue_id: string }`. Load yaml via `parseBacklogIssue`; read `parent_plan` + `task_key`; read plan file; regex-match `^\ | ${task_key} \ | ` to find row line. Return `{ plan, step, stage, phase, task_key, row_line, row_raw }`. Error when yaml missing fields or plan path absent. Register in `tools/mcp-ia-server/src/index.ts`. |
| T10.2 | Fixture + tests for locate | **TECH-414** | Done (archived) | `tools/mcp-ia-server/tests/tools/master-plan-locate.test.ts` — fixture yaml with full v2 fields + fixture plan with matching row. Assert row_line + row_raw. Plus negative cases: yaml w/o `parent_plan` (error), plan-path-not-on-disk (error), task_key not found in plan (error with drift reason). |
| T10.3 | Implement `master_plan_next_pending` | **TECH-415** | Done (archived) | `tools/mcp-ia-server/src/tools/master-plan-next-pending.ts (new)` — input `{ plan: string, stage?: string }`. Read plan file; scan task tables; optionally filter to stage heading (`#### Stage X.Y`); return first row whose Status column matches `_pending_` / `Draft` (top-of-table order). Shape `{ issue_id, task_key, row_line, status } \ | null`. Register in `tools/mcp-ia-server/src/index.ts`. |
| T10.4 | Fixture + tests for next-pending | **TECH-416** | Done (archived) | `tools/mcp-ia-server/tests/tools/master-plan-next-pending.test.ts` — fixture plan with mixed Status column values. Assert: first `_pending_` wins; `Draft` wins over later `_pending_` only if top-of-table; stage filter respected; fully-`Done` stage returns `null`. Deterministic ordering per S3. |
| T10.5 | Tool descriptors + schema cache note | **TECH-417** | Done (archived) | Update both tool descriptors (`master_plan_locate`, `master_plan_next_pending`) with canonical use-case prose. Add schema-cache-restart note to descriptor text + to `docs/mcp-ia-server.md` tool catalog entries (N4). Document `--dry` NOT needed. |
