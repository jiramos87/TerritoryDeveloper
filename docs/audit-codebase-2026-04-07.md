# Codebase Audit — Territory Developer

**Date:** 2026-04-07  
**Scope:** Full review of `Assets/Scripts/` (Managers, Controllers, Models, Utils, Systems)

---

## Top 10 Most Important Bugs

### 1. CRITICAL — Division by zero in EmploymentManager
**File:** `Assets/Scripts/Managers/GameManagers/EmploymentManager.cs` ~line 136  
**Problem:** When `totalRatio = 0` (no commercial or industrial jobs available), the division `commercialRatio / totalRatio` causes a crash.  
**Impact:** Game crashes when population grows but no employment zones exist. Likely scenario at game start.  
**Suggested fix:** Add guard `if (totalRatio == 0) return;` before the division.

---

### 2. CRITICAL — AutoZoningManager spends budget without placing zone
**File:** `Assets/Scripts/Managers/GameManagers/AutoZoningManager.cs` ~line 164  
**Problem:** `TrySpend()` is evaluated first in an `&&` expression. If it succeeds but `PlaceZoneAt()` fails, the money is deducted but the zone is not placed. No refund mechanism exists.  
**Impact:** Growth budget corruption; auto-zoning silently wastes funds.  
**Suggested fix:** Separate the calls: validate placement first, then spend.

---

### 3. CRITICAL — CellData.ValidateData() forces minimum height to 1, but MIN_HEIGHT is 0
**File:** `Assets/Scripts/Managers/UnitManagers/CellData.cs` ~line 225  
**Problem:** `height = Mathf.Max(1, height)` raises cells that legitimately have height=0 (map borders, special terrain) to height=1 on every save/load cycle.  
**Impact:** Progressive terrain corruption. Map borders and cliffs render incorrectly after loading a save.  
**Suggested fix:** Use `Mathf.Max(TerrainManager.MIN_HEIGHT, height)` or `Mathf.Max(0, height)`.

---

### 4. HIGH — GrowthBudgetManager.GetAvailableBudget() does not enforce guaranteed minimum
**File:** `Assets/Scripts/Managers/GameManagers/GrowthBudgetManager.cs` ~lines 62-63  
**Problem:** When `available < minAvailablePerCategory`, it returns `Mathf.Min(available, minAvailablePerCategory)`, which always returns `available` (the smaller value). It should return `minAvailablePerCategory` to guarantee the minimum.  
**Impact:** Growth stalls at end of each month when budget runs out early. The "guaranteed minimum per category" mechanism does not work.  
**Suggested fix:** Replace `Mathf.Min(...)` with `minAvailablePerCategory` directly.

---

### 5. HIGH — Road cache invalidation not synchronized within tick
**File:** `Assets/Scripts/Managers/GameManagers/AutoRoadBuilder.cs` ~lines 193-268  
**Problem:** `GetAllRoadPositions()` is called at the start of `ProcessTick()`, but roads placed during the same tick do not invalidate the cache until the NEXT tick. Pathfinding queries within the same frame use stale data.  
**Impact:** New roads don't connect properly; road network fragmentation; zones can't be placed adjacent to newly built roads.  
**Suggested fix:** Invalidate cache immediately after each placement, or recalculate at end of tick before pathfinding.

---

### 6. HIGH — BuildingTracker counts ALL zones, not just empty ones
**File:** `Assets/Scripts/Managers/GameManagers/DemandManager.cs` ~lines 139-146  
**Problem:** `residentialZonesWithoutBuildings` is calculated as the sum of ALL residential zones (including those that already have buildings). The variable name does not reflect the actual calculation.  
**Impact:** Demand bonuses apply even in fully developed cities, creating unrealistic growth cycles that never stabilize.  
**Suggested fix:** Subtract existing building count, or rename the variable and adjust demand logic.

---

### 7. HIGH — Unprotected Enum.Parse() in Cell constructor
**File:** `Assets/Scripts/Managers/UnitManagers/Cell.cs` ~lines 105, 114  
**Problem:** Uses `System.Enum.Parse()` without try-catch. If serialized data contains invalid enum values (due to save corruption or migration), the game crashes. The `RestoreFrom()` method in the same file correctly uses `TryParse()` with fallback.  
**Impact:** Crash when loading a save with corrupt or legacy enum data.  
**Suggested fix:** Replace `Enum.Parse()` with `Enum.TryParse()` and a default value.

---

### 8. HIGH — Memory leaks from event listeners without cleanup in multiple controllers
**Affected files:**
- `Controllers/UnitControllers/SimulateGrowthToggle.cs` (line 31)
- `Controllers/UnitControllers/GrowthBudgetSlidersController.cs` (lines 45-52)
- `Controllers/GameControllers/CityStatsUIController.cs` (lines 175-176)

**Problem:** `onClick.AddListener()` and `RegisterCallback()` are called in `Start()` but none have `OnDestroy()` to unregister them. On scene reload, listeners accumulate.  
**Impact:** Progressive memory leak; duplicate callbacks execute the same logic multiple times after each scene reload.  
**Suggested fix:** Add `OnDestroy()` with `RemoveListener()`/`UnregisterCallback()` to each affected controller.

---

### 9. MEDIUM — Water height logic error in RoadStrokeTerrainRules
**File:** `Assets/Scripts/Utilities/RoadStrokeTerrainRules.cs` ~line 41  
**Problem:** `heightMap.GetHeight(x, y) <= 0` classifies cells with height exactly 0 as water, but `TerrainManager.MIN_HEIGHT = 0` is a valid terrain height. Should be `< 0` or compare against an explicit water threshold.  
**Impact:** Road pathfinding may truncate paths or skip valid cells at terrain edges. Same issue in `GridPathfinder.cs` line 216.  
**Suggested fix:** Use `< 0` or `< waterThreshold` instead of `<= 0`.

---

### 10. MEDIUM — Asymmetric demand between residential and commercial/industrial
**File:** `Assets/Scripts/Managers/GameManagers/DemandManager.cs` ~lines 223-227  
**Problem:** Residential penalty per unemployment is 1.5 per 1% excess, but commercial/industrial boost is only 1.2. If unemployment rises 10% above threshold, residential demand drops 15 points but C/I demand rises only 12.  
**Impact:** Demand system oscillates instead of converging to equilibrium. Cities experience boom-bust cycles that never stabilize.  
**Suggested fix:** Equalize coefficients or introduce a dampening factor to smooth oscillations.

---

## Additional Findings (beyond Top 10)

### Performance
| File | Issue |
|------|-------|
| `CityStatsUIController.cs` | `UpdateStatisticsDisplay()` runs every frame without throttling or dirty flag |
| `MiniMapLayerButton.cs` | `RefreshVisual()` in `LateUpdate()` every frame; only changes on click |
| `SimulateGrowthToggle.cs` | `RefreshVisual()` every frame unnecessarily |
| `GrowthBudgetSlidersController.cs` | `SetActive()` every frame (expensive operation even with same value) |
| `GridPathfinder.cs` | MinHeap does not remove duplicates; O(n^2) memory on large grids |

### Null Safety
| File | Issue |
|------|-------|
| `BuildingPlacementService.cs` | `cellArray[x,y]` accessed without null check in multiple paths |
| `TimeManager.cs` ~line 127 | `animatorManager` used without null check |
| `BuildingSelectorMenuManager.cs` ~lines 47-49 | Chained `transform.Find()` without null checks |
| `PathTerraformPlan.cs` ~line 219 | Returns `null` instead of empty `HashSet` |

### Data Integrity
| File | Issue |
|------|-------|
| `CellData.cs` ~lines 249-250 | `waterBodyType` auto-inferred as Lake but `waterBodyId` can be 0 |
| `UrbanMetrics.cs` ~line 221 | Interstate excluded from urban centroid calculation |
| `EconomyManager.cs` ~line 103 | String concatenation without separator: "Insufficient FundsCannot spend..." |

### Code Quality
| File | Issue |
|------|-------|
| `Cell.cs` ~lines 95-133 | Fields assigned twice in constructor (copy-paste) |
| Multiple selector buttons | `items[0]` without bounds check; index out of range if list is empty |
| Multiple managers | `FindObjectOfType()` in Start() as recurring pattern without singleton |
| Selector buttons | Magic strings in switch-cases instead of enums |

---

## Prioritization Recommendations

1. **Immediate (crashers):** Bugs #1 (EmploymentManager div/0), #7 (Enum.Parse)
2. **Before next release:** Bugs #2 (AutoZoning refund), #3 (CellData height), #4 (GrowthBudget min), #5 (road cache)
3. **Next sprint:** Bugs #6 (BuildingTracker), #8 (memory leaks), #9 (water height), #10 (demand asymmetry)
4. **Backlog:** Performance issues (per-frame Update), null safety, code quality
