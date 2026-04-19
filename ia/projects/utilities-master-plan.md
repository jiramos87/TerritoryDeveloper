# Utilities — Master Plan (Bucket 4-a MVP)

> **Last updated:** 2026-04-17
>
> **Status:** In Progress — Step 1 / Stage 1.1 (4 tasks filed: TECH-331..TECH-334, all Draft)
>
> **Scope:** Utilities v1 — water / power / sewage as country-pool-first resources with local contributor buildings feeding per-scale pools (city / region / country). EMA soft warning → cliff-edge deficit (freeze + happiness decay + desirability decay). Infrastructure category with 2–3 capacity-based upgrade tiers. Natural wealth seeds water pool via adjacency; forests / mountains ambient-only; sea → port/commerce. Landmarks contributor-registry contract owned here; landmark catalog plugs in via `RegisterWithMultiplier`. **OUT of scope:** landmarks proper (sibling `docs/landmarks-exploration.md` + future `landmarks-master-plan.md`), Zone S economy (Bucket 3), signal integration (Bucket 2), CityStats overhaul (Bucket 8), multi-scale core (Bucket 1), energy storage, rolling blackouts, grid-loss transfer, private operators, climate modifiers.
>
> **Exploration source:** `docs/utilities-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples, Review Notes).
>
> **Umbrella:** `ia/projects/full-game-mvp-master-plan.md` Bucket 4 row. Schema bump coordinates with Bucket 3 (`zone-s-economy`) — Bucket 3 owns the v3 schema jump; this plan stages additions against the v3 envelope (no mid-tier v2.x bump).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B — country-pool first, local contributors. Rejected A (local-only), C (signal-integrated), D (defer).
> - Pool accounting = instantaneous flow-rate + EMA(~5 ticks) warning; no stored capacity, no ring buffer.
> - Deficit = cliff-edge. Freeze expansion (spawn / road / auto / manual) + slow happiness decay + map-wide desirability decay. No rolling blackouts, no lighting effects.
> - Natural wealth: water body → water pool via Moore-adjacency of treatment building. Forests / mountains = ambient only (no pool feed). Sea → port commerce bonus.
> - Terrain-sensitive placement, no in-range indicator. Discover by try.
> - Infrastructure = own category, not Zone S. Basic tier ungated; 2–3 capacity tiers by output threshold (no tech tree).
> - Cross-scale rollup lossless (grid losses deferred post-MVP). Country deficit cascades down to child regions / cities.
> - Save schema: per-pool floats + contributor ids. `schemaVersion` bump coordinated with Bucket 3 (do not own migration).
> - Landmarks hook = `UtilityContributorRegistry.RegisterWithMultiplier`. Contract owned here, consumed by sibling landmarks doc.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Read first if landing cold:**
> - `docs/utilities-exploration.md` — full design + architecture + deficit-entry example + save JSON sample. Design Expansion block is ground truth.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella Bucket 4 row + schema-bump coordination rule (§Gap B3).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons), #5 (no direct `cellArray` outside `GridManager` — helper-service carve-out), #6 (do not add responsibilities to `GridManager`).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; `spec_section save load-pipeline` for persistence step ordering; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Stage 1 — Pool core + contributor registry / Data contracts + enums

**Status:** In Progress — 4 tasks filed (TECH-331..TECH-334, all Draft)

**Objectives:** Define the five value types + two interfaces the services operate on. No runtime logic — just typed scaffolding other stages consume.

**Exit:**

- `UtilityKind` enum (`Water`, `Power`, `Sewage`), `ScaleTag` enum (`City`, `Region`, `Country`), `PoolStatus` enum (`Healthy`, `Warning`, `Deficit`).
- `PoolState` struct w/ `float net`, `float ema`, `PoolStatus status`, `int consecutiveNegativeEmaTicks`, `int consecutivePositiveEmaTicks` (hysteresis counters).
- `IUtilityContributor` interface: `UtilityKind Kind`, `float ProductionRate`, `ScaleTag Scale`.
- `IUtilityConsumer` interface: `UtilityKind Kind`, `float ConsumptionRate`, `ScaleTag Scale`.
- Files compile clean (`npm run unity:compile-check`); no references to the new types from runtime code yet.
- Phase 1 — Enum + struct scaffolding.
- Phase 2 — Interface contracts + assembly wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Utility enums | **TECH-331** | Draft | Add `Assets/Scripts/Data/Utilities/UtilityKind.cs`, `ScaleTag.cs`, `PoolStatus.cs` — plain enums, no behavior. XML doc each value (e.g. `Water` → "potable supply pool"). |
| T1.2 | PoolState struct | **TECH-332** | Draft | Add `Assets/Scripts/Data/Utilities/PoolState.cs` — blittable struct w/ `net`, `ema`, `status`, `consecutiveNegativeEmaTicks`, `consecutivePositiveEmaTicks`. Default ctor sets `Healthy` + zeros. |
| T1.3 | Contributor/consumer interfaces | **TECH-333** | Draft | Add `IUtilityContributor.cs` + `IUtilityConsumer.cs` under `Assets/Scripts/Data/Utilities/`. Read-only properties; implementations land in Step 2. |
| T1.4 | Assembly + compile check | **TECH-334** | Draft | Add `Utilities.asmdef` under `Assets/Scripts/Data/Utilities/` (if repo uses asmdefs) OR ensure types live in main asm; run `npm run unity:compile-check` green. |

### Stage 2 — Pool core + contributor registry / UtilityPoolService (per-scale)

**Objectives:** Implement the per-scale service: tick `ComputeNet()`, `UpdateEma()` (5-tick window), threshold state machine w/ hysteresis, `PoolStatusChanged` event. Parent pointer for rollup; does NOT yet consume contributors (Step 2 wires real producers) — use an internal `Sum(IEnumerable<IUtilityContributor>)` + `Sum(IEnumerable<IUtilityConsumer>)` so tests inject fakes.

**Status:** Draft (tasks _pending_ — not yet filed)

**Exit:**

- `UtilityPoolService : MonoBehaviour` w/ `[SerializeField] private ScaleTag scale`, `[SerializeField] private UtilityPoolService parent` (nullable — Country has none), `Dictionary<UtilityKind, PoolState> pools`.
- `OnEnable` / `Awake` seeds three pools (Water / Power / Sewage) to `Healthy` defaults; no singletons, `FindObjectOfType` fallback pattern per invariant #4.
- `TickPools(IReadOnlyList<IUtilityContributor> prods, IReadOnlyList<IUtilityConsumer> cons)` — testable entry point.
- Threshold rule per Implementation Points §1: `Warning` on EMA < 0 for ≥3 consecutive ticks; `Deficit` on `net ≤ 0 AND ema ≤ -0.2 × max(prodSum, consSum)`. Exit Deficit only when EMA > 0 for ≥3 consecutive ticks.
- `event Action<UtilityKind, PoolStatus, PoolStatus> PoolStatusChanged` fires on transition (kind, from, to).
- EditMode tests: Healthy→Warning→Deficit→Warning→Healthy round trip w/ synthetic tick stream.
- Phase 1 — Service scaffolding + pool initialization.
- Phase 2 — EMA + threshold state machine.
- Phase 3 — EditMode tests for transitions.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Service scaffold + seed | _pending_ | _pending_ | Add `UtilityPoolService.cs` as MonoBehaviour; `[SerializeField] private ScaleTag scale`, `[SerializeField] private UtilityPoolService parent`; `Awake` initializes `Dictionary<UtilityKind, PoolState>` w/ all three kinds in `Healthy`. |
| T2.2 | TickPools entry point | _pending_ | _pending_ | Implement `TickPools(prods, cons)` summing by kind; updates `net` on each pool. No EMA / status transition yet — that's T1.2.3. |
| T2.3 | EMA + threshold state machine | _pending_ | _pending_ | Implement 5-tick EMA (`α = 2/(5+1) ≈ 0.333`); apply state-machine rule per Implementation Points §1 w/ hysteresis counters. Fire `PoolStatusChanged` on transition. |
| T2.4 | EditMode transition tests | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Utilities/UtilityPoolServiceTests.cs` — drive synthetic contributor/consumer lists across ≥20 ticks, assert Healthy→Warning→Deficit→Warning→Healthy with correct hysteresis gate counts. |

### Stage 3 — Pool core + contributor registry / UtilityContributorRegistry

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Central registration surface for producers / consumers. Separates bookkeeping from pool math. Exposes `RegisterWithMultiplier` for landmarks hook.

**Exit:**

- `UtilityContributorRegistry : MonoBehaviour` w/ `[SerializeField] private UtilityPoolService cityPool / regionPool / countryPool` and internal `List<(IUtilityContributor, float mult)>` / `List<IUtilityConsumer>` keyed by `ScaleTag`.
- API: `Register(IUtilityContributor)`, `RegisterWithMultiplier(IUtilityContributor, float)`, `Deregister(IUtilityContributor)`, `RegisterConsumer(IUtilityConsumer)`, `DeregisterConsumer(IUtilityConsumer)`.
- `GetContributors(ScaleTag)` / `GetConsumers(ScaleTag)` — read-only views the `SimulationManager` hands to `UtilityPoolService.TickPools`.
- EditMode tests: round-trip register / deregister; multiplier applied to `ProductionRate` in the view; scale filtering correct.
- Phase 1 — Registry data structures + register/deregister API.
- Phase 2 — Scale-filtered view helpers + multiplier application.
- Phase 3 — EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Registry MonoBehaviour scaffold | _pending_ | _pending_ | Add `UtilityContributorRegistry.cs` — `[SerializeField]` slots for the three `UtilityPoolService` refs, internal `Dictionary<ScaleTag, List<(IUtilityContributor, float)>>` + consumer list. `Awake` with `FindObjectOfType` fallbacks. |
| T3.2 | Register / deregister API | _pending_ | _pending_ | Implement `Register` / `Deregister` / `RegisterConsumer` / `DeregisterConsumer`. Guard against duplicate add; log warn on missing remove. |
| T3.3 | RegisterWithMultiplier + view helpers | _pending_ | _pending_ | Implement `RegisterWithMultiplier(IUtilityContributor, float)`; add `GetContributors(ScaleTag)` returning a wrapped `IUtilityContributor` whose `ProductionRate` = raw × multiplier. Landmarks consume this in Step 4. |
| T3.4 | Registry EditMode tests | _pending_ | _pending_ | Add `UtilityContributorRegistryTests.cs` — register / deregister round trip; multiplier applied; scale filtering; duplicate-add guard. |

### Stage 4 — Pool core + contributor registry / Rollup + deficit cascade + DeficitResponseService skeleton

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire cross-scale rollup (child surplus → parent, 1:1 lossless) and deficit cascade (parent Deficit → child `FreezeFlags.Expansion = true`). Land `DeficitResponseService` as event subscriber w/ `FreezeFlags.Expansion` flag — happiness / desirability coroutines land in Step 3, just the flag + event plumbing here. Integrate tick order into `SimulationManager` so pools update each sim tick.

**Exit:**

- `UtilityPoolService.RollupToParent()` — adds child `net` (clamped ≥ 0 — surplus only) to parent's synthetic producer list. Country-scale service has no rollup step.
- `UtilityPoolService.InheritDeficitFromParent(PoolStatus)` — when any parent pool `Deficit`, child raises `ForcedDeficit` flag (does not overwrite own status math; stacks w/ local status for freeze-flag purposes).
- `DeficitResponseService : MonoBehaviour` subscribes to all three `UtilityPoolService.PoolStatusChanged` + also listens to parent's forced-deficit broadcasts. Sets `public bool ExpansionFrozen { get; private set; }` when ANY scale reports Deficit OR ForcedDeficit for ANY kind.
- `FreezeFlags.Expansion` exposed as `DeficitResponseService.ExpansionFrozen` (no global singleton — consumers `FindObjectOfType` at Awake).
- `SimulationManager.Tick()` updated to call registry → `TickPools` in order City → Region → Country, then rollup Country ← Region ← City.
- EditMode tests: child surplus rolls to parent; country deficit freezes all children; recovery clears freeze.
- Phase 1 — Rollup + cascade math on `UtilityPoolService`.
- Phase 2 — `DeficitResponseService` flag + event subscriptions.
- Phase 3 — `SimulationManager` tick integration.
- Phase 4 — Integration EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Rollup math | _pending_ | _pending_ | Implement `UtilityPoolService.RollupToParent()` — for each kind, `parent.pools[kind].net += max(0, this.pools[kind].net)`. Document 0% loss invariant (grid loss deferred post-MVP). |
| T4.2 | Deficit cascade | _pending_ | _pending_ | Implement `InheritDeficitFromParent` — parent raises event `ForcedDeficitChanged(UtilityKind, bool)`, child service sets a per-kind `bool forcedDeficit` mask. |
| T4.3 | DeficitResponseService skeleton | _pending_ | _pending_ | Add `DeficitResponseService.cs` MonoBehaviour — subscribe to three pools' `PoolStatusChanged` + forced-deficit events in `OnEnable`; expose `public bool ExpansionFrozen`; unsubscribe in `OnDisable`. No coroutines yet. |
| T4.4 | SimulationManager tick wiring | _pending_ | _pending_ | Edit `SimulationManager.cs` tick loop — after demand compute, before CityStats read: registry views per scale → `TickPools(city)` → `TickPools(region)` → `TickPools(country)` → rollups bottom-up. Cache refs in `Awake` (invariant #3). |
| T4.5 | Integration EditMode tests | _pending_ | _pending_ | Add `UtilityRollupCascadeTests.cs` — surplus rolls 1:1; country deficit → all scales' `ExpansionFrozen = true`; recovery clears. Use test-scene fixture w/ three services + registry + response service. |

### Stage 5 — Infrastructure buildings + terrain-sensitive placement / Infrastructure category + building def SO

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Data-model scaffolding for infrastructure buildings. Category tag, ScriptableObject def, five authored archetype assets. No runtime placement yet.

**Exit:**

- `InfrastructureCategory` enum distinct from Zone S classification.
- `InfrastructureBuildingDef` SO w/ fields listed in Step 2 exit criteria; `OnValidate` clamps `tierCount` to 2–3, `tierThresholds.Length == tierCount - 1`, `tierMultipliers.Length == tierCount`.
- Five archetype `.asset` files authored + fields populated (rates, thresholds, terrain requirements, costs).
- `TerrainRequirement` enum (`None`, `AdjacentWater`, `AdjacentWaterPollutesDownstream`, `Mountain`, `OpenTerrain`).
- Phase 1 — Category + terrain-requirement enums.
- Phase 2 — `InfrastructureBuildingDef` SO w/ `OnValidate`.
- Phase 3 — Author five archetype assets.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | InfrastructureCategory enum | _pending_ | _pending_ | Add `Assets/Scripts/Data/Buildings/InfrastructureCategory.cs` — enum distinct from existing Zone S tags. XML doc explains split rationale (own cost line, not Zone S budget). |
| T5.2 | TerrainRequirement enum | _pending_ | _pending_ | Add `TerrainRequirement.cs` — `None`, `AdjacentWater`, `AdjacentWaterPollutesDownstream`, `Mountain`, `OpenTerrain`. Consumed by placement validator in Stage 2.2. |
| T5.3 | InfrastructureBuildingDef SO | _pending_ | _pending_ | Add `InfrastructureBuildingDef.cs` ScriptableObject — `UtilityKind kind`, `float baseProductionRate`, `TerrainRequirement terrainReq`, `int tierCount`, `float[] tierThresholds`, `float[] tierMultipliers`, `int constructionCost`, `int dailyMaintenance`. `OnValidate` clamps tier arrays. |
| T5.4 | Author 5 archetype assets | _pending_ | _pending_ | Create `Assets/Data/Infrastructure/CoalPlant.asset`, `SolarFarm.asset`, `WindFarm.asset`, `WaterTreatment.asset`, `SewageTreatment.asset`. Populate rates + terrain reqs per Implementation Points §9. |

### Stage 6 — Infrastructure buildings + terrain-sensitive placement / Placement validators + freeze gate

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Terrain-sensitive placement: check Moore-adjacent water for water/sewage treatment; terrain tag for large regionals (hydro / wind). No in-range indicator. Also gate manual placement when `ExpansionFrozen` per Implementation Points §2.

**Exit:**

- `InfrastructureBuildingService.ValidatePlacement(def, x, y)` returns `Valid | InvalidTerrain | InvalidFrozen`; no UI indicator emitted (discover-by-try per locked decision).
- Helper uses Moore-adjacent cell query via `GridManager.GetCell(x, y)` (invariant #5 — service is under `GameManagers/*Service.cs` carve-out, may touch `grid.cellArray` if needed; document reason).
- Manual placement entry point in existing `BuildingPlacementService` + `ZoneManager` placement path checks `DeficitResponseService.ExpansionFrozen` before calling validator.
- EditMode tests: validator returns correct verdict per terrain + freeze state.
- Phase 1 — Service + terrain checks.
- Phase 2 — Freeze-gate integration into placement entry points.
- Phase 3 — Validator EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | InfrastructureBuildingService scaffold | _pending_ | _pending_ | Add `InfrastructureBuildingService.cs` (MonoBehaviour, helper under `GameManagers/*Service.cs` per invariant #5 carve-out). `[SerializeField] private GridManager grid` + `FindObjectOfType` fallback. |
| T6.2 | Terrain validators | _pending_ | _pending_ | Implement per-`TerrainRequirement` check: `AdjacentWater` → any Moore-neighbor `CellData.IsWater`; `Mountain` → neighbor `heightTier >= MountainThreshold`; `OpenTerrain` → no buildings within radius 2. Document `cellArray` touch rationale per carve-out. |
| T6.3 | Freeze-gate wiring | _pending_ | _pending_ | Edit `BuildingPlacementService.cs` + `ZoneManager.cs` manual-placement entry points — early-return `InvalidFrozen` if `DeficitResponseService.ExpansionFrozen`. Cache the service ref in `Awake` (invariant #3). |
| T6.4 | Auto-path freeze-gate | _pending_ | _pending_ | Edit `AutoZoningManager.TrySpawn()` + `AutoRoadBuilder.ExtendRoad()` — check `ExpansionFrozen` before spawn / extend per Implementation Points §2. Single source of truth flag, no ad-hoc checks. |
| T6.5 | Placement EditMode tests | _pending_ | _pending_ | Add `InfrastructureBuildingTests.cs` validator suite — terrain matrix (water / mountain / open / forbidden) × freeze flag on/off. Assert verdict enums. |

### Stage 7 — Infrastructure buildings + terrain-sensitive placement / Placement lifecycle + registry wiring + tier promotion

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** On successful placement: instantiate contributor, register with `UtilityContributorRegistry`; on demolition: deregister. Tier promotion driven by output threshold (not player input, not tech tree). Integrates with `EconomyManager` for construction cost + per-day maintenance.

**Exit:**

- `InfrastructureBuildingService.Place(def, x, y)` — instantiates prefab, attaches `InfrastructureContributor` component implementing `IUtilityContributor`, calls `registry.Register(contributor)`. Returns placed building ref.
- `InfrastructureContributor : MonoBehaviour, IUtilityContributor` — reads def, reports current tier's `ProductionRate = base × tierMultiplier[currentTier]`. Tier recomputed on each game-day when service calls `PromoteIfEligible`.
- `InfrastructureBuildingService.Demolish(building)` deregisters contributor before destroying GameObject.
- `EconomyManager` deducts `def.constructionCost` on place + `def.dailyMaintenance` on `OnGameDay`. Tracked under separate infrastructure line (not Zone S budget).
- EditMode tests: place→registered, demolish→deregistered, tier promotes at threshold, maintenance deducted per day.
- Phase 1 — `InfrastructureContributor` component + place/demolish hooks.
- Phase 2 — Tier promotion logic.
- Phase 3 — `EconomyManager` cost/maintenance line.
- Phase 4 — Lifecycle EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | InfrastructureContributor component | _pending_ | _pending_ | Add `Assets/Scripts/Buildings/Infrastructure/InfrastructureContributor.cs` — MonoBehaviour implementing `IUtilityContributor`. Reads `InfrastructureBuildingDef def` + `int currentTier`. `ProductionRate` getter computes `def.baseProductionRate × def.tierMultipliers[currentTier]`. |
| T7.2 | Place + register hook | _pending_ | _pending_ | Implement `InfrastructureBuildingService.Place(def, x, y)` — instantiate prefab, attach `InfrastructureContributor`, call `registry.Register(contributor)`. Wire into existing `BuildingPlacementService` dispatch. |
| T7.3 | Demolish + deregister hook | _pending_ | _pending_ | Implement `InfrastructureBuildingService.Demolish(building)` — `registry.Deregister(contributor)` before `Destroy(go)`. Wire into demolition entry point. |
| T7.4 | Tier promotion | _pending_ | _pending_ | Implement `PromoteIfEligible(contributor)` called on `OnGameDay` — compare accumulated output vs `def.tierThresholds[currentTier]`; increment `currentTier` (clamped to `tierCount - 1`) when exceeded. No demotion. |
| T7.5 | EconomyManager infrastructure line | _pending_ | _pending_ | Edit `EconomyManager.cs` — new `infrastructureBudget` line. `DeductConstruction(def.constructionCost)` on place; `DeductMaintenance(Σ def.dailyMaintenance)` on `OnGameDay`. Distinct from Zone S budget. |
| T7.6 | Lifecycle EditMode tests | _pending_ | _pending_ | Add tests: place → registered in registry; demolish → deregistered; tier promotes at threshold; maintenance deducted daily; construction cost deducted on place. |

### Stage 8 — Infrastructure buildings + terrain-sensitive placement / Natural wealth adjacency probe

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Water treatment gets a per-adjacent-water-cell production bonus via `WaterBodyAdjacencyProbe`. Forests / mountains stay ambient-only (no pool feed — locked). Sea access bonus flagged for Bucket 4-b / landmarks as TODO-comment link (not implemented here).

**Exit:**

- `WaterBodyAdjacencyProbe.cs` MonoBehaviour helper — given a placed water-treatment building, counts Moore-adjacent `CellData.IsWater` cells, adds a synthetic `IUtilityContributor` (`kind = Water`, rate = cells × perCellBonus) registered via `registry.Register`. Deregistered on demolish.
- Scriptable value `perCellBonus` (e.g. 0.5/tick) tunable on `InfrastructureBuildingDef` extension `waterAdjacencyBonusPerCell`.
- Forests / mountains bonus surface is left untouched (no edits to `ForestManager.cs`; ambient bonus already flows through existing air-quality path).
- TODO comment in `WaterBodyAdjacencyProbe.cs` linking sea-access bonus to Bucket 4-b landmarks doc.
- EditMode tests: 0 / 1 / 4 adjacent water cells → production = 0 / 0.5 / 2.0 per tick.
- Phase 1 — Probe + synthetic contributor.
- Phase 2 — Def field + lifecycle hook.
- Phase 3 — Probe EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | WaterBodyAdjacencyProbe scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/WaterBodyAdjacencyProbe.cs` — helper service (invariant #5 carve-out); `[SerializeField] GridManager grid`. Expose `int CountAdjacentWater(int x, int y)` using 8-neighbor Moore walk via `grid.GetCell`. |
| T8.2 | Synthetic water-bonus contributor | _pending_ | _pending_ | Add nested `WaterAdjacencyBonusContributor : IUtilityContributor` — rate computed at register time from probe count × `perCellBonus`. Registered on water-treatment place, deregistered on demolish. |
| T8.3 | Def bonus field + hook | _pending_ | _pending_ | Extend `InfrastructureBuildingDef` w/ `float waterAdjacencyBonusPerCell` (default 0.5). `InfrastructureBuildingService.Place` calls probe + registers bonus when def has nonzero field + terrainReq is `AdjacentWater`. |
| T8.4 | Probe EditMode tests | _pending_ | _pending_ | Add `WaterBodyAdjacencyProbeTests.cs` — fixture grid w/ water clusters; assert 0/1/4/8 neighbor counts; assert synthetic contributor rate = count × bonus. |

### Stage 9 — Deficit response + UI dashboard / Happiness + desirability decay coroutines

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the two decay effects under `DeficitResponseService`. Happiness penalty accumulator + desirability decay through new `GeographyManager` helper (invariant #6 extraction).

**Exit:**

- `DeficitResponseService.HappinessPenalty` (int, -20..0) accumulates −1/game-day while `AnyDeficit`; resets to 0 when all pools recover.
- `GeographyManager.ApplyGlobalDesirabilityDelta(float mult)` — new public method, loops `grid.cellArray`, `cell.desirability = max(0, cell.desirability * mult)`. XML doc cites invariant #5 carve-out rationale.
- `DeficitResponseService` calls `ApplyGlobalDesirabilityDelta(0.98f)` per game-day while Deficit.
- EditMode tests: penalty arithmetic (floor -20, rises at -1/day); desirability floor 0; no decay when pools healthy.
- Phase 1 — Happiness penalty accumulator.
- Phase 2 — `GeographyManager.ApplyGlobalDesirabilityDelta` helper.
- Phase 3 — Decay EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | HappinessPenalty accumulator | _pending_ | _pending_ | Add `HappinessPenalty` field + `OnGameDay` handler on `DeficitResponseService` — `-=1` while `AnyDeficit`, clamp `[-20, 0]`; reset to 0 when all scales Healthy. |
| T9.2 | AnyDeficit helper | _pending_ | _pending_ | Add `DeficitResponseService.AnyDeficit` property — true when any tracked `(scale, kind)` pool has `status == Deficit` OR `forcedDeficit == true`. |
| T9.3 | GeographyManager desirability helper | _pending_ | _pending_ | Edit `GeographyManager.cs` — add `public void ApplyGlobalDesirabilityDelta(float multiplier)`. Loop `grid.cellArray`, `cell.desirability = Mathf.Max(0f, cell.desirability * multiplier)`. XML doc invariant #5 carve-out. |
| T9.4 | Desirability decay hook | _pending_ | _pending_ | `DeficitResponseService.OnGameDay` — while `AnyDeficit`, call `geography.ApplyGlobalDesirabilityDelta(0.98f)`. Cache `GeographyManager` ref in `Awake` (invariant #3). |
| T9.5 | Decay EditMode tests | _pending_ | _pending_ | Add `DeficitResponseTests.cs` — penalty arithmetic per day w/ deficit on/off transitions; desirability floor 0; decay skipped when healthy. |

### Stage 10 — Deficit response + UI dashboard / CityStats + DemandManager readers

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** `CityStats.HappinessTarget` subtracts `HappinessPenalty`. `DemandManager` floors RCI demand when matching utility Deficit (e.g. Industrial floor while Power Deficit). Complete Subsystem Impact wiring.

**Exit:**

- `CityStats.ComputeHappinessTarget()` subtracts `deficitResponse.HappinessPenalty` before existing lerp.
- `DemandManager.GetDemand(RCI kind)` applies a multiplier floor (e.g. `0.3f`) when the mapping table (`RCI → UtilityKind`) reports Deficit: R → Water, C → Power, I → Power + Sewage (table documented in method XML).
- EditMode tests: penalty subtracted from happiness target; demand floor applied when matching Deficit raised.
- Phase 1 — CityStats wiring.
- Phase 2 — DemandManager floor + mapping.
- Phase 3 — Reader EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | CityStats happiness subtract | _pending_ | _pending_ | Edit `CityStats.cs` — `ComputeHappinessTarget` fetches `deficitResponse.HappinessPenalty` (cached ref, invariant #3) and subtracts before existing lerp. |
| T10.2 | RCI→UtilityKind mapping | _pending_ | _pending_ | Add `DemandManager.RciUtilityDependency` static readonly table: R→{Water}, C→{Power}, I→{Power, Sewage}. |
| T10.3 | DemandManager demand floor | _pending_ | _pending_ | `DemandManager.GetDemand(rci)` multiplies by `0.3f` if any mapped utility reports Deficit at city scale. Reads `UtilityPoolService.pools[kind].status`. |
| T10.4 | Reader EditMode tests | _pending_ | _pending_ | Add reader tests — happiness target subtracts penalty correctly; demand floored when Power Deficit (I), Water Deficit (R), etc. |

### Stage 11 — Deficit response + UI dashboard / UIManager utilities dashboard + HUD indicator

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Per-scale dashboard panel in `UIManager.Utilities.cs` — 3×3 grid (scale × kind) w/ net / EMA / status colour. HUD indicator top-bar glyph for any Deficit. Info panel on infrastructure building shows tier + production rate.

**Exit:**

- `UIManager.Utilities.cs` renders dashboard panel invoked from existing utilities toolbar entry; rows = Water / Power / Sewage, columns = City / Region / Country. Each cell shows `net` (signed float), `ema` (signed float), status dot color.
- `UIManager.Hud.cs` top-bar glyph flips colour Healthy→Warning→Deficit per worst pool status across all scales.
- Info panel (existing per-building path) reads `InfrastructureContributor` fields + renders tier label + production rate.
- PlayMode smoke test: debug command forces Deficit, dashboard + HUD reflect within one tick.
- Phase 1 — Dashboard panel render.
- Phase 2 — HUD indicator + info panel.
- Phase 3 — PlayMode smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Utilities dashboard panel layout | _pending_ | _pending_ | Edit `UIManager.Utilities.cs` — add `BuildUtilitiesDashboard()` creating UGUI grid (rows Water/Power/Sewage × cols City/Region/Country). Placeholder cell labels, no live data yet. |
| T11.2 | Dashboard live bindings | _pending_ | _pending_ | Wire dashboard cells to `UtilityPoolService[scale].pools[kind].{net, ema, status}`; refresh on `PoolStatusChanged` event + per-game-day. Cache service refs in `Awake` (invariant #3). |
| T11.3 | HUD deficit glyph | _pending_ | _pending_ | Edit `UIManager.Hud.cs` — add utility-status glyph; colour = worst status across all (scale, kind). Subscribe to `PoolStatusChanged`. |
| T11.4 | Info panel contributor readout | _pending_ | _pending_ | Edit existing building info-panel renderer — when target is `InfrastructureContributor`, show `def.kind`, `currentTier`, `ProductionRate`. |
| T11.5 | PlayMode deficit smoke | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Utilities/UtilityPlayModeSmoke.cs` — debug-command forces pool to Deficit; assert dashboard cell flips red, HUD glyph red, `ExpansionFrozen == true`. Recover → green. |

### Stage 12 — Save/load + landmarks hook + glossary/spec closeout / Save/load schema + restore pipeline

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Serialize + deserialize pool state against Bucket 3's v3 schema envelope. Wire restore into existing load pipeline after grid cells land. No new `schemaVersion` bump owned here — defer to Bucket 3.

**Exit:**

- `UtilityPoolsDto` + `PoolStateDto` structs serialized by `GameSaveManager`.
- Write path: serializes `UtilityPoolService[scale].pools` → `utilityPoolsData` per-scale per-kind `{net, ema, status}`.
- Read path: restore step 5 (after grid cells) — hydrates pools, THEN existing building-restore path re-instantiates infrastructure buildings which re-register via lifecycle hooks (no separate contributor-id persistence beyond building placements; contributor list rebuilds from buildings).
- Schema bump: code comment `// schemaVersion bump owned by Bucket 3 (zone-s-economy) — utilities stages against v3 envelope only` on the new section.
- Guard against reading `utilityPoolsData` when version < 3 — skip section, leave pools at `Healthy` default.
- Phase 1 — DTOs + write path.
- Phase 2 — Read path + restore hook.
- Phase 3 — Round-trip PlayMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | PoolState DTOs | _pending_ | _pending_ | Add `Assets/Scripts/Data/Utilities/PoolStateDto.cs` + `UtilityPoolsDto.cs` — serializable mirrors of `PoolState` + `Dictionary<ScaleTag, Dictionary<UtilityKind, PoolStateDto>>`. |
| T12.2 | GameSaveManager write | _pending_ | _pending_ | Edit `GameSaveManager.cs` — serialize all three `UtilityPoolService.pools` into `utilityPoolsData` section. Comment pins Bucket 3 v3 schema coordination. |
| T12.3 | Load pipeline restore hook | _pending_ | _pending_ | Edit load pipeline — add restore step AFTER grid cells. Hydrate pool dictionaries; guard `if (saveData.schemaVersion >= 3)`. Note: building re-registration handled by existing building restore path, which runs before pool restore so contributor list rebuilds. Reorder if needed. |
| T12.4 | Contributor rebuild ordering verification | _pending_ | _pending_ | Verify existing building-restore path re-instantiates infrastructure building prefabs → `InfrastructureContributor.OnEnable` calls `registry.Register`. If ordering reversed, move pool-state restore AFTER building restore so registry repopulates first. Document final step number in `ia/specs/persistence-system.md`. |
| T12.5 | Save round-trip PlayMode test | _pending_ | _pending_ | Add `UtilitySaveRoundTripTests.cs` — place coal plant + water treatment, tick to Warning state, save, reload, assert `pools[Power].status == Warning`, contributor registry repopulated, `ExpansionFrozen` restored. |

### Stage 13 — Save/load + landmarks hook + glossary/spec closeout / Canonical spec + glossary closeout + landmarks contract freeze

**Status:** In Progress (BUG-20 filed)

**Objectives:** Author `ia/specs/utility-system.md` per invariant #12 (utilities = permanent domain). Link glossary rows from Step 1 to this spec. Freeze landmarks registry contract documentation. Note BUG-20 orthogonal status.

**Exit:**

- `ia/specs/utility-system.md` new spec — covers pool state machine, EMA thresholds, rollup math, deficit cascade, contributor lifecycle, natural-wealth adjacency, `RegisterWithMultiplier` landmarks hook. Frontmatter per `ia/templates/spec-template.md`.
- Glossary rows (added Step 1.1 / 1.2) updated to reference `ia/specs/utility-system.md` as `specReference`.
- Sibling `docs/landmarks-exploration.md` cross-linked: landmarks-contract section in the new spec is marked authoritative; landmarks doc consumes.
- BUG-20 note in `ia/specs/utility-system.md` §BUG-20 interaction — orthogonal, not resolved by this plan.
- MCP spec index regenerated (`npm run validate:all` includes this).
- Phase 1 — Spec authoring.
- Phase 2 — Glossary link updates + MCP index regen.
- Phase 3 — Landmarks-doc cross-link.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | Author utility-system.md | _pending_ | _pending_ | Create `ia/specs/utility-system.md` using `ia/templates/spec-template.md` — sections: §State machine, §Rollup + cascade, §Contributor lifecycle, §Natural wealth adjacency, §Landmarks hook contract, §BUG-20 interaction, §Save schema. |
| T13.2 | Spec prose + invariants | _pending_ | _pending_ | Fill spec sections — cite invariants #3, #4, #5, #6 at relevant touchpoints; copy state-machine pseudocode from exploration Implementation Points §1; link architecture diagram from exploration §Architecture. |
| T13.3 | Glossary specReference updates | _pending_ | _pending_ | Edit `ia/specs/glossary.md` rows added in Step 1 (Utility pool, Utility contributor, Utility consumer, Pool status, Freeze flag, EMA warning, Deficit cascade) — set `specReference` to `utility-system §{section}`. |
| T13.4 | MCP index regen | _pending_ | _pending_ | Run `npm run validate:all` → regenerates `tools/mcp-ia-server/data/glossary-index.json`, `glossary-graph-index.json`, `spec-index.json` including new spec + updated glossary rows. Commit regen artifacts w/ spec. |
| T13.5 | Landmarks doc cross-link + BUG-20 note | _pending_ | _pending_ | Edit `docs/landmarks-exploration.md` — reference `ia/specs/utility-system.md §Landmarks hook contract` as authoritative. Add BUG-20 orthogonal note to spec's §BUG-20 interaction. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/utilities-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/utilities-exploration.md` §Design Expansion.
- Keep this orchestrator synced with `ia/projects/full-game-mvp-master-plan.md` Bucket 4 row — per `project-spec-close` / `closeout` umbrella-sync rule.
- Coordinate schema bump with `ia/projects/zone-s-economy-master-plan.md` Step 1. Never introduce a mid-tier `schemaVersion` bump from this plan — Bucket 3 owns v3.
- Keep `UtilityContributorRegistry.RegisterWithMultiplier` contract stable once Step 1.3 closes — sibling landmarks doc consumes it.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (energy storage, rolling blackouts, grid losses, private operators, climate modifiers, tier visual variants, sea-access commerce bonus impl) into MVP stages — they belong in a future `docs/utilities-post-mvp-extensions.md` stub.
- Add responsibilities to `GridManager` (invariant #6). Cell-loop helpers belong on `GeographyManager` or under `GameManagers/*Service.cs` carve-out (invariant #5). Document rationale at each touch site.
- Add singletons (invariant #4). All three services = MonoBehaviour + Inspector + `FindObjectOfType` fallback in `Awake`.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` + `npm run unity:compile-check`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Resolve BUG-20 in this plan — orthogonal to contributor registration; track separately.

---
