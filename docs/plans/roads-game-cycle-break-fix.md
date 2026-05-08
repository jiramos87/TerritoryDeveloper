---
title: Roads ↔ Game asmdef cycle — real fix
audience: agent
status: ready
scope: in-place fix on `feature/asset-pipeline` (no new master plan, no stage)
verification_gate: `validate:no-domain-game-cycle` + `unity:compile-check` + `validate:fast` all exit 0; Unity Editor "Cyclic dependencies detected" error gone
---

# Roads ↔ Game cycle — real fix plan

## Why

Stage 20 closed `validate:no-domain-game-cycle` exit 0 via Roads exemption. Validator hides cycle from CI but Unity Editor compile-time detector ignores exemption → 23-asmdef cycle error persists. Roads.asmdef refs Game GUID `7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a`; Game.asmdef refs `Roads` by name. Closes ring.

Real fix = drop Game GUID from Roads.asmdef. Requires 4 Roads service `.cs` files to reach Game-resident types only via Core-resident interfaces. Surface = ~10 concrete types + 3 statics + 2 mislocated files + 1 nested-struct collision.

## Strategy

**DDD core-vs-context inversion via Territory.Core leaf.** Every Game-resident class Roads touches gets:

1. An `I{X}` interface authored in `Assets/Scripts/Core/{Subfolder}/I{X}.cs` (Core has zero-deps; Game already refs Core).
2. The concrete class implements the interface (`partial class X : MonoBehaviour, IX`).
3. Roads service fields retyped from `X` → `IX`. Concrete-type constructor params → interface params.
4. Game-side wiring (`*.bind(this, ...)`) passes the MonoBehaviour as `IX` (auto upcast).

Nested types + statics → either move to Core (preferred) or expose via interface accessor.

## Phases

### Phase 1 — Move mislocated files (zero behavior change)

Files declare a domain namespace but live under `Managers/` → swept into Game.asmdef. Move them under their declared domain so they compile to the matching domain asmdef.

| File | Current path | New path | New asmdef |
|---|---|---|---|
| `PathTerraformPlan.cs` | `Assets/Scripts/Managers/GameManagers/PathTerraformPlan.cs` | `Assets/Scripts/Domains/Terrain/PathTerraformPlan.cs` | `Terrain` |
| `AutoSimulationRoadRules.cs` | `Assets/Scripts/Utilities/AutoSimulationRoadRules.cs` | `Assets/Scripts/Domains/Roads/Services/AutoSimulationRoadRules.cs` | `Roads` |
| `RoadPathValidationContext` (struct) | inline in `Assets/Scripts/Managers/GameManagers/RoadManager.cs` lines 19–24 | new file `Assets/Scripts/Domains/Roads/RoadPathValidationContext.cs` | `Roads` |

Per-file edits:

- `PathTerraformPlan.cs`:
  - `git mv` to `Assets/Scripts/Domains/Terrain/PathTerraformPlan.cs` (carry `.meta` GUID).
  - References `TerraformingService.TerraformAction` + `TerraformingService.OrthogonalDirection` (nested enums). Replace with fully-qualified `Territory.Terrain.TerraformingService.TerraformAction` for now (file is already in `Territory.Terrain` ns, but legacy MonoBehaviour `TerraformingService` is in Game.asmdef → Phase 5 promotes the enums to Core).
  - Until Phase 5 lands, declare a temp `[Serializable] public enum TerraformAction` in PathTerraformPlan.cs scope OR keep the FQN ref pointing at Game-resident type. Cleanest: extract the two enums to Core in Phase 5 first, then this move compiles clean.
  - **Order: do Phase 5 (enum promotion) BEFORE Phase 1.**
- `AutoSimulationRoadRules.cs`:
  - `git mv` to `Assets/Scripts/Domains/Roads/Services/AutoSimulationRoadRules.cs`.
  - Sig `IsAutoRoadLandCell(GridManager grid, int x, int y)` → `IsAutoRoadLandCell(IGridManager grid, int x, int y)`. Add `using Territory.Core;`.
  - Confirm zero other call sites in Game.asmdef (grep). If any → update to pass `IGridManager` (concrete `GridManager` upcasts).
- `RoadPathValidationContext`:
  - Cut struct + xml-doc from `RoadManager.cs` lines 16–24.
  - Write to new file `Assets/Scripts/Domains/Roads/RoadPathValidationContext.cs` under `namespace Territory.Roads`.
  - `RoadManager.cs` still uses the struct → file stays accessible (Game.asmdef refs `Roads`).

### Phase 2 — Promote shared enums + constants to Core

Surface used by both sides; cleanest in Core.

- Create `Assets/Scripts/Core/Terrain/TerraformAction.cs` with `namespace Territory.Terrain { public enum TerraformAction { None, Flatten, Raise, Lower, ApplySlope, ApplyCliffFace } }` — values lifted from `Managers/GameManagers/TerraformingService.cs` lines 29–35.
- Create `Assets/Scripts/Core/Terrain/OrthogonalDirection.cs` with the same `OrthogonalDirection { North, South, East, West }` enum — lifted from `TerraformingService.cs` lines 37–42.
- Edit `Managers/GameManagers/TerraformingService.cs`: delete the two nested `public enum`s (now in Core via `Territory.Terrain` namespace). Keep `using Territory.Terrain;` at top.
- Edit `Domains/Terrain/Services/TerraformingService.cs`: update the FQN refs `Territory.Terrain.TerraformingService.TerraformAction.Flatten` (lines 157+) → bare `TerraformAction.Flatten`.
- Edit `PathTerraformPlan.cs` (post-Phase 5 location under Terrain): `TerraformingService.TerraformAction action;` → `TerraformAction action;`. Same for `OrthogonalDirection`.
- `RoadConstants.cs` already in Core. Replace every `RoadManager.RoadCostPerTile` call site in `Domains/Roads/Services/AutoBuildService.cs` (lines 467, 711, 740, 1173, 1272, plus `using Territory.Roads;` for `RoadConstants`) → `RoadConstants.RoadCostPerTile`. Roads side switch only — `RoadManager.RoadCostPerTile` stays defined in Game; cycle break does not require deleting the Game-side mirror.

### Phase 3 — Extract `ResolvedRoadTile` canonical location

Two parallel definitions (collision via legacy `RoadPrefabResolver.ResolvedRoadTile` nested struct + `Domains.Roads.Services.PrefabResolverService.ResolvedRoadTile` nested struct).

- Pick canonical: `Assets/Scripts/Domains/Roads/Services/PrefabResolverService.cs` (already in Roads) → promote nested struct to top-level `Domains/Roads/Services/ResolvedRoadTile.cs` (its own file under `namespace Domains.Roads.Services` — same ns as PrefabResolverService).
- Edit `Managers/GameManagers/RoadPrefabResolver.cs`: delete its nested `ResolvedRoadTile` struct. Add `using Domains.Roads.Services;`. All Game-side call sites (`RoadManager.cs`, others — grep `RoadPrefabResolver.ResolvedRoadTile`) → switch to bare `ResolvedRoadTile`.
- Edit `Domains/Roads/Services/AutoBuildService.cs` lines 134, 383, 387, 423, 1152, 1251 — replace every `RoadPrefabResolver.ResolvedRoadTile` with bare `ResolvedRoadTile`. `using Domains.Roads.Services;` already implicit (same ns).

### Phase 4 — Author / expand interfaces in Core

All under `Assets/Scripts/Core/{Subfolder}/I{X}.cs`. Core has zero refs → all interfaces zero-dep relative to Game.

#### 4a. Expand `IGridManager` (existing, in `Managers/UnitManagers/IGridManager.cs`)

Move file to `Assets/Scripts/Core/Core/IGridManager.cs` (folder hierarchy under Territory.Core asmdef). Keep `namespace Territory.Core`.

Add members (all already implemented on `GridManager`):

```csharp
int width { get; }
int height { get; }
int CountRoadNeighbors(int x, int y);
```

#### 4b. Expand `IRoadManager` (existing, in `Managers/UnitManagers/IRoadManager.cs`)

Move file to `Assets/Scripts/Core/Roads/IRoadManager.cs`. Keep `namespace Territory.Roads`.

Add members:

```csharp
// Tile prefabs (read-only)
GameObject roadTilePrefab1 { get; }
GameObject roadTilePrefab2 { get; }
GameObject roadTileBridgeHorizontal { get; }
GameObject roadTileBridgeVertical { get; }
GameObject roadTilePrefabElbowNorthEast { get; }
GameObject roadTilePrefabElbowNorthWest { get; }
GameObject roadTilePrefabElbowSouthEast { get; }
GameObject roadTilePrefabElbowSouthWest { get; }
GameObject roadTilePrefabNorthSlope { get; }
GameObject roadTilePrefabSouthSlope { get; }
GameObject roadTilePrefabEastSlope { get; }
GameObject roadTilePrefabWestSlope { get; }
GameObject roadTilePrefabCrossing { get; }
GameObject roadTilePrefabTIntersectionNorth { get; }
GameObject roadTilePrefabTIntersectionSouth { get; }
GameObject roadTilePrefabTIntersectionEast { get; }
GameObject roadTilePrefabTIntersectionWest { get; }

// Methods
void PlaceRoadTileFromResolved(ResolvedRoadTile resolved);
bool PlaceRoadTileAt(Vector2Int gridPos);
void RefreshRoadPrefabsAfterBatchPlacement(HashSet<Vector2Int> batchCells);
List<ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan);
bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull);
bool TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(List<Vector2> straightCardinalPath, Vector2Int segmentDir, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan);
bool StrokeHasWaterOrWaterSlopeCells(List<Vector2> path);
bool StrokeLastCellIsFirmDryLand(List<Vector2> path);
bool TryExtendCardinalStreetPathWithBridgeChord(List<Vector2> pathVec2, Vector2Int dir);
```

`using Territory.Terrain;` for `PathTerraformPlan`. `using Domains.Roads.Services;` for `ResolvedRoadTile` (Phase 3 moves it there).

⚠ **Cross-asmdef concern:** Core has zero refs. Adding `using Domains.Roads.Services;` to a Core-resident interface = Core depends on Roads = Roads can't ref Core. **Resolution: move `ResolvedRoadTile` to Core too** — `Assets/Scripts/Core/Roads/ResolvedRoadTile.cs` under `namespace Territory.Roads`. (Phase 3 amended: canonical = Core, not Roads.) Then `IRoadManager.ResolvePathForRoads` returns `Territory.Roads.ResolvedRoadTile` from Core.

Same concern for `PathTerraformPlan` — it's in `Territory.Terrain` ns, currently in Managers/. Move to **Core** (`Assets/Scripts/Core/Terrain/PathTerraformPlan.cs`), not Domains/Terrain. Phase 1 amended.

`RoadPathValidationContext` — same. Move to **Core** (`Assets/Scripts/Core/Roads/RoadPathValidationContext.cs`), not Domains/Roads. Phase 1 amended.

#### 4c. Expand `ITerrainManager` (existing, in `Managers/UnitManagers/ITerrainManager.cs`)

Move file to `Assets/Scripts/Core/Terrain/ITerrainManager.cs`. Keep `namespace Territory.Terrain`.

Add members:

```csharp
HeightMap GetHeightMap();
bool IsWaterSlopeCell(int x, int y);
bool IsRegisteredOpenWaterAt(int x, int y);
bool CanPlaceRoad(int x, int y, bool allowWaterSlopeForWaterBridgeTrace);
IWaterManager waterManager { get; }
```

#### 4d. New `IWaterManager` interface

Create `Assets/Scripts/Core/Water/IWaterManager.cs` under `namespace Territory.Water`.

Surface needed (read from PrefabResolverService.cs lines 416–482):

```csharp
namespace Territory.Water
{
    public interface IWaterManager
    {
        bool IsRegisteredWaterAt(int x, int y);
        bool IsWaterSlopeAt(int x, int y);
        // add only what PrefabResolverService.ResolveWaterManagerForBridge consumers actually call;
        // grep `wm\\.` inside that file for the exact set
    }
}
```

Edit `Managers/GameManagers/WaterManager.cs`: `class WaterManager : MonoBehaviour, IWaterManager`. Implement (likely already-public methods).

#### 4e. New `IGrowthBudgetManager` interface

`Assets/Scripts/Core/Economy/IGrowthBudgetManager.cs` under `namespace Territory.Economy`:

```csharp
public interface IGrowthBudgetManager
{
    int GetAvailableBudget(GrowthCategory category);
    bool TrySpend(GrowthCategory category, int amount);
}
```

`GrowthCategory` enum needs to be Core-visible. Confirm location (`Managers/UnitManagers/GrowthBudgetData.cs` likely). Move to `Assets/Scripts/Core/Economy/GrowthCategory.cs` if not already.

`GrowthBudgetManager.cs` (Game) → `class GrowthBudgetManager : MonoBehaviour, IGrowthBudgetManager`.

#### 4f. Expand `IInterstate` (existing, in `Domains/Roads/IInterstate.cs`)

Add members:

```csharp
bool IsConnectedToInterstate { get; }
List<Vector2Int> InterstatePositions { get; }
```

`InterstateManager.cs` (Game) → `class InterstateManager : MonoBehaviour, IInterstate`. Already routed through `IInterstate` partly per Stage 16 — confirm.

⚠ Same Core cross-ref concern. `IInterstate` lives in Domains/Roads currently. AutoBuildService is in Domains/Roads → fine, Roads can ref its own facade. But `IInterstate` does not need to be in Core for the cycle break — Roads code consumes it within Roads. ✅ Stays in Domains/Roads.

#### 4g. New `IUrbanCentroidService` interface

`Assets/Scripts/Core/Economy/IUrbanCentroidService.cs` under `namespace Territory.Economy` (or wherever `UrbanRing`/`RingStreetParams` already live):

```csharp
public interface IUrbanCentroidService
{
    UrbanRing GetUrbanRing(Vector2 worldPos);
    bool CentroidShiftedRecently { get; }
    RingStreetParams GetStreetParamsForRing(UrbanRing ring);
}
```

Confirm `UrbanRing` + `RingStreetParams` are Core-resident or move them. Grep their namespaces.

`UrbanCentroidService.cs` (Game) → `class UrbanCentroidService : MonoBehaviour, IUrbanCentroidService`.

#### 4h. New `IAutoZoningManager` interface (only if AutoBuildService actually invokes methods on it)

Re-grep `_autoZoningManager\.` inside `AutoBuildService.cs` — current scan shows zero method calls (passed-through ref only, line 30 + ctor params 95, 120). **Skip dedicated interface — retype field as `object` or expose via `IRoadAutoBuildHost`** (see Phase 4i). If lifecycle eventually mutates AutoZoningManager, add interface then.

If method calls discovered → mirror pattern from 4e/4g.

#### 4i. Expand `ICityStats`

Add members (used by AutoBuildService line 158, 160):

```csharp
bool simulateGrowth { get; }
int cityPowerOutput { get; }
bool GetCityPowerAvailability();
```

`CityStats.cs` (Game) already implements `ICityStats` — just expose the missing properties on the interface.

### Phase 5 — Swap Roads service field types concrete → interface

Per-file edits in `Assets/Scripts/Domains/Roads/Services/`:

#### `AutoBuildService.cs`

Field declarations (lines 25–31):

```diff
-    private GrowthBudgetManager _growthBudgetManager;
-    private CityStats _cityStats;
-    private InterstateManager _interstateManager;
-    private AutoZoningManager _autoZoningManager;
-    private UrbanCentroidService _urbanCentroidService;
+    private IGrowthBudgetManager _growthBudgetManager;
+    private ICityStats _cityStats;
+    private IInterstate _interstateManager;
+    private object _autoZoningManager;          // pass-through only
+    private IUrbanCentroidService _urbanCentroidService;
```

Constructor params + Bind methods (lines 90–121): same swap concrete → interface. Add `using Territory.Economy;`, `using Territory.Roads;` (for `IInterstate`), keep existing `using` block.

Body refs:
- Line 158, 160 (`_cityStats.simulateGrowth`, etc.) → no change after Phase 4i adds props to ICityStats.
- Line 179 (`_interstateManager.IsConnectedToInterstate`) → no change after Phase 4f adds prop to IInterstate.
- Line 1115 (`_interstateManager.InterstatePositions`) → no change after Phase 4f adds.
- Line 304, 513, 578, 635, 736, 754, 795, 872 (`_gridManager.width`, `_gridManager.height`) → no change after Phase 4a adds props.
- Line 647, 648 (`_gridManager.CountRoadNeighbors`) → no change after Phase 4a adds.
- Line 376 (`_terrainManager.GetHeightMap()`) → no change after Phase 4c adds.
- Line 1115 ref to `RoadManager.RoadCostPerTile` (and lines 467, 711, 740, 1173, 1272) → swap to `RoadConstants.RoadCostPerTile` per Phase 2.
- Line 423 (`new List<RoadPrefabResolver.ResolvedRoadTile>()`) → `new List<ResolvedRoadTile>()` per Phase 3.

Drop `using` of any Game-only namespaces (e.g. `using Territory.Roads;` for legacy `RoadPrefabResolver` if no longer needed; verify with `grep RoadPrefabResolver` post-edit).

#### `PrefabResolverService.cs`

Concrete refs (lines 178, 416, 419, 422, 436, 446, 461, 482):

```diff
-    WaterManager wm = ResolveWaterManagerForBridge();
+    IWaterManager wm = ResolveWaterManagerForBridge();
...
-    WaterManager ResolveWaterManagerForBridge()
+    IWaterManager ResolveWaterManagerForBridge()
     {
         if (_terrainManager?.waterManager != null) return _terrainManager.waterManager;
-        return Object.FindObjectOfType<WaterManager>();
+        return UnityEngine.Object.FindObjectOfType<WaterManager>() as IWaterManager;
     }
```

Note: `FindObjectOfType<WaterManager>()` still uses concrete type → Roads.asmdef would need ref to whatever assembly defines `WaterManager`. **Swap to a Core-resident locator:** add `IWaterManager` lookup via `_terrainManager.waterManager` (Phase 4c added). If no terrain ref → consume an `IWaterManager` injected via constructor (preferred — see PrefabResolverService.cs ctor signature; add `IWaterManager waterManager` param, drop `FindObjectOfType` entirely).

Drop `using` of Game-only `Territory.Water` if it forced a Game-asmdef cross-reference. Add `using Territory.Water;` (Core's `IWaterManager` namespace).

#### `StrokeService.cs` + `InterstateService.cs`

Already concrete-clean per audit. Re-grep post-Phase-1 (RoadPathValidationContext move) for any FQN refs needing update.

#### `Domains/Roads/Roads.cs` (facade)

Update Bind / pass-through to use interface types only. Verify constructor for AutoBuildService gets `IGrowthBudgetManager` etc.

### Phase 6 — Game-side bind site updates

`RoadManager.cs` (or wherever Game wires up Roads facade) hands `this` to facade methods. C# auto-upcasts MonoBehaviour `GrowthBudgetManager` → `IGrowthBudgetManager` etc. → typically zero edits required. Confirm by compiling.

If any Game-side code calls back into Roads via `roadManager.SomeRoadInternal()` that no longer exists on `IRoadManager`, surface it (rare; AutoBuildService methods are all new public surface that already work via the partial class).

### Phase 7 — Drop Game GUID from Roads.asmdef + add new refs

Edit `Assets/Scripts/Domains/Roads/Roads.asmdef`:

```diff
 {
     "name": "Roads",
     "rootNamespace": "Domains.Roads",
     "references": [
-        "GUID:7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a",
         "GUID:a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6"
     ],
```

If new dependencies surfaced during Phase 5 (e.g., Roads needs `Domains.Terrain` for `HeightMap` or `Domains.Water` for IWaterManager and they're not in Core), add their GUIDs. Read `.asmdef.meta` for each domain to fetch GUID. Likely additions: Water, Terrain (if HeightMap stays in Domains/Terrain; currently in Core/Terrain/HeightMap.cs per audit → no add needed).

### Phase 8 — Validator + test cleanup

`tools/scripts/validate-no-domain-game-cycle.mjs`:
- Delete lines 15–20 (Roads exception block + `KNOWN_DOMAIN_TO_GAME_GUID_EXCEPTIONS = ['Assets/Scripts/Domains/Roads/Roads.asmdef']`).
- Verify scanner now treats Roads identically to other domains.

`Assets/Tests/EditMode/Atomization/core-leaf/CoreLeafAsmdefTests.cs`:
- Delete `KnownRoadsException` HashSet (lines ~24–28).
- Delete `Roads_Asmdef_References_Core` special-case (lines ~100–115). Standard domain assertion already covers it.
- Delete `if (KnownRoadsException.Contains(repoRel)) continue;` (line ~164) so Roads gets the same Game-GUID = 0 assertion every other domain gets.

### Phase 9 — Verification

Run in order, all must exit 0:

1. `npm run validate:no-domain-game-cycle` — no exemption left, must report Roads clean.
2. `npm run unity:compile-check` — Editor compile gate (uses `$UNITY_EDITOR_PATH`).
3. `npm run validate:fast` — full fast chain incl. asmdef topology.
4. **Open Unity Editor** (or `npm run db:bridge-preflight`) → assert no "One or more cyclic dependencies detected" console error.

If any red:
- Cycle reappears → grep newly-introduced Game-asmdef ref in Roads (e.g. forgotten `using Territory.Roads;` for legacy class). Use `tools/scripts/validate-no-domain-game-cycle.mjs` output to pinpoint.
- Compile error → likely missing interface member (Phase 4) or namespace `using` add.
- Test fail → Phase 8 left a stale exemption.

## Hard boundaries

- **No** new master plan, no new stage, no `/ship-cycle`. In-place fix on current branch (`feature/asset-pipeline`).
- **No** revert of Stage 20 atomization. Roads services keep their extracted shape; only the asmdef ref + interface surface evolve.
- **No** behavior change. Every interface member maps 1:1 to existing concrete member. Acceptance = same Editor + Play Mode behavior.
- **No** commit until Phase 9 all green.

## Risk register

| Risk | Likelihood | Mitigation |
|---|---|---|
| `FindObjectOfType<WaterManager>` in PrefabResolverService cannot be replaced cleanly | Med | Inject `IWaterManager` via ctor; if circular wiring problem at boot, add `Territory.Core` resolver service that holds singletons |
| `RoadPrefabResolver.ResolvedRoadTile` has callers in Game beyond RoadManager | Low | Phase 3 grep covers; if found, all upcast to bare `ResolvedRoadTile` |
| New interface members miss real RoadManager surface (compile-time catches all but reflection) | Low | No reflection in Roads service code per audit |
| `UrbanRing` / `RingStreetParams` not currently Core-resident → moving them breaks unrelated Game callers | Med | Verify with grep before move; if Game side has many callers, leave types in their current ns and add `using` to Core interface file (Core already permits) |
| `PathTerraformPlan` references `TerraformingService.TerraformAction` nested enum → moving plan to Core requires moving enum first | High | Phase 2 sequenced before Phase 1 explicitly |

## Execution sequence (mechanical)

1. Phase 2 (enums + constants to Core) — pure additive, breaks nothing.
2. Phase 3 (ResolvedRoadTile to Core) — additive + delete legacy nested.
3. Phase 1 (move mislocated files) — `git mv` + namespace cleanup.
4. Phase 4 (author / expand interfaces) — additive only.
5. Phase 5 (Roads service swap concrete → interface) — type-only edits.
6. Phase 6 (Game-side compile check; usually noop).
7. Phase 7 (Roads.asmdef edit) — drop Game GUID.
8. Phase 8 (validator + test exemption removal).
9. Phase 9 (verification gate).

Compile after each phase. Rollback unit = single phase (git stash).

## Out of scope

- AutoBuildService further atomization.
- New tests beyond what the validator + existing CoreLeaf suite already enforce.
- Any refactor of legacy `Managers/GameManagers/` files beyond namespace fixups + interface impl declarations.
- Migration of remaining Domains/* to similar pattern (Roads is the last cycle holdout per Stage 20 close).
