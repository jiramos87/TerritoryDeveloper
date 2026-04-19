---
purpose: "TECH-306 — SignalField + SignalMetadataRegistry ScriptableObject."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-306 — `SignalField` + `SignalMetadataRegistry` ScriptableObject

> **Issue:** [TECH-306](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add data primitives for **city-sim depth** signal contract — `SignalField` plain C# class (per-signal `float[,]` backing store w/ clamp-floor-0 writes) + `SignalMetadataRegistry` ScriptableObject holding per-signal diffusion/decay/anisotropy/rollup metadata. Consumes TECH-305 `SimulationSignal` enum. No MonoBehaviour wiring — types + SO asset only.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `SignalField` — `float[,]` backing store sized `(w, h)`; `Get(x,y)`, `Set(x,y,v)`, `Add(x,y,v)`, `Snapshot()` methods; every write clamps floor to 0.
2. `Snapshot()` returns new independent `float[,]` copy (different reference from backing store).
3. `SignalMetadataRegistry` ScriptableObject — per `SimulationSignal` entry: `float diffusionRadius`, `float decayPerStep`, `Vector2 anisotropy`, `enum RollupRule { Mean, P90 }`; indexed by ordinal.
4. Inspector-authorable SO asset sample with 12 rows.

### 2.2 Non-Goals (Out of Scope)

1. No registry MonoBehaviour (TECH-307).
2. No diffusion kernel (Stage 1.2).
3. No scene wiring.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Read `SignalField.Get(10,10)` after a sequence of `Add` writes w/ negative emissions | Result ≥ 0 (floor clamp) |
| 2 | Designer | Tune `diffusionRadius` per signal in Inspector | SO field editable; persists in asset |

## 4. Current State

### 4.1 Domain behavior

No per-cell signal store. Pollution aggregate lives as single scalar on `CityStats`.

### 4.2 Systems map

- `Assets/Scripts/Simulation/Signals/SimulationSignal.cs` (TECH-305) — enum consumed.
- New files: `SignalField.cs`, `SignalMetadataRegistry.cs`.
- Stage 1.2 `DiffusionKernel` reads `SignalMetadataRegistry` per-signal params.

## 5. Proposed Design

### 5.1 Target behavior (product)

Data types only. Floor-clamp-0 invariant guaranteed at `SignalField` boundary — negative emissions (forest sinks) still cancel positive sources via accumulation, but stored field never holds negative value.

### 5.2 Architecture / implementation

- `SignalField` plain C#; constructor `SignalField(int width, int height)` allocates `float[width, height]`.
- `Add(x,y,v)`: `float next = values[x,y] + v; values[x,y] = next < 0f ? 0f : next;`.
- `Set(x,y,v)`: same clamp.
- `Snapshot()`: `float[,] copy = new float[w,h]; Array.Copy` (or manual loop) → return copy.
- `SignalMetadataRegistry` — `CreateAssetMenu` ScriptableObject; serialized `Entry[] entries` sized 12; `GetMetadata(SimulationSignal s)` returns `entries[(int)s]`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Clamp at `SignalField` boundary (not in diffusion / composer) | Single enforcement point; Stage 1.2 kernel + composers stay pure | Clamp in kernel; rejected — scattered invariant |

## 7. Implementation Plan

### Phase 1 — `SignalField`

- [ ] Create `Assets/Scripts/Simulation/Signals/SignalField.cs`.
- [ ] Implement `Get`/`Set`/`Add`/`Snapshot` w/ clamp-floor-0 on writes.

### Phase 2 — `SignalMetadataRegistry`

- [ ] Create `Assets/Scripts/Simulation/Signals/SignalMetadataRegistry.cs` (ScriptableObject).
- [ ] Define `Entry` struct + `GetMetadata(SimulationSignal)` accessor.
- [ ] Author sample asset w/ 12 rows (seed values — TECH-308 spec provides tuning guidance).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `SignalField` floor clamp + Snapshot independence | EditMode test (optional, can defer to Stage 1.2) | — | Primary coverage via TECH-306+ compile checks |
| Compile | Unity compile | `npm run unity:compile-check` | — |
| IA indexes clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `SignalField.Get` always returns ≥ 0 after any sequence of writes.
- [ ] `Snapshot()` returns new `float[,]` (different reference from backing).
- [ ] `SignalMetadataRegistry` SO has entry per `SimulationSignal` ordinal.
- [ ] `GetMetadata(SimulationSignal)` accessor returns matching entry.
- [ ] `npm run unity:compile-check` clean; `npm run validate:all` clean.

## Open Questions

1. Sample metadata seed values (diffusionRadius per signal) — TECH-308 spec supplies final values; implementer can use placeholder (e.g. radius=4 for all) and adjust once TECH-308 lands.
