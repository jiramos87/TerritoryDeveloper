# Utilities — Exploration (stub)

> Pre-plan exploration stub for **Bucket 4-a** of the polished-ambitious MVP (per `docs/full-game-mvp-exploration.md` + `ia/projects/full-game-mvp-master-plan.md`). **Split** off from the original merged `utilities-landmarks-exploration.md` stub — sibling is `docs/landmarks-exploration.md`. Seeds a `/design-explore` pass that expands Approaches + Architecture + Subsystem impact + Implementation points. **Scope = utilities v1 (water / power / sewage as country-level resources + local contributors). NOT landmarks (sibling doc), NOT Zone S + per-service budgets (Bucket 3), NOT city-sim signals (Bucket 2), NOT CityStats (Bucket 8), NOT multi-scale core (Bucket 1). Those land in sibling buckets / docs.**

---

## Problem

Territory Developer has no utility sim. A polished ambitious MVP needs one:

- **Utilities absent.** No water / power / sewage production, distribution, or consumption. "Utility building" placeholders exist (BUG-20 restores visuals on load) but they contribute to nothing. A city can grow indefinitely without power or water — genre break.
- **No scale-level resource surface.** Natural wealth (forest, water body, mineral placeholder) has no economic dimension beyond pollution sink. No "country owns the watershed" pool that regions + cities draw from.
- **Deferred utility depth risks re-opening scope.** If utilities ship as local-only buildings, the cross-scale utility pool story (country wealth → region allocation → city consumption) has no home and gets filed as Bucket 4.5 later.

**Design goal (high-level):** utilities v1 = water / power / sewage framed as country-level resource pools derived from natural wealth + savings / assets, with local utility buildings (per-city / per-region) as contributors to those pools.

## Approaches surveyed

_(To be expanded by `/design-explore` — seed list only.)_

- **Approach A — Local-only utilities.** Utility buildings produce / consume per-cell or per-city only, no cross-scale pool. Minimal churn; fails the "country-level resource" framing.
- **Approach B — Country-pool first, local contributors feed it.** Define `UtilityPool` per scale per utility (3 utilities × 3 scales = 9 pool instances with rollup). Local contributors (power plants, water treatment, sewage treatment) register as producers. Cities consume against city-pool; city-pool draws from region-pool; region-pool draws from country-pool. Natural wealth seeds country-pool.
- **Approach C — Signal-integrated utilities.** Utilities become another signal in Bucket 2's signal contract (`UtilityWater`, `UtilityPower`, `UtilitySewage`). Reuses diffusion + consumer formula. Loses the country-pool "resource" framing (signals are per-cell, pools are per-scale aggregates).
- **Approach D — Defer utilities entirely.** File as Bucket 4.5 post-MVP. Trims scope at cost of the "genre table stakes" check.

## Recommendation

_TBD — `/design-explore` Phase 2 gate decides._ Author's prior lean: **Approach B** (country-pool utilities with local contributors). Matches bucket framing, keeps Bucket 2 signal surface free of cross-scale aggregates. Approach C collapses the cross-scale resource story into per-cell signals. Approach D defers and re-opens scope later.

## Locked decisions (prior interview)

- **Pool accounting = instantaneous flow-rate + soft warning.** Each tick: `net = production − consumption`. No stored capacity, no ring-buffer history in v1. A rolling average (EMA, ~5 ticks) of net balance drives a "running low" warning color before hard deficit fires. Deficit severity scales with EMA depth (not cliff-edge). Save schema: per-pool floats only, no storage state.
- **Explicit rejections:** no energy-storage buildings, no reserve-capacity mechanics, no ring-buffer history in v1 (plot the EMA directly if a sparkline is needed later).
- **Deficit is cliff-edge, not gradual.** When a utility pool hits zero: expansion freezes immediately (no new spawning, no road extension, no auto mode, no manual placement). Happiness decays slowly. Desirability decays across the map. No visual darkness / lighting effects in scope.
- **Natural wealth is geographically gated.** Water bodies (lakes, rivers) feed the water pool only when a utility building is physically adjacent on the city-scale map. Forests and mountains provide passive ambient bonuses (air quality, happiness) — they do NOT feed utility pools directly. Sea access enables ports → commerce + industry bonus cross-scale.
- **Terrain-sensitive placement — no explicit indicator.** Water treatment must be adjacent to a water cell. Sewage treatment must be adjacent to water and pollutes downstream (direction of flow). Power plants can be placed freely in city scale, except large regionals (hydro → must be adjacent to water, wind farms → mountain / open terrain). No in-range indicator shown to player — discovers valid terrain by trying placement.
- **Infrastructure = own category, not Zone S.** Contributor buildings (power, water treatment, sewage treatment) belong to a dedicated "infrastructure" category with their own construction cost + maintenance line. Basic tier available at scale entry (no gate). 2–3 capacity-based upgrade tiers unlocked by output thresholds (not tech tree).
- **Cross-scale surplus rollup.** Surpluses flow up to the parent scale pool. Each scale tier manages its own utility pools and aggregates contributions from its children (city → region → country). Separate per-scale dashboards. A country-level shortage cascades down — cities under a deficit region/country hit the freeze threshold.

## Open questions

- **Natural wealth surface.** Forest = renewable wood / carbon / air quality; water body = water supply; mineral placeholder = ore for industry. Which natural-wealth cells feed which utility pool? Authority: glossary rows + new `utility-system.md` spec vs extension of existing specs?
- **Contributor building archetypes.** Power plants (coal / solar / wind — sub-types?), water treatment, sewage treatment. Each as Zone S building (cost to budget) or separate utility-building category? Coordinate with Bucket 3 S classification.
- **Per-scale rollup rule.** Sum across cities → region pool; sum across regions → country pool? Loss on transfer (grid losses)? Country wealth decay (renewable regrowth vs extraction depletion)?
- **Deficit behaviour (concrete).** Once warning escalates: rolling blackouts (random cells lose power)? Happiness penalty? Desirability hit? Construction halts?
- **UI surface.** Utility pool dashboard (per scale). Which elements ship MVP? Coordinate with Bucket 6 UI polish.
- **Save schema impact.** Utility pool state per scale, contributor registrations. `schemaVersion` bump — coordinate with Bucket 3's bump.
- **Consumer-count inventory.** Which surfaces read utility pool state (HUD, info panels, CityStats Bucket 8, web dashboard)? Decide at exploration time for Bucket 8 parity contract.
- **Invariant compliance.** No new singletons. `UtilityPoolService` as MonoBehaviour + Inspector-wired. `GridManager` extraction carve-out if any (invariant #6).
- **BUG-20 interaction.** Existing utility-building visuals bug — does contributor registration inherently fix it, or is BUG-20 a separate save-restore bug that fires before utility pools land?
- **Hard deferrals re-check.** Climate / geographic base utility variation (rain → water pool modifier), renewable vs fossil granular mechanics, private utility operators — confirmed OUT at bucket level.
- **Interface with landmarks.** Some "super-utility" landmarks (e.g. 10× power plant) register as utility contributors via a scaling factor. Narrow catalog interface: landmark catalog row points at contributor registry entry; when placed, contributor registers as normal with a multiplier. Contract owned by sibling `docs/landmarks-exploration.md`; this doc owns the contributor registry contract it plugs into.

---

_Next step._ Run `/design-explore docs/utilities-exploration.md` to expand Approaches → selected approach → Architecture → Subsystem impact → Implementation points → subagent review. Then `/master-plan-new` to author `ia/projects/utilities-master-plan.md`.

---

## Design Expansion

### Chosen Approach

**Approach B — Country-pool first, local contributors feed it.** Confirmed by locked decisions (cross-scale rollup, per-scale pools, contributor registrations) + author lean. Rejected alternatives:

| Criterion | A Local-only | **B Country-pool** | C Signal-integrated | D Defer |
|---|---|---|---|---|
| Bucket framing fit (country resource) | fail | **pass** | partial | fail |
| Effort | low | **medium** | medium-high | zero |
| Output control (per-scale dashboards) | fail | **pass** | fail (per-cell only) | n/a |
| Maintainability (save schema clarity) | ok | **ok** | muddy (signal + pool dual model) | n/a |
| Dependencies on Bucket 2 signal contract | none | **none** | hard coupling | n/a |
| Genre table-stakes check | fail | **pass** | pass | fail |

Comparison matrix score: B wins 5/6; only "effort" ties with A.

**Interview delta (locked this session):** power deficit = cliff-edge freeze (no spawning, no road extension, no auto/manual placement) + slow happiness decay + map-wide desirability decay. **No rolling blackouts in v1**, no per-cell blackout effects, no lighting changes. Answers Open Question "Deficit behaviour".

### Architecture

Three new MonoBehaviour services, Inspector-wired, no singletons (invariant #4):

- **`UtilityPoolService`** — one per scale tier (city / region / country). Owns `Dictionary<UtilityKind, PoolState>` where `UtilityKind ∈ {Water, Power, Sewage}`. Per-tick `ComputeNet()` = Σ producers − Σ consumers. Maintains EMA(~5 ticks) of net balance. Emits `PoolStatusChanged` event on threshold cross (Healthy → Warning → Deficit). Rollup to parent scale via parent pointer.
- **`UtilityContributorRegistry`** — list of `IUtilityContributor` instances (production side) and `IUtilityConsumer` instances (consumption side). Registered on building placement, deregistered on demolition. Shared across scales via scale tag.
- **`DeficitResponseService`** — subscribes to `PoolStatusChanged`. On Deficit entry: sets `FreezeFlags.Expansion = true` (gates `AutoZoningManager`, `AutoRoadBuilder`, manual placement validators). Starts happiness decay coroutine (~−1 / game-day). Starts desirability decay (map-wide multiplicative decay on `CellData.desirability`, ~−2% / game-day, floor 0). On exit: clears flags + halts decay (no auto-rebound — recovery is content-driven).

Data contracts:

```
interface IUtilityContributor { UtilityKind Kind; float ProductionRate; ScaleTag Scale; }
interface IUtilityConsumer    { UtilityKind Kind; float ConsumptionRate; ScaleTag Scale; }
struct PoolState { float net; float ema; PoolStatus status; }
enum PoolStatus { Healthy, Warning, Deficit }
enum UtilityKind { Water, Power, Sewage }
enum ScaleTag { City, Region, Country }
```

Natural wealth seeding: `WaterBodyAdjacencyProbe` walks water-treatment placements, adds per-adjacent-water-cell production. Forest / mountain = ambient only (no pool feed — locked).

### Subsystem Impact

| Subsystem | Change | Invariant flag |
|---|---|---|
| `GridManager` | none direct — new services hold composition ref via carve-out (invariant #5 carve-out) | #5 carve-out documented at touch site |
| `CellData` | no new fields in v1 (pool state lives in services, not per-cell) | — |
| `DemandManager` | read `PoolStatus` from city `UtilityPoolService`; Deficit → demand multiplier floor | none |
| `CityStats` | happiness target reads `DeficitResponseService.HappinessPenalty` | none |
| `AutoZoningManager` / `AutoRoadBuilder` | check `FreezeFlags.Expansion` before spawn / extend | none |
| `ZoneManager` placement validators | reject manual placement when freeze active | none |
| `ForestManager` | unchanged (ambient bonus already pollution-side) | none |
| `PersistenceSystem` | new `utilityPoolsData` section: per-scale per-kind `{ net, ema, status }` floats + contributor registry ids. `schemaVersion` bump — **coordinate with Bucket 3 bump** | save spec §Load pipeline — restore pools AFTER grid cells (step 3), step 5 new | 
| `MiniMapController` | no change v1 (desirability decay flows through existing reader) | none |
| `UIManager.Utilities.cs` | new per-scale dashboard panel reading pool status | none |

**Spec gap:** no canonical `utility-system.md` spec exists yet — candidate domain for Bucket 4-a spec creation during `/master-plan-new`. Until then, glossary rows + project spec under `ia/projects/utilities-*.md`.

**BUG-20 interaction:** orthogonal. BUG-20 restores visuals on load — unrelated to contributor registration. Contributor re-registration on load piggybacks on existing building restore path; does not resolve or re-open BUG-20.

### Implementation Points

1. **Pool state machine (EMA thresholds).** `Warning` on EMA < 0 for ≥3 consecutive ticks; `Deficit` on instantaneous net ≤ 0 AND EMA ≤ −0.2 × |max(production, consumption)|. Hysteresis: exit Deficit only when EMA > 0 for ≥3 consecutive ticks.
2. **Freeze gate.** Single source of truth `FreezeFlags.Expansion` on `DeficitResponseService`. All expansion paths (spawn, road extend, auto, manual) check this flag before acting. Centralizes lockout, avoids ad-hoc checks.
3. **Happiness decay.** `HappinessPenalty` starts at 0 on Deficit entry, accumulates −1/day, capped at −20. `CityStats` subtracts from target, then lerps as usual. Locked decision: "slow decay".
4. **Desirability decay.** Map-wide multiplicative: on each game-day while Deficit, `CellData.desirability *= 0.98` (floor 0). Route through `GeographyManager.ApplyGlobalDesirabilityDelta()` (new helper, carve-out invariant #5) to avoid direct `cellArray` loops outside `GridManager`. No per-cell blackout logic.
5. **Cross-scale rollup.** Country pool = Σ regions + country-only contributors. Region pool = Σ cities + region-only contributors. Loss-on-transfer = **0% in v1** (grid losses deferred). Deficit cascades down: country Deficit → all child regions / cities inherit `FreezeFlags.Expansion` until country recovers.
6. **Save schema.** `utilityPoolsData: { scale: { kind: { net, ema, status } } }` + `utilityContributorIds: [...]`. Restore in persistence step 5 (after grid cells, so building registrations already exist). No contributor state to persist beyond building placement.
7. **Consumer inventory (Bucket 8 parity).** Readers: per-scale dashboard, HUD indicator, info panel on contributor buildings, web dashboard via CityStats export. Bucket 8 CityStats receives pool status per scale.
8. **Terrain-sensitive placement.** Placement validators return `Invalid` without indicator — player discovers by attempting. Water/sewage treatment check Moore-adjacent water cell. Large regionals check terrain tag.
9. **Contributor archetypes v1.** Power: coal (high pollution), solar (low output, no pollution), wind (mountain-gated). Water: treatment (water-adjacent required). Sewage: treatment (water-adjacent + pollutes downstream — flags downstream cells). 2–3 tier upgrades by output threshold (not tech tree — locked).
10. **Landmarks hook.** `UtilityContributorRegistry` exposes `RegisterWithMultiplier(IUtilityContributor, float)` — landmarks plug in via sibling landmarks doc. This doc owns the registry contract.

### Examples

**Deficit entry flow (power pool, city scale):**

```
Tick N:   net = -3.0, ema = -0.5 → Warning (3rd tick < 0)
Tick N+5: net = -4.0, ema = -2.1, max(prod, cons) = 8.0 → -2.1 < -1.6 → Deficit
          DeficitResponseService:
            FreezeFlags.Expansion = true
            HappinessPenalty coroutine starts
            DesirabilityDecay coroutine starts
          AutoZoningManager.TrySpawn() → returns false (flag check)
          ZoneManager.ValidateManualPlace() → returns Invalid (flag check)
```

**Save round-trip:**

```json
{
  "schemaVersion": 14,
  "utilityPoolsData": {
    "city": {
      "Water":  { "net":  2.0, "ema":  1.8, "status": "Healthy" },
      "Power":  { "net": -4.0, "ema": -2.1, "status": "Deficit" },
      "Sewage": { "net":  0.5, "ema":  0.3, "status": "Healthy" }
    },
    "region":  { /* … */ },
    "country": { /* … */ }
  }
}
```

### Review Notes

Subagent review skipped (Phase 8) — exploration doc, no code changes. Blocking items to resolve at `/master-plan-new` phase:

- **Non-blocking — Bucket 3 schema coordination.** `schemaVersion` bump must align with Zone S economy bump. Master plan must sequence after Bucket 3 schema lands or share the same bump.
- **Non-blocking — spec authority.** New `ia/specs/utility-system.md` spec vs glossary-only + project spec. Decide at `/master-plan-new` Phase 0.
- **Non-blocking — grid loss deferral.** Cross-scale transfer loss = 0 in v1. Flag as post-MVP tunable.
- **Non-blocking — landmarks contract.** Registry multiplier API owned here, consumed by landmarks doc. Sync before both ship.

### Expansion metadata

- Date: 2026-04-17
- Model: claude-opus-4-7
- Approach selected: B — Country-pool first, local contributors
- Blocking items resolved: 0 (none raised — all open items non-blocking, deferred to master plan)
