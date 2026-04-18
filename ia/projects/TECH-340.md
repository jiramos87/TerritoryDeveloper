---
purpose: "TECH-340 — Release-plan filter shaper + unit tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-340 — Release-plan filter shaper + unit tests

> **Issue:** [TECH-340](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Pure filter shaper `getReleasePlans(release, allPlans)` matches `plan.filename` basename against `release.children`; silently drops missing-on-disk entries. Unit tests cover registry resolver + filter + edge cases. Satisfies Stage 7.1 Exit bullet 2 (`web/lib/releases/resolve.ts` ships w/ missing-child drop behavior + shared test file).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/lib/releases/resolve.ts` exports `getReleasePlans(release: Release, allPlans: PlanData[]): PlanData[]` — pure basename filter.
2. Silently drop children not present in `allPlans` (no throw).
3. `web/lib/__tests__/releases.test.ts` covers 5 cases: `resolveRelease` found + not-found; `getReleasePlans` filter + missing-child drop + umbrella self-inclusion edge.
4. `npm run validate:web` green.

### 2.2 Non-Goals

1. Default-expand predicate (TECH-341).
2. Tree builder (TECH-342).
3. Warning logs for missing children (silent drop is spec).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 7.2 RSC author | As author of progress page, I want a ready filter so I can compose `loadAllPlans → getReleasePlans → buildPlanTree` in RSC. | Shaper ships + typed + tested. |

## 4. Current State

### 4.1 Domain behavior

`loadAllPlans()` (Stage 3.1) returns all master plans. No filter pass exists; pages would bloat consuming unrelated plans w/o this shaper.

### 4.2 Systems map

- New file `web/lib/releases/resolve.ts` (nested folder).
- Test file `web/lib/__tests__/releases.test.ts`.
- Imports: `PlanData` from `web/lib/plan-loader-types.ts`; `Release` from `web/lib/releases.ts` (TECH-339).

## 5. Proposed Design

### 5.1 Target behavior

Pure, no I/O, no throw. Basename match — `plan.filename.replace(/\.md$/, '')` vs `release.children[i]`. Returns filtered copy (preserve order of `release.children`).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Silent drop for missing children | Rollout tracker may list planned-but-unshipped children; throwing breaks dashboard | Throw / console.warn — rejected; silent matches extension doc spec |

## 7. Implementation Plan

### Phase 1 — Shaper + tests

- [ ] Create `web/lib/releases/resolve.ts`.
- [ ] Implement `getReleasePlans` pure filter.
- [ ] Create `web/lib/__tests__/releases.test.ts` w/ 5 cases.
- [ ] `npm run validate:web` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Filter correctness | Unit | `web/lib/__tests__/releases.test.ts` (vitest / jest per repo) | 5 cases |
| Typecheck + build | Node | `npm run validate:web` | — |

## 8. Acceptance Criteria

- [ ] `resolve.ts` exports pure `getReleasePlans`.
- [ ] 5 unit tests green.
- [ ] Missing-child silently dropped (verified by test).
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
