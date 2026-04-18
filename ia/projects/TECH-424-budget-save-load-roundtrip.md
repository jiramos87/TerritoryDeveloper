---
purpose: "TECH-424 — Save/load round-trip wiring for BudgetAllocationService."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T1.3.7"
---
# TECH-424 — Save/load round-trip wiring for `BudgetAllocationService`

> **Issue:** [TECH-424](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Stage 1.3 Phase 3. Bridge `BudgetAllocationService` runtime state ↔ `GameSaveData.budgetAllocation`. Adds `CaptureSaveData()` + `RestoreFromSaveData(BudgetAllocationData)` methods; wires them into `GameSaveManager.SaveGame` (pre-write) + `GameSaveManager.LoadGame` (post-migration). Ensures envelope pct + global cap + current month remaining survive save → load → identity.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BudgetAllocationService.CaptureSaveData() → BudgetAllocationData` clones internal state into save payload.
2. `BudgetAllocationService.RestoreFromSaveData(BudgetAllocationData)` copies payload into internal state; normalize pct post-restore.
3. `GameSaveManager.SaveGame` calls `budgetAllocation = service.CaptureSaveData()` before write.
4. `GameSaveManager.LoadGame` calls `service.RestoreFromSaveData(data.budgetAllocation)` after migration chain.
5. `unity:compile-check` green.

### 2.2 Non-Goals

1. S zones list stays empty — placement wiring lands Step 2.
2. No integration test — TECH-425.

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs` — capture / restore API.
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `SaveGame` / `LoadGame` call sites.

## 5. Proposed Design

### 5.2 Architecture

Deep-copy on capture (allocate new arrays) — save payload must not alias live state. Restore path: null-guard on `data.budgetAllocation` (defensive — should never be null post-migration); if null, seed via `BudgetAllocationData.Default(fallbackCap)` + log warning.

## 7. Implementation Plan

### Phase 1 — Service API

- [ ] `CaptureSaveData()` — deep copy.
- [ ] `RestoreFromSaveData(BudgetAllocationData)` — deep copy + normalize.

### Phase 2 — Manager wiring

- [ ] `SaveGame`: populate `data.budgetAllocation` pre-serialize.
- [ ] `LoadGame`: restore service after `MigrateLoadedSaveData`.

## 7b. Test Contracts

| Acceptance | Check | Command | Notes |
|---|---|---|---|
| compile | Unity | `npm run unity:compile-check` | Post-impl |
| round-trip | Test coverage | Deferred TECH-425 | EditMode |

## 8. Acceptance Criteria

- [ ] Post save/load → service internal state byte-identical to pre-save.
- [ ] Capture deep-copies (mutating saved `envelopePct` array does not mutate service state).
- [ ] Restore normalizes pct (defensive).
- [ ] `npm run unity:compile-check` green.

## Open Questions

1. None — scaffold only.
