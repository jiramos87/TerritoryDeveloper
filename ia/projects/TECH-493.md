---
purpose: "TECH-493 — Ship-stage chain-journal persistence follow-up."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.5"
---
# TECH-493 — Ship-stage chain-journal persistence follow-up

> **Issue:** [TECH-493](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

File crash-survivable ship-stage journal tracker. Resume semantics + lockfile + Phase 4 digest-source swap.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Journal file accumulator post-closeout.
2. Phase 0 resume line + skip-completed behavior.
3. Phase 4 journal-as-source + conditional delete.

### 2.2 Non-Goals (Out of Scope)

1. Spec-implementer mid-phase transactional markers (separate concern).
2. Refactoring `subagent-progress-emit` stderr channel.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want ship-stage to survive a mid-Stage crash so that re-invocation resumes rather than re-dispatches completed tasks. | Kill mid-Stage at task 2/3, re-invoke, observe resume line + only T3 dispatched. |

## 4. Current State

### 4.1 Domain behavior

`ship-stage` has no journal persistence; interrupted run re-dispatches all tasks from scratch on next invocation.

### 4.2 Systems map

- `ia/skills/ship-stage/SKILL.md` — Step 2.5 + Phase 0 + Phase 4 edit targets.
- `ia/state/` — journal + lockfile destination.
- `ia/rules/invariants.md` §Guardrails — flock rule anchor.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Journal accumulates `{task_id, lessons[], decisions[], verify_iterations}` entries post-closeout. Phase 0 reads journal on re-invocation; emits resume line; skips already-Done tasks. Phase 4 digest reads journal; deletes on PASSED exit; preserves on STOPPED / STAGE_VERIFY_FAIL. Lockfile per concurrency-domain rule.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Journal path: `ia/state/ship-stage-{master-plan-slug}-{stage-id}.json`. Lockfile: `ia/state/.ship-stage-{master-plan-slug}-{stage-id}.lock` (flock; Phase 0 read-only skips flock per invariant rule).

### 5.3 Method / algorithm notes (optional)

None.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Journal in `ia/state/` not `ia/backlog/` | State artifact, not a backlog record | In-memory accumulator — lost on crash |

## 7. Implementation Plan

### Phase 1 — File tracker issue

- [ ] This spec is the tracker. File as open TECH issue via yaml + BACKLOG materialize.
- [ ] Cross-reference from `ia/skills/ship-stage/SKILL.md` §Open Questions (read-only; edit deferred to implementer).

### Phase 2 — (deferred to implementer)

- [ ] Implement journal accumulator (Step 2.5 write).
- [ ] Implement Phase 0 resume detection + emit.
- [ ] Implement Phase 4 journal-as-source + conditional delete.
- [ ] Add lockfile per invariants Guardrails §IF flock guard.
- [ ] Acceptance test: kill mid-Stage at task 2/3; re-invoke; verify resume line + only T3 dispatched + final digest contains all 3 tasks' lessons/decisions.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Resume behavior correct | Manual | `/ship-stage` interrupted + re-invoked | Observe resume line + only pending task dispatched |
| Lockfile present during write | Manual | `ls ia/state/.ship-stage-*.lock` during run | Verify flock applied |

## 8. Acceptance Criteria

- [ ] TECH issue filed with scope (a)–(e) verbatim.
- [ ] Acceptance in filed issue includes: kill `/ship-stage` mid-Stage at task 2/3, re-invoke same args, observe resume line + only T3 dispatched + final digest contains all 3 tasks' lessons/decisions.
- [ ] Issue priority = Medium.
- [ ] Issue id cross-referenced from `ia/skills/ship-stage/SKILL.md` §Open Questions (read-only reference; edit deferred to filed issue's implementer).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- None yet.

## §Plan Author

### §Audit Notes

- Risk: this Stage 9 task is tracker-only — scope (a)–(e) must land verbatim or downstream implementer receives partial contract. Mitigation: yaml `scope:` field mirrors T9.5 notes (a)–(e) byte-for-byte; `backlog_record_validate` before materialize.
- Risk: implementer reads spec + misses that spec-implementer mid-phase markers are Out of Scope → scope creep. Mitigation: §2.2 Non-Goals explicit; §Audit Notes restates boundary.
- Risk: lockfile invariant violated (shared lockfile across concurrency domains). Mitigation: scope (e) names dedicated lockfile `ia/state/.ship-stage-{slug}-{stage}.lock` per invariants Guardrails §IF flock guard; read-only Phase 0 inspection skips flock per same rule.
- Risk: journal file lingers after PASSED exit → stale resume state on next run. Mitigation: scope (d) mandates delete on PASSED; preserve only on STOPPED / STAGE_VERIFY_FAIL.
- Risk: journal path collision when multiple ship-stage runs share slug+stage-id. Mitigation: lockfile prevents concurrent runs; re-entry on same slug+stage-id = resume not fork.
- Ambiguity: cross-reference edit to `ia/skills/ship-stage/SKILL.md` §Open Questions — done here or deferred. Resolution: read-only cross-reference added now (single line pointing at tracker id); implementation edits deferred to tracker's own implementer.
- Invariant touch: flock per concurrency domain — new lockfile `.ship-stage-{slug}-{stage}.lock` is a new domain, must not share with `.id-counter.lock` / `.closeout.lock` / `.materialize-backlog.lock`.

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Tracker yaml `scope:` field | Contains (a) journal accumulator, (b) Phase 0 resume, (c) Phase 4 source swap, (d) conditional delete, (e) lockfile | Verbatim from T9.5 notes. |
| `/ship-stage` kill at task 2/3 + re-invoke | Resume line "Resuming at task 3/3 (skipped: T1..T2 already Done)"; only T3 dispatched | Acceptance test from T9.5 notes. |
| Digest after resumed run | `chain.tasks[]` contains all 3 tasks' lessons+decisions | Journal-as-source aggregation. |
| `SHIP_STAGE PASSED` exit | Journal file deleted | Clean exit. |
| `STAGE_VERIFY_FAIL` exit | Journal file preserved | Next-run resume. |
| Concurrent `/ship-stage` same slug+stage | Second invocation blocks on flock | Lockfile enforced. |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| tracker_yaml_scope_abcde | `ia/backlog/{tracker-id}.yaml` `scope:` field | Contains (a) + (b) + (c) + (d) + (e) verbatim | bridge |
| tracker_title_match | Tracker yaml `title:` field | "Ship-stage chain-journal persistence — crash-survivable stage digest + resume UX" | manual |
| tracker_priority_medium | Tracker yaml `priority:` field | Value `medium` | manual |
| tracker_validated | `backlog_record_validate` on yaml | Passes | bridge |
| backlog_materialized | `BACKLOG.md` post-materialize | Tracker row present | node |
| skill_cross_ref_present | `ia/skills/ship-stage/SKILL.md` §Open Questions | One-line cross-reference to tracker id | manual |
| acceptance_test_documented | Tracker spec §Acceptance | Explicit kill-at-2/3 + re-invoke + resume line + T3-only + digest aggregation | manual |

### §Acceptance

- [ ] Tracker yaml filed in `ia/backlog/` with title "Ship-stage chain-journal persistence — crash-survivable stage digest + resume UX".
- [ ] Tracker priority = Medium.
- [ ] Tracker `scope:` field contains (a)–(e) verbatim from T9.5 notes.
- [ ] Tracker §Acceptance includes kill-mid-Stage resume test verbatim.
- [ ] `backlog_record_validate` passes on tracker yaml.
- [ ] `BACKLOG.md` re-materialized with tracker row.
- [ ] One-line cross-reference added to `ia/skills/ship-stage/SKILL.md` §Open Questions pointing at tracker id (read-only).
- [ ] `npm run validate:all` green.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
