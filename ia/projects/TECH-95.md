---
purpose: "TECH-95 — Back-compat GetCell(x,y) defaults to CityCell; update all callers; invariant #5 preserved."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-95 — Back-compat `GetCell(x,y)` → `CityCell`; caller migration

> **Issue:** [TECH-95](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 3 — ensure existing untyped `GetCell(x,y)` explicitly returns `CityCell` (not base type); update all callers to use the correct type. Invariant #5 (no direct `gridArray`/`cellArray` access outside `GridManager`) fully enforced across all files post-rename.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GridManager.GetCell(int x, int y)` return type is `CityCell` (was `Cell` pre-TECH-91).
2. All callers in `Assets/Scripts/` updated to use `CityCell` (or typed accessor from TECH-94) — no implicit `Cell` casts remaining.
3. No direct `gridArray`/`cellArray` access outside `GridManager` (invariant #5) — audit confirms clean.
4. Project compiles clean.

### 2.2 Non-Goals (Out of Scope)

1. Generic `GetCell<T>` — that is TECH-94.
2. Any behavior change.
3. Adding `RegionCell` / `CountryCell` accessors to public API (dormant in MVP).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Existing `GetCell(x,y)` returns `CityCell` unambiguously; no surprise base-type return | Return type is `CityCell`; all callers compile |

## 4. Current State

### 4.1 Domain behavior

Post-TECH-94, `GridManager` has typed `GetCell<T>` but untyped `GetCell(x,y)` may still return the abstract base or require explicit cast. All city sim callers must be audited.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | `GetCell(x,y)` return type update |
| All city sim files calling `GetCell(x,y)` | Caller audit + type update |
| `Assets/Scripts/Managers/UnitManagers/HeightMap.cs` | Invariant #1 — height sync site audit |
| `ia/specs/isometric-geography-system.md` §1 | Coord + GetCell authority |

### 4.3 Implementation investigation notes (optional)

Broad grep for `GetCell(` and `gridArray` / `cellArray` to find all sites. Confirm each returns or casts to `CityCell`. No behavioral changes.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Update `GetCell(int x, int y)` return type to `CityCell`. Grep all `GetCell(` callers; update any that assigned to `Cell` variable (now `CityCell`). Grep `gridArray` / `cellArray` to confirm invariant #5 — no direct access outside `GridManager`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Explicit `CityCell` return type (not base) | Prevents implicit base-type misuse; callers stay type-safe | Return base type (more flexible but loses type safety for city code) |

## 7. Implementation Plan

### Phase 1 — Back-compat return type + caller audit

- [ ] Update `GridManager.GetCell(x,y)` return type to `CityCell`.
- [ ] Grep `GetCell(` across `Assets/Scripts/` — update all `Cell` variable assignments to `CityCell`.
- [ ] Grep `gridArray` / `cellArray` — confirm no direct access outside `GridManager` (invariant #5).
- [ ] Run `npm run unity:compile-check` — must pass clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean | Unity compile | `npm run unity:compile-check` | Mandatory gate |
| Invariant #5 audit | Manual/grep | `grep -r "gridArray\|cellArray" Assets/Scripts/` | No hits outside GridManager |

## 8. Acceptance Criteria

- [ ] `GridManager.GetCell(x,y)` return type is `CityCell`.
- [ ] All callers compile without `Cell`-typed variables.
- [ ] No direct `gridArray`/`cellArray` access outside `GridManager` (invariant #5).
- [ ] `npm run unity:compile-check` passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
