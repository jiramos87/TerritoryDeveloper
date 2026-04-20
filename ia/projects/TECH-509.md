---
purpose: "TECH-509 — P1 savings-band validation replay + user sign-off + M9 migration flip."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.8"
---
# TECH-509 — P1 savings-band validation replay + user sign-off + M9 migration flip

> **Issue:** [TECH-509](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Replay ≥3 post-merge Stages under Tier 1 + Tier 2 cache enabled. Capture per-Stage telemetry (hit-rate + write/read counts + token-delta). Assert actual savings within ±5% of R5 predicted band. Present report to user; record sign-off in migration JSON M9.signoff. Run final `npm run validate:all` + `npm run verify:local`. Flip M9 done.

## 2. Goals and Non-Goals

### 2.1 Goals

1. ≥3 Stages replayed under cache-enabled config; per-Stage telemetry captured.
2. Actual savings % computed + compared against R5 band; within-±5% assertion documented.
3. User sign-off recorded verbatim in migration JSON M9.signoff field with timestamp.
4. Migration JSON M9 `done: true` + `signoff_timestamp` stamped.
5. `npm run validate:all` + `npm run verify:local` green on main post-Stage-10.

### 2.2 Non-Goals (Out of Scope)

1. Patching F2 sizing gate regression (back to T10.2 if triggered).
2. F5 cascade violation investigation (back to T10.4 if triggered).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Confirm cache optimization saves within predicted band | Per-Stage telemetry within ±5% of R5 band |
| 2 | Developer | Stage 10 signed off + M9 closed | M9.signoff + M9.done written |

## 4. Current State

### 4.1 Domain behavior

Tier 1 + Tier 2 cache layer completed (T10.2–T10.7). P1 savings-band validation pending. Q9 baseline (T9.4 TECH-492) available as reference if telemetry captured.

### 4.2 Systems map

Reads: Q9 baseline from TECH-492 telemetry (Stage 9 T9.4).
Replays: ≥3 post-merge Stages under Tier 1 + Tier 2 cache.
Writes: ia/state/lifecycle-refactor-migration.json M9.signoff + M9.done.
Runs: npm run validate:all + npm run verify:local.

### 4.3 Implementation investigation notes (optional)

R5 predicted band: −10% at 2 reads/Stage; +23% at 3 reads; +50% at 5 reads; +57% at 6 reads. Measured read count determines which band point to compare against. If delta > ±5% → investigate regression (F2 or F5) + re-replay before presenting to user.

## 5. Proposed Design

### 5.1 Target behavior (product)

Validate that actual cache savings match R5 predictions at the measured read count. Present findings to user. Gate on explicit sign-off ("LGTM" / "ship cache layer"). Record sign-off + flip M9 done.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Select ≥3 Stages from Q9 baseline (T9.4).
2. Run each Stage with Tier 1 + Tier 2 cache; capture SSE usage telemetry.
3. Compute savings % = (baseline_tokens − cached_tokens) / baseline_tokens.
4. Compare against R5 band at measured read count; assert within ±5%.
5. If fail: investigate + patch + re-replay.
6. Present report to user; wait for sign-off.
7. Write M9.signoff (verbatim quote + timestamp) + M9.done: true.
8. Run npm run validate:all + npm run verify:local.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | ±5% tolerance | Allows minor measurement noise; still validates model | ±10% (too loose) |

## 7. Implementation Plan

### Phase 1 — Replay + telemetry

- [ ] Select ≥3 Stages from Q9 baseline.
- [ ] Replay each under Tier 1 + Tier 2 cache; capture usage data.
- [ ] Compute savings vs R5 band; assert ±5%.

### Phase 2 — User sign-off

- [ ] Present validation report to user.
- [ ] Wait for explicit sign-off.
- [ ] Write M9.signoff verbatim + M9.done: true.

### Phase 3 — Final validation

- [ ] `npm run validate:all` green.
- [ ] `npm run verify:local` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Final validate:all | Node | `npm run validate:all` | Tooling only |
| Final verify:local | Agent | `npm run verify:local` | Full local chain post-Stage-10 |

## 8. Acceptance Criteria

- [ ] ≥3 Stages replayed under cache-enabled config; per-Stage telemetry captured (hit-rate + write/read counts + token-delta).
- [ ] Actual savings % computed + compared against R5 band at measured read count; within-±5% assertion documented in validation report.
- [ ] User sign-off recorded verbatim in migration JSON M9.signoff field with timestamp.
- [ ] Migration JSON M9 `done: true` + `signoff_timestamp` stamped.
- [ ] `npm run validate:all` + `npm run verify:local` green on main post-Stage-10.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/lifecycle-refactor-master-plan.md Stage 10`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

### §Audit Notes

### §Examples

### §Test Blueprint

### §Acceptance

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
