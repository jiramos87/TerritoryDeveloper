---
purpose: "TECH-513 — Gate validation + provenance."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.1.4"
---
# TECH-513 — Gate validation + provenance

> **Issue:** [TECH-513](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Final gating task for Stage 1.1. Schema-validates baseline-summary.json via TECH-511 script,
records provenance in the exploration doc, confirms validate:all green. Done flip unblocks
Stage 1.2 MCP server split.

## 2. Goals and Non-Goals

### 2.1 Goals

1. validate:telemetry-schema passes against baseline-summary.json.
2. All 6 metric keys asserted present (total_input_tokens, cache_read_tokens, cache_write_tokens, mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms).
3. docs/session-token-latency-audit-exploration.md §Provenance appended with session count, date range, model, seam mix.
4. npm run validate:all green.

### 2.2 Non-Goals (Out of Scope)

1. Per-theme attribution — single aggregate only at this stage.
2. Stage 1.2 work — gated on this task Done.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Confirm Stage 1.1 gate is met so Stage 1.2 opens | validate:all green; §Provenance appended; all 6 metrics confirmed |

## 4. Current State

### 4.1 Domain behavior

Stage 1.1 gate unmet until baseline-summary.json validated + provenance recorded.

### 4.2 Systems map

Reads: tools/scripts/agent-telemetry/baseline-summary.json.
Touches: docs/session-token-latency-audit-exploration.md (§Provenance only).
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

Run validate:telemetry-schema against baseline-summary.json. Assert 6 metric keys. Append
§Provenance block to exploration doc. Confirm validate:all green.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 2 — Gate + provenance.
1. Run npm run validate:telemetry-schema; confirm zero exit.
2. Sanity-check all 6 metric keys present in baseline-summary.json.
3. Append §Provenance block to exploration doc (session count, date range, model, seam mix).
4. npm run validate:all.
5. Hand off: Stage 1.2 entry unblocked.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Stage 1.2 entry conditional on this task Done | Baseline gates all subsequent remediation Steps | No gate |

## 7. Implementation Plan

### Phase 2 — Gate + provenance

- [ ] Run npm run validate:telemetry-schema; confirm zero exit
- [ ] Sanity-check all 6 metric keys present in baseline-summary.json
- [ ] Append §Provenance block to docs/session-token-latency-audit-exploration.md
- [ ] npm run validate:all

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:telemetry-schema + validate:all | Node | `npm run validate:all` | Tooling only |

## 8. Acceptance Criteria

- [ ] validate:telemetry-schema passes against baseline-summary.json.
- [ ] All 6 metric keys asserted present (total_input_tokens, cache_read_tokens, cache_write_tokens, mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms).
- [ ] docs/session-token-latency-audit-exploration.md §Provenance appended with session count, date range, model, seam mix.
- [ ] npm run validate:all green.

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
