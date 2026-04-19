---
purpose: "TECH-491 — Merge branch."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.3"
---
# TECH-491 — Merge branch

> **Issue:** [TECH-491](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Land lifecycle-refactor branch on main. Preserve migration history. Re-materialize BACKLOG views if needed.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Merge commit on main (no squash).
2. M8 flip to done.

### 2.2 Non-Goals (Out of Scope)

1. MCP restart (T9.2 scope).
2. Freeze-note removal (T9.4 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want the refactor branch merged with history preserved so that migration decisions are traceable. | Merge commit on main; no squash. |

## 4. Current State

### 4.1 Domain behavior

`feature/lifecycle-collapse-cognitive-split` carries all refactor work; main has not received it yet.

### 4.2 Systems map

- `BACKLOG.md` / `BACKLOG-ARCHIVE.md` — generated views, may conflict.
- `tools/scripts/materialize-backlog.sh` — regen tool.
- `ia/state/lifecycle-refactor-migration.json` — M8 row.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

`git merge --no-ff feature/lifecycle-collapse-cognitive-split`. Resolve BACKLOG conflicts by re-running materialize. Flip M8 done.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Standard git merge; conflict resolution limited to generated files only (materialize re-runs as authoritative source).

### 5.3 Method / algorithm notes (optional)

None.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | No squash — preserve merge history | Migration traceability required | Squash — rejected, loses commit context |

## 7. Implementation Plan

### Phase 1 — Merge

- [ ] Confirm M8.gate row present (T9.1 prerequisite).
- [ ] `git merge --no-ff feature/lifecycle-collapse-cognitive-split`.
- [ ] Resolve BACKLOG.md / BACKLOG-ARCHIVE.md conflicts via `bash tools/scripts/materialize-backlog.sh`.

### Phase 2 — Post-merge

- [ ] Flip migration JSON M8 `done` with timestamp.
- [ ] Run `npm run validate:all` on main; confirm green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Merge clean + validate green | Node | `npm run validate:all` | Run on main post-merge |

## 8. Acceptance Criteria

- [ ] Branch merged to main with merge commit (no squash).
- [ ] BACKLOG.md + BACKLOG-ARCHIVE.md re-materialized post-merge if conflicts surfaced.
- [ ] Migration JSON M8 flipped to `done` with timestamp.
- [ ] `npm run validate:all` green on main post-merge.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- None yet.

## §Plan Author

### §Audit Notes

- Risk: `BACKLOG.md` / `BACKLOG-ARCHIVE.md` conflict resolution by hand → yaml-source drift vs generated view. Mitigation: conflicts resolved ONLY by re-running `bash tools/scripts/materialize-backlog.sh`; never hand-edit the generated views (authoritative source is `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml`).
- Risk: squash-merge executed by mistake (loses migration commit history). Mitigation: explicit `--no-ff` flag; refuse any `--squash` variant in Phase 1.
- Risk: M8.gate row missing → merge fires prematurely. Mitigation: Phase 1 first step asserts `M8.gate.signed_at` present (TECH-489 exit).
- Risk: `validate:all` fails on main post-merge due to concurrent activity drift. Mitigation: if red, halt; route failure to its owning Stage patch lane (per Stage 8 T8.3 / M7 pattern); do NOT fix on main directly.
- Invariant touch: `ia/state/id-counter.json` must not be hand-edited in conflict resolution; if counter drift → re-run `reserve-id.sh` (no-op on clean state).

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Clean merge, no BACKLOG conflict | Merge commit on main; `M8.done` flipped | Happy path. |
| BACKLOG.md conflict at merge time | Resolved via `materialize-backlog.sh` re-run; yaml-source wins | Never hand-edit views. |
| `validate:all` red post-merge | Halt, route failure to owning Stage lane | No main-branch direct fix. |
| Attempted squash merge | Rejected in Phase 1 | Preserve migration history. |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| gate_precondition | `M8.gate.signed_at` field | Present before `git merge` | manual |
| merge_commit_no_squash | `git log main --oneline -1` | Shows merge commit (2 parents) | manual |
| backlog_views_materialized | Post-conflict resolution | `diff <(materialize-backlog.sh --dry-run) BACKLOG.md` = empty | node |
| validate_all_green | `npm run validate:all` on main post-merge | Exit 0 | node |
| m8_done_flipped | Migration JSON post-merge | `M8.status == "done"` + timestamp | manual |

### §Acceptance

- [ ] `M8.gate.signed_at` present before merge dispatch.
- [ ] `git merge --no-ff feature/lifecycle-collapse-cognitive-split` executed (no squash).
- [ ] BACKLOG.md / BACKLOG-ARCHIVE.md conflicts (if any) resolved by `materialize-backlog.sh`, not hand-edit.
- [ ] `npm run validate:all` green on main post-merge.
- [ ] Migration JSON `M8.status == "done"` with ISO8601 timestamp.
- [ ] Merge commit on main preserves branch history (2-parent).

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
