---
purpose: "TECH-512 — Baseline collection run."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.1.3"
---
# TECH-512 — Baseline collection run

> **Issue:** [TECH-512](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Run ≥10 real lifecycle sessions under active collect.sh; aggregate raw JSONL to committed
baseline-summary.json with p50/p95/p99 per metric. Produces the Stage 1.2 gating floor.

## 2. Goals and Non-Goals

### 2.1 Goals

1. ≥10 session captures under .claude/telemetry/.
2. Aggregation script → tools/scripts/agent-telemetry/baseline-summary.json committed.
3. All 6 required metrics present with p50/p95/p99 keys.
4. Representative seam mix documented alongside.

### 2.2 Non-Goals (Out of Scope)

1. Per-theme attribution — single aggregate floor only (attribution handled post-Stage-1.3).
2. Hook infrastructure — handled by TECH-510.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Commit baseline-summary.json so Stage 1.2 entry is gated on real data | File present; p50/p95/p99 for all 6 metrics; ≥10 sessions |

## 4. Current State

### 4.1 Domain behavior

No baseline data exists. Stage 1.2 cannot open without committed baseline-summary.json.

### 4.2 Systems map

New: tools/scripts/agent-telemetry/aggregate-baseline.{sh|mjs} (if not folded into TECH-510).
Writes: tools/scripts/agent-telemetry/baseline-summary.json (committed).
Consumes: .claude/telemetry/*.jsonl (raw, gitignored).
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

Collect ≥10 sessions across /implement, /ship, /stage-file seams. Run aggregator to produce
baseline-summary.json. Commit. No per-theme attribution.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 2 — Collection + aggregation.
1. Author aggregate-baseline.sh computing percentiles over raw JSONL.
2. Run ≥10 sessions across /implement, /ship, /stage-file (log seam per session).
3. Run aggregator; produce baseline-summary.json.
4. Commit baseline-summary.json.
5. npm run validate:all (schema gate via TECH-511).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Single aggregate floor — no per-theme attribution | Attribution deferred to post-Stage-1.3 sweep per locked decision | Per-theme from start |

## 7. Implementation Plan

### Phase 2 — Collection + aggregation

- [ ] Author aggregate-baseline.sh (or mjs) computing p50/p95/p99 percentiles over raw JSONL
- [ ] Run ≥10 representative sessions (mix of /implement, /ship, /stage-file)
- [ ] Run aggregator; produce tools/scripts/agent-telemetry/baseline-summary.json
- [ ] Commit baseline-summary.json
- [ ] npm run validate:all (schema gate via TECH-511)

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| baseline-summary.json schema + validate:all | Node | `npm run validate:all` + `npm run validate:telemetry-schema` | Tooling only |

## 8. Acceptance Criteria

- [ ] ≥10 session captures under .claude/telemetry/.
- [ ] Aggregation script → tools/scripts/agent-telemetry/baseline-summary.json committed.
- [ ] All 6 required metrics present with p50/p95/p99 keys.
- [ ] Representative seam mix documented alongside.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/session-token-latency-master-plan.md Stage 1.1`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

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
