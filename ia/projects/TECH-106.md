---
purpose: "TECH-106 — GridManager.GetNeighborStub(side) inert read contract."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-106 — `GridManager.GetNeighborStub(side)` inert read contract

> **Issue:** [TECH-106](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.3 Phase 3 opener. Add `GridManager.GetNeighborStub(BorderSide side)` returning `NeighborCityStub?` or null. Inert — surface only; city sim does not consume yet. Future cross-scale flow consumers read this.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GridManager` read-only surface `GetNeighborStub(BorderSide side) → NeighborCityStub?`.
2. Returns `null` when no stub bound on that side.
3. Zero behavior change in city sim — no caller yet.
4. Mirror `ParentRegionId` / `ParentCountryId` (TECH-88) read-only pattern.

### 2.2 Non-Goals

1. Flow computation (post-MVP).
2. Mutation API — stubs set at new-game + binding only.
3. Placement on arbitrary cells.

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — ParentRegionId / ParentCountryId pattern (TECH-88).
- `Assets/Scripts/Managers/UnitManagers/IGridManager.cs` — interface surface.
- Depends: TECH-103 (list), TECH-104 (stubs present).
- Invariants: #6 — no new GridManager responsibility; thin read accessor only (acceptable — follows TECH-88 precedent).
- Orchestrator: Stage 1.3.

## 5. Proposed Design

### 5.2 Architecture / implementation

- `GridManager` caches neighbor-stub list from hydrated save (like parent-ids).
- Add `public NeighborCityStub? GetNeighborStub(BorderSide side)` — linear scan (≤4 entries).
- Mirror on `IGridManager`.

## 7. Implementation Plan

### Phase 1 — Read surface

- [ ] Hydrate neighbor list alongside parent-ids.
- [ ] Add accessor on `GridManager` + `IGridManager`.
- [ ] No callers yet.
- [ ] `validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Surface present | Unity | `unity:compile-check` | |
| Null on missing side | Manual | New-game w/ known side, query opposite → null | |

## 8. Acceptance Criteria

- [ ] `GetNeighborStub` present on `GridManager` + `IGridManager`.
- [ ] Returns null when no stub bound.
- [ ] Zero city-sim behavior change.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — read-only inert accessor; product behavior trivial.
