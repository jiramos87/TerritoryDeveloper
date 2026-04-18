---
purpose: "TECH-341 — Default-expand predicate (deriveDefaultExpandedStepId)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-341 — Default-expand predicate

> **Issue:** [TECH-341](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Pure predicate `deriveDefaultExpandedStepId(plan, metrics)` — returns id of first step where `done < total`, else `null`. Drives initial expanded state in Stage 7.2 `PlanTree` Client component. Uses task-count metrics as ground truth; stale step-header Status prose ignored. Satisfies Stage 7.1 Exit bullet 3 (`web/lib/releases/default-expand.ts` ships w/ JSDoc rules + unit tests).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/lib/releases/default-expand.ts` exports `deriveDefaultExpandedStepId(plan: PlanData, metrics: PlanMetrics): string | null`.
2. Iterate `plan.steps` in order; return first id where `metrics.stepCounts[step.id]?.done < metrics.stepCounts[step.id]?.total`.
3. Return `null` when all done or steps empty.
4. JSDoc: "tasks are ground truth; stale step-header Status prose ignored" + `'blocked'` unreachable note.
5. 5 unit test cases — first-non-done, all-done null, all-pending returns first, stale-header ignored, empty-steps null.
6. `npm run validate:web` green.

### 2.2 Non-Goals

1. UI rendering (Stage 7.2 `TreeNode` / `PlanTree`).
2. Multi-expanded state management (client `useState`, Stage 7.2).
3. Blocked-status handling in UI (predicate ignores it — unreachable per JSDoc note).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 7.2 RSC author | As author of progress RSC page, I want one-call seed for `<PlanTree initialExpanded>` so SSR renders meaningful open state. | Predicate returns deterministic step id or null. |

## 4. Current State

### 4.1 Domain behavior

Dashboard currently renders flat cards; no tree. Without predicate, all steps collapsed → empty dashboard. Need signal "first step w/ work in progress".

### 4.2 Systems map

- New file `web/lib/releases/default-expand.ts`.
- Test file `web/lib/__tests__/default-expand.test.ts`.
- Imports: `PlanData`, `PlanMetrics` from `web/lib/plan-loader-types.ts`.

## 5. Proposed Design

### 5.1 Target behavior

Pure function. No I/O. Iterate `plan.steps[]` in declared order. Ground-truth check: `stepCounts[id]?.done < stepCounts[id]?.total`. Missing entry in metrics → treat as 0/0 → skip (not a match).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Tasks ground truth — ignore step-header Status prose | Status prose drifts; task counts live-derived from yaml rows | Read step.status field — rejected, known stale |
| 2026-04-17 | `'blocked'` unreachable at MVP | Status union includes `'blocked'` but no step emits it yet | Handle explicitly — YAGNI, JSDoc note suffices |

## 7. Implementation Plan

### Phase 1 — Predicate + tests

- [ ] Create `web/lib/releases/default-expand.ts`.
- [ ] Implement `deriveDefaultExpandedStepId`.
- [ ] Add JSDoc NB — ground truth + blocked unreachable.
- [ ] Create `web/lib/__tests__/default-expand.test.ts` w/ 5 cases.
- [ ] `npm run validate:web` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Predicate correctness | Unit | `web/lib/__tests__/default-expand.test.ts` | 5 cases |
| Typecheck + build | Node | `npm run validate:web` | — |

## 8. Acceptance Criteria

- [ ] `default-expand.ts` exports predicate.
- [ ] JSDoc cites ground-truth rule + blocked-unreachable note.
- [ ] 5 unit tests green.
- [ ] `npm run validate:web` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling / data-model scaffolding only; no gameplay rules.
