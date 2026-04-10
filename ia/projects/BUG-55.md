---
purpose: "Project spec for BUG-55 — Codebase audit: critical simulation, data integrity, and controller bugs."
audience: both
loaded_by: ondemand
slices_via: none
---
# BUG-55 — Codebase audit: critical simulation, data integrity, and controller bugs

> **Issue:** [BUG-55](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

**Audit report:** [`docs/audit-codebase-2026-04-07.md`](../../docs/audit-codebase-2026-04-07.md)

## 1. Summary

Full codebase audit uncovered 10 bugs across simulation, economy, data persistence, and UI controller layers. The set includes two crashers (division by zero in EmploymentManager, unprotected `Enum.Parse` in Cell constructor), two data-corruption paths (AutoZoningManager budget leak, CellData height validation), two broken game-balance mechanisms (GrowthBudgetManager minimum enforcement, BuildingTracker zone miscounting), and several high-impact issues in road caching, terrain classification, demand balancing, and event listener cleanup. This issue bundles all 10 fixes into a single workstream.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Eliminate the two crash paths (EmploymentManager div/0, Cell `Enum.Parse`).
2. Fix the two data-corruption paths (AutoZoningManager spend-without-place, CellData height floor).
3. Correct GrowthBudgetManager minimum budget enforcement.
4. Fix BuildingTracker zone counting to reflect only empty zones.
5. Synchronize road cache invalidation within the simulation tick.
6. Fix water-height boundary classification in RoadStrokeTerrainRules and GridPathfinder.
7. Balance demand coefficients to prevent boom-bust oscillation.
8. Add `OnDestroy()` cleanup for event listeners in affected controllers.

### 2.2 Non-Goals (Out of Scope)

1. Broad refactor of `FindObjectOfType` patterns (covered by **BUG-14** / **TECH-05**).
2. Per-frame `Update()` performance optimizations in UI controllers (separate backlog candidates).
3. Null-safety sweep beyond the specific paths identified here.
4. Singleton or DI architecture changes.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Game does not crash when population grows without employment zones | EmploymentManager handles `totalRatio == 0` gracefully |
| 2 | Player | Loading a save with legacy or corrupt enum data does not crash | Cell constructor uses `TryParse` with fallback |
| 3 | Player | Auto-zoning does not silently waste my growth budget | Budget only deducted when zone placement succeeds |
| 4 | Player | Terrain stays consistent across save/load cycles | CellData allows height=0 for valid border cells |
| 5 | Player | City growth does not stall at end of month | GrowthBudgetManager enforces minimum per category |
| 6 | Player | Demand reflects actual empty zone availability | BuildingTracker counts only zones without buildings |
| 7 | Player | New roads connect properly within the same tick | Road cache is fresh before pathfinding queries |
| 8 | Player | Roads can be built on valid height-0 terrain | Water height check uses `< 0` not `<= 0` |
| 9 | Player | City economy reaches stable equilibrium | Demand penalty/boost coefficients are symmetric |
| 10 | Developer | Scene reloads do not leak memory | Controllers unregister listeners in `OnDestroy()` |

## 4. Current State

### 4.1 Domain behavior

See audit report for detailed per-bug observations vs expected behavior.

### 4.2 Systems map

| Area | Files |
|------|-------|
| Employment / economy | `EmploymentManager.cs`, `EconomyManager.cs` |
| Demand / growth | `DemandManager.cs`, `GrowthBudgetManager.cs` |
| Auto simulation | `AutoZoningManager.cs`, `AutoRoadBuilder.cs` |
| Data persistence | `Cell.cs`, `CellData.cs` |
| Terrain / pathfinding | `RoadStrokeTerrainRules.cs`, `GridPathfinder.cs` |
| UI controllers | `SimulateGrowthToggle.cs`, `GrowthBudgetSlidersController.cs`, `CityStatsUIController.cs` |

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible feature change. Crashes eliminated, budget/demand/growth simulation corrected, terrain data stable across save/load, memory leaks closed.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Each fix is a small, isolated change described in the Implementation Plan below.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Bundle 10 bugs into single issue | All found in same audit; each fix is small and isolated; reduces backlog noise | Individual issues per bug |
| 2026-04-07 | Demand coefficient symmetry: equalize to 1.2 | Simpler than adding dampening; preserves existing tuning baseline | Dampening factor; separate tuning pass |

## 7. Implementation Plan

### Phase 1 — Crasher fixes (Critical)

- [x] **Fix 1: EmploymentManager division by zero** (`EmploymentManager.cs` ~line 128-140)
  - **Already guarded:** Code has `if (totalRatio > 0)` at line 134; division only happens inside that block. No change needed.

- [x] **Fix 7: Cell constructor Enum.Parse → TryParse** (`Cell.cs` ~lines 105, 114)
  - Replace `(Zone.ZoneType)System.Enum.Parse(typeof(Zone.ZoneType), cellData.zoneType)` with `Enum.TryParse(cellData.zoneType, out Zone.ZoneType zt) ? zt : Zone.ZoneType.Grass` (and similarly for `forestType`).
  - Match the pattern already used in `RestoreFrom()` in the same file.

### Phase 2 — Data corruption fixes (Critical)

- [x] **Fix 2: AutoZoningManager spend-without-place** (`AutoZoningManager.cs` ~line 164)
  - Reordered: `PlaceZoneAt()` called first, then `TrySpend()` only on success. Applied to both left and right zoning loops.

- [x] **Fix 3: CellData.ValidateData() height floor** (`CellData.cs` ~line 225)
  - Changed `height = Mathf.Max(1, height)` to `height = Mathf.Max(0, height)`.

### Phase 3 — Simulation logic fixes (High)

- [x] **Fix 4: GrowthBudgetManager minimum enforcement** (`GrowthBudgetManager.cs` ~lines 62-63)
  - Changed `return Mathf.Min(available, minAvailablePerCategory)` to `return minAvailablePerCategory`.

- [x] **Fix 5: Road cache invalidation** (`AutoRoadBuilder.cs`)
  - Added `InvalidateRoadCache()` + re-fetch of `edges`/`roadSet` after the street-project while loop when `newProjectsStarted > 0`, so expropriation and subsequent pathfinding see fresh road data.

- [x] **Fix 6: BuildingTracker zone counting** (`DemandManager.cs` ~lines 139-146)
  - Subtracted building counts from zone counts for all three categories (R/C/I) with `Mathf.Max(0, ...)` guard.

### Phase 4 — Terrain and demand balance (Medium)

- [x] **Fix 9: Water height boundary** (`RoadStrokeTerrainRules.cs` ~line 41, `GridPathfinder.cs` ~line 216)
  - Changed `<= 0` to `< 0` in both files.

- [x] **Fix 10: Demand asymmetry** (`DemandManager.cs` ~line 79)
  - Changed `unemploymentResidentialPenalty` default from `1.5f` to `1.2f` to match `unemploymentJobBoost`.

### Phase 5 — Memory leak cleanup (High)

- [x] **Fix 8a: SimulateGrowthToggle** (`SimulateGrowthToggle.cs`)
  - Added `OnDestroy()`: `toggleButton.onClick.RemoveListener(OnToggleClick);`

- [x] **Fix 8b: GrowthBudgetSlidersController** (`GrowthBudgetSlidersController.cs`)
  - Added `OnDestroy()` with `RemoveAllListeners()` for all 5 sliders.

- [x] **Fix 8c: CityStatsUIController** (`CityStatsUIController.cs`)
  - Added `OnDestroy()` with `UnregisterCallback<MouseEnterEvent>` and `UnregisterCallback<MouseLeaveEvent>`.

### Phase 6 — Verification

- [x] Unity compile (`unity_compile`) — passed, no errors.
- [ ] Smoke test: New Game → let AUTO simulation run 5+ ticks → verify no crash, zones placed, budget not drained without zones.
- [ ] Smoke test: Load a save → verify terrain heights preserved, no enum crash.
- [ ] Verify road connectivity after AUTO road builder places roads mid-tick.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| C# compiles | MCP / CLI | `unity_compile` | All fixes must compile cleanly |
| No `Enum.Parse` in Cell constructor | Grep | `grep -n "Enum.Parse" Assets/Scripts/Managers/UnitManagers/Cell.cs` → 0 hits | Only `TryParse` allowed |
| No `<= 0` water height check | Grep | `grep -n "<= 0" Assets/Scripts/Utilities/RoadStrokeTerrainRules.cs` → 0 hits for height | Same for GridPathfinder |
| Budget only spent on successful placement | Code review | AutoZoningManager placement-first ordering | Manual review |
| Listeners cleaned up | Code review | `OnDestroy()` present in 3 controllers | Manual review |

## 8. Acceptance Criteria

- [x] EmploymentManager does not crash when `totalRatio == 0` (already guarded).
- [x] AutoZoningManager only deducts budget when zone is successfully placed.
- [x] CellData.ValidateData() allows height=0.
- [x] GrowthBudgetManager returns `minAvailablePerCategory` when available < min.
- [x] Road cache is invalidated before any mid-tick pathfinding query.
- [x] BuildingTracker counts only empty zones (without buildings).
- [x] Cell constructor uses `TryParse` with fallback for all enum fields.
- [x] Memory leak controllers have `OnDestroy()` with listener cleanup.
- [x] Water height check uses `< 0` (strict) in RoadStrokeTerrainRules and GridPathfinder.
- [x] Demand penalty/boost coefficients are symmetric (both 1.2).
- [x] Unity compiles without errors.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. **BuildingTracker (Fix 6):** Does `CityStats` currently expose per-type building counts (e.g. `residentialBuildingCount`)? If not, a simple counter may need to be added. The implementing agent should verify and adapt.
2. **Demand coefficients (Fix 10):** Should both be 1.2, or should a shared tuning constant be introduced? Product owner may want to tune later — a `const float` is sufficient for now.
3. **Road cache (Fix 5):** Need to verify whether `InvalidateRoadCache()` is already called after each placement or only at end of tick. The fix depends on the actual call order.
