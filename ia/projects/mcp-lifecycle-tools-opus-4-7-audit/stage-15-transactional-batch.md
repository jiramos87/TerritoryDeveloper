### Stage 15 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Transactional Batch


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement `mutation_batch` tool wrapping N mutation / authorship calls in an atomic boundary. Uses in-memory snapshot-based rollback: reads all affected files before any write, executes ops sequentially, restores all snapshots on any failure (`all_or_nothing`) or continues past failures with partial-result response (`best_effort`).

**Exit:**

- `mutation_batch({ ops, mode: "all_or_nothing" })` — any op fails → all prior ops' file writes reverted from snapshot; envelope `ok: false, error.code: "batch_aborted", error.details: { failed_op_index, rollback_complete: true }`.
- `mutation_batch({ ops, mode: "best_effort" })` — continues past failures; returns `{ results: {[op_index]: ...}, errors: {[op_index]: ...}, meta.partial }`; envelope `ok: true` when ≥1 succeeds.
- Concurrent `mutation_batch` calls coordinate via `flock` on `tools/.mutation-batch.lock` (distinct lockfile per invariants guardrail).
- Callers updated: `stage-file` wraps per-stage file-batch in `all_or_nothing`; `closeout` wraps yaml-archive-move + BACKLOG regen + spec-delete sequence.
- Phase 1 — Snapshot helper + batch infrastructure.
- Phase 2 — Tests + caller adoption.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | File snapshot helper + flock guard | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/mutation/file-snapshot.ts` — `snapshotFiles(paths: string[]): Map<string, Buffer>` reads current content for later restore; `restoreSnapshots(snapshots): void` writes back via atomic temp-file swap per path (preserves mtime semantics). Add `tools/.mutation-batch.lock` sentinel + helper `withBatchLock(fn)` wrapping `flock` for batch lifetime. Batch lockfile distinct from `.id-counter.lock` / `.closeout.lock` / `.materialize-backlog.lock` per invariants guardrail. |
| T15.2 | mutation_batch tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/mutation-batch.ts` via `wrapTool` + `checkCaller`. Input: `ops: Array<{ tool: string, args: object }>`, `mode: "all_or_nothing" | "best_effort"`. Wrap entire body in `withBatchLock`. Static-analyze each op to collect affected paths (per-tool dispatch map: `orchestrator_task_update` → `{slug}-master-plan.md`; `glossary_row_create` → `glossary.md` + index; etc.); `snapshotFiles(paths)`; execute ops sequentially dispatching to existing tool handlers; on failure + `all_or_nothing` → `restoreSnapshots` + `batch_aborted` envelope; on failure + `best_effort` → append to `errors`, continue. |
| T15.3 | Atomic + partial batch tests | _pending_ | _pending_ | Tests in `tools/mcp-ia-server/tests/tools/mutation-batch.test.ts`: `all_or_nothing` happy path (3 ops all succeed, all writes persist); mid-batch failure (op 2 of 3 fails) → rollback verified via SHA-256 equality between pre-batch snapshot and post-rollback files; `best_effort` returns `{results: {0:..., 2:...}, errors: {1:...}}` + `meta.partial: {succeeded:2, failed:1}`; flock contention — two concurrent batches serialize deterministically; unauthorized op `caller_agent` → `unauthorized_caller` from inner op (batch still rolls back under `all_or_nothing`). |
| T15.4 | Caller skill adoption | _pending_ | _pending_ | Update `ia/skills/stage-file-apply/SKILL.md` (post-M6 name; was `stage-file`) to wrap per-stage file-creation ops (N × `backlog_record_create` + N × `spec_create` + 1 × `orchestrator_task_update` + 1 × BACKLOG regen) in `mutation_batch(mode: "all_or_nothing")` — prevents half-filed stages. Update `ia/skills/plan-applier/SKILL.md` Mode stage-closeout (Stage-scoped `/closeout` pair tail; absorbs retired `project-spec-close`) closeout sequence (yaml archive move + BACKLOG row delete + spec-file delete + orchestrator status flip) to batch. Both skills document bash fallback path when MCP unavailable. |

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
