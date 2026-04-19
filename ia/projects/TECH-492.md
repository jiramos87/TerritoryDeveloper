---
purpose: "TECH-492 — Freeze close + token-cost telemetry tracker + Q9 baseline instrumentation."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.4"
---
# TECH-492 — Freeze close + token-cost telemetry tracker + Q9 baseline instrumentation

> **Issue:** [TECH-492](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Retire freeze window + file Q9 baseline telemetry tracker. Gate feed for Stage 10 cache-layer activation.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Remove freeze prose from `CLAUDE.md` §Key commands.
2. File telemetry tracker with read-count + cache-usage scope.
3. Baseline ready for Stage 10 T10.1 precondition.

### 2.2 Non-Goals (Out of Scope)

1. Implementing the telemetry collector (tracker scope, separate issue).
2. Landing any Stage 10 cache wiring.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want the freeze window retired so agents can run any lifecycle command on main. | Freeze note absent from `CLAUDE.md`. |
| 2 | Developer | As a developer, I want a Q9 baseline tracker filed so Stage 10 activation is gated on real data. | Tracker issue filed with scope (a)–(d). |

## 4. Current State

### 4.1 Domain behavior

Freeze note active in `CLAUDE.md` §Key commands (FREEZE block). No telemetry tracker filed yet.

### 4.2 Systems map

- `CLAUDE.md` §Key commands — freeze note target.
- `ia/backlog/` — tracker yaml destination.
- `docs/prompt-caching-mechanics.md` §4 R5 — read-count semantics source.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Delete freeze FREEZE block from `CLAUDE.md`. File tracker yaml + spec stub for Q9 baseline telemetry.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Edit `CLAUDE.md` §Key commands to remove the `> **FREEZE — ...` block. Run `reserve-id.sh TECH` for tracker; write yaml + stub via skill.

### 5.3 Method / algorithm notes (optional)

None.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Tracker scopes (a)–(d) per rev 4 R5 | Required for Stage 10 T10.1 precondition gate | Inline telemetry in ship-stage — too early, needs separate issue |

## 7. Implementation Plan

### Phase 1 — Freeze close

- [ ] Remove freeze FREEZE block from `CLAUDE.md` §Key commands (T1.1.1 note).
- [ ] Run `npm run validate:all` to confirm clean state post-removal.

### Phase 2 — File Q9 baseline tracker

- [ ] Reserve id via `bash tools/scripts/reserve-id.sh TECH`.
- [ ] Write tracker yaml with scope (a)–(d) per T9.4 notes.
- [ ] Write spec stub.
- [ ] Materialize BACKLOG.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Freeze note absent + validate green | Node | `npm run validate:all` | Run post-freeze-removal on main |
| Tracker issue filed | Manual inspect | `cat ia/backlog/{tracker-id}.yaml` | Scope (a)–(d) must be present |

## 8. Acceptance Criteria

- [ ] Freeze note removed from `CLAUDE.md` §Key commands.
- [ ] Q9 baseline tracker TECH issue filed in `ia/backlog/` with scope (a)–(d) verbatim.
- [ ] `npm run validate:all` green on main post-merge.
- [ ] Filed issue id cross-referenced from Stage 10 T10.1 precondition note (read-only reference, no edit required here).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- None yet.

## §Plan Author

### §Audit Notes

- Risk: freeze note removed but retired-command references still present in `CLAUDE.md` §Key commands. Mitigation: grep `CLAUDE.md` for `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file` post-removal; confirm they appear only as normal usage references (not under FREEZE block).
- Risk: tracker issue filed without all 4 scope items (a)–(d) → Stage 10 precondition gate weak. Mitigation: yaml `scope:` field lists (a)–(d) verbatim from T9.4 notes; `backlog_record_validate` before materialize.
- Risk: tracker priority accidentally set high → conflicts with Q9 gating semantics. Mitigation: priority = Low (per T9.4 notes).
- Risk: `validate:all` fails on main post-merge unrelated to this task. Mitigation: same Stage 8 patch-lane rule — route to owning Stage lane, do not patch here.
- Ambiguity: Stage 10 T10.1 cross-reference is read-only; who edits Stage 10 note. Resolution: cross-reference recorded in migration JSON as follow-up; actual edit deferred to Stage 10 T10.1 author (not this task).

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Pre-task `CLAUDE.md` with `> **FREEZE — ...` block | Block removed; rest of §Key commands intact | Surgical removal. |
| Reserve id via `reserve-id.sh TECH` | New TECH-{N} id allocated; counter bumped | Canonical id-counter path. |
| Tracker yaml `scope:` field | Contains (a) total prompt tokens, (b) pair-head read count, (c) cache tokens, (d) bundle byte+token size | Verbatim from T9.4 notes. |
| `validate:all` on main post-merge | Exit 0 | Clean state. |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| freeze_note_absent | `grep FREEZE CLAUDE.md` | No match | node |
| tracker_yaml_shape | `ia/backlog/{tracker-id}.yaml` | `backlog_record_validate` passes; `scope` contains (a)-(d) | bridge |
| tracker_priority_low | Tracker yaml `priority:` field | Value `low` | manual |
| tracker_title_match | Tracker yaml `title:` field | Exact match: "Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count" | manual |
| validate_all_green | `npm run validate:all` | Exit 0 | node |
| backlog_materialized | `BACKLOG.md` post-materialize | New tracker row present | node |

### §Acceptance

- [ ] FREEZE block removed from `CLAUDE.md` §Key commands (verbatim).
- [ ] Tracker yaml filed in `ia/backlog/` with title "Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count".
- [ ] Tracker priority = Low.
- [ ] Tracker `scope:` field contains (a) total prompt tokens per Stage, (b) pair-head read count per Stage, (c) cache-write/read/miss token counts, (d) per-Stage bundle byte + token size.
- [ ] `backlog_record_validate` passes on tracker yaml.
- [ ] `BACKLOG.md` re-materialized with tracker row.
- [ ] `npm run validate:all` green on main.
- [ ] Tracker issue id recorded in migration JSON as Stage 10 T10.1 precondition cross-reference.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
