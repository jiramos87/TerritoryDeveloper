---
purpose: "BUG-16 — Race condition in GeographyManager vs TimeManager initialization."
audience: both
loaded_by: ondemand
slices_via: none
---
# BUG-16 — Possible race condition in GeographyManager vs TimeManager initialization

> **Issue:** [BUG-16](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Unity `Start()` order not guaranteed across MonoBehaviours. `TimeManager.Update()` may tick before `GeographyManager` completes **geography initialization**, causing access to null/empty grid data. Gate ticks on an `isInitialized` flag (or use Script Execution Order) so time-driven systems wait for geography ready signal.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `TimeManager` never reads `GeographyManager` state before **geography initialization** complete.
2. Deterministic init order — explicit gate, not implicit script-order reliance.
3. No NRE / silent empty reads on fresh scene load.

### 2.2 Non-Goals (Out of Scope)

1. Refactor of `GeographyManager` internals beyond readiness signal.
2. General manager-bootstrap overhaul across other subsystems.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Load scene without races so time + geography stay consistent | No NRE; first tick reads populated grid |

## 4. Current State

### 4.1 Domain behavior

`GeographyManager` builds `HeightMap`, `WaterMap`, `Cell[]` in `Start()`. `TimeManager.Update()` drives sim ticks — if it fires before geography ready, downstream consumers hit null/empty data.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GeographyManager.cs` — **geography initialization** owner.
- `Assets/Scripts/Managers/GameManagers/TimeManager.cs` — tick driver.
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — grid data backing.
- Spec refs: `ia/specs/isometric-geography-system.md` (init flow), invariants §1 (`HeightMap`/`Cell.height` sync).

### 4.3 Implementation investigation notes

Two viable approaches: (a) Unity Script Execution Order — brittle, hidden config; (b) `isInitialized` flag on `GeographyManager` + early-return in `TimeManager.Update()` until ready — explicit, testable. Prefer (b).

## 5. Proposed Design

### 5.1 Target behavior (product)

Time does not advance until geography reports ready. No visible effect in normal play — defensive correctness only.

### 5.2 Architecture / implementation

- Add `public bool IsInitialized { get; private set; }` on `GeographyManager`; set true at end of init.
- `TimeManager.Update()` early-return when `geography.IsInitialized == false`.
- Cache `GeographyManager` ref in `Awake()` via `FindObjectOfType` fallback (invariant #3).

### 5.3 Method / algorithm notes

N/A — trivial gate.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Gate via `isInitialized` flag | Explicit, testable, no hidden Unity config | Script Execution Order (brittle) |

## 7. Implementation Plan

### Phase 1 — Ready gate

- [ ] Add `IsInitialized` to `GeographyManager`; set at init end.
- [ ] Cache `geography` ref in `TimeManager.Awake()`.
- [ ] Early-return in `TimeManager.Update()` until ready.
- [ ] Edit-mode test: fresh scene load → first tick fires only after init.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `TimeManager` does not tick pre-init | Edit mode test | `npm run unity:testmode-batch` | Fresh scene → assert tick count 0 until `IsInitialized` |
| No NRE on load | Play mode smoke | `unity_bridge_command get_console_logs` | Clean console after scene load |
| C# compile clean | Node | `npm run unity:compile-check` | After edits |

## 8. Acceptance Criteria

- [ ] `GeographyManager.IsInitialized` flips true only after **geography initialization** complete.
- [ ] `TimeManager.Update()` returns early until flag true.
- [ ] No NRE in console on fresh scene load.
- [ ] Edit-mode test passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — defensive gate; no gameplay change.
