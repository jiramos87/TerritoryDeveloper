---
purpose: "TECH-91 — Rename Cell → CityCell across all city sim files."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-91 — Rename `Cell` → `CityCell` across city sim files

> **Issue:** [TECH-91](../../BACKLOG.md)
> **Status:** In Progress
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
| `Assets/Scripts/Managers/UnitManagers/CellBase.cs` | Abstract base from TECH-90 — stays untouched; still named `CellBase` |
| `Assets/Scripts/Managers/UnitManagers/IGridManager.cs` | `Cell GetCell(int x, int y)` signature → `CityCell GetCell(...)` |
| `Assets/Scripts/Managers/UnitManagers/HeightMap.cs` | Invariant #1 — height sync callers |
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | `GetCell(x,y)` return type; invariant #5; 72 `Cell` occurrences |
| 33 other files under `Assets/Scripts/` referencing `Cell` type | Update type references (≈300 occurrences total, 35 files) |
| `ia/specs/isometric-geography-system.md` §1, §2 | Spec authority — may need terminology follow-up (deferred, not in scope) |

### 4.3 Implementation investigation notes (optional)

Scope scan (2026-04-13): `\bCell\b` matches 300 occurrences across 35 `.cs` files. Top hits: `GridManager.cs` (72), `TerrainManager.cs` (35), `RoadManager.cs` (31), `WaterManager.Membership.cs` (10), `ForestManager.cs` (10), `AgentDiagnosticsReportsMenu.cs` (9), `RoadPrefabResolver.cs` (9), `ZoneManager.cs` (8), `BuildingPlacementService.cs` (8), `GameDebugInfoBuilder.cs` (8).

Watch-outs:
- `IGridManager.GetCell` return type must flip to `CityCell` alongside `GridManager`.
- Rename must NOT touch `CellBase` (abstract base from TECH-90).
- Word-boundary regex `\bCell\b` is safe: matches `Cell`, `Cell[]`, `Cell.height`, `new Cell()`; skips `CellBase`, `cellArray`, `GetCell`, `HashSet<Cell>`. IDE "rename symbol" preferred over `sed` to avoid false positives in strings / comments.
- `gridArray` / `cellArray` access stays inside `GridManager` (invariant #5) — no new access introduced.
- Unity `.meta` GUID for `Cell.cs` must travel with rename (git mv + rename asset in Editor, or preserve `.meta`) to avoid prefab / scene re-serialization. Open Question covers this.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. City sim behavior identical.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. `git mv Assets/Scripts/Managers/UnitManagers/Cell.cs → CityCell.cs` (preserve `.meta` GUID).
2. Rename class `Cell` → `CityCell` inside the file; keep `: CellBase`.
3. Flip `IGridManager.GetCell` return type from `Cell` to `CityCell`.
4. Update all `\bCell\b` references across `Assets/Scripts/` (≈300 sites, 35 files) — managers, controllers, Editor, services. Leave `CellBase` untouched.
5. Verify invariant #1 dual-write sites still reference `CityCell.height` (field inherited from `CellBase`; dual-write syntax unchanged).
6. Confirm `gridArray` / `cellArray` access scope unchanged (invariant #5).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Mechanical rename only — no field changes | Minimize diff; invariant #1 easiest to verify with narrow change | Combined rename + base extraction (wider diff, harder to review) |
| 2026-04-13 | Preserve `Cell.cs.meta` GUID via `git mv` + keep `.meta` file | Avoid prefab / scene re-serialization; no Unity asset-id churn | Let Unity regenerate GUID (would break every prefab referencing the type) |
| 2026-04-13 | IDE "rename symbol" preferred over `sed` | `\bCell\b` word boundary would match comments / string literals / XML docs; IDE rename scopes to the C# symbol only | `sed -i 's/\bCell\b/CityCell/g'` (rejected — false positives, touches comments) |
| 2026-04-13 | Invariant #1 text update (`Cell.height` → `CityCell.height`) deferred, not in scope | Rename is compile-only; invariant-text edit is a separate doc PR to avoid scope creep | Update invariants.md here (rejected — mixes doc edit with refactor) |
| 2026-04-13 | `CellBase` name kept (not `CityCellBase`) | Base is scale-universal per TECH-90 — serves future `RegionCell` / `CountryCell` (TECH-92 / TECH-93) | Rename to `CityCellBase` (rejected — contradicts scale-universal design) |

## 7. Implementation Plan

### Phase 1 — File + class rename (mechanical)

- [x] `git mv Assets/Scripts/Managers/UnitManagers/Cell.cs CityCell.cs` (preserve `.meta` GUID so prefab / scene refs survive).
- [x] Rename class `Cell` → `CityCell` inside `CityCell.cs`; keep `: CellBase`.
- [x] Flip `IGridManager.GetCell(int,int)` return type from `Cell` to `CityCell`.

### Phase 2 — Propagate references across `Assets/Scripts/`

- [x] IDE "rename symbol" from `Cell` → `CityCell` (preferred over `sed` — skips strings / comments / `CellBase` / `cellArray` / `GetCell`).
- [x] Spot-check fallback with `rg '\bCell\b' Assets/Scripts` — zero matches of the bare type expected outside `CityCell.cs` (self) and any intentional comments.
- [x] Verify `CellBase`, `cellArray`, `GetCell`, `HashSet<Cell>` generic sites untouched where appropriate (`HashSet<Cell>` → `HashSet<CityCell>`).

### Phase 3 — Invariants + compile gate

- [x] Verify `HeightMap` ↔ `CityCell.height` dual-write sites (invariant #1) — field inherited from `CellBase`; syntax unchanged.
- [x] Confirm `gridArray` / `cellArray` remain `GridManager`-private (invariant #5).
- [x] `npm run unity:compile-check` clean.
- [x] `npm run validate:all` clean (IA indexes).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Class renamed; file renamed | Grep | `rg -l '^public class CityCell'` finds `CityCell.cs`; `rg -l '^public class Cell\b'` returns empty | Single concrete class survives under new name |
| No stray `Cell` type refs | Grep | `rg '\bCell\b' Assets/Scripts \| rg -v 'CellBase\|cellArray\|GetCell\|CityCell'` — zero matches | Bare `Cell` type gone outside comments |
| `GetCell` signature flipped | Grep | `rg 'CityCell GetCell' Assets/Scripts/Managers/UnitManagers/IGridManager.cs Assets/Scripts/Managers/GameManagers/GridManager.cs` | Interface + impl both return `CityCell` |
| Invariant #1 dual-write intact | Grep | `rg 'HeightMap\[.*\].*=' Assets/Scripts` sites still write `CityCell.height` | Field inherited from `CellBase`; syntax preserved |
| Compile-clean after rename | Unity compile | `npm run unity:compile-check` | Mandatory gate |
| IA indexes consistent | Node | `npm run validate:all` | After BACKLOG + spec changes |

## 8. Acceptance Criteria

- [x] Class named `CityCell`; file named `CityCell.cs`.
- [x] All city sim files reference `CityCell` (no stray `Cell` concrete references).
- [x] `HeightMap` ↔ `CityCell.height` dual-write intact (invariant #1).
- [x] `GridManager.GetCell(x,y)` returns `CityCell`; invariant #5 preserved.
- [x] `npm run unity:compile-check` passes (bridge: compilation_failed=false, 0 errors).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | macOS `sed` ignores `\b` word boundary | BSD `sed` does not support `\b`; applied `perl -pi -e` instead | Used `perl` for word-boundary rename; all 34 files updated correctly |
| 2 | `Cell.cs.meta` not auto-renamed by `git mv` | Unity `.meta` files must be renamed separately | `git mv Cell.cs.meta CityCell.cs.meta` preserves GUID |

## 10. Lessons Learned

- macOS `sed` does not support `\b` word boundaries; use `perl -pi -e 's/\bFoo\b/Bar/g'` for symbol renames in CLI agents.
- `git mv` renames the `.cs` file but leaves the `.meta` alongside original name; must `git mv` the `.meta` separately to preserve GUID.
- Bridge `get_compilation_status` is reliable compile gate when Unity Editor holds the project lock (batchmode blocked).

## Open Questions (resolve before / during implementation)

1. **Resolved** — Unity prefab / scene references: preserve `Cell.cs.meta` GUID via `git mv` + keep `.meta` file alongside the renamed `.cs`. Unity tracks `MonoBehaviour` by file GUID + class name; GUID survives → prefab serialized fields rebind automatically after editor reimport. Verified by Editor reimport + scene load as part of `npm run unity:compile-check` + manual open-scene check. No `.asmdef` edit needed (same assembly).
2. **Deferred to separate doc PR** — Invariant #1 wording in `ia/rules/invariants.md` still reads `Cell.height`. Post-rename the literal is technically stale but semantically correct (field inherited from `CellBase`, dual-write unchanged). Text update owned by a follow-up doc task, not this refactor.
