---
purpose: "TECH-305 — SimulationSignal enum + ISignalProducer/ISignalConsumer interfaces."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-305 — `SimulationSignal` enum + `ISignalProducer`/`ISignalConsumer` interfaces

> **Issue:** [TECH-305](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Type surface foundation for **city-sim depth** 12-signal contract. Author `SimulationSignal` enum (12 locked entries) + `ISignalProducer` / `ISignalConsumer` interfaces under new `Assets/Scripts/Simulation/Signals/` dir. No runtime wiring — downstream tasks (TECH-306 `SignalField`, TECH-307 registry MonoBehaviour, Stage 1.2 diffusion + scheduler) consume these types.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `SimulationSignal` enum w/ exactly 12 entries matching locked inventory: `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`.
2. `ISignalProducer` interface — `void EmitSignals(SignalFieldRegistry registry)`.
3. `ISignalConsumer` interface — `void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)`.
4. New dir `Assets/Scripts/Simulation/Signals/` compiles clean.

### 2.2 Non-Goals (Out of Scope)

1. No `SignalField` impl (TECH-306).
2. No registry MonoBehaviour (TECH-307).
3. No diffusion kernel (Stage 1.2).
4. No producer/consumer implementations (Step 2+).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Cite `SimulationSignal.PollutionAir` in downstream code | Enum compiles; 12 entries present |
| 2 | Developer | Implement `ISignalProducer` on `PowerPlant` (Step 2) | Interface signature stable; reference resolves |

## 4. Current State

### 4.1 Domain behavior

No shared signal contract. `CityStats` carries single-scalar happiness + pollution. Per-cell spatial signals absent.

### 4.2 Systems map

- `ia/specs/simulation-system.md` §Tick execution order — future insertion point (Stage 1.2).
- `Assets/Scripts/Simulation/Signals/` — new dir.
- Invariants #12 (permanent domain), #4 (MonoBehaviour pattern) — apply downstream, not this task.

## 5. Proposed Design

### 5.1 Target behavior (product)

Type surface only. No runtime behavior change. Enum values + interface method signatures locked.

### 5.2 Architecture / implementation

Plain enum + 2 interfaces. Forward-declare `SignalFieldRegistry` + `DistrictSignalCache` types in parameter positions (compile-check requires those types exist — gate w/ TECH-307 + Stage 1.3 respectively). Resolution: interfaces reference types by name; `SignalFieldRegistry` lands TECH-307; for `DistrictSignalCache` (Stage 1.3) use forward-declared placeholder class OR defer `ISignalConsumer` signature to land alongside Stage 1.3. Implementer decides — prefer placeholder class in `Assets/Scripts/Simulation/Signals/` marked TODO, replaced by Stage 1.3 real type.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Include `ISignalConsumer.DistrictSignalCache` param now despite Stage 1.3 dep | Avoid interface churn mid-step | Land interface Stage 1.3; rejected — producer/consumer pair cited throughout Step 1 |

## 7. Implementation Plan

### Phase 1 — Enum + interfaces

- [ ] Create `Assets/Scripts/Simulation/Signals/SimulationSignal.cs` w/ 12 entries.
- [ ] Create `Assets/Scripts/Simulation/Signals/ISignalProducer.cs`.
- [ ] Create `Assets/Scripts/Simulation/Signals/ISignalConsumer.cs` (placeholder `DistrictSignalCache` type or forward-decl).
- [ ] `npm run unity:compile-check` clean.
- [ ] `npm run validate:all` clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Files compile; enum has 12 entries | Unity compile | `npm run unity:compile-check` | Runs Editor batch compile |
| IA indexes clean | Node | `npm run validate:all` | Chains dead-project-specs + test:ia + fixtures + ia-indexes --check |

## 8. Acceptance Criteria

- [ ] `SimulationSignal.cs` enum has 12 entries matching locked inventory.
- [ ] `ISignalProducer.EmitSignals(SignalFieldRegistry)` signature matches stage spec.
- [ ] `ISignalConsumer.ConsumeSignals(SignalFieldRegistry, DistrictSignalCache)` signature matches stage spec.
- [ ] `npm run unity:compile-check` clean.
- [ ] `npm run validate:all` clean.

## Open Questions

1. `DistrictSignalCache` forward-decl vs placeholder — implementer picks based on compile outcome. Affects only local compile, not interface semantics.
