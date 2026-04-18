---
purpose: "TECH-422 — Save-schema v3→v4 bump + BudgetAllocationData + StateServiceZoneData."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T1.3.5"
---
# TECH-422 — Save-schema v3→v4 bump + `BudgetAllocationData` + `StateServiceZoneData`

> **Issue:** [TECH-422](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Stage 1.3 Phase 3. Bump `GameSaveData.CurrentSchemaVersion` 3 → 4. Introduce two new `[Serializable]` payload types — `BudgetAllocationData` (envelope pct + global cap + current month remaining) and `StateServiceZoneData` (cellX / cellY / subTypeId / densityTier). Add as fields on `GameSaveData`. No migration branch this task — TECH-423 seeds defaults for legacy v3.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GameSaveData.CurrentSchemaVersion = 4` (was 3, see `GameSaveManager.cs:404`). Update XML doc comment to append "Schema 4 adds `budgetAllocation` + `stateServiceZones` (Stage 1.3 envelope budget)."
2. New `[Serializable] public class BudgetAllocationData { public float[] envelopePct; public int globalMonthlyCap; public int[] currentMonthRemaining; public static BudgetAllocationData Default(int cap); }` — under `namespace Territory.Economy`.
3. New `[Serializable] public class StateServiceZoneData { public int cellX; public int cellY; public int subTypeId; public int densityTier; }` — under `namespace Territory.Economy`.
4. `GameSaveData` carries `public BudgetAllocationData budgetAllocation` + `public List<StateServiceZoneData> stateServiceZones = new List<StateServiceZoneData>();` (match existing list-field defaulting convention at lines 411 / 419).
5. `Default(cap)` seeds `envelopePct = new float[7]` each slot `1f/7f` (≈ 0.142857) then explicit normalize pass so `sum == 1.0` (matches `BudgetAllocationService.NormalizeInPlace` at lines 150–170). Seed `currentMonthRemaining[i] = (int)(cap * envelopePct[i])` (matches cast at `BudgetAllocationService.cs:177`).
6. `unity:compile-check` green.

### 2.2 Non-Goals

1. No migration branch — TECH-423.
2. No save/load round-trip — TECH-424.

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `GameSaveData` class (§277+), `CurrentSchemaVersion` (line 404), `MigrateLoadedSaveData`.
- `ia/specs/persistence-system.md` §Save — serialization pattern.

## 5. Proposed Design

### 5.2 Architecture

`BudgetAllocationData.Default(cap)` is the single source for default envelope seed — both migration (TECH-423) and fresh-game init (future) route through it. Ensures consistency. Default pct array normalized post-seed; inline the normalize loop inside `Default` (short, local) — don't extract a shared helper across `BudgetAllocationService` + `BudgetAllocationData` this task (separate concerns: Data = pure POCO, Service = runtime mutation). TECH-424 may lift if duplication accumulates.

### 5.3 File placement

- New file: `Assets/Scripts/Managers/GameManagers/BudgetAllocationData.cs` — holds `[Serializable] BudgetAllocationData` under `namespace Territory.Economy`.
- New file: `Assets/Scripts/Managers/GameManagers/StateServiceZoneData.cs` — holds `[Serializable] StateServiceZoneData` under `namespace Territory.Economy`.
- Rationale: mirrors existing sibling split (`BudgetAllocationService.cs`, `IBudgetAllocator.cs`, `TreasuryFloorClampService.cs`). Keeps `GameSaveManager.cs` from growing new types inline.
- `GameSaveManager.cs` imports `Territory.Economy` at file top (check existing usings; add only if missing).

## 7. Implementation Plan

### Phase 1 — Create `BudgetAllocationData` POCO

- [ ] Create `Assets/Scripts/Managers/GameManagers/BudgetAllocationData.cs` under `namespace Territory.Economy`.
- [ ] `[System.Serializable] public class BudgetAllocationData` with three fields: `public float[] envelopePct;`, `public int globalMonthlyCap;`, `public int[] currentMonthRemaining;`.
- [ ] Static factory `public static BudgetAllocationData Default(int cap)`: allocate `envelopePct = new float[7]` each slot `1f/7f`; normalize in-place so `sum == 1.0` (guard `sum < 1e-9` → log + return uniform anyway); set `globalMonthlyCap = cap`; allocate `currentMonthRemaining = new int[7]` with `(int)(cap * envelopePct[i])`.
- [ ] XML doc on class: "Persisted snapshot of `BudgetAllocationService` state (envelope pct + cap + remaining). Added schema 4. See `ia/projects/TECH-422-save-schema-v3-v4-bump.md`."

### Phase 2 — Create `StateServiceZoneData` POCO

- [ ] Create `Assets/Scripts/Managers/GameManagers/StateServiceZoneData.cs` under `namespace Territory.Economy`.
- [ ] `[System.Serializable] public class StateServiceZoneData { public int cellX; public int cellY; public int subTypeId; public int densityTier; }`.
- [ ] XML doc: fields + "Placement / restore lands in Step 2; field carried forward this task as empty list on fresh games."

### Phase 3 — `GameSaveData` fields + schema bump

- [ ] Add `using Territory.Economy;` to `GameSaveManager.cs` if missing.
- [ ] Add `public BudgetAllocationData budgetAllocation;` on `GameSaveData` (nullable — legacy v3 saves deserialize to null; TECH-423 seeds via `Default(cap)`).
- [ ] Add `public List<StateServiceZoneData> stateServiceZones = new List<StateServiceZoneData>();` on `GameSaveData` (matches `neighborStubs` / `neighborCityBindings` list-init pattern).
- [ ] Flip `CurrentSchemaVersion = 3` → `= 4` at `GameSaveManager.cs:404`.
- [ ] Append to `CurrentSchemaVersion` XML doc: "Schema 4 adds `budgetAllocation` + `stateServiceZones` (envelope budget + state-service zone registry — Stage 1.3 Phase 3)."

### Phase 4 — Verify

- [ ] `npm run unity:compile-check` green.
- [ ] No migration branch added to `MigrateLoadedSaveData` this task (TECH-423 owns v3→v4 branch).

## 7b. Test Contracts

| Acceptance | Check | Command | Notes |
|---|---|---|---|
| G1 schema = 4 | Grep | `rg 'CurrentSchemaVersion = 4' Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` | Expect one hit |
| G2 `BudgetAllocationData` compiles | Unity | `npm run unity:compile-check` | Post-impl |
| G3 `StateServiceZoneData` compiles | Unity | Same | Same run |
| G4 fields on `GameSaveData` | Grep | `rg 'public BudgetAllocationData budgetAllocation\|public List<StateServiceZoneData>' Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` | Expect 2 hits |
| G5 `Default(cap)` seed sums to 1.0 | Test coverage | Deferred TECH-425 | Round-trip |
| G6 serialize round-trip | Test coverage | Deferred TECH-424 | Wire capture/restore |

## 8. Acceptance Criteria

- [ ] `CurrentSchemaVersion = 4`.
- [ ] Both new types compile + serialize (verified by downstream migration test).
- [ ] `GameSaveData` fields accessible.
- [ ] `npm run unity:compile-check` green.

## 9. Decision Log

- **D1 — POCO file split.** Two new files (`BudgetAllocationData.cs`, `StateServiceZoneData.cs`) under `namespace Territory.Economy`, not inline inside `GameSaveManager.cs`. Rationale: matches sibling `BudgetAllocationService.cs` / `IBudgetAllocator.cs` layout; keeps `GameSaveManager.cs` focused on orchestration.
- **D2 — `Default(cap)` normalizes inline.** No shared `Normalize(float[])` helper lifted between Data POCO + Service this task. Data = pure POCO; Service owns runtime mutation. Reassess on TECH-424 if duplication grows.
- **D3 — `budgetAllocation` field nullable.** Legacy v3 saves deserialize `budgetAllocation = null`; TECH-423 seeds via `Default(cap)` inside migration branch. `stateServiceZones` defaults to empty list (no null path).

## Open Questions

1. `StateServiceZoneData.densityTier` value domain — exploration §IP-1 subtype catalogue expected to define Light=1 / Medium=2 / Heavy=3. Carry forward as raw `int` this task; Step 2 placement task resolves + documents in Decision Log. No behavior impact on Step 1 (field unused until placement lands).
