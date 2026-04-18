---
purpose: "TECH-423 — MigrateLoadedSaveData v3→v4 branch."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T1.3.6"
---
# TECH-423 — `MigrateLoadedSaveData` v3→v4 branch

> **Issue:** [TECH-423](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Stage 1.3 Phase 3. Add migration branch for legacy v3 → v4 saves. Seeds equal 14.28% × 7 envelope (normalized sum 1.0) + empty S zone list. Pre-existing save data preserved byte-identical outside new fields.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `if (data.schemaVersion < 4)` branch in `MigrateLoadedSaveData`:
   - `data.stateServiceZones = new List<StateServiceZoneData>();`
   - `data.budgetAllocation = BudgetAllocationData.Default(cap);`
   - `data.schemaVersion = 4;`
2. Cap value chosen — default constant (e.g. `10_000`) for MVP determinism. Documented in Decision Log w/ rationale.
3. Existing v3 data (cells, heightmap, water, buildings, money) preserved byte-identical.
4. `unity:compile-check` green.

### 2.2 Non-Goals

1. No service state restore — TECH-424.
2. No UI reflection — Step 3.

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `MigrateLoadedSaveData` body.
- `ia/specs/persistence-system.md` §Save / Load pipeline — migration chain pattern.

## 5. Proposed Design

### 5.2 Architecture

Branch placed after existing v2→v3 migration (find via Grep for `schemaVersion < 3`). Single `if` block — idempotent (flips version to 4 at end so it never runs twice). Cap derivation: constant `const int DEFAULT_S_CAP = 10_000;` in `GameSaveManager` or `BudgetAllocationService` (decide at impl; prefer `GameSaveManager` since it's save-lifecycle).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|---|---|---|---|
| 2026-04-18 | Default cap = 10_000 (draft) | Round + sizable relative to typical early-game treasury (~5k). Deterministic across legacy saves. | Derive from current treasury × factor — rejected (non-deterministic per-save); tie to city population — rejected (N/A in v3 schema reliably). |

## 7. Implementation Plan

### Phase 1 — Branch

- [ ] Locate existing migration chain tail in `MigrateLoadedSaveData`.
- [ ] Add v4 branch — list init + `BudgetAllocationData.Default(cap)` + version flip.
- [ ] Pick constant + document rationale in Decision Log.
- [ ] `unity:compile-check` green.

## 7b. Test Contracts

| Acceptance | Check | Command | Notes |
|---|---|---|---|
| compile | Unity | `npm run unity:compile-check` | Post-impl |
| v3 round-trip | Test coverage | Deferred TECH-425 | Migration fixture |

## 8. Acceptance Criteria

- [ ] Branch executes only on `schemaVersion < 4` input.
- [ ] Post-migration `data.schemaVersion == 4`, `budgetAllocation != null`, `stateServiceZones != null`.
- [ ] Envelope pct array sums to 1.0 w/in 1e-6.
- [ ] Pre-existing fields untouched.
- [ ] `npm run unity:compile-check` green.

## Open Questions

1. Default cap — locked at 10_000 MVP per Decision Log; player adjusts via Stage 3.1 budget panel once shipped. Confirm constant value at impl if downstream design says otherwise.
