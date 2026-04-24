### Stage 16 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Dry-run Preview


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add `dry_run?: boolean` to every mutation + authorship tool (Stages 4.1, 4.2, 5.1, 5.2). When `dry_run: true`, tool returns `payload.diff` (unified diff format) + `affected_paths` per file without writing. Lets `/closeout`, `/release-rollout`, `/stage-file` preview the full migration before committing. `caller_agent` gate still runs first — unauthorized callers still reject without computing the diff.

**Exit:**

- All 8+ mutation / authorship tools accept `dry_run?: boolean` (default `false`).
- Dry-run response: `{ ok: true, payload: { diff: string, affected_paths: string[], would_write: true } }` — no file write, no index regen spawn.
- `mutation_batch({ dry_run: true })` propagates to each nested op; aggregates into `payload.diffs: { [op_index]: { diff, affected_paths } }`.
- Non-dry-run path unchanged; existing Step 4 tests pass without modification.
- Snapshot tests for diff output fixtures per tool.
- Phase 1 — Dry-run helper + wire into orchestrator mutations.
- Phase 2 — Wire into authorship + authoring + batch + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | Dry-run helper + orchestrator mutation wiring | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/mutation/dry-run.ts` — `computeDiff(path, newContent): string` unified-diff generator (use `diff` npm package or hand-roll via existing text-diff util). Wire dry-run branch into `orchestrator_task_update` + `rollout_tracker_flip` (Stage 4.1 tools): if `dry_run: true` → compute diff from current file content + proposed write, return `{ diff, affected_paths, would_write: true }` under envelope without calling atomic-swap writer. |
| T16.2 | Dry-run for IA authorship tools | _pending_ | _pending_ | Wire dry-run path into `glossary_row_create`, `glossary_row_update`, `spec_section_append`, `rule_create` (Stage 4.2 tools). Each computes proposed new file content, generates diff, returns without writing or spawning index regen. Multi-file ops (e.g. glossary row insert + graph-index regen) return `diff: string` for primary file only + `affected_paths: [primary, index]` listing both; index regen explicitly marked as side-effect in response `meta.side_effects: ["glossary_index_regen"]`. |
| T16.3 | Dry-run for master-plan authoring + mutation_batch | _pending_ | _pending_ | Wire dry-run path into `master_plan_create`, `master_plan_step_append`, `stage_decompose_apply` (Stage 5.1 tools) — each computes serialized output, diffs against current file (empty string for `master_plan_create` new file), returns without writing. Extend `mutation_batch` (Stage 5.2) to propagate `dry_run: true` into each nested op's args; aggregate per-op diffs into `payload.diffs: { [op_index]: { diff, affected_paths, would_write: true } }`; skip `snapshotFiles` + `restoreSnapshots` entirely when dry-run. |
| T16.4 | Dry-run tests + release prep | _pending_ | _pending_ | Snapshot tests in `tools/mcp-ia-server/tests/mutation/dry-run.test.ts`: fixture input → stable diff string per tool (one `ok: true` fixture per mutation + authorship tool). Behavioral tests: dry-run never writes (compare SHA-256 of affected files before + after call); `dry_run: true` + unauthorized `caller_agent` → still `unauthorized_caller` (auth gate runs before dry-run branch); dry-run via `mutation_batch` returns aggregated `payload.diffs` map. Bump `tools/mcp-ia-server/package.json` to `1.1.0`; `CHANGELOG.md` entry `v1.1.0 — Master-plan authoring (create/step_append/stage_decompose_apply) + mutation_batch (all_or_nothing/best_effort) + dry_run across all mutation/authorship tools`. |

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
