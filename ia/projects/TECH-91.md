---
purpose: "TECH-91 — Rename Cell → CityCell across all city sim files."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-91 — Rename `Cell` → `CityCell` across city sim files

> **Issue:** [TECH-91](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 1 — second step of cell-type split. Rename concrete `Cell` class to `CityCell` across all city sim files after the abstract base is extracted (TECH-90). `HeightMap` ↔ `CityCell.height` dual-write (invariant #1) preserved. No behavior change; compile-only refactor.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Concrete city-scale cell class renamed `CityCell` in all files that reference it.
2. `HeightMap` ↔ `CityCell.height` sync (invariant #1) preserved — dual-write sites updated to use new type name.
3. `GridManager.GetCell(x,y)` returns `CityCell`; existing callers updated.
4. Project compiles clean; zero behavior regression.

### 2.2 Non-Goals (Out of Scope)

1. Adding `RegionCell` / `CountryCell` — that is TECH-92 / TECH-93.
2. Typed `GetCell<T>` generic — that is TECH-94.
3. Any simulation behavior change.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | City-scale cell clearly named so scale origin is unambiguous | All references say CityCell; code compiles |

## 4. Current State

### 4.1 Domain behavior

`Cell` (post-TECH-90) is concrete `MonoBehaviour`; accessed throughout city sim. Rename must be mechanical — no field or method changes.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/UnitManagers/Cell.cs` | File to rename → `CityCell.cs` |
| `Assets/Scripts/Managers/UnitManagers/HeightMap.cs` | Invariant #1 — height sync callers |
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | `GetCell(x,y)` return type; invariant #5 |
| All city sim files referencing `Cell` type | Update type references |
| `ia/specs/isometric-geography-system.md` §1, §2 | Spec authority |

### 4.3 Implementation investigation notes (optional)

Broad rename via IDE or `sed`; then targeted verification of HeightMap dual-write sites. `gridArray` / `cellArray` access stays inside `GridManager` (invariant #5) — check no breakage.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. City sim behavior identical.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Rename file `Cell.cs` → `CityCell.cs`.
2. Rename class `Cell` → `CityCell` inside the file.
3. Update all references across `Assets/Scripts/` — `GridManager`, `HeightMap`, managers, controllers, Editor scripts.
4. Verify invariant #1 dual-write sites still reference `CityCell.height`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Mechanical rename only — no field changes | Minimize diff; invariant #1 easiest to verify with narrow change | Combined rename + base extraction (wider diff, harder to review) |

## 7. Implementation Plan

### Phase 1 — Rename and update all references

- [ ] Rename `Cell.cs` → `CityCell.cs`; rename class `Cell` → `CityCell`.
- [ ] Update all `Cell` type references across `Assets/Scripts/` (managers, controllers, Editor, testing).
- [ ] Verify `HeightMap` dual-write sites reference `CityCell.height` (invariant #1).
- [ ] Confirm no direct `gridArray`/`cellArray` access introduced outside `GridManager` (invariant #5).
- [ ] Run `npm run unity:compile-check` — must pass clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean after rename | Unity compile | `npm run unity:compile-check` | Mandatory gate |
| IA indexes consistent | Node | `npm run validate:all` | After BACKLOG + spec changes |

## 8. Acceptance Criteria

- [ ] Class named `CityCell`; file named `CityCell.cs`.
- [ ] All city sim files reference `CityCell` (no stray `Cell` concrete references).
- [ ] `HeightMap` ↔ `CityCell.height` dual-write intact (invariant #1).
- [ ] `GridManager.GetCell(x,y)` returns `CityCell`; invariant #5 preserved.
- [ ] `npm run unity:compile-check` passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Any `.asmdef` or Unity asset references that use the class name `Cell` directly (serialized fields in prefabs)? May need scene/prefab re-serialization if type GUID changes.
