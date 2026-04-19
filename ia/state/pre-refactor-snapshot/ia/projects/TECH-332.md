---
purpose: "TECH-332 — PoolState struct."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-332 — PoolState struct

> **Issue:** [TECH-332](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Blittable value-type struct capturing per-pool state — instantaneous `net`, smoothed `ema`, current `PoolStatus`, and two hysteresis counters (consecutive neg / pos EMA ticks). Consumed by `UtilityPoolService.pools` dictionary in Stage 1.2 and by threshold state-machine transitions. Default ctor returns `Healthy` + zeros.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Create `Assets/Scripts/Data/Utilities/PoolState.cs` — struct w/ fields `float net`, `float ema`, `PoolStatus status`, `int consecutiveNegativeEmaTicks`, `int consecutivePositiveEmaTicks`.
2. Struct stays blittable (no managed refs) — safe for future Burst / native containers if needed.
3. Default value → `Healthy`, all floats / ints = 0.
4. XML doc each field citing role in state machine.

### 2.2 Non-Goals

1. State-machine logic (Stage 1.2 Phase 2).
2. Serialization DTO (Stage 4.1).
3. Equality / hashing overrides — defer until needed.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.2 implementer | As `UtilityPoolService` author, I want `PoolState` fields ready so `TickPools` + EMA can mutate them. | Struct exists, fields named per spec. |

## 4. Current State

### 4.1 Domain behavior

No pool state type yet.

### 4.2 Systems map

- New file `Assets/Scripts/Data/Utilities/PoolState.cs`.
- Depends on TECH-331 (`PoolStatus`).
- Consumers: `UtilityPoolService` (Stage 1.2), save DTO (Stage 4.1).

## 5. Proposed Design

### 5.2 Architecture

```csharp
public struct PoolState {
    public float net;
    public float ema;
    public PoolStatus status;
    public int consecutiveNegativeEmaTicks;
    public int consecutivePositiveEmaTicks;
}
```

Default(`PoolState`) → all zeros, `status == Healthy` (first enum value).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | struct not class | Blittable + cheap copy + dict-value mutation pattern standard | Class — rejected, allocation noise per tick |

## 7. Implementation Plan

### Phase 1 — Struct

- [ ] Create `Assets/Scripts/Data/Utilities/PoolState.cs`.
- [ ] Fields per §5.2; XML doc each.
- [ ] Confirm default value → Healthy + zeros.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Struct compiles | Unity | `npm run unity:compile-check` | — |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `PoolState.cs` exists w/ five fields named per spec.
- [ ] Blittable (no ref-type fields).
- [ ] XML doc every field.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling / data-model scaffolding only.
