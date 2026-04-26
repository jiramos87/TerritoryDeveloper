---
purpose: "Reference spec for the city-sim depth signal contract — 12-signal inventory, diffusion physics, producer/consumer interface, rollup taxonomy."
audience: agent
loaded_by: ondemand
slices_via: spec_section
---
# Simulation Signals — Reference Spec

> Permanent contract for the city-sim depth signal layer. Closes the spec gap flagged in the city-sim-depth master plan exploration. Invariant 12 (signal contract = permanent shared domain).

## Purpose and scope

City-sim depth Bucket 2 introduces 12 named per-cell signals (pollution, services, crime, traffic, waste, land value) that cross-link production buildings to demand, growth, and budget pressure. This spec is the canonical source for:

- Locked signal inventory (12 entries, ordinal-stable).
- Diffusion physics contract (separable Gaussian + decay + clamp).
- Producer/consumer interface contract + tick ordering.
- Rollup rule per signal (Mean vs P90 for district aggregation).
- Insertion point in `simulation-system.md` §Tick execution order.

Source code:

- `Assets/Scripts/Simulation/Signals/SimulationSignal.cs` — locked enum.
- `Assets/Scripts/Simulation/Signals/SignalField.cs` — per-cell store w/ clamp-floor-0 invariant.
- `Assets/Scripts/Simulation/Signals/SignalMetadataRegistry.cs` — ScriptableObject w/ per-signal diffusion / decay / anisotropy / rollup.
- `Assets/Scripts/Simulation/Signals/SignalFieldRegistry.cs` — per-scene MonoBehaviour owning 12 fields.
- `Assets/Scripts/Simulation/Signals/ISignalProducer.cs` + `ISignalConsumer.cs` — emit / consume contracts.

Out of scope (lands in later Stages):

- `DiffusionKernel` impl — Stage 1.2.
- `SignalTickScheduler` — Stage 1.2.
- `DistrictSignalCache` real impl + rollup pipeline — Stage 1.3.
- Per-building producer / consumer impls — Step 2 of the city-sim depth master plan.

## Signal inventory

Locked 12-entry contract; ordinal order = enum order in `SimulationSignal.cs`. Inventory order is itself a contract (`SignalMetadataRegistry.entries[i]` indexes by ordinal). Source / sink lists are non-exhaustive examples lifted from the city-sim-depth exploration.

| Ordinal | Signal | Source types (examples) | Sink types (examples) | Rollup | Cadence |
|---|---|---|---|---|---|
| 0 | `PollutionAir` | factories, power plants, traffic | forests, parks | Mean | daily |
| 1 | `PollutionLand` | landfills, factories | forests, parks | Mean | daily |
| 2 | `PollutionWater` | factories, sewage outflow | water treatment | Mean | daily |
| 3 | `Crime` | high-density poor zones | police stations | P90 | daily |
| 4 | `ServicePolice` | police stations | residential / commercial demand | Mean | daily |
| 5 | `ServiceFire` | fire stations | residential / commercial demand | Mean | daily |
| 6 | `ServiceEducation` | schools, universities | residential demand | Mean | monthly |
| 7 | `ServiceHealth` | clinics, hospitals | residential demand | Mean | monthly |
| 8 | `ServiceParks` | parks, plazas | residential / land value | Mean | monthly |
| 9 | `TrafficLevel` | road usage estimator | residents, commercial | P90 | daily |
| 10 | `WastePressure` | residential / commercial / industrial | landfills, recycling | Mean | daily |
| 11 | `LandValue` | composite (parks + low pollution + service coverage) | growth + tax | Mean | monthly |

## Diffusion physics contract

Each tick, after producers emit, a separable Gaussian diffusion runs once per signal field.

- **Kernel shape:** separable horizontal pass + separable vertical pass (Gaussian blur).
- **Per-axis sigma:** `sigmaX = diffusionRadius * anisotropy.x`, `sigmaY = diffusionRadius * anisotropy.y` — both pulled from `SignalMetadataRegistry.GetMetadata(signal)`. Anisotropy lets road-aligned signals (e.g. `TrafficLevel`) bias one axis.
- **Decay:** after diffusion each tick, multiply every cell by `(1 - decayPerStep)` (clamped to `[0, 1]` via metadata authoring discipline).
- **Floor clamp:** `SignalField.Set` and `SignalField.Add` clamp resulting cell value to `>= 0`. Negative emission contributions (forest sinks) cancel positive sources via accumulation but stored value never goes negative. Diffusion + decay never produce negative values, so floor clamp at the field boundary is sufficient.
- **Allocation:** diffusion uses `SignalField.Snapshot()` for ping-pong buffers; the snapshot copy is deep (independent reference).

## Interface contract

Two interfaces under `Territory.Simulation.Signals`:

```csharp
public interface ISignalProducer
{
    void EmitSignals(SignalFieldRegistry registry);
}

public interface ISignalConsumer
{
    void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache);
}
```

**Ordering guarantees (per signal-tick phase, once per `SimulationManager.ProcessSimulationTick`):**

1. All `ISignalProducer.EmitSignals` calls run first; producers may write to any signal field but must not read post-diffusion data.
2. Diffusion kernel runs over every signal field (separable Gaussian + decay).
3. District rollup populates `DistrictSignalCache` per `RollupRule` (Mean / P90).
4. All `ISignalConsumer.ConsumeSignals` calls run last; consumers see the post-diffusion + post-rollup state.

Producers and consumers are discovered via Inspector lists on the `SignalTickScheduler` (Stage 1.2) — no per-frame `FindObjectOfType` (invariant 3).

## Rollup rule table

Per-signal aggregation rule applied during step 3 of the tick phase to populate `DistrictSignalCache`:

| Signal | Rollup |
|---|---|
| `PollutionAir` | Mean |
| `PollutionLand` | Mean |
| `PollutionWater` | Mean |
| `Crime` | P90 |
| `ServicePolice` | Mean |
| `ServiceFire` | Mean |
| `ServiceEducation` | Mean |
| `ServiceHealth` | Mean |
| `ServiceParks` | Mean |
| `TrafficLevel` | P90 |
| `WastePressure` | Mean |
| `LandValue` | Mean |

**Rationale:** Mean models steady exposure (pollution, services). P90 captures peak / hot-spot pressure where the worst cell drives gameplay response (crime hot-spots trigger station demand; traffic peaks throttle commute satisfaction).

## Tick phase insertion

The signal phase inserts into `simulation-system.md` §Tick execution order between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`. New step list:

1. `GrowthBudgetManager.EnsureBudgetValid`
2. `UrbanCentroidService.RecalculateFromGrid`
3. **Signal phase** (`SignalTickScheduler` — Stage 1.2): producers → diffusion → rollup → consumers.
4. `AutoRoadBuilder`
5. `AutoZoningManager`
6. `AutoResourcePlanner`

## Spec-gap closure note

City-sim-depth master plan (`ia/projects/city-sim-depth/index.md`) flagged the signal contract as a permanent shared domain that needed a canonical reference spec rather than living only in the exploration doc. This spec closes that gap. Source truths consulted while authoring:

- `docs/city-sim-depth-exploration.md` §Design Expansion — original signal inventory + rollup design.
- City-sim-depth master plan locked-decisions header — confirmed 12-entry list + P90 vs Mean assignments.

Future revisions follow the standard reference-spec workflow (edit + `npm run validate:all`); do NOT rely on the exploration doc as a parallel source after this spec lands.
