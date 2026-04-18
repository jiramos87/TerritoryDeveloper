# City-Sim Depth — Master Plan (Bucket 2 MVP)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Shared simulation-signal contract (12 signals) + district aggregation layer + migration of existing happiness/pollution scalar to `HappinessComposer` + 7 new simulation sub-surfaces (pollution split, crime, services, traffic, waste, construction evolution, density evolution + industrial sub-types) + signal overlays + HUD/district panel parity. Excludes Zone S / economy (Bucket 3), utilities (Bucket 4), CityStats UI overhaul (Bucket 8), per-vehicle pathing, animation pipeline (Bucket 5), region/country feedback consumers.
>
> **Exploration source:** `docs/city-sim-depth-exploration.md` (§Design Expansion — Architecture, Subsystem Impact, Implementation Points are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E (hybrid signal contract + district aggregation) — selected; criteria matrix clear.
> - Signal inventory fixed at 12 for MVP: `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`.
> - FEAT-43 toggle — explicit `bool useSignalDesirability`; NOT parallel A/B; old path disabled when new path on.
> - District auto-derivation from `UrbanCentroidService` centroid + ring bands (single-centroid MVP); player-drawn districts deferred.
> - `SignalField` data NOT persisted; `SignalWarmupPass` deterministic recompute on load.
> - `DistrictMap` + tuning weights ARE persisted; signal fields are NOT.
> - Per-frame signal updates deferred; MVP = daily or monthly tick only.
> - Rollup rules: `Crime` + `TrafficLevel` = P90; all other signals = mean.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/city-sim-depth-exploration.md` — full design + architecture + examples. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality (≥2 tasks per phase).
> - `ia/rules/invariants.md` — `#3` (no `FindObjectOfType` in Update/per-frame loops), `#4` (no new singletons — Inspector + `FindObjectOfType` fallback), `#5` (no direct `gridArray`/`cellArray` outside `GridManager`), `#6` (don't add responsibilities to `GridManager`), `#1` (`HeightMap` sync — signal systems read water/terrain cells but never write `HeightMap`).
> - `ia/specs/simulation-system.md` §Tick execution order — tick phase insertion point (signal phase inserts between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`).
> - `ia/specs/managers-reference.md` §Demand (R/C/I), §World features — current happiness + pollution API.
> - `ia/specs/persistence-system.md` §Load pipeline — save/load restore order.
> - `ia/specs/simulation-signals.md` — authored in Stage 1.1 (T1.1.4); signal inventory, rollup rules, diffusion physics contract.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

---

### Step 1 — Signal Layer Foundation

**Status:** In Progress — Stage 1.1

**Backlog state (Step 1):** 4 filed (TECH-305..TECH-308)

**Objectives:** Bootstrap the shared simulation-signal contract that all subsequent sub-surfaces depend on. Ships `SimulationSignal` enum (12 entries), `SignalField`/`SignalFieldRegistry` primitives, `ISignalProducer`/`ISignalConsumer` interfaces, `DiffusionKernel`, `SignalTickScheduler` wired into `SimulationManager.ProcessSimulationTick`, and the district aggregation layer (`DistrictMap`, `DistrictAggregator`, `DistrictSignalCache`, `DistrictManager`). No gameplay-visible effects — foundation only. Also closes the spec gap by authoring `ia/specs/simulation-signals.md`.

**Exit criteria:**

- `SignalFieldRegistry` on scene with 12 initialized `SignalField` instances sized from `GridManager` dimensions.
- `SignalTickScheduler.Tick(TickContext)` invoked from `SimulationManager.ProcessSimulationTick` between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick` (lines ~74–77 of `SimulationManager.cs`).
- `DiffusionKernel.Apply` passes Example 1 numeric assertions (PollutionAir source ≈2.7 at (10,10); forest-sink cell clamped to 0 at (15,15)).
- `DistrictSignalCache.Get(districtId, Crime)` returns P90; `Get(districtId, PollutionAir)` returns mean (Example 2 asserted).
- `ia/specs/simulation-signals.md` reference spec authored.
- `npm run validate:all` passes.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/city-sim-depth-exploration.md` §Architecture (Mermaid diagram, entry/exit points) + §Implementation Points 6.1–6.2
- `ia/specs/simulation-system.md` §Tick execution order
- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` line 61 — `ProcessSimulationTick` insertion point
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — `gridWidth`/`gridHeight` sizing source
- `Assets/Scripts/Managers/GameManagers/UrbanCentroidService.cs` — centroid + ring data for district derivation
- Invariants `#3`, `#4`, `#5`, `#6`
- `Assets/Scripts/Simulation/Signals/` (new dir)

---

#### Stage 1.1 — Signal Contract Primitives

**Status:** In Progress (4 tasks filed 2026-04-17 — TECH-305..TECH-308)

**Objectives:** Author core type surface — `SimulationSignal` enum, `SignalField`, `SignalMetadataRegistry` ScriptableObject, `ISignalProducer`/`ISignalConsumer` interfaces, `SignalFieldRegistry` MonoBehaviour — and the canonical `ia/specs/simulation-signals.md` reference spec that closes the spec gap flagged in the exploration review.

**Exit:**

- `SimulationSignal` enum has exactly 12 entries matching locked signal inventory.
- `SignalField.Get(x,y)` always returns ≥ 0 (floor clamp); `Snapshot()` returns independent copy.
- `SignalFieldRegistry` creates 12 fields on `Awake` sized from `GridManager`; `FindObjectOfType` fallback present (invariant #4).
- `ia/specs/simulation-signals.md` authored; linked from `ia/specs/simulation-system.md` §Tick execution order.
- `npm run validate:all` passes.

**Phases:**

- [ ] Phase 1 — Core types: enum + interfaces + SignalField + SignalMetadataRegistry.
- [ ] Phase 2 — SignalFieldRegistry MonoBehaviour + reference spec.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | SimulationSignal enum + interfaces | 1 | **TECH-305** | Draft | Author `SimulationSignal` enum (12 entries: `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`) in new `Assets/Scripts/Simulation/Signals/`. Author `ISignalProducer` (`void EmitSignals(SignalFieldRegistry)`) + `ISignalConsumer` (`void ConsumeSignals(SignalFieldRegistry, DistrictSignalCache)`) interfaces in same dir. |
| T1.1.2 | SignalField + SignalMetadataRegistry | 1 | **TECH-306** | Draft | `SignalField` — `float[,]` backing store; `Get(x,y)`, `Set(x,y,v)`, `Add(x,y,v)`, `Snapshot()` (returns new `float[,]` copy); clamp floor 0 on all writes. `SignalMetadataRegistry` ScriptableObject — per `SimulationSignal` entry: `diffusionRadius`, `decayPerStep`, `anisotropy (Vector2)`, `rollupRule (Mean/P90 enum)`. |
| T1.1.3 | SignalFieldRegistry MonoBehaviour | 2 | **TECH-307** | Draft | `SignalFieldRegistry` MonoBehaviour — allocates one `SignalField` per `SimulationSignal` in `Awake` sized from `GridManager.gridWidth`/`gridHeight`; `GetField(SimulationSignal)` accessor; `[SerializeField] GridManager grid` + `FindObjectOfType` fallback (invariant #4); resize method for map reload. |
| T1.1.4 | simulation-signals.md reference spec | 2 | **TECH-308** | Draft | Author `ia/specs/simulation-signals.md` — signal inventory table (12 entries: source types, sink types, rollup rule, update cadence per entry), diffusion physics contract (separable Gaussian, anisotropy, decay, clamp-floor-0 rule), `ISignalProducer`/`ISignalConsumer` interface contract, rollup rule table (P90 for `Crime`+`TrafficLevel`; mean for rest), spec-gap closure note. Link new spec from `ia/specs/simulation-system.md` §Tick execution order addendum. |

---

#### Stage 1.2 — DiffusionKernel + SignalTickScheduler

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the per-signal diffusion pass and the tick scheduler that orchestrates producer → diffusion → consumer flow per simulation tick. After this stage a seeded signal field diffuses and decays correctly.

**Exit:**

- `DiffusionKernel.Apply` passes Example 1 numeric assertions (±0.2 tolerance; no negative values anywhere in output).
- `SignalTickScheduler.Tick(TickContext)` called from `SimulationManager.ProcessSimulationTick` between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`.
- No `FindObjectOfType` in `Update` or per-frame loops — invariant #3 verified.
- EditMode DiffusionKernel test passes.

**Phases:**

- [ ] Phase 1 — DiffusionKernel implementation + test.
- [ ] Phase 2 — SignalTickScheduler MonoBehaviour + SimulationManager wiring.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | DiffusionKernel core | 1 | _pending_ | _pending_ | `DiffusionKernel` static class — `Apply(SignalField field, float[,] sourceAccum, SignalMetadata meta)`: reads `sourceAccum`, blurs via separable horizontal + vertical Gaussian passes (kernel radius from `meta.diffusionRadius`), decays by `meta.decayPerStep`, writes back to `field`; clamp floor 0 on output; anisotropy scales H vs V pass sigma from `meta.anisotropy`. |
| T1.2.2 | DiffusionKernel EditMode test | 1 | _pending_ | _pending_ | EditMode test — 64×64 `SignalField`; 3 heavy-industry sources at (10,10),(11,10),(10,11) weight +4.0; 5-cell forest sink centered (15,15) weight −0.5; `DiffusionSpec{Radius=6, DecayPerStep=0.15f, Anisotropy=(0,0)}`; run `Apply`; assert (10,10)≈2.7 ±0.2, neighbors ≈1.8–2.2, (15,15)=0 (clamped); assert zero negative values. |
| T1.2.3 | SignalTickScheduler MonoBehaviour | 2 | _pending_ | _pending_ | `SignalTickScheduler` MonoBehaviour — `List<ISignalProducer>` + `List<ISignalConsumer>` resolved once in `Awake` via `FindObjectsOfType` (cached — not called in `Update`; invariant #3); `Tick(TickContext)` loop: clear source accum buffers → call all producers → `DiffusionKernel.Apply` per signal → call all consumers; `[SerializeField]` refs for `SignalFieldRegistry`, `SignalMetadataRegistry`. |
| T1.2.4 | SimulationManager tick wiring | 2 | _pending_ | _pending_ | Edit `SimulationManager.ProcessSimulationTick` (line 61, `SimulationManager.cs`) — insert `if (signalTickScheduler != null) signalTickScheduler.Tick(ctx)` after `urbanCentroidService.RecalculateFromGrid()` call (~line 74) and before `autoRoadBuilder.ProcessTick()` (~line 77). Add `[SerializeField] SignalTickScheduler signalTickScheduler` field + `FindObjectOfType` fallback in `Start`. |

---

#### Stage 1.3 — District Layer

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the district aggregation layer: `DistrictMap` auto-derived from `UrbanCentroidService` centroid+rings, `DistrictManager` MonoBehaviour, `DistrictAggregator` mean/P90 rollup, `DistrictSignalCache` read API (including Bucket 8 `GetAll` stub), and save/load round-trip for `DistrictMap`.

**Exit:**

- `DistrictMap` assigns each cell a district id by ring band from `UrbanCentroidService`.
- `DistrictSignalCache.Get(districtId, Crime)` returns P90; `Get(districtId, PollutionAir)` returns mean; empty district returns `NaN`.
- `DistrictMap` round-trips through `GameSaveManager` save/load without corruption.
- `npm run validate:all` passes.

**Phases:**

- [ ] Phase 1 — District data model + DistrictManager.
- [ ] Phase 2 — DistrictAggregator + DistrictSignalCache + save/load.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | District + DistrictMap | 1 | _pending_ | _pending_ | `District` record (`int id`, `string name`) + `DistrictMap` (`int[,]` sized from `GridManager`); `Assign(x,y,districtId)`, `Get(x,y)`. Auto-derivation: district id = ring-band index from `UrbanCentroidService.GetRingBand(x,y)` (single-centroid MVP per locked decision). |
| T1.3.2 | DistrictManager MonoBehaviour | 1 | _pending_ | _pending_ | `DistrictManager` MonoBehaviour — owns `DistrictMap`; `RederiveFromCentroid()` called after `UrbanCentroidService.RecalculateFromGrid` each tick via `SignalTickScheduler` pre-pass; exposes `GetDistrictId(x,y)` + `DistrictCount`; `[SerializeField] UrbanCentroidService` + `FindObjectOfType` fallback; invariant #6 (new MonoBehaviour, not added to `GridManager`). |
| T1.3.3 | DistrictAggregator | 2 | _pending_ | _pending_ | `DistrictAggregator` — called by `SignalTickScheduler` after diffusion pass; for each signal, iterates all cells bucketing values by district id; computes mean or P90 per `SignalMetadataRegistry.rollupRule`; P90 impl: sort bucket, index at `(int)floor(n * 0.9)`; writes result to `DistrictSignalCache`; empty district (0 cells) → writes `float.NaN`. |
| T1.3.4 | DistrictSignalCache + save/load | 2 | _pending_ | _pending_ | `DistrictSignalCache` — `float[districtCount, signalCount]` table; `Get(districtId, signal)` returns float (`NaN` if empty); `GetAll(districtId)` returns `Dictionary<SimulationSignal, float>` (12 entries — Bucket 8 read-model facade stub). Add `districtMapData` serialization field to `GameSaveManager` save DTO (version bump); restore on load; migration: if absent in old save, set `needsRederive = true` (re-derive on first tick). |

---

### Step 2 — Happiness Migration + Warmup

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Migrate `CityStats` single-scalar happiness and pollution formula to `HappinessComposer` (12-signal weighted sum); register existing industrial buildings + power plants + forests as `ISignalProducer` implementors on the `PollutionAir` signal; ship `DesirabilityComposer` with explicit FEAT-43 toggle; close the save/load gap with `SignalWarmupPass` + `GameSaveManager` schema version bump. After this step the city sim runs on the new signal contract with happiness output parity vs pre-migration.

**Exit criteria:**

- `CityStats.happiness` getter returns `HappinessComposer.Current` — API preserved.
- Happiness parity EditMode test passes (pre/post migration delta < 5 on golden fixture).
- `DesirabilityComposer.CellValue(x,y)` wired into `AutoZoningManager` + `GrowthManager` behind `useSignalDesirability` toggle.
- `SignalWarmupPass.Run()` called by `GameSaveManager` on load; signal fields stable on tick-0 post-load.
- Save schema version bumped; old saves without `DistrictMap` auto-migrate.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/city-sim-depth-exploration.md` §Implementation Points 6.3
- `ia/specs/managers-reference.md` §Demand (R/C/I) — happiness formula canonical
- `Assets/Scripts/Managers/GameManagers/CityStats.cs` lines ~715–856 — existing happiness scalar
- `Assets/Scripts/Managers/GameManagers/DemandManager.cs` line ~54 — happiness consumer
- `Assets/Scripts/Managers/GameManagers/ForestManager.cs` — forest sink producer
- `Assets/Scripts/Managers/GameManagers/AutoZoningManager.cs` — FEAT-43 toggle call site
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — load hook
- `Assets/Scripts/Managers/UnitManagers/PowerPlant.cs` — PollutionAir source
- Step 1 outputs: `SignalFieldRegistry`, `ISignalProducer`/`ISignalConsumer`, `SignalTickScheduler`

---

#### Stage 2.1 — HappinessComposer Migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Replace `CityStats` inline happiness/pollution scalar with `HappinessComposer`; register existing industrial buildings, power plants, forests as `PollutionAir` producers/sinks; verify output parity.

**Exit:**

- `HappinessComposer.Current` within 5 points of pre-migration scalar on golden fixture.
- `CityStats.happiness` getter delegates to `HappinessComposer.Current` — no external API break.
- Industrial buildings + `PowerPlant` registered as `PollutionAir` producers; `ForestManager` registered as `PollutionAir` sink.
- EditMode parity test passes; no NaN / Infinity from composer.

**Phases:**

- [ ] Phase 1 — HappinessComposer MonoBehaviour + producer/sink registration.
- [ ] Phase 2 — CityStats migration wiring + parity test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | HappinessComposer MonoBehaviour | 1 | _pending_ | _pending_ | `HappinessComposer` MonoBehaviour — reads `SignalFieldRegistry` for all 12 signals; weighted sum via `[SerializeField] float[] signalWeights` array (Inspector-tunable, indexed by `SimulationSignal` ordinal); `Current` property returns normalized 0–100 score; initial weights matched to current `CityStats` scalar formula (pollution penalty, forest bonus, service bonus); `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` fallback. |
| T2.1.2 | Producer/sink registration | 1 | _pending_ | _pending_ | Implement `ISignalProducer` on `PowerPlant.cs` (`PollutionAir`: nuclear=medium weight 1.5f, fossil=high weight 3.0f per glossary), on `ZoneManager` industrial building emit (heavy=2.5f, medium=1.5f, light=0.8f), and on `ForestManager` (negative PollutionAir emission per forest cell: sparse=−0.3f, medium=−0.5f, dense=−0.8f). `EmitSignals` calls `registry.GetField(PollutionAir).Add(x,y,weight)`. |
| T2.1.3 | CityStats happiness migration | 2 | _pending_ | _pending_ | Edit `CityStats.cs` — replace inline scalar happiness compute (lines ~715–856) with `happinessComposer.Current` call; add `[SerializeField] HappinessComposer happinessComposer` + `FindObjectOfType` fallback; preserve `happiness` getter signature exactly; preserve `RefreshHappinessAfterPolicyChange()` public API; inject `HappinessComposer` into `DemandManager` via Inspector. |
| T2.1.4 | Happiness parity EditMode test | 2 | _pending_ | _pending_ | EditMode test — construct minimal CityStats + producers state matching a known-good scenario; run 1 tick old scalar path to capture baseline happiness; enable `HappinessComposer` path (set weights to match); run same tick; assert delta < 5 on [0,100] scale; assert no NaN / Infinity. |

---

#### Stage 2.2 — DesirabilityComposer + FEAT-43 Toggle

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `DesirabilityComposer` with explicit FEAT-43 toggle; wire into `AutoZoningManager` and `GrowthManager`; create shell MonoBehaviours for `ConstructionStageController` + `DensityEvolutionTicker` with pre-wired Inspector refs (Step 4 fills in logic).

**Exit:**

- `DesirabilityComposer.CellValue(x,y)` returns float in `[0,1]` (clamp at composer boundary per Example 3).
- `AutoZoningManager` + `GrowthManager` use `DesirabilityComposer` when `useSignalDesirability = true`; toggle off = old path unchanged.
- `ConstructionStageController` + `DensityEvolutionTicker` shell files compile with no-op `SetDesirabilitySource` stub.
- `npm run validate:all` passes.

**Phases:**

- [ ] Phase 1 — DesirabilityComposer MonoBehaviour + FEAT-43 toggle.
- [ ] Phase 2 — AutoZoningManager/GrowthManager wiring + Step 4 shell files.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | DesirabilityComposer MonoBehaviour | 1 | _pending_ | _pending_ | `DesirabilityComposer` MonoBehaviour — per-cell value = weighted sum of `LandValue` + `ServicePolice` + `ServiceFire` + `ServiceEducation` + `ServiceHealth` + `ServiceParks` signals (weights Inspector-tunable); `CellValue(x,y)` returns `Mathf.Clamp01(sum)` — clamp at composer boundary per Example 3 (not at consumer site); `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` fallback. |
| T2.2.2 | FEAT-43 explicit toggle | 1 | _pending_ | _pending_ | Add `[SerializeField] public bool useSignalDesirability = false` to `AutoZoningManager` + `GrowthManager`; when true, replace existing desirability reads with `desirabilityComposer.CellValue(x,y)`; when false, old path unchanged. NOT parallel A/B (locked decision). Add `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType` fallback to both. |
| T2.2.3 | AutoZoningManager + GrowthManager wiring | 2 | _pending_ | _pending_ | In `AutoZoningManager.ProcessTick` wrap desirability reads in `if (useSignalDesirability)` guard routing to `desirabilityComposer.CellValue(x,y)`. Same in `GrowthManager` growth-ring expansion. Wire `DesirabilityComposer` Inspector ref in both. Old path remains functional when toggle off. |
| T2.2.4 | ConstructionStageController + DensityEvolutionTicker shells | 2 | _pending_ | _pending_ | Create `ConstructionStageController.cs` + `DensityEvolutionTicker.cs` shell MonoBehaviours (new, `Assets/Scripts/Simulation/`) — each has `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType` fallback + no-op `SetDesirabilitySource(DesirabilityComposer c)` stub; compiles cleanly; Step 4 stages fill in all logic. |

---

#### Stage 2.3 — SignalWarmupPass + Save Schema

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `SignalWarmupPass` deterministic recompute on load; bump `GameSaveManager` schema version to persist `DistrictMap` + tuning weights; verify round-trip integrity.

**Exit:**

- `SignalWarmupPass.Run()` called by `GameSaveManager` after grid restore; signal fields stable and idempotent (two consecutive calls produce identical output).
- `DistrictMap` round-trips through save/load.
- Old saves without `DistrictMap` field auto-migrate (re-derive on first tick).
- EditMode integration test passes.

**Phases:**

- [ ] Phase 1 — SignalWarmupPass implementation + idempotency test.
- [ ] Phase 2 — GameSaveManager schema bump + migration + integration test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | SignalWarmupPass MonoBehaviour | 1 | _pending_ | _pending_ | `SignalWarmupPass` MonoBehaviour — `Run()` iterates all `ISignalProducer` implementors (via `SignalTickScheduler` producer list), calls `EmitSignals` for each, then runs `DiffusionKernel.Apply` per signal; does NOT call consumers (no happiness/desirability update on load); safe to call from `GameSaveManager` before first normal tick. |
| T2.3.2 | SignalWarmupPass idempotency test | 1 | _pending_ | _pending_ | EditMode test — construct minimal scene (GridManager + SignalFieldRegistry + PollutionAir producer); call `SignalWarmupPass.Run()` twice; assert `SignalField[PollutionAir]` bit-identical after run-1 and run-2 (deterministic); assert no NaN in any signal field after warmup. |
| T2.3.3 | GameSaveManager schema version bump | 2 | _pending_ | _pending_ | Edit `GameSaveManager.cs` — add `districtMapData int[]` serialization field to save DTO (flatten `DistrictMap` 2D array); increment schema version int; `OnAfterLoad`: if field present, restore `DistrictMap` + call `SignalWarmupPass.Run()` after grid restore (per `ia/specs/persistence-system.md` load pipeline order); if absent (old save), set `districtMap.needsRederive = true`. |
| T2.3.4 | Save/load integration test | 2 | _pending_ | _pending_ | EditMode integration test — create city with 2 I-heavy cells + 1 forest cell; save via `GameSaveManager`; reload; assert `DistrictMap` restored (district ids match pre-save); assert `SignalField[PollutionAir]` non-zero after warmup; assert `DistrictSignalCache.Get(0, PollutionAir)` > 0 after first post-load tick. |

---

### Step 3 — New Simulation Signals

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Ship the 9 remaining simulation signals not covered by the Phase 6.3 migration — `PollutionLand`, `PollutionWater`, `LandValue`, `Crime`, `ServicePolice/Fire/Education/Health/Parks`, `TrafficLevel`, `WastePressure` — each as `ISignalProducer`/`ISignalConsumer` MonoBehaviours registered with `SignalTickScheduler`. After this step the full 12-signal inventory is live and `HappinessComposer` + `DesirabilityComposer` read all slots.

**Exit criteria:**

- All 12 signals non-zero in a city with industry + residential + service buildings after 30 in-game days.
- `CrimeHotspotEvent` emitted when P90 district crime exceeds threshold.
- `ServiceCoverageComputer`, `TrafficFlowHeuristic`, `WasteSystem` registered in `SignalTickScheduler` producer list.
- `LandValue` contributes non-zero bonus to `CityStats` tax base.
- EditMode tests pass per system.
- `npm run validate:all` passes.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/city-sim-depth-exploration.md` §Implementation Points 6.4–6.5 + Examples 1–2
- `ia/specs/simulation-signals.md` — producer/sink weight tables (authored Stage 1.1)
- `Assets/Scripts/Managers/GameManagers/RoadManager.cs` — road-tier data for traffic heuristic
- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — RCI building types for crime + waste producers
- `Assets/Scripts/Managers/GameManagers/WaterManager.cs` — water cell membership for `PollutionWater` diffusion gating
- Step 1 outputs: `SignalFieldRegistry`, `SignalTickScheduler`, `DiffusionKernel`
- Step 2 outputs: `HappinessComposer`, `DesirabilityComposer` (signal slot wiring)

---

#### Stage 3.1 — Pollution Split + LandValue

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `PollutionLand` + `PollutionWater` producer/sink tables (completing 3-type pollution split from `PollutionAir` Step 2) and `LandValue` producer with tax-base wiring.

**Exit:**

- `PollutionLand` non-zero near chemical plants / landfills.
- `PollutionWater` non-zero near water-adjacent industry; diffusion bounded to water cells.
- `LandValue` rises near high-service, low-pollution, high-density zones.
- `CityStats` tax base receives non-zero land-value bonus in high-density city.
- EditMode smoke test per signal passes.

**Phases:**

- [ ] Phase 1 — PollutionLand + PollutionWater producers/sinks.
- [ ] Phase 2 — LandValue producer + CityStats tax base wiring.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | PollutionLand producer/sink | 1 | _pending_ | _pending_ | Implement `ISignalProducer` for `PollutionLand` on industrial buildings (manufacturing/chemical sub-types) + landfill buildings; sinks: forest (lower weight than PollutionAir), parks; per-building weight table in `SignalMetadataRegistry` PollutionLand entry; diffusion radius=4, standard separable Gaussian. |
| T3.1.2 | PollutionWater producer/sink | 1 | _pending_ | _pending_ | Implement `ISignalProducer` for `PollutionWater` on water-adjacent industrial cells (checked via `WaterManager` membership); diffusion kernel gated to water cells only (skip dry cells in kernel traversal); sinks: wetland cells + water treatment buildings; diffusion radius=5 along water connectivity. |
| T3.1.3 | LandValue producer | 2 | _pending_ | _pending_ | `LandValueProducer` implements `ISignalProducer` — per-cell `LandValue = densityWeight * zoneLevel + serviceBonus - pollutionPenalty`; `serviceBonus` reads mean of 5 ServiceXxx signals; `pollutionPenalty` reads mean of 3 PollutionXxx signals; updates monthly tick only (not daily); weights in `SignalMetadataRegistry` LandValue entry. |
| T3.1.4 | LandValue tax base wiring + test | 2 | _pending_ | _pending_ | Edit `CityStats.cs` — monthly tax income += `SignalFieldRegistry.GetField(LandValue).Sum() * [SerializeField] float landValueTaxRate`; EditMode test: city with 5 high-service R-high cells after 30 days → monthly tax bonus > 0 vs baseline with no service coverage. |

---

#### Stage 3.2 — CrimeSystem

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `CrimeSystem` — `Crime` signal producer (density × low-service weight) + consumer (`ServicePolice` coverage reduces crime) + hotspot event emitter placeholder for Bucket 5.

**Exit:**

- `Crime` signal non-zero in high-density unpoliced zones after 10 ticks.
- `ServicePolice` coverage reduces crime in covered cells.
- `CrimeHotspotEvent(districtId, level)` emitted when P90 district crime > `crimeHotspotThreshold`.
- EditMode test: no-police district accumulates crime above threshold; police station drops below threshold.

**Phases:**

- [ ] Phase 1 — CrimeSystem producer + ServicePolice consumer formula.
- [ ] Phase 2 — Hotspot event emitter + EditMode test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | CrimeSystem producer | 1 | _pending_ | _pending_ | `CrimeSystem` MonoBehaviour implements `ISignalProducer` — `Crime` source per cell = `zoneLevel * (1f - Mathf.Clamp01(ServicePolice.Get(x,y)))` (low police → high crime weight); diffusion radius=3; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` (invariants #3, #4). |
| T3.2.2 | ServicePolice crime consumer | 1 | _pending_ | _pending_ | Extend `CrimeSystem` to also implement `ISignalConsumer` — `ConsumeSignals` reads `ServicePolice` field post-diffusion; for cells where `ServicePolice.Get(x,y) > 0`, applies post-diffusion crime reduction multiplier `(1f - Mathf.Clamp01(policeValue * 0.5f))`; writes multiplied value back to `Crime` field in place. |
| T3.2.3 | CrimeHotspot event emitter | 2 | _pending_ | _pending_ | After each `CrimeSystem.ConsumeSignals`, iterate `DistrictSignalCache` P90 Crime per district; if P90 > `[SerializeField] float crimeHotspotThreshold = 2.5f`, emit `CrimeHotspotEvent { districtId, level }` via `GameNotificationManager`; Bucket 5 animation placeholder — event registered only, no animation instantiation. |
| T3.2.4 | CrimeSystem EditMode test | 2 | _pending_ | _pending_ | EditMode test — 10-cell high-density R zone, no police; run 30 ticks; assert `DistrictSignalCache.Get(0, Crime)` P90 > 2.5f. Second fixture: add police station within radius 3; run 30 ticks; assert P90 < 2.5f. |

---

#### Stage 3.3 — Services + Traffic + Waste

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `ServiceCoverageComputer` (5 service types), `TrafficFlowHeuristic`, `WasteSystem`; register all three with `SignalTickScheduler`; smoke-test each.

**Exit:**

- `ServicePolice/Fire/Education/Health/Parks` non-zero within coverage radius of placed service buildings.
- `TrafficLevel` non-zero on road-adjacent cells with RCI density.
- `WastePressure` non-zero in high-density zones without recycling coverage.
- All 3 MonoBehaviours in `SignalTickScheduler` producer list.
- EditMode smoke tests pass.

**Phases:**

- [ ] Phase 1 — ServiceCoverageComputer + TrafficFlowHeuristic.
- [ ] Phase 2 — WasteSystem + scheduler registration + smoke tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | ServiceCoverageComputer MonoBehaviour | 1 | _pending_ | _pending_ | `ServiceCoverageComputer` MonoBehaviour implements `ISignalProducer` — for each of 5 service types, iterates placed service buildings; writes coverage signal via Manhattan-distance radius (per-service radius tunable per `SignalMetadataRegistry` entry); road-connectivity gated (service building unreachable by road → zero coverage emitted); `[SerializeField] GridManager` + `FindObjectOfType`. |
| T3.3.2 | TrafficFlowHeuristic MonoBehaviour | 1 | _pending_ | _pending_ | `TrafficFlowHeuristic` MonoBehaviour implements `ISignalProducer` — for each road cell: `TrafficLevel = rciNeighborhoodSum / roadTierCapacity`; `rciNeighborhoodSum` = Moore-sum of zone levels in radius 2 from `GridManager.GetCell`; `roadTierCapacity` from `RoadManager` tier lookup; non-road cells = 0; updates daily; `[SerializeField] RoadManager` + `FindObjectOfType`. |
| T3.3.3 | WasteSystem MonoBehaviour | 2 | _pending_ | _pending_ | `WasteSystem` MonoBehaviour implements `ISignalProducer` — `WastePressure.Add(x,y, zoneLevel * wasteRate)` per RCI cell; sinks: landfill buildings write negative emission over coverage radius; recycling center writes stronger negative emission; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType`; updates monthly tick. |
| T3.3.4 | Scheduler registration + smoke tests | 2 | _pending_ | _pending_ | Register `ServiceCoverageComputer`, `TrafficFlowHeuristic`, `WasteSystem` in `SignalTickScheduler` — extend `[SerializeField] List<MonoBehaviour>` explicit producer slots + `FindObjectsOfType` fallback. EditMode smoke: police station placed → `ServicePolice.Get(stationX, stationY)` > 0; R zone + road → `TrafficLevel.Get` road cell > 0; R zone no landfill → `WastePressure` > 0. |

---

### Step 4 — Construction + Density + Industrial

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Ship `ConstructionStageController` (stage machine + ScriptableObject curve tables + `DesirabilityComposer`-modulated construction time), `DensityEvolutionTicker` refactored to consume `DesirabilityComposer` with upgrade/downgrade hysteresis, and `IndustrialSubtypeResolver` (4 sub-types feeding distinct pollution + tax + desirability weights). After this step buildings show construction stages before reaching full build, density evolves organically, and industrial zones carry sub-type identity.

**Exit criteria:**

- New zone cell transitions through construction stages (sprite swap per stage) before full build state.
- Example 3 numeric assertion passes: R-medium at desirability=0.6 → ≈27.3-day total; desirability=0 → 60 days; desirability=1 → 20 days.
- `DensityEvolutionTicker` upgrades cells at desirability > 0.7; downgrades at < 0.3 after 60-day hold (hysteresis).
- `IndustrialSubtypeResolver` assigns one of 4 sub-types; sub-type weights affect `PollutionAir` + `LandValue` producers.
- `npm run validate:all` passes.

**Art:** Construction stage sprites per zone × density combination needed (stub with placeholder if Bucket 5 art pipeline not ready; `ConstructionCurveTable` SO references sprite by slot index).

**Relevant surfaces (load when step opens):**
- `docs/city-sim-depth-exploration.md` §Implementation Points 6.6 + Example 3
- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — zone placement hook
- `Assets/Scripts/Managers/UnitManagers/Zone.cs` — zone data model
- `Assets/Scripts/Managers/GameManagers/GrowthManager.cs` — density evolution current impl
- Step 2 outputs: `DesirabilityComposer.CellValue`, shell files `ConstructionStageController.cs` + `DensityEvolutionTicker.cs`
- Step 3 outputs: `LandValue` signal, industrial sub-type weight table stubs

---

#### Stage 4.1 — ConstructionStageController

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill in `ConstructionStageController` shell (from Stage 2.2) — stage machine with ScriptableObject curve tables and desirability-modulated construction time + `ZoneManager` placement hook.

**Exit:**

- New zone cell enters construction stage 0 on placement; advances to final stage after `effectiveTime` in-game days.
- `effectiveTime = baseTime / (0.5f + Mathf.Clamp01(desirability))` (Example 3 verified ±1 day).
- Sprite swap fires on stage boundary; placeholder sprite if art absent.
- EditMode test: R-medium at desirability=0.6 completes in 27.3±1 days; edge cases verified.

**Phases:**

- [ ] Phase 1 — Stage machine + ScriptableObject curve tables + time formula.
- [ ] Phase 2 — ZoneManager placement hook + EditMode test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | ConstructionStageController stage machine | 1 | _pending_ | _pending_ | Fill `ConstructionStageController.cs` shell — per-cell `ConstructionState { int stageIndex; float daysInCurrentStage; }` dictionary keyed by cell coords; `ConstructionCurveTable` ScriptableObject (zone type × density → `int stageCount`, `float baseDays`, `Sprite[] stageSprites`); `Tick(CityCell cell, float desirability)` advances `daysInCurrentStage`; fires `OnStageComplete(cell, stageIndex)` event on stage boundary. |
| T4.1.2 | Construction time formula + edge guards | 1 | _pending_ | _pending_ | Implement `effectiveTime = baseDays / (0.5f + Mathf.Clamp01(desirability))` with guards: desirability clamp at `[0,1]` at composer boundary (Stage 2.2) ensures no negative input; per-stage time = effectiveTime / stageCount. Validate Example 3: R-medium baseDays=30, stages=4, d=0.6 → total≈27.3, per-stage≈6.8. Add `[SerializeField] DesirabilityComposer desirabilityComposer` in `ConstructionStageController`; fill in `SetDesirabilitySource` stub. |
| T4.1.3 | ZoneManager placement hook + sprite swap | 2 | _pending_ | _pending_ | Edit `ZoneManager` — on new zone cell placement, register cell with `ConstructionStageController.Register(cell)`; subscribe to `OnStageComplete`: swap cell's renderer sprite to `ConstructionCurveTable.stageSprites[stageIndex]` (placeholder `Sprite` if art asset absent); final stage complete → swap to full building sprite + mark cell `isFullyBuilt = true`. |
| T4.1.4 | ConstructionStage EditMode test | 2 | _pending_ | _pending_ | EditMode test — R-medium cell, desirability=0.6; advance `ConstructionStageController` tick loop 30 times (1 day per call); assert cell reaches final stage by day 28 (27.3±1). Boundary: d=0 → 60±1 days; d=1 → 20±1 days. Assert no divide-by-zero at d=0 (denominator = 0.5 + 0 = 0.5f). |

---

#### Stage 4.2 — DensityEvolution + IndustrialSubtypeResolver

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill in `DensityEvolutionTicker` shell (from Stage 2.2) — refactor to consume `DesirabilityComposer` with upgrade/downgrade hysteresis; ship `IndustrialSubtypeResolver` (4 sub-types wired into pollution + tax + desirability weights).

**Exit:**

- High-desirability cells (> 0.7) upgrade density tier within 1 tick.
- Low-desirability cells (< 0.3) downgrade after ≥60-day hold; hysteresis `[0.3, 0.7]` band prevents oscillation.
- I-zone cells resolved to one of 4 sub-types; sub-type persisted to `CellData.industrialSubtype`.
- Manufacturing cell produces higher `PollutionAir` than tech cell at same density.
- `npm run validate:all` passes.

**Phases:**

- [ ] Phase 1 — DensityEvolutionTicker desirability refactor + decay/hysteresis.
- [ ] Phase 2 — IndustrialSubtypeResolver + weight wiring + test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.2.1 | DensityEvolutionTicker desirability refactor | 1 | _pending_ | _pending_ | Fill `DensityEvolutionTicker.cs` shell — monthly tick; replace old growth heuristic with `desirabilityComposer.CellValue(x,y)` consumption; upgrade threshold `[SerializeField] float upgradeAt = 0.7f`; upgrade fires immediately when condition met. Fill `SetDesirabilitySource` stub from Stage 2.2; `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType`. |
| T4.2.2 | Density decay + hysteresis | 1 | _pending_ | _pending_ | Add downgrade path — cells at desirability < `[SerializeField] float downgradeAt = 0.3f` for ≥ `[SerializeField] int decayWaitDays = 60` consecutive days downgrade one density tier; per-cell `int daysBelow` counter reset when desirability rises above `downgradeAt`; hysteresis band `[downgradeAt, upgradeAt]` prevents oscillation; both thresholds tunable Inspector. |
| T4.2.3 | IndustrialSubtypeResolver MonoBehaviour | 2 | _pending_ | _pending_ | `IndustrialSubtypeResolver` MonoBehaviour — on I-zone cell placement (or monthly re-eval for unresolved cells): score 4 sub-types (agriculture=farm adjacency bonus, manufacturing=high road density, tech=high desirability + low pollution, tourism=high LandValue + low crime); assign highest-scoring sub-type; persist to `CellData.industrialSubtype` field; `[SerializeField] DesirabilityComposer` + `FindObjectOfType`. |
| T4.2.4 | Sub-type weight wiring + test | 2 | _pending_ | _pending_ | Edit `ZoneManager` industrial `ISignalProducer.EmitSignals` — look up `cell.industrialSubtype` and apply per-sub-type `PollutionAir` weight (manufacturing=2.5f, agriculture=1.0f, tech=0.5f, tourism=0f); apply per-sub-type `LandValue` bonus weight similarly. EditMode test: manufacturing cell → higher PollutionAir than tech cell at same zone level. |

---

### Step 5 — Overlays + HUD Parity

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 5):** 0 filed

**Objectives:** Ship `SignalOverlayRenderer` (all 12 signals + district boundary mode), district info panel (reads `DistrictSignalCache`), Bucket 8 `GetAll` facade, and end-to-end smoke test. After this step testers can visualize all simulation signals as overlays, click a district for aggregate stats, and the Bucket 8 read-model is ready for the CityStats overhaul.

**Exit criteria:**

- Overlay mode cycles through 12 signal overlays + district boundary; HUD toggle wired.
- District info panel shows non-zero values for all active signals after 30 days.
- `DistrictSignalCache.GetAll(districtId)` returns 12-entry dictionary (verified in smoke test).
- `npm run validate:all` passes.

**Art:** Per-signal `OverlayConfig` color gradients needed (seed values provided; Art tunes post-ship).

**Relevant surfaces (load when step opens):**
- `docs/city-sim-depth-exploration.md` §Implementation Points 6.7 + §Architecture exit points
- `ia/specs/managers-reference.md` §3.1 HUD information density
- `Assets/Scripts/Managers/GameManagers/UIManager.cs` + `UIManager.Hud.cs` — popup stack + HUD wiring pattern
- `Assets/Scripts/Controllers/GameControllers/CityStatsUIController.cs` — existing overlay model reference
- Step 1 outputs: `SignalFieldRegistry`, `DistrictSignalCache`, `DistrictManager`

---

#### Stage 5.1 — SignalOverlayRenderer

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `SignalOverlayRenderer` (per-cell color gradient texture per signal + district boundary mode) and HUD toggle.

**Exit:**

- `SignalOverlayRenderer` renders visible per-cell color gradient for active signal.
- District boundary mode renders `DistrictMap` assignments as distinct per-district colors.
- HUD toggle cycles overlay modes (off → signals 0–11 → DistrictBoundary → off).
- No per-frame `FindObjectOfType` — invariant #3 verified.

**Phases:**

- [ ] Phase 1 — SignalOverlayRenderer MonoBehaviour + OverlayConfig ScriptableObject.
- [ ] Phase 2 — Auto-normalization + district boundary mode + HUD toggle.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T5.1.1 | SignalOverlayRenderer MonoBehaviour | 1 | _pending_ | _pending_ | `SignalOverlayRenderer` MonoBehaviour — `Texture2D overlayTex` sized `gridWidth × gridHeight`; `Render(SimulationSignal s)` iterates cells, maps `SignalField.Get(x,y)` through `OverlayConfig.colorRamp` gradient, sets texture pixel; `RenderDistricts()` path maps `DistrictMap.Get(x,y)` to palette color; `SetActive(bool)` shows/hides overlay quad; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType`. |
| T5.1.2 | OverlayConfig ScriptableObject | 1 | _pending_ | _pending_ | `OverlayConfig` ScriptableObject — fields: `SimulationSignal signal`, `Gradient colorRamp` (seed: green→yellow→red for pollution signals, blue gradient for service signals, grey for traffic/waste), `bool autoNormalize`, `float fixedMax`; one asset per signal (12 total); referenced array on `SignalOverlayRenderer`. |
| T5.1.3 | Auto-normalization + district boundary | 2 | _pending_ | _pending_ | When `OverlayConfig.autoNormalize = true`: compute `fieldMax = SignalField.Max()` per render call; normalize before gradient lookup; clamp `fieldMax` to `epsilon` floor to avoid divide-by-zero. District boundary `RenderDistricts()`: iterate `DistrictMap`, map `districtId % paletteSize` to `Color[] districtPalette` array (hardcoded 8-color seed). |
| T5.1.4 | HUD overlay toggle | 2 | _pending_ | _pending_ | Add overlay toggle button to HUD (follow `UIManager.Hud.cs` pattern); `OverlayMode` enum (`Off`, then 12 signal entries, `DistrictBoundary`); `UIManager.CycleOverlayMode()` increments enum mod length, calls `SignalOverlayRenderer.Render(signal)` or `RenderDistricts()` accordingly; integrate with `UIManager.Theme.cs` button style. |

---

#### Stage 5.2 — District Info Panel

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship district info panel (click-to-open, shows `DistrictSignalCache` aggregates for all 12 signals); `DistrictSignalCache.GetAll` Bucket 8 facade; end-to-end smoke test confirming all signal chains produce expected values.

**Exit:**

- Clicking district cell (or in district overlay mode) opens info panel.
- Panel shows all 12 signal aggregate values for selected district.
- `DistrictSignalCache.GetAll(districtId)` returns 12-entry dictionary; no throw on empty district.
- Smoke test: 30-day city shows non-zero PollutionAir, Crime, ServicePolice, LandValue in district panel.

**Phases:**

- [ ] Phase 1 — District info panel UI + district click selection.
- [ ] Phase 2 — Bucket 8 GetAll facade + end-to-end smoke test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T5.2.1 | DistrictInfoPanel UI | 1 | _pending_ | _pending_ | `DistrictInfoPanel` UI component — popup panel following `UIManager.PopupStack` pattern; shows district name + scrollable table of 12 signal rows (signal name, aggregate value, unit label from `OverlayConfig`); `Show(int districtId)` populates from `DistrictSignalCache.GetAll(districtId)`; NaN shown as "—"; `Hide()` via popup stack `Pop`. |
| T5.2.2 | DistrictSelector + click handler | 1 | _pending_ | _pending_ | `DistrictSelector` component on camera/input system — on left-click when `OverlayMode == DistrictBoundary`, call `DistrictManager.GetDistrictId(gridX, gridY)` → `DistrictInfoPanel.Show(id)`; highlight selected district boundary in `SignalOverlayRenderer`; deselect on second click same cell or Escape. |
| T5.2.3 | Bucket 8 GetAll facade | 2 | _pending_ | _pending_ | Finalize `DistrictSignalCache.GetAll(int districtId)` — return `Dictionary<SimulationSignal, float>` (12 entries; NaN for empty district, not missing key); add `GetAllDistricts()` returning `List<(int districtId, Dictionary<SimulationSignal, float> values)>` — read-model API surface Bucket 8 CityStats overhaul will consume without per-feature glue code. |
| T5.2.4 | End-to-end smoke test | 2 | _pending_ | _pending_ | EditMode (or minimal PlayMode) smoke test — place 3 I-heavy + 5 R-medium + 1 police station; advance 30 in-game days via tick loop; assert `DistrictSignalCache.GetAll(0)[PollutionAir]` > 0, `[Crime]` > 0, `[ServicePolice]` > 0, `[LandValue]` > 0; assert `DistrictInfoPanel.Show(0)` does not throw; assert overlay `Render(PollutionAir)` produces non-zero texture pixel at industry location. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/city-sim-depth-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/city-sim-depth-exploration.md` Design Expansion block.
- Author `ia/specs/simulation-signals.md` in Stage 1.1 Task T1.1.4 per invariant #12 — signal contract is a permanent domain.
- `SignalTickScheduler` inserts between `UrbanCentroidService.RecalculateFromGrid` (~line 74) and `AutoRoadBuilder.ProcessTick` (~line 77) in `SimulationManager.ProcessSimulationTick`.
- Save manager is `GameSaveManager.cs` (not `SaveManager.cs`) — load hook target for `SignalWarmupPass.Run()`.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Terminal step Final status flips; file stays.
- Promote post-MVP items into MVP stages — deferred: per-vehicle pathing, named sims, multi-centroid districts, region/country feedback consumers, crime → protest animation (Bucket 5 event placeholder only), player-drawn districts, per-frame signal updates, Zone S / economy (Bucket 3), utilities (Bucket 4).
- Merge partial stage state — every stage lands on a green bar.
- Insert BACKLOG rows directly — only `stage-file` materializes them.
- Run FEAT-43 old desirability path and new `DesirabilityComposer` path in parallel — explicit `bool useSignalDesirability` toggle only (locked decision).
- Write `HeightMap` or `Cell.height` from any signal system — invariant #1; `PollutionWater` reads water cells via `WaterManager` but does not write terrain.
- Call `FindObjectOfType` in `Update` or per-frame loops — invariant #3; all producer/consumer lists resolved once in `Awake`.
