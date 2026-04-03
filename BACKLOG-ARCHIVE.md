# Backlog Archive — Territory Developer

> Completed issues archived from `BACKLOG.md`. The **Recent archive** block holds items moved from `BACKLOG.md` § Completed (last 30 days) on **2026-04-10**. Older completions follow under **Pre-2026-03-22 archive**.

---

## Recent archive (moved from BACKLOG.md, 2026-04-10)

- [x] **TECH-28** — Unity Editor: **agent diagnostics** (context JSON + sorting debug export) (2026-04-02)
  - Type: tooling / agent workflow
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`, `tools/reports/` (generated output; see `.gitignore`), `.gitignore`
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** **Territory Developer → Reports → Export Agent Context** writes `tools/reports/agent-context-{timestamp}.json` (`schema_version`, `exported_at_utc`, scene, selection, bounded **Cell** / **HeightMap** / **WaterMap** sample via **`GridManager.GetCell`** only). **Export Sorting Debug (Markdown)** writes `sorting-debug-{timestamp}.md` in **Play Mode** using **`TerrainManager`** sorting APIs and capped **`SpriteRenderer`** `sortingOrder` listing. **Agents:** reference `@tools/reports/agent-context-….json` or `@tools/reports/sorting-debug-….md` in Cursor prompts (paths under repo root). `docs/agent-tooling-verification-priority-tasks.md` tasks 2, 23. **Canonical expected behavior** and troubleshooting pointer: `.cursor/specs/unity-development-context.md` §10; **follow-up:** **BUG-53** if menus or **Sorting** export fail in practice.
  - Depends on: none

- [x] **TECH-25** — Incremental authoring milestones for `unity-development-context.md` (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `.cursor/specs/unity-development-context.md`; `projects/agent-friendly-tasks-with-territory-ia-context.md` (pointer wording); `docs/agent-tooling-verification-priority-tasks.md`; `BACKLOG.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts` (backlog smoke test → **TECH-28**)
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** Merged milestone slices **M1**–**M7** into **`unity-development-context.md`** — lifecycle (**`ZoneManager`**, **`WaterManager`**, coroutine/`Invoke` examples), Inspector / **Addressables** guard, **`SerializeField`** scan note + **`DemandManager`**, prefab/**YAML**/**meta** cautions, **`GridManager`** + **`GridSortingOrderService`** sorting entry points (formula still geo §7), **`GeographyManager`** init + **BUG-16** pointer, **`GetComponent`** per-frame row, glossary (**Geography initialization**), §1 roadmap (**TECH-18**, **TECH-26**, **TECH-28**). **`npm run verify`** under **`tools/mcp-ia-server/`**.
  - Depends on: **TECH-20** (umbrella spec)

- [x] **TECH-20** — In-repo Unity development context for agents (spec + concept index) (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `.cursor/specs/unity-development-context.md`; `AGENTS.md`; `.cursor/rules/agent-router.mdc`; `tools/mcp-ia-server/src/config.ts` (`unity` / `unityctx` → `unity-development-context`); `docs/mcp-ia-server.md`; `tools/mcp-ia-server/README.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts`; `tools/mcp-ia-server/tests/tools/build-registry.test.ts`; `tools/mcp-ia-server/tests/tools/config-aliases.test.ts`; [`.cursor/specs/REFERENCE-SPEC-STRUCTURE.md`](.cursor/specs/REFERENCE-SPEC-STRUCTURE.md) (router authoring note)
  - Spec: [`.cursor/specs/unity-development-context.md`](.cursor/specs/unity-development-context.md) (authoritative); project spec removed after closure
  - Notes: **Completed (verified per user):** First-party **Unity** reference for **MonoBehaviour** / **Inspector** / **`FindObjectOfType`** / execution order; **territory-ia** `list_specs` key `unity-development-context`; **agent-router** row avoids **`router_for_task`** token collisions with geography queries (see **REFERENCE-SPEC-STRUCTURE**). Unblocks **TECH-18** `unity_context_section`; follow-up polish shipped in **TECH-25** (completed).
  - Depends on: none

- [x] **BUG-37** — Manual **street** drawing clears **buildings** and **zones** on cells adjacent to the **road stroke** (2026-04-02)
  - Type: bug
  - Files: `TerrainManager.cs` (`RestoreTerrainForCell` — **BUG-37**: skip `PlaceFlatTerrain` / slope rebuild when `GridManager.IsCellOccupiedByBuilding`; sync **HeightMap** / **cell** height + transform first); `RoadManager.cs`, `PathTerraformPlan.cs` (call path unchanged)
  - Spec: `.cursor/projects/BUG-37.md`; `.cursor/specs/isometric-geography-system.md` §14 (manual **streets**)
  - Notes: **Completed (verified per user):** Commit/AUTO `PathTerraformPlan.Apply` Phase 2/3 was refreshing **Moore** neighbors and stacking **grass** under **RCI** **buildings** / footprint **cells** (preview skipped **Apply**, so only commit showed the bug). **Fix:** preserve development by returning after height/sync when the **cell** is **building**-occupied. **Follow-up:** **BUG-52** if **AUTO** zoning shows persistent **grass** buffers beside new **streets** (investigate correlation).
  - Depends on: none

- [x] **TECH-22** — Canonical terminology pass on **reference specs** (`.cursor/specs`) (2026-04-02)
  - Type: documentation / refactor (IA)
  - Files: `.cursor/specs/glossary.md`, `isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, `REFERENCE-SPEC-STRUCTURE.md`; `BACKLOG.md` (one **map border** wording fix); `tools/mcp-ia-server/tests/parser/fuzzy.test.ts` (§13 heading fixture); [`.cursor/projects/TECH-22.md`](.cursor/projects/TECH-22.md)
  - Spec: [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md); [`.cursor/specs/REFERENCE-SPEC-STRUCTURE.md`](.cursor/specs/REFERENCE-SPEC-STRUCTURE.md) (deprecated → canonical table + MCP **`glossary_discover`** hint)
  - Notes: **Completed (verified per user):** Glossary/spec alignment — **map border** vs local **cell** edges; umbrella **street or interstate**; **road validation pipeline** wording; §13 retitled in geo; authoring table in `REFERENCE-SPEC-STRUCTURE.md`. `AGENTS.md` / MCP `config.ts` unchanged (no spec key changes).
  - Depends on: none

- [x] **FEAT-45** — MCP **`glossary_discover`**: keyword-style discovery over **glossary** rows (2026-04-02)
  - Type: feature (IA / tooling)
  - Files: `tools/mcp-ia-server/src/tools/glossary-discover.ts`, `tools/mcp-ia-server/src/tools/glossary-lookup.ts`, `tools/mcp-ia-server/src/parser/glossary-discover-rank.ts`, `tools/mcp-ia-server/src/index.ts`, `tools/mcp-ia-server/package.json`, `tools/mcp-ia-server/tests/parser/glossary-discover-rank.test.ts`, `tools/mcp-ia-server/tests/tools/glossary-discover.test.ts`, `tools/mcp-ia-server/scripts/verify-mcp.ts`, [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`docs/mcp-markdown-ia-pattern.md`](docs/mcp-markdown-ia-pattern.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md), [`AGENTS.md`](AGENTS.md), [`.cursor/rules/agent-router.mdc`](.cursor/rules/agent-router.mdc), [`.cursor/rules/mcp-ia-default.mdc`](.cursor/rules/mcp-ia-default.mdc)
  - Spec: [`.cursor/projects/FEAT-45.md`](.cursor/projects/FEAT-45.md)
  - Notes: **Completed (verified per user):** **`glossary_discover`** tool (territory-ia **v0.4.2**): Phase A deterministic ranking over **Term** / **Definition** / **Spec** / category; optional **`spec`** alias + **`registryKey`** from Spec cell; `hint_next_tools`; empty-query branch with fuzzy **term** suggestions. Agents must pass **English** in glossary tools; documented in MCP README, `docs/mcp-ia-server.md`, `AGENTS.md`, and Cursor rules. **`npm test`** / **`npm run verify`** under `tools/mcp-ia-server/`. **Phase B** (scoring linked spec body) deferred.
  - Depends on: **TECH-17** (MCP IA server — baseline)

- [x] **TECH-17** — MCP server for agentic Information Architecture (Markdown sources) (2026-04-02)
  - Type: infrastructure / tooling
  - Files: `tools/mcp-ia-server/`; `.cursor/mcp.json`; `.cursor/specs/*.md`, `.cursor/rules/*.mdc`, `AGENTS.md`, `ARCHITECTURE.md` as sources; `docs/mcp-ia-server.md`; docs updates in `AGENTS.md`, `ARCHITECTURE.md`, `.cursor/rules/project-overview.mdc`, `agent-router.mdc` (MCP subsection)
  - Notes: **Shipped:** Node + `@modelcontextprotocol/sdk` stdio server with tools including `list_specs`, `spec_outline`, `spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `backlog_issue` (BACKLOG.md by id); spec aliases; fuzzy glossary/section fallbacks; `spec_section` input aliases for LLM mis-keys; parse cache; stderr timing; `node:test` + c8 coverage on `src/parser/**`; `npm run verify`. **Reference:** `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md` (generic pattern), `tools/mcp-ia-server/README.md`. **Retrospective / design history:** `.cursor/projects/TECH-17a.md`, `TECH-17b.md`, `TECH-17c.md` (§9–11 post-ship; delete when no longer needed).
  - Depends on: none

- [x] **BUG-51** — Diagonal / corner-up land slopes vs roads: design closure (2026-04-01)
  - Type: bug (closed by policy + implementation, not by fixing prefab-on-diagonal art)
  - Files: `RoadStrokeTerrainRules.cs`, `RoadManager.cs` (`TryBuildFilteredPathForRoadPlan`, `TryPrepareRoadPlacementPlanLongestValidPrefix`, `TryPrepareDeckSpanPlanFromAdjacentStroke`), `GridPathfinder.cs`, `InterstateManager.cs` (`IsCellAllowedForInterstate`), `RoadPrefabResolver.cs`, `TerraformingService.cs`, `Cell.cs` (route-first / BUG-51 technical work — see spec)
  - Spec: `.cursor/specs/roads-system.md` (land slope stroke policy, route-first paragraph), `.cursor/specs/isometric-geography-system.md` §3.3.3–§3.3.4, §13.10
  - Notes: **Closed (verified):** The original report asked for **correct road prefabs on diagonal and corner-up terrain**. The chosen resolution was **not** to fully support roads on those land slope types. Instead, **road strokes are invalid on land that is not flat and not a cardinal ramp** (`TerrainSlopeType`: `Flat`, `North`, `South`, `East`, `West` only). Pure diagonals (`NorthEast`, …) and corner-up types (`*Up`) are excluded. **Behavior:** silent **prefix truncation** — preview and commit only include cells up to the last allowed cell; cursor may keep moving diagonally without extending preview. **Scope:** manual, AUTO, and interstate. **First cell blocked:** no placement, no notification. **`Road cannot extend further…`** is **not** posted when the only issue is no slope-valid prefix (e.g. stroke starts on diagonal). **Exceptions in stroke truncation / walkability:** path cells at `HeightMap` height ≤ 0 (wet span) and `IsWaterSlopeCell` shore tiles still pass the truncator so FEAT-44 bridges are not cut. **Still in codebase:** BUG-51 **route-first** resolver topology (`pathOnlyNeighbors`), `Cell` path hints, terraform preservation on diagonal wedge when `preferSlopeClimb && dSeg == 0`, `GetWorldPositionForPrefab` anchoring — documented under roads spec **BUG-51 (route-first)**.
  - Depends on: none

- [x] **BUG-47** — AUTO simulation: perpendicular street stubs, reservations, junction prefab refresh (2026-04-01)
  - Type: bug / feature
  - Files: `AutoRoadBuilder.cs` (`FindPath*ForAutoSimulation`, `HasParallelRoadTooClose` + `excludeAlongDir`, batch prefab refresh), `AutoSimulationRoadRules.cs`, `AutoZoningManager.cs`, `RoadCacheService.cs`, `GridPathfinder.cs`, `GridManager.cs`, `IGridManager.cs`, `RoadManager.cs` (`RefreshRoadPrefabsAfterBatchPlacement`, bridge-deck skip); `.cursor/specs/isometric-geography-system.md` §13.9, `.cursor/rules/roads.mdc`, `.cursor/rules/simulation.mdc`
  - Spec: `.cursor/specs/isometric-geography-system.md` §13.9
  - Notes: **Completed (verified in-game):** AUTO can trace perpendicular stubs/connectors and crossings: land = grass/forest/undeveloped light zoning; dedicated AUTO pathfinder; road frontier and extension cells include that class; perpendicular branches pass parent-axis `excludeAlongDir` in `HasParallelRoadTooClose`; auto-zoning skips axial corridor and extension cells. **Visual:** `PlaceRoadTileFromResolved` did not refresh neighbors; added deduplicated per-tick refresh (`RefreshRoadPrefabsAfterBatchPlacement`), skipping bridge deck re-resolve. **Lessons:** any batch `FromResolved` flow must document explicit junction refresh; keep generic `FindPath` separate from AUTO pathfinding.
  - Depends on: none

- [x] **FEAT-44** — High-deck water bridges: cliff banks, uniform deck height, manual + AUTO placement (2026-03-30)
  - Type: feature
  - Files: `RoadManager.cs` (`TryPrepareDeckSpanPlanFromAdjacentStroke`, `TryPrepareLockedDeckSpanBridgePlacement`, `TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord`, `TryExtendCardinalStreetPathWithBridgeChord`, `StrokeHasWaterOrWaterSlopeCells`, `StrokeLastCellIsFirmDryLand`, FEAT-44 validation / chord walk), `TerraformingService.cs` (`TryBuildDeckSpanOnlyWaterBridgePlan`, `TryAssignWaterBridgeDeckDisplayHeight`), `AutoRoadBuilder.cs` (`TryGetStreetPlacementPlan`, `BuildFullSegmentInOneTick` — atomic water-bridge completion), `PathTerraformPlan.cs` (`HasTerraformHeightMutation`, deck display height docs), `RoadPrefabResolver.cs` (bridge deck resolution); rules/spec: `.cursor/rules/roads.mdc`, `.cursor/specs/isometric-geography-system.md` §13
  - Spec: `.cursor/specs/isometric-geography-system.md` §13 (bridges, shared validation, AUTO behavior)
  - Notes: **Completed (verified per user):** **Manual:** locked lip→chord preview uses a **deck-span-only** plan (`TerraformAction.None`, `TryBuildDeckSpanOnlyWaterBridgePlan`) so valid crossings are not blocked by cut-through / Phase-1 on complex tails; commit matches preview via shared `TryPrepareDeckSpanPlanFromAdjacentStroke`. **AUTO:** extends cardinal strokes with the same `WalkStraightChordFromLipThroughWetToFarDry` when the next step is wet/shore; runs longest-prefix plus programmatic deck-span and **prefers** deck-span when the stroke is wet or yields a longer expanded path. **AUTO water crossings** are **all-or-nothing in one tick**: require a **firm dry exit**, enough remaining tile budget for every new tile, a **single lump** `TrySpend` for the bridge, otherwise **`Revert`** — no half bridges. **Uniform deck:** one `waterBridgeDeckDisplayHeight` for all bridge deck prefabs on the span; assignment **prefers the exit (mesa) dry cell** after the wet run, then entry, then legacy lip fallback. **Description (issue):** Elevated road / bridge crossings across cliff-separated banks and variable terrain with correct clearance, FEAT-44 path rules, and consistent sorting/pathfinding per geography spec.

- [x] **BUG-50** — River–river junction: shore Moore topology, junction post-pass diagonal SlopeWater, upper-brink cliff water stacks + isometric anchor at shore grid (2026-03-28)
  - Type: bug / polish
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `IsOpenWaterForShoreTopology`, `NeighborMatchesShoreOwnerForJunctionTopology`, `ApplyJunctionCascadeShorePostPass`, `ApplyUpperBrinkShoreWaterCascadeCliffStacks`, `TryPlaceWaterCascadeCliffStack` / `waterSurfaceAnchorGrid`, `PlaceCliffWallStackCore` sorting reference), `WaterManager.Membership.cs`, `WaterMap.cs` (`TryFindRiverRiverSurfaceStepBetweenBodiesNear`)
  - Spec: `.cursor/specs/isometric-geography-system.md` **§12.8.1**
  - Notes: **Completed (verified):** Default shore masks use **`IsOpenWaterForShoreTopology`** (junction-brink dry land not counted). **`RefreshShoreTerrainAfterWaterUpdate`** runs **`ApplyJunctionCascadeShorePostPass`** (extended topology + **`forceJunctionDiagonalSlopeForCascade`**) then **`ApplyUpperBrinkShoreWaterCascadeCliffStacks`** ( **`CliffSouthWater`** / **`CliffEastWater`** on **`UpperBrink`** only). Cascade **Y** anchor and sorting use **`waterSurfaceAnchorGrid`** at the **shore** cell so wide-river banks align with the isometric water plane. **`ARCHITECTURE.md`** Water bullet and **§12.8.1** document pipeline and authority.

- [x] **BUG-45** — Adjacent water bodies at different surface heights: merge, prefab refresh at intersections, straight slope/cliff transitions (2026-03-27)
  - Type: bug / polish
  - Files: `WaterManager.cs` (`UpdateWaterVisuals` — Pass A/B, `ApplyLakeHighToRiverLowContactFallback`), `WaterMap.cs` (`ApplyMultiBodySurfaceBoundaryNormalization`, `ApplyWaterSurfaceJunctionMerge`, `IsLakeSurfaceStepContactForbidden`, lake–river fallback), `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `SelectPerpendicularWaterCornerPrefabs`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`), `ProceduralRiverGenerator.cs` / `TestRiverGenerator.cs` as applicable; `docs/water-junction-merge-implementation-plan.md`
  - Spec: `.cursor/specs/isometric-geography-system.md` — **§5.6.2**, **§12.7**
  - Notes: **Completed (verified):** Pass A/B multi-body surface handling; lake-at-step exclusions; full-cardinal **`RefreshWaterCascadeCliffs`** (incl. mirror N/W lower pool); perpendicular multi-surface shore corner preference; lake-high vs river-low rim fallback. **Assign** `cliffWaterSouthPrefab` / **`cliffWaterEastPrefab`** on `TerrainManager` for visible cascades (west→east steps use **East**). Residual: **map border** water × cliff **BUG-44**; bridges × cliff-water **BUG-43**; optional N/W cascade art (camera).

- [x] **BUG-42** — Water shores & cliffs: terrain + water (lakes + rivers); water–water cascades; shore coherence — merged **BUG-33** + **BUG-41** (2026-03-26)
  - Type: bug / feature
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `PlaceCliffWalls`, `PlaceCliffWallStackCore`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`, `ClampShoreLandHeightsToAdjacentWaterSurface`, `IsLandEligibleForWaterShorePrefabs`), `WaterManager.cs` (`PlaceWater`, `UpdateWaterVisuals`), `ProceduralRiverGenerator.cs` (inner-corner shore continuity §13.5), `ProceduralRiverGenerator` / `WaterMap` as applicable; `cliffWaterSouthPrefab` & `cliffWaterEastPrefab` under `Assets/Prefabs/`
  - Spec: `.cursor/specs/isometric-geography-system.md` (§2.4.1 shore band height coherence, §4.2 gate, §5.6–§5.7, §5.6.2 water–water cascades, §12–§13, §15)
  - Notes: **Completed (verified):** **Shore band height coherence** — `HeightMap` clamp on Moore shore ring vs adjacent logical surface; water-shore prefab gate uses **`V = max(MIN_HEIGHT, S−1)`** vs **land height**. **River** inner-corner promotion + bed assignment guard. **Water–water cascades** — `RefreshWaterCascadeCliffs` after full `UpdateWaterVisuals`; **`PlaceCliffWallStackCore`** shared with brown cliffs; cascade Y anchor matches **water tile** (`GetWorldPositionVector` at `visualSurfaceHeight` + `tileHeight×0.25`). **Out of scope / follow-up:** visible **north/west** cliff meshes (camera); map edge water × cliff (**BUG-44**); bridges × cliff-water (**BUG-43**); optional **N/S/E/W** “waterfall” art beyond **S/E** stacks — track separately if needed. **Multi-body junctions:** completed **[BUG-45](#bug-45)** (2026-03-27).

- [x] **BUG-33** — Lake shore / edge prefab bugs — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **BUG-41** — River corridors: shore prefabs + cliff stacks — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **FEAT-38** — Procedural rivers during geography / terrain generation (2026-03-24)
  - Type: feature
  - Files: `GeographyManager.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterMap.cs`, `WaterManager.cs`, `WaterBody.cs`, `Cell.cs` / `CellData.cs` (as needed)
  - Spec: `.cursor/specs/isometric-geography-system.md` §12–§13
  - Notes: **Completed:** `WaterBody` classification + merge (river vs lake/sea); `GenerateProceduralRiversForNewGame()` after `InitializeWaterMap`, before interstate; `ProceduralRiverGenerator` (BFS / forced centerline, border margin, transverse + longitudinal monotonicity, `WaterMap` river bodies). **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (merged **BUG-33** + **BUG-41**, 2026-03-26).

- [x] **BUG-39** — Bay / inner-corner shore prefabs: cliff art alignment vs stacked cliffs (2026-03-24)
  - Type: fix (art vs code)
  - Files: `TerrainManager.cs` (`GetCliffWallSegmentWorldPositionOnSharedEdge`, `PlaceCliffWallStack`), `Assets/Sprites/Cliff/CliffEast.png`, `Assets/Sprites/Cliff/CliffSouth.png`, cliff prefabs under `Assets/Prefabs/Cliff/`
  - Notes: **Resolved:** Inspector-tunable per-face placement (`cliffWallSouthFaceNudgeTileWidthFraction` / `HeightFraction`, `cliffWallEastFaceNudgeTileWidthFraction` / `HeightFraction`) and water-shore Y offset (`cliffWallWaterShoreYOffsetTileHeightFraction`) so cliff sprites align with the south/east diamond faces and water-shore cells after art was moved inside the textures. Further shore/gap / cascade work → completed **[BUG-42](#bug-42)** (2026-03-26) where applicable.

- [x] **BUG-40** — Shore cliff walls draw in front of nearer (foreground) water tiles (2026-03-24)
  - Type: fix (sorting / layers)
  - Files: `TerrainManager.cs` (`PlaceCliffWallStack`, `GetMaxCliffSortingOrderFromForegroundWaterNeighbors`)
  - Notes: **Resolved:** Cliff `sortingOrder` is capped against registered **foreground** water neighbors (`nx+ny < highX+highY`) using their `Cell.sortingOrder`, so brown cliff segments do not draw above nearer water tiles. See `.cursor/specs/isometric-geography-system.md` §15.2.

- [x] **BUG-36** — Lake generation: seeded RNG (reproducible + varied per New Game) (2026-03-24)
  - Type: fix
  - Files: `WaterMap.cs` (`InitializeLakesFromDepressionFill`, `LakeFillSettings`), `WaterManager.cs`, `MapGenerationSeed.cs` (`GetLakeFillRandomSeed`), `TerrainManager.cs` (`EnsureGuaranteedLakeDepressions` shuffle)
  - Notes: `LakeFillSettings.RandomSeed` comes from map generation seed; depression-fill uses a seeded `System.Random`; bowl shuffle uses a derived seed. Same template no longer forces identical lake bodies across unrelated runs; fixed seed still reproduces. Spec: `.cursor/specs/isometric-geography-system.md` §12.3. **Related:** **BUG-08**, **FEAT-38**.

- [x] **BUG-35** — Load Game: multi-cell buildings — grass on footprint (non-pivot) could draw above building; 1×1 grass + building under one cell (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs` (`DestroyCellChildren`), `ZoneManager.cs` (`PlaceZoneBuilding`, `PlaceZoneBuildingTile`), `BuildingPlacementService.cs` (`UpdateBuildingTilesAttributes`), `GridSortingOrderService.cs` (`SetZoneBuildingSortingOrder`, `SyncCellTerrainLayersBelowBuilding`)
  - Notes: `DestroyCellChildren(..., destroyFlatGrass: true)` when placing/restoring **RCI and utility** buildings so flat grass prefabs are not kept alongside the building (runtime + load). Multi-cell `SetZoneBuildingSortingOrder` still calls **grass-only** `SyncCellTerrainLayersBelowBuilding` for each footprint cell. **BUG-20** may be re-verified against this. Spec: [`.cursor/specs/isometric-geography-system.md`](.cursor/specs/isometric-geography-system.md) §7.4.

- [x] **BUG-34** — Load Game: zone buildings / utilities render under terrain or water edges (`sortingOrder` snapshot vs building layer) (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs`, `ZoneManager.cs`, `TerrainManager.cs`, `BuildingPlacementService.cs`, `GridSortingOrderService.cs`, `Cell.cs`, `CellData.cs`, `GameSaveManager.cs`
  - Notes: Deterministic restore order; open water and shores aligned with runtime sorting; multi-cell RCI passes `buildingSize`; post-load building sort pass; optional grass sync via `SyncCellTerrainLayersBelowBuilding`. **BUG-35** (completed 2026-03-22) adds `destroyFlatGrass` on building placement/restore. Spec summary: `.cursor/specs/isometric-geography-system.md` §7.4.

- [x] **FEAT-37c** — Persist `WaterMapData` in saves + snapshot load (no terrain/water regen on load) (2026-03-22)
  - Type: feature
  - Files: `GameSaveManager.cs`, `WaterManager.cs`, `TerrainManager.cs`, `GridManager.cs`, `Cell.cs`, `CellData.cs`, `WaterBodyType.cs`
  - Notes: `GameSaveData.waterMapData`; `WaterManager.RestoreWaterMapFromSaveData`; `RestoreGridCellVisuals` applies saved `sortingOrder` and prefabs; legacy saves without `waterMapData` supported. **Follow-up:** building vs terrain sorting on load — **BUG-34** (completed); multi-cell footprint / grass under building — **BUG-35** (completed 2026-03-22).

- [x] **FEAT-37b** — Variable-height water: sorting, roads/bridges, `SEA_LEVEL` removal (no lake shore prefab scope) (2026-03-24)
  - Type: feature + refactor
  - Files: `GridSortingOrderService.cs`, `RoadPrefabResolver.cs`, `RoadManager.cs`, `AutoRoadBuilder.cs`, `ForestManager.cs`, `TerrainManager.cs` (water height queries, bridge/adjacency paths — **exclude** shore placement methods)
  - Notes: Legacy `SEA_LEVEL` / `cell.height == 0` assumptions removed or generalized for sorting, roads, bridges, non-shore water adjacency. Shore tiles **not** in scope (37a + completed **[BUG-42](#bug-42)**). Verified in Unity.

- [x] **BUG-32** — Lakes / `WaterMap` water not shown on minimap (desync with main map) (2026-03-23)
  - Type: fix (UX / consistency)
  - Files: `MiniMapController.cs`, `GeographyManager.cs`, `WaterManager.cs`, `WaterMap.cs`
  - Notes: Minimap water layer aligned with `WaterManager` / `WaterMap` (rebuild timing, `GetCellColor`, layer toggles). Verified in Unity.

- [x] **FEAT-37a** — WaterBody + WaterMap depression-fill (lake data & procedural placement) (2026-03-22)
  - Type: feature + refactor
  - Files: `WaterBody.cs`, `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `LakeFeasibility.cs`
  - Notes: `WaterBody` + per-cell body ids; `WaterMap.InitializeLakesFromDepressionFill` + `LakeFillSettings` (depression-fill, bounded pass, artificial fallback, merge); `LakeFeasibility` / `EnsureGuaranteedLakeDepressions` terrain bowls; `WaterMapData` v2 + legacy load; centered 40×40 template + extended terrain. **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (2026-03-26); **FEAT-37b** / **FEAT-37c** completed; building sort on load **BUG-34** (completed); multi-cell footprint / grass under building **BUG-35** (completed 2026-03-22).

---

## Pre-2026-03-22 archive

- [x] **TECH-12** — Water system refactor: planning pass (objectives, rules, scope, child issues) (2026-03-21)
  - Type: planning / documentation
  - Files: `.cursor/specs/isometric-geography-system.md` (§12), `BACKLOG.md` (FEAT-37, BUG-08 splits), `ARCHITECTURE.md` (Terrain / Water as needed)
  - Notes: **Goal:** Before implementation of **FEAT-37**, produce a single agreed definition of **objectives**, **rules** (data + gameplay + rendering), **known bugs** to fold in, **non-goals / phases**, and **concrete child issues** (IDs) ordered for development. Link outcomes in this spec and in `FEAT-37`. Overlaps **BUG-08** (generation), **FEAT-15** (ports/sea). **Does not** implement code — only backlog + spec updates and issue breakdown.
  - Depends on: nothing (blocks structured FEAT-37 execution)

- [x] **BUG-30** — Incorrect road prefabs when interstate climbs slopes (2026-03-20)
  - Type: fix
  - Files: `TerraformingService.cs`, `RoadPrefabResolver.cs`, `PathTerraformPlan.cs`, `RoadManager.cs` (shared pipeline)
  - Notes: Segment-based Δh for scale-with-slopes; corner/upslope cells use `GetPostTerraformSlopeTypeAlongExit` (aligned with travel); live-terrain fallback + `RestoreTerrainForCell` force orthogonal ramp when `action == None` and cardinal `postTerraformSlopeType`. Spec: `.cursor/specs/isometric-geography-system.md` §14.7. Verified in Unity.

- [x] **TECH-09** — Remove obsolete `TerraformNeeded` from TerraformingService (2026-03-20)
  - Type: refactor (dead code removal)
  - Files: `TerraformingService.cs`
  - Notes: Removed `[Obsolete]` `TerraformNeeded` and `GetOrthogonalFromRoadDirection` (only used by it). Path-based terraforming uses `ComputePathPlan` only.

- [x] **TECH-10** — Fix `TerrainManager.DetermineWaterSlopePrefab` north/south sea logic (2026-03-20)
  - Type: fix (code health)
  - Files: `TerrainManager.cs`
  - Notes: Replaced impossible `if (!hasSeaLevelAtNorth)` under `hasSeaLevelAtNorth` with NE/NW corner handling and East-style branch for sea north+south strips (`southEast` / `southEastUpslope`). South-only coast mirrors East; removed unreachable `hasSeaLevelAtSouth` else (handled by North block first).

- [x] **TECH-11** — Namespace `Territory.Terrain` for TerraformingService and PathTerraformPlan (2026-03-20)
  - Type: refactor
  - Files: `TerraformingService.cs`, `PathTerraformPlan.cs`, `ARCHITECTURE.md`, `.cursor/rules/project-overview.mdc`
  - Notes: Wrapped both types in `namespace Territory.Terrain`. Dependents already had `using Territory.Terrain`. Docs updated to drop "global namespace" examples for these files.

- [x] **TECH-08** — UI design system docs: TECH-07 (ControlPanel sidebar) ticketed and wired (2026-03-20)
  - Type: documentation
  - Files: `BACKLOG.md` (TECH-07), `docs/ui-design-system-project.md` (Backlog bridge), `docs/ui-design-system-context.md` (Toolbar — ControlPanel), `.cursor/specs/ui-design-system.md` (§3.3 layout variants), `ARCHITECTURE.md`, `AGENTS.md`, `.cursor/rules/managers-guide.mdc`
  - Notes: Executable toolbar refactor remains **TECH-07** (open). This issue records the documentation and cross-links only.

- [x] **BUG-25** — Fix bugs in manual street segment drawing (2026-03-19)
  - Type: fix
  - Files: `RoadManager.cs`, `RoadPrefabResolver.cs` (also: `GridManager.cs`, `TerraformingService.cs`, `PathTerraformPlan.cs`, `GridPathfinder.cs` for prior spec work)
  - Notes: Junction/T/cross prefabs: `HashSet` path membership + `SelectFromConnectivity` for 3+ cardinal neighbors in `RoadPrefabResolver`; post-placement `RefreshRoadPrefabAt` pass on placed cells in `TryFinalizeManualRoadPlacement`. Spec: `.cursor/specs/isometric-geography-system.md` §14. Optional follow-up: `postTerraformSlopeType` on refresh, crossroads prefab audit.
- [x] **BUG-27** — Interstate pathfinding bugs (2026-03-19)
  - Border endpoint scoring (`ComputeInterstateBorderEndpointScore`), sorted candidates, `PickLowerCostInterstateAStarPath` (avoid-high vs not, pick cheaper), `InterstateAwayFromGoalPenalty` and cost tuning in `RoadPathCostConstants`. Spec: `.cursor/specs/isometric-geography-system.md` §14.5.
- [x] **BUG-29** — Cut-through: high hills cut through disappear leaving crater (2026-03-19)
  - Reject cut-through when `maxHeight - baseHeight > 1`; cliff/corridor context in `TerrainManager` / `PathTerraformPlan`; map-edge margin `cutThroughMinCellsFromMapEdge`; Phase 1 validation ring in `PathTerraformPlan`; interstate uses `forbidCutThrough`. Spec: `.cursor/specs/isometric-geography-system.md` §14.6.

- [x] **FEAT-24** — Auto-zoning for Medium and Heavy density (2026-03-19)
- [x] **BUG-23** — Interstate route generation is flaky; never created in New Game flow (2026-03-19)
- [x] **BUG-26** — Interstate prefab selection and pathfinding improvements (2026-03-19)
  - Elbow audit, validation, straightness bonus, slope cost, parallel sampling, bridge approach (Rule F), cut-through expansion. Follow-up: BUG-27 / BUG-29 / **BUG-30** completed 2026-03-19–2026-03-20; remaining: BUG-28 (sorting), BUG-31 (prefabs at entry/exit).
- [x] **TECH-06** — Documentation sync: specs aligned with backlog and rules; BUG-26, FEAT-36 added; ARCHITECTURE, file counts, helper services updated; zoning plan translated to English (2026-03-19)
- [x] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs (2026-03-18)
- [x] **FEAT-34** — Zoning and building on slopes (2026-03-16)
- [x] **FEAT-33** — Urban remodeling: expropriations and redevelopment (2026-03-12)
- [x] **FEAT-31** — Auto roads grow toward high desirability areas (2026-03-12)
- [x] **FEAT-30** — Mini map layer toggles + desirability visualization (2026-03-12)
- [x] **BUG-24** — Growth budget not recalculated when income changes (2026-03-12)
- [x] **BUG-06** — Streets should not cost so much energy (2026-03-12)
- [x] **FEAT-32** — More streets and intersections in central and mid-urban areas (AUTO mode) (2026-03-12)
- [x] **BUG-22** — Auto zoning must not block street segment ends (AUTO mode) (2026-03-11)
- [x] **FEAT-25** — Growth budget tied to real income (2026-03-11)
- [x] **BUG-10** — `IndustrialHeavyZoning` never generates buildings (2026-03-11)
- [x] **FEAT-26** — Use desirability for building spawn selection (2026-03-10)
- [x] **BUG-07** — Better zone distribution: less random, more homogeneous by neighbourhoods/sectors (2026-03-10)
- [x] **FEAT-29** — Density gradient around urban centroids (AUTO mode) (2026-03-10)
- [x] **FEAT-17** — Mini-map (2026-03-09)
- [x] **FEAT-01** — Add delta change to total budget (e.g. $25,000 (+$1,200)) (2026-03-09)
- [x] **BUG-03** — Growth % sets amount instead of percentage of total budget (2026-03-09)
- [x] **BUG-02** — Taxes do not work (2026-03-09)
- [x] **BUG-05** — Do not remove cursor preview from buildings when constructing (2026-03-09)
- [x] **BUG-21** — Zoning cost is not charged when placing zones (2026-03-09)
- [x] **FEAT-02** — Add construction cost counter to mouse cursor (2026-03-09)
- [x] **FEAT-28** — Right-click drag-to-pan (grab and drag map) with inertia/fling (2026-03-09)
- [x] **BUG-04** — Pause mode stops camera movement; camera speed tied to simulation speed (2026-03-09)
- [x] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths (2026-03-09)
- [x] **FEAT-27** — Main menu with Continue, New Game, Load City, Options (2026-03-08)
- [x] **BUG-11** — Demand uses `Time.deltaTime` causing framerate dependency (2026-03-11)
- [x] **BUG-21** — Demand fix: unemployment-based RCI, remove environmental from demand, desirability for density (2026-03-11)
- [x] **BUG-01** — Save game, Load game and New game were broken (2026-03-07)
- [x] **BUG-09** — `Cell.GetCellData()` does not serialize cell state (2026-03-07)
- [x] **DONE** — Forest cannot be placed adjacent to water (2026-03)
- [x] **DONE** — Demolish forests at all heights + all building types (2026-03)
- [x] **DONE** — When demolishing forest on slope, correct terrain prefab restored via heightMap read (2026-03)
- [x] **DONE** — Interstate Road (2026-03)
- [x] **DONE** — CityNetwork sim (2026-03)
- [x] **DONE** — Forests on slopes (2026-03)
- [x] **DONE** — Growth simulation — AUTO mode (2026-03)
- [x] **DONE** — Simulation optimization (2026-03)
- [x] **DONE** — Codebase improvement for efficient AI agent contextualization (2026-03)