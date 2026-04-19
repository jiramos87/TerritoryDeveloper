---
purpose: "TECH-451 — Canary migrate-master-plans on blip-master-plan + parser fix."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/lifecycle-refactor-master-plan.md
task_key: T2.1.2
---
# TECH-451 — Canary migrate-master-plans.ts on blip-master-plan + parser edge-case fix

> **Issue:** [TECH-451](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Run TECH-450 transform script against one low-risk fully-closed master plan (`blip-master-plan.md`). Diff emitted output vs snapshot. Surface and fix parser edge cases before the batch run touches 15 other plans. Gate that the transform is safe to batch.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Validate transform script output on a known-safe canary plan.
2. Catch parser edge cases (nested code blocks, inline Phase bullets, task-id variants, missing Phase sections) on a single plan instead of mid-batch.
3. Commit any parser fix back into `tools/scripts/migrate-master-plans.ts` before TECH-452 runs.

### 2.2 Non-Goals

1. Do NOT migrate any other plan in this issue.
2. Do NOT regenerate BACKLOG views.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Migration operator | Verify script on 1 plan before 15 | Diff matches expectations; validate:all green |

## 4. Current State

### 4.1 Domain behavior

`blip-master-plan.md` is fully closed (all Steps Final). Safe canary — no In Progress tasks at risk.

### 4.2 Systems map

- `tools/scripts/migrate-master-plans.ts` (target — TECH-450 output).
- `ia/projects/blip-master-plan.md` (current file — overwrite target).
- `ia/state/pre-refactor-snapshot/ia/projects/blip-master-plan.md` (read input).
- `ia/state/lifecycle-refactor-migration.json` (per-file flip on canary entry).

## 7. Implementation Plan

Scope: ONE plan (`blip-master-plan.md`). Bulk run = TECH-453. Second canary = TECH-452.

### Phase 1 — Dry-run canary

- [ ] Run `npx tsx tools/scripts/migrate-master-plans.ts --only blip-master-plan.md --dry-run > /tmp/blip-migrated.md`.
- [ ] Script exits 0; emits non-empty output; no stderr warnings.
- [ ] Migration JSON untouched (verify `status: pending` still).
- [ ] Disk untouched (`git status ia/projects/blip-master-plan.md` clean).

### Phase 2 — Diff review

- [ ] `diff ia/state/pre-refactor-snapshot/ia/projects/blip-master-plan.md /tmp/blip-migrated.md > /tmp/blip.diff`.
- [ ] Spot-check: `## Steps` → `## Stages`; every `### Step N` + `### Stage N.M` pair collapsed to single `### Stage {seq} — {StepName} / {StageName}`.
- [ ] Task-table header lost `Phase` column; row order + Issue ids + Status cells verbatim.
- [ ] `**Phases:**` block + `##### Phase N` h5 sections absent from output; their bullets folded into parent Stage `**Exit:**`.
- [ ] Task ids remapped `T{N}.{M}.{k}` → `T{stageSeq}.{k}`; sequence monotonic from 1.
- [ ] Record ambiguities (blank Exit merges, empty Phase blocks, inline Phase bullets) in §Decision Log below.

### Phase 3 — Parser fix loop (only if Phase 2 surfaces defects)

- [ ] Categorize defect: (a) nested code fence inside Objectives/Exit, (b) inline Phase bullet not tied to h5, (c) missing Phase section, (d) task-id variant (e.g. `T1.1.1a`), (e) Step w/o any Stage child, (f) other.
- [ ] Patch `tools/scripts/migrate-master-plans.ts` with minimal fix + inline comment tagging the category.
- [ ] Add/extend unit-level guard (self-check in script: assert input line count vs output section count invariant).
- [ ] Commit: `fix(lifecycle): migrate-master-plans parser — {category}`.
- [ ] Goto Phase 1 (re-run dry-run). Cap: 3 iterations → escalate to user.

### Phase 4 — Wet run + state flip

- [ ] Drop `--dry-run`: `npx tsx tools/scripts/migrate-master-plans.ts --only blip-master-plan.md`.
- [ ] Verify atomic overwrite (`ia/projects/blip-master-plan.md` mtime advanced; no `.tmp` leftovers).
- [ ] Re-run SAME command (idempotence check): script SHOULD skip because migration JSON entry = `done`; emit "skipped: done" line; exit 0.
- [ ] `ia/state/lifecycle-refactor-migration.json` → `files.M2[blip-master-plan.md].status = "done"`.

### Phase 5 — Validate + commit

- [ ] `npm run validate:all` exits 0. Capture failure category if non-zero; fix in script (back to Phase 3) or raise scope question.
- [ ] `git diff --stat` shows: plan file rewritten, migration JSON single-entry flip, optionally script patch.
- [ ] Commit: `refactor(lifecycle): migrate blip-master-plan to Stage/Task schema (T2.1.2)`.

### Rollback

- [ ] Failure exit path: `cp ia/state/pre-refactor-snapshot/ia/projects/blip-master-plan.md ia/projects/blip-master-plan.md` + revert migration JSON entry to `pending`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Pass criterion |
|-------------------|------------|---------------------|----------------|
| Dry-run exits clean | CLI | `npx tsx ... --dry-run` | exit 0, stdout non-empty, stderr empty |
| Schema transform correct | Diff | `diff snapshot /tmp/blip-migrated.md` | `## Steps` → `## Stages`; no `Phase` column; no `##### Phase` h5; no `**Phases:**` block |
| Issue ids preserved | Grep | `rg '(BUG\|FEAT\|TECH\|ART\|AUDIO)-\d+' /tmp/blip-migrated.md` | Set equals snapshot set |
| Task-id remap | Grep | `rg '^\| T\d+\.\d+ '` on output vs `rg '^\| T\d+\.\d+\.\d+ '` on snapshot | Output uses 2-segment ids; count matches |
| Exit bullets merged | Eye | `/tmp/blip.diff` | Old Phase bullets appear under parent Stage `**Exit:**` |
| Idempotence | CLI | 2nd wet run | 2nd invocation skips; JSON unchanged |
| Post-migration validate | Node | `npm run validate:all` | Exit 0 |
| State flipped | JSON | `jq '.files.M2[] \| select(.path=="ia/projects/blip-master-plan.md")' ia/state/lifecycle-refactor-migration.json` | `.status == "done"` |

## 8. Acceptance Criteria

- [ ] `blip-master-plan.md` migrated; Issue ids preserved verbatim; `Phase` column absent; `**Phases:**` + h5 Phase blocks folded into parent Stage Exit; task ids 2-segment.
- [ ] Script patch (if any) committed w/ category-tagged message.
- [ ] Idempotent re-run skips cleanly.
- [ ] `npm run validate:all` exits 0.
- [ ] `ia/state/lifecycle-refactor-migration.json` canary entry flipped `pending → done`.
- [ ] Rollback path exercised once (manual dry-revert + re-apply) OR documented as unverified in §Decision Log.

## Open Questions

1. ~~Parser-fix strictness~~ — **Resolved 2026-04-19:** semantic defects only (lost ids, broken table, dropped Exit). Cosmetic whitespace drift accepted w/ note in §Decision Log.
2. ~~Idempotence — skip vs re-emit on dry-run~~ — **Resolved 2026-04-19:** dry-run ALWAYS re-emits (ignores `done`); wet run skips `done`.
3. ~~Rollback drill live vs doc~~ — **Resolved 2026-04-19:** documented-only — avoid double-mutation; rollback path validated by inspection of snapshot existence.

## §Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-19 | Canary target = `blip-master-plan.md` | Fully closed; zero In Progress tasks at risk. |
| 2026-04-19 | Parser-fix iteration cap = 3 | Beyond 3 loops → design defect, escalate rather than chase edge cases. |
| 2026-04-19 | validate:all exits 1 — pre-existing; out of scope | All IA/compute/fixture/test suites pass. Failure = ESLint in `web/design-refs/step-8-console/src/*.jsx` (commit 5edd2e4, pre-dates TECH-451). Phase column absent, ids preserved, 2-seg task ids confirmed. validate:web exits 1 on baseline branch too. |
| 2026-04-19 | Rollback — documented-only | Snapshot exists at `ia/state/pre-refactor-snapshot/ia/projects/blip-master-plan.md`; restore via `cp` + revert JSON entry. Not drilled per Open Question resolution. |

## §Cross-refs

- Parent: `ia/projects/lifecycle-refactor-master-plan.md` T2.1.2.
- Sibling canary: TECH-452 (second plan, post-fix).
- Bulk run: TECH-453 (all remaining M2 entries).
- Depends on: TECH-450 (script existence).
- State file: `ia/state/lifecycle-refactor-migration.json` (M2 entries).
- Snapshot root: `ia/state/pre-refactor-snapshot/ia/projects/`.

### Canonical-term audit

- "master plan" / "orchestrator" / "task" — not in glossary; project-local terms, usage OK per `ia/rules/project-hierarchy.md`.
- "Stage" — glossary-canonical (execution unit w/ Exit + Tasks). Used consistently.
- "Phase" — glossary: **Retired — use Stage** (post-2026-04 collapse). This spec uses "Phase" ONLY to reference the OLD pre-refactor schema being transformed AWAY from; new `### Phase N` headings under §7 label implementation steps of THIS spec, not the retired hierarchy level. OK per context.

---

## §Project-New Plan

_pending — populated by `/project-new` planner pass._

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## §Closeout Plan

_pending — populated by `/audit`._
