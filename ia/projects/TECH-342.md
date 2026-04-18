---
purpose: "TECH-342 — Plan tree builder + TreeNodeData union + tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-342 — Plan tree builder + TreeNodeData union

> **Issue:** [TECH-342](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Pure builder `buildPlanTree(plan, metrics)` synthesizes a renderable `TreeNodeData[]` forest — step → stage → phase → task. Phase nodes synthesized from `groupBy(task.phase)` within each stage, NOT from `Stage.phases` checklist (distinct concept; JSDoc NB1 flags divergence). Per-node status from `BadgeChip` Status union. Satisfies Stage 7.1 Exit bullet 4 (`web/lib/plan-tree.ts` ships w/ union + builder + tests).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/lib/plan-tree.ts` exports `TreeNodeData` discriminated union — `kind: 'step' | 'stage' | 'phase' | 'task'`; fields `id`, `label`, `status`, `counts`, `children`.
2. Export `buildPlanTree(plan: PlanData, metrics: PlanMetrics): TreeNodeData[]`.
3. Phase nodes from `groupBy(task.phase)` inside each stage — not `Stage.phases` checklist.
4. JSDoc NB1 flags phase-node source distinction.
5. Per-node status from `BadgeChip` Status union (`done | in-progress | pending | blocked`); all-done propagates up stage/step.
6. 4 unit test cases — stage-node counts, phase synthesis from tasks, status derivation, all-done propagation.
7. `npm run validate:web` green.

### 2.2 Non-Goals

1. UI rendering (Stage 7.2 `TreeNode.tsx`).
2. Expansion state (Stage 7.2 client component).
3. Blocked-status propagation rules (`'blocked'` unreachable at MVP per TECH-341 JSDoc).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 7.2 `PlanTree` author | As author of recursive `TreeNode`, I want a ready forest so my render is pure render-props. | Builder ships + union exported + tested. |

## 4. Current State

### 4.1 Domain behavior

Plans already parse into `PlanData` (Stage 3.1) w/ `steps[].stages[].tasks[]`. No tree projection exists. Phase grouping intentionally ignores `Stage.phases` checklist field (task-derived view = ground truth).

### 4.2 Systems map

- New file `web/lib/plan-tree.ts`.
- Test file `web/lib/__tests__/plan-tree.test.ts`.
- Imports: `PlanData`, `PlanMetrics`, `TaskRow`, `Step`, `Stage` from `web/lib/plan-loader-types.ts`.
- Status union source: `web/components/BadgeChip.tsx` (Stage 1.2).

## 5. Proposed Design

### 5.1 Target behavior

Pure. Walks `plan.steps` → `step.stages` → `groupBy(stage.tasks, t => t.phase)` → tasks. Each node carries counts `{done, total}` + derived status. Stage/step status = `'done'` when all descendants done, else `'in-progress'` when any done, else `'pending'`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Phase nodes from task groupBy | Stage.phases checklist drifts + lacks task linkage | Use Stage.phases — rejected, would require phase↔task join + drift risk |
| 2026-04-17 | Discriminated union w/ `kind` field | Enables exhaustive switch in `TreeNode.tsx` render | Separate interfaces per level — rejected, harder to recurse |

## 7. Implementation Plan

### Phase 1 — Union + builder + tests

- [ ] Create `web/lib/plan-tree.ts`.
- [ ] Define `TreeNodeData` discriminated union.
- [ ] Implement `buildPlanTree` w/ groupBy phase synthesis.
- [ ] Add JSDoc NB1 on phase-source distinction.
- [ ] Create `web/lib/__tests__/plan-tree.test.ts` w/ 4 cases.
- [ ] `npm run validate:web` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Builder correctness | Unit | `web/lib/__tests__/plan-tree.test.ts` | 4 cases |
| Typecheck + build | Node | `npm run validate:web` | — |

## 8. Acceptance Criteria

- [ ] `plan-tree.ts` exports union + builder.
- [ ] Phase synthesis by `task.phase` groupBy (not `Stage.phases`).
- [ ] JSDoc NB1 present.
- [ ] 4 unit tests green.
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
