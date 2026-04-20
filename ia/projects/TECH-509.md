---
purpose: "TECH-509 — P1 savings-band validation replay + user sign-off + M9 migration flip."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.8"
phases:
  - "Phase 1 — Replay + telemetry"
  - "Phase 2 — User sign-off"
  - "Phase 3 — Final validation"
---
# TECH-509 — P1 savings-band validation replay + user sign-off + M9 migration flip

> **Issue:** [TECH-509](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-20

## 1. Summary

Replay ≥3 post-merge Stages under Tier 1 stable cache block + Tier 2 per-Stage ephemeral bundle enabled (see `docs/prompt-caching-mechanics.md`). Capture per-Stage telemetry (hit-rate + write/read counts + token-delta). Assert actual savings within ±5% of R5 predicted band. Present report to user; record sign-off in migration JSON `M9.signoff`. Run final `npm run validate:all` + `npm run verify:local`. Flip M9 `done`.

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

Tier 1 stable block + Tier 2 per-Stage bundle layer completed (T10.2–T10.7). P1 savings-band validation pending. Q9 baseline (Stage 9 T9.4 **TECH-492**) available as reference if telemetry captured.

### 4.2 Systems map

Reads: Q9 baseline from TECH-492 telemetry (Stage 9 T9.4).
Replays: ≥3 post-merge Stages under Tier 1 + Tier 2 cache.
Writes: ia/state/lifecycle-refactor-migration.json M9.signoff + M9.done.
Runs: npm run validate:all + npm run verify:local.

### 4.3 Implementation investigation notes (optional)

R5 predicted band: −10% at 2 reads/Stage; +23% at 3 reads; +50% at 5 reads; +57% at 6 reads. Measured read count determines which band point to compare against. If delta > ±5% → investigate regression (F2 or F5) + re-replay before presenting to user.

## 5. Proposed Design

### 5.1 Target behavior (product)

Validate that actual cache savings match R5 predictions at the measured pair-head read count per Stage. Present findings to user. Gate on explicit sign-off ("LGTM" / "ship cache layer"). Record sign-off + flip M9 `done`.

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

### §Audit Notes

- Risk: R5 band interpolation between read counts (2 vs 3 vs 5 vs 6) — pick nearest point or linear interpolate; document method in validation report. Mitigation: lock comparison rule in report appendix before user sign-off.
- Risk: **TECH-492** telemetry incomplete — fewer than 3 Stages usable. Mitigation: master-plan Stage 10 exit allows ≥3 Stages from any post-merge plans with measured read counts; if still short, escalate to user before M9 flip.
- Ambiguity: "Savings %" definition — Resolution: `(baseline_tokens − cached_path_tokens) / baseline_tokens` with same tokenization basis as T10.2 F2 estimator; document formula in report.
- Invariant touch: none (IA / migration JSON + tooling verification).

### §Examples

| Measured reads/Stage | R5 band point (illustrative) | Pass if actual savings within |
|---------------------|------------------------------|------------------------------|
| 2 | −10% | ±5% of −10% |
| 3 | +23% | ±5% of +23% |
| 6 | +57% | ±5% of +57% |

(Exact R5 numbers in §4.3 — implementer uses same table as Stage 10 header.)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| tech_492_baseline_present | `ia/backlog/TECH-492.yaml` | Telemetry or explicit "informational only" note in report | manual |
| replay_three_stages | ≥3 Stage replays | Per-Stage telemetry rows (hit-rate, writes, reads, token-delta) | manual |
| savings_vs_r5 | measured read count + R5 table | Within ±5% assertion documented | manual |
| user_signoff_recorded | user message | Verbatim quote + timestamp in `M9.signoff` | manual |
| migration_m9_done | `ia/state/lifecycle-refactor-migration.json` | `done: true` + `signoff_timestamp` | manual |
| validate_all_green | repo root | `npm run validate:all` exit 0 | node |
| verify_local_green | dev machine | `npm run verify:local` exit 0 | node |

### §Acceptance

- [ ] ≥3 Stages replayed under cache-enabled config; per-Stage telemetry captured (hit-rate + write/read counts + token-delta).
- [ ] Actual savings % vs R5 band at measured read count; within-±5% assertion documented in validation report.
- [ ] User sign-off recorded verbatim in migration JSON `M9.signoff` with timestamp.
- [ ] Migration JSON M9 `done: true` + `signoff_timestamp` stamped.
- [ ] `npm run validate:all` + `npm run verify:local` green on main post-Stage-10.

### §Findings

- **TECH-492** backlog ref resolves (`ia/backlog/TECH-492.yaml` present).
- `T10.8` task_key in `lifecycle-refactor-master-plan.md` — no stale task ref.

## Open Questions (resolve before / during implementation)

1. Candidate **glossary rows** (closeout, not this task): **Tier 1 stable cache block**, **Tier 2 per-Stage ephemeral bundle** — definitions live in `docs/prompt-caching-mechanics.md`; add to `ia/specs/glossary.md` + spec anchor when IA team extends glossary for prompt-caching vocabulary.

Tooling acceptance: see §8.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `plan-applier` reads tuples + applies + re-enters `/verify-loop`._
