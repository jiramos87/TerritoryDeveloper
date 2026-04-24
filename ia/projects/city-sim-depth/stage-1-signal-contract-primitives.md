### Stage 1 — Signal Layer Foundation / Signal Contract Primitives

**Status:** In Progress (4 tasks filed 2026-04-17 — TECH-305..TECH-308)

**Objectives:** Author core type surface — `SimulationSignal` enum, `SignalField`, `SignalMetadataRegistry` ScriptableObject, `ISignalProducer`/`ISignalConsumer` interfaces, `SignalFieldRegistry` MonoBehaviour — and the canonical `ia/specs/simulation-signals.md` reference spec that closes the spec gap flagged in the exploration review.

**Exit:**

- `SimulationSignal` enum has exactly 12 entries matching locked signal inventory.
- `SignalField.Get(x,y)` always returns ≥ 0 (floor clamp); `Snapshot()` returns independent copy.
- `SignalFieldRegistry` creates 12 fields on `Awake` sized from `GridManager`; `FindObjectOfType` fallback present (invariant #4).
- `ia/specs/simulation-signals.md` authored; linked from `ia/specs/simulation-system.md` §Tick execution order.
- `npm run validate:all` passes.
- Phase 1 — Core types: enum + interfaces + SignalField + SignalMetadataRegistry.
- Phase 2 — SignalFieldRegistry MonoBehaviour + reference spec.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | SimulationSignal enum + interfaces | **TECH-305** | Draft | Author `SimulationSignal` enum (12 entries: `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`) in new `Assets/Scripts/Simulation/Signals/`. Author `ISignalProducer` (`void EmitSignals(SignalFieldRegistry)`) + `ISignalConsumer` (`void ConsumeSignals(SignalFieldRegistry, DistrictSignalCache)`) interfaces in same dir. |
| T1.2 | SignalField + SignalMetadataRegistry | **TECH-306** | Draft | `SignalField` — `float[,]` backing store; `Get(x,y)`, `Set(x,y,v)`, `Add(x,y,v)`, `Snapshot()` (returns new `float[,]` copy); clamp floor 0 on all writes. `SignalMetadataRegistry` ScriptableObject — per `SimulationSignal` entry: `diffusionRadius`, `decayPerStep`, `anisotropy (Vector2)`, `rollupRule (Mean/P90 enum)`. |
| T1.3 | SignalFieldRegistry MonoBehaviour | **TECH-307** | Draft | `SignalFieldRegistry` MonoBehaviour — allocates one `SignalField` per `SimulationSignal` in `Awake` sized from `GridManager.gridWidth`/`gridHeight`; `GetField(SimulationSignal)` accessor; `[SerializeField] GridManager grid` + `FindObjectOfType` fallback (invariant #4); resize method for map reload. |
| T1.4 | simulation-signals.md reference spec | **TECH-308** | Draft | Author `ia/specs/simulation-signals.md` — signal inventory table (12 entries: source types, sink types, rollup rule, update cadence per entry), diffusion physics contract (separable Gaussian, anisotropy, decay, clamp-floor-0 rule), `ISignalProducer`/`ISignalConsumer` interface contract, rollup rule table (P90 for `Crime`+`TrafficLevel`; mean for rest), spec-gap closure note. Link new spec from `ia/specs/simulation-system.md` §Tick execution order addendum. |

---
