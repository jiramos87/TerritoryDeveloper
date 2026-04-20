---
purpose: "TECH-527 — Flag-flip timeline doc."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.2.4"
---
# TECH-527 — Flag-flip timeline doc

> **Issue:** [TECH-527](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Doc-only task: cross-reference `MCP_SPLIT_SERVERS` flag-flip timeline in Stage 1.3 header + close NB-6 open question on B1 in exploration doc. No code touched.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Stage 1.3 header in master plan carries `MCP_SPLIT_SERVERS` flip timeline note.
2. `docs/session-token-latency-audit-exploration.md §Open questions` B1 entry marked Closed.
3. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Flipping the flag — Stage 1.3 T1.3.6.
2. Any code changes — doc-only task.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want the flag-flip timeline recorded so that Stage 1.3 executor knows exactly when to flip. | Stage 1.3 header carries flip timeline note. |

## 4. Current State

### 4.1 Domain behavior

Stage 1.3 header has no `MCP_SPLIT_SERVERS` flip timeline note. Exploration doc §Open questions B1 row still open.

### 4.2 Systems map

Touches: `ia/projects/session-token-latency-master-plan.md` (Stage 1.3 header).
Touches: `docs/session-token-latency-audit-exploration.md` (§Open questions).
No code / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

Phase 2 — Doc closeout.
1. Edit Stage 1.3 header: add inline note pointing to `MCP_SPLIT_SERVERS` flip step (T1.3.6).
2. Edit exploration `§Open questions`: flip B1 row to Closed with resolution pointer (Stage 1.2 + Stage 1.3 sweep).
3. Run `npm run validate:all`.

## 5. Proposed Design

### 5.1 Target behavior (product)

Tooling-only. No gameplay surface touched.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

_Pending — plan-author populates._

### 5.3 Method / algorithm notes (optional)

_None._

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Separate doc-only task | Paper trail closes NB-6 cleanly; B1 decision durable before Stage 1.3 entry. | Inline as footnote in T1.2.2 — loses explicit tracking. |

## 7. Implementation Plan

### Phase 2 — Doc closeout

- [ ] Edit Stage 1.3 header in master plan: add `MCP_SPLIT_SERVERS` flip timeline note.
- [ ] Edit exploration doc `§Open questions`: mark B1 row Closed with resolution pointer.
- [ ] Run `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc edits valid | Node | `npm run validate:all` | Chains validate:dead-project-specs + validate:backlog-yaml |

## 8. Acceptance Criteria

- [ ] Stage 1.3 header in master plan carries `MCP_SPLIT_SERVERS` flip timeline note.
- [ ] `docs/session-token-latency-audit-exploration.md §Open questions` B1 entry marked Closed.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/session-token-latency-master-plan.md Stage 1.2`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

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
