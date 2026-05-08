---
purpose: "TECH-307 — SignalFieldRegistry MonoBehaviour."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-307 — `SignalFieldRegistry` MonoBehaviour

> **Issue:** [TECH-307](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

MonoBehaviour owner of 12 per-signal `SignalField` instances. Allocates fields in `Awake` sized from `GridManager`; exposes `GetField(SimulationSignal)` accessor; resizes on map reload. Injected into Stage 1.2 `SignalTickScheduler` + Step 2 composers via Inspector. Respects invariants #3 (no hot-path `FindObjectOfType`), #4 (Inspector + fallback), #6 (new MonoBehaviour, not added to `GridManager`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `SignalFieldRegistry` MonoBehaviour allocates exactly 12 `SignalField` instances in `Awake`, sized from `GridManager.gridWidth` + `gridHeight`.
2. `GetField(SimulationSignal)` returns per-signal `SignalField` (O(1) array lookup).
3. `[SerializeField] private GridManager grid` + `FindObjectOfType<GridManager>()` fallback in `Awake` (invariant #4).
4. `ResizeForMap(int w, int h)` reallocates all 12 fields on map reload.
5. Zero `FindObjectOfType` calls in `Update` or per-frame loops (invariant #3).

### 2.2 Non-Goals (Out of Scope)

1. No tick scheduling (Stage 1.2 `SignalTickScheduler`).
2. No diffusion (Stage 1.2 `DiffusionKernel`).
3. No producer/consumer iteration.
4. No save persistence (signal fields NOT persisted — locked decision).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Call `registry.GetField(SimulationSignal.PollutionAir).Add(x,y,2f)` from producer | Field mutated |
| 2 | Developer | Reload map → signal fields resize to new grid dims | `ResizeForMap` reallocates; `GetField` returns new-size field |

## 4. Current State

### 4.1 Domain behavior

No signal registry exists.

### 4.2 Systems map

- `Assets/Scripts/Simulation/Signals/SimulationSignal.cs` (TECH-305) — enum.
- `Assets/Scripts/Simulation/Signals/SignalField.cs` (TECH-306) — per-signal store.
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — grid dim source.
- Invariant #3 (no hot-path `FindObjectOfType`), #4 (`[SerializeField]` + fallback), #6 (new MonoBehaviour, not `GridManager` ext).

## 5. Proposed Design

### 5.1 Target behavior

Scene component; one per scene. Downstream systems get injected ref or fall back via `FindObjectOfType` in their `Awake`.

### 5.2 Architecture / implementation

- `private SignalField[] fields;` sized `Enum.GetValues(typeof(SimulationSignal)).Length` (12).
- `Awake` — resolve `grid` via `[SerializeField]` or fallback; allocate fields sized `grid.gridWidth × grid.gridHeight`.
- `GetField(SimulationSignal s) => fields[(int)s];`.
- `ResizeForMap(int w, int h)` — reallocate all 12 fields.
- No `Update` / `FixedUpdate` — pure data holder.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Array indexed by enum ordinal (not Dictionary) | O(1) lookup; zero alloc | Dictionary<SimulationSignal, SignalField>; rejected — hash overhead per tick |

## 7. Implementation Plan

### Phase 1 — MonoBehaviour + `Awake` allocation

- [ ] Create `Assets/Scripts/Simulation/Signals/SignalFieldRegistry.cs`.
- [ ] `[SerializeField] private GridManager grid;` + `FindObjectOfType` fallback in `Awake`.
- [ ] Allocate `SignalField[12]` sized from grid.
- [ ] `GetField(SimulationSignal)` accessor.

### Phase 2 — Resize + verification

- [ ] `ResizeForMap(int, int)` reallocator.
- [ ] Verify no `FindObjectOfType` in `Update` (grep gate).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compiles | Unity compile | `npm run unity:compile-check` | — |
| No hot-path `FindObjectOfType` (invariant #3) | Manual grep | `rg "FindObjectOfType" Assets/Scripts/Simulation/Signals/SignalFieldRegistry.cs` | Hits only in `Awake` / `Start` |
| IA indexes clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `SignalFieldRegistry` MonoBehaviour compiles.
- [ ] `Awake` allocates 12 `SignalField` instances sized from `GridManager`.
- [ ] `GetField(SimulationSignal)` returns matching field.
- [ ] `[SerializeField] GridManager grid` + `FindObjectOfType` fallback present.
- [ ] `ResizeForMap` reallocates fields.
- [ ] Zero `FindObjectOfType` calls in `Update` / per-frame paths.
- [ ] `npm run unity:compile-check` + `npm run validate:all` clean.

## Open Questions

None — tooling + foundation infra only; behavior locked by stage spec.
