### Stage 10 — Mutations + Authorship + Bridge + Journal Lifecycle / Orchestrator + Rollout Mutations


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement two mutation tools that replace fragile regex-based `Edit` calls in lifecycle skills: `orchestrator_task_update` for task-table + phase-checkbox + status-pointer edits, and `rollout_tracker_flip` for rollout lifecycle cell advances.

**Exit:**

- `orchestrator_task_update({ slug, issue_id: "TECH-301", patch: { status: "Draft" }, caller_agent: "stage-file" })` flips task-table row; writes back atomically.
- `rollout_tracker_flip` advances cell; preserves glyph vocabulary exactly.
- Unauthorized caller → `unauthorized_caller` from `checkCaller`.
- File outside `ia/projects/` → `invalid_input` (invariant #12).
- Tests green.
- Phase 1 — Mutation tool authoring.
- Phase 2 — Tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | orchestrator_task_update | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/orchestrator-task-update.ts` via `wrapTool` + `checkCaller`: resolve `ia/projects/{slug}-master-plan.md` (validate path per invariant #12); load via orchestrator-parser; apply `patch` — `status` flips task-table Status cell; `phase_checkbox` toggles `- [ ]`/`- [x]`; `top_status_pointer` rewrites `**Status:**` header line; write back via atomic temp-file swap. Never touch `id:` field. |
| T10.2 | rollout_tracker_flip | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/rollout-tracker-flip.ts` via `wrapTool` + `checkCaller` (allowlist: `release-rollout-track`, `release-rollout`): resolve `ia/projects/{slug}-rollout-tracker.md`; find row by `row` slug; find column by `cell` label `(a)`–`(g)`; replace value; preserve glyph vocabulary `❓`/`⚠️`/`🟢`/`✅`/`🚀`/`—` — validate `value` is one of these glyphs or raises `invalid_input`. |
| T10.3 | orchestrator mutation tests | _pending_ | _pending_ | Tests for `orchestrator_task_update`: status flip `_pending_ → Draft` in task table; phase checkbox toggle; top-status-pointer rewrite; unauthorized caller → `unauthorized_caller`; file outside `ia/projects/` → `invalid_input`; issue_id not found in table → `invalid_input`; no `id:` field mutation. |
| T10.4 | rollout flip tests | _pending_ | _pending_ | Tests for `rollout_tracker_flip`: cell advance happy path with snapshot of written markdown; glyph-preservation: invalid glyph → `invalid_input`; valid glyph set passes; unauthorized caller → `unauthorized_caller`; cell label not found in row → `invalid_input`; row slug not found in tracker → `invalid_input`. |

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
