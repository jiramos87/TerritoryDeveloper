# Backlog Archive — Territory Developer

> Completed issues archived from `BACKLOG.md`. Items completed before 2026-03-22.

---

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
