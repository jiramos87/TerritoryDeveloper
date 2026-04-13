---
purpose: "TECH-94 — Generic GetCell<T>(x,y) or scale-indexed overloads on GridManager. Compile gate."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-94 — Typed `GetCell<T>` surface on `GridManager`

> **Issue:** [TECH-94](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 3 — add generic `GetCell<T>(x,y)` (or scale-indexed overloads) to `GridManager` so callers can retrieve scale-typed cells. Compile gate only; no behavior shift. Back-compat `GetCell(x,y)` → `CityCell` default handled in TECH-95.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GridManager` exposes `GetCell<T>(int x, int y)` where `T : CellBase` — or equivalent scale-indexed overloads.
2. Typed surface compiles clean.
3. `GridManager` does not grow new runtime responsibilities beyond the typed accessor (invariant: extract helper if needed).

### 2.2 Non-Goals (Out of Scope)

1. Back-compat `GetCell(x,y)` default — that is TECH-95.
2. Any caller migration to typed surface — that is TECH-95.
3. Any multi-scale storage behind the accessor — MVP: `CityCell` grid only; typed accessor returns cast or null for non-city types.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Can call `gridManager.GetCell<CityCell>(x, y)` without casting | Method compiles; returns CityCell or null for out-of-range |

## 4. Current State

### 4.1 Domain behavior

`GridManager.GetCell(x,y)` returns untyped `Cell` (now `CityCell` post-TECH-91). No generic overload exists. All callers use the untyped accessor.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | Primary target — add typed accessor |
| `Assets/Scripts/Managers/UnitManagers/IGridManager.cs` | Interface — may need typed overload added |
| Abstract base (post-TECH-90) | Type constraint for `T` |
| `ia/specs/isometric-geography-system.md` | Coord + GetCell spec authority |

### 4.3 Implementation investigation notes (optional)

Two patterns:
- Generic method: `public T GetCell<T>(int x, int y) where T : CellBase` — casts and returns; null if wrong type.
- Scale-indexed overloads: `GetCityCell(x,y)`, `GetRegionCell(x,y)` — more verbose but no casting.

Generic preferred: cleaner consumer API; aligns with Stage 1.2 exit criteria. If `GridManager` grows too large, extract to a `CellAccessorHelper` (invariant: do not add responsibilities to GridManager).

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Add `GetCell<T>(int x, int y) where T : CellBase` to `GridManager` (and `IGridManager` if applicable). In MVP implementation, internal storage is still `CityCell[][]` — generic method returns cast to `T` or null. Does not break existing untyped `GetCell` (that is TECH-95).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Generic method over per-scale overloads | Cleaner API; one entry point | Scale-indexed overloads (more verbose, no generic benefit) |

## 7. Implementation Plan

### Phase 1 — Add typed accessor + compile gate

- [ ] Add `GetCell<T>(int x, int y) where T : CellBase` to `GridManager` (and `IGridManager` if interface exists).
- [ ] MVP implementation: cast internal `CityCell` to `T`; return null if incompatible (no throw).
- [ ] Do NOT break existing `GetCell(x,y)` untyped overload.
- [ ] Run `npm run unity:compile-check` — must pass clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean | Unity compile | `npm run unity:compile-check` | Mandatory gate |
| IA indexes consistent | Node | `npm run validate:all` | After BACKLOG + spec changes |

## 8. Acceptance Criteria

- [ ] `GetCell<T>(x,y)` compiles on `GridManager`.
- [ ] Existing `GetCell(x,y)` untyped overload still present and unchanged.
- [ ] `GridManager` did not grow new non-accessor responsibilities (extract helper if needed).
- [ ] `npm run unity:compile-check` passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Does `IGridManager` interface exist and need the generic overload? Check `Assets/Scripts/Managers/UnitManagers/IGridManager.cs`.
