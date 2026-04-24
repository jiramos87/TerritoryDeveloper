### Stage 8 — Composite Bundles + Graph Freshness / `orchestrator_snapshot`


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement a Markdown parser for master-plan task tables + status pointers, and the `orchestrator_snapshot` tool that surfaces current orchestrator state in one call. Replaces the Glob + Grep + Read chain agents currently use to inspect master plans.

**Exit:**

- `orchestrator_snapshot({ slug: "mcp-lifecycle-tools-opus-4-7-audit" })` returns `{ status_pointer, stages: [{id, title, phases, tasks}], rollout_tracker_row? }`.
- Slug pointing outside `ia/projects/` → `{ ok: false, error: { code: "invalid_input" } }` (invariant #12).
- Rollout-tracker sibling absent → `ok: true`, `rollout_tracker_row: null`.
- `- [ ]` / `- [x]` phase checkboxes parsed; task rows with `_pending_` preserved.
- Tests green.
- Phase 1 — Parser + snapshot tool.
- Phase 2 — Tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Orchestrator parser | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/parser/orchestrator-parser.ts`: parses `ia/projects/*master-plan*.md` → `{ status_pointer: string, stages: [{id, title, status, phases: [{label, checked}], tasks: [{id, name, phase, issue, status, intent}]}] }`. Validates file path starts with `ia/projects/` (invariant #12 guard). Parse task-table rows: pipe-separated markdown table, extract Issue + Status columns. |
| T8.2 | orchestrator_snapshot tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/orchestrator-snapshot.ts` via `wrapTool`: resolve `ia/projects/{slug}-master-plan.md`, call parser (T3.2.1); Glob `ia/projects/{slug}-*rollout-tracker.md` for optional sibling; if found parse rollout-tracker row into `rollout_tracker_row?`; return full snapshot under envelope; rollout absent → `rollout_tracker_row: null`, `meta.partial` unchanged. |
| T8.3 | Snapshot tool tests | _pending_ | _pending_ | Tests for `orchestrator_snapshot`: multi-stage master-plan with mixed `_pending_`/`Draft`/`Done` task rows parsed; file outside `ia/projects/` → `invalid_input`; rollout sibling absent → `ok: true`, `rollout_tracker_row: null`; slug not found → `issue_not_found`. |
| T8.4 | Parser unit tests | _pending_ | _pending_ | Tests for `orchestrator-parser.ts`: partial stage table (some `_pending_`) → `_pending_` preserved in output; phase checkbox `- [ ]` → `checked: false`, `- [x]` → `checked: true`; task row without Issue id → `issue: "_pending_"`; status pointer regex: `**Status:** In Progress — Stage 1.1` → `{ pointer: "In Progress — Stage 1.1" }`. |

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
