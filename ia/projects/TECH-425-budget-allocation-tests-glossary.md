---
purpose: "TECH-425 — EditMode tests + glossary rows for BudgetAllocationService (Stage 1.3 close)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T1.3.8"
---
# TECH-425 — EditMode tests + glossary rows for `BudgetAllocationService`

> **Issue:** [TECH-425](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Stage 1.3 Phase 3 closer. Lands 2 EditMode test classes (service behavior + v3→v4 save migration), 2 new glossary rows (`BudgetAllocationService`, `IBudgetAllocator`), MCP index regen. Closes Stage 1.3 → Step 1 fully landed (structural primitives ready for Step 2 consumers: `BondLedgerService`, `IMaintenanceContributor`, `ZoneSService`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BudgetAllocationServiceTests` cases (a)–(f):
   - a: `TryDraw` blocks when envelope exhausted (return false, no mutation).
   - b: `TryDraw` blocks when treasury empty even w/ fat envelope (no mutation).
   - c: `TryDraw` succeeds when both OK — BOTH envelope + treasury decrement.
   - d: `MonthlyReset` restores `currentMonthRemaining[i] == (int)(cap × pct[i])`.
   - e: `SetEnvelopePct` normalizes sum to 1.0 w/in 1e-6.
   - f: All-zero input rejected, state preserved.
2. `SaveMigrationV3ToV4Tests`: legacy v3 fixture loads into v4 w/ equal envelope + empty S list + existing fields preserved.
3. Glossary rows added (`ia/specs/glossary.md`):
   - `BudgetAllocationService` — forward-ref `ia/specs/economy-system.md#budget-envelope` (Stage 3.3 authors target) + fallback `docs/zone-s-economy-exploration.md §IP-2`.
   - `IBudgetAllocator` — same ref targets.
4. `npm run generate:ia-indexes` regenerates `glossary-index.json` + `glossary-graph-index.json`.
5. `npm run validate:all` green.

### 2.2 Non-Goals

1. No PlayMode integration test (Step 2 territory — placement pipeline).
2. No `economy-system.md` spec authoring — Stage 3.3.

## 4. Current State

### 4.2 Systems map

- `Assets/Tests/EditMode/Economy/BudgetAllocationServiceTests.cs` *(new)*.
- `Assets/Tests/EditMode/Economy/SaveMigrationV3ToV4Tests.cs` *(new)*.
- `ia/specs/glossary.md` — 2 new rows.
- `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json`.
- Pattern reference: `Assets/Tests/EditMode/Economy/TreasuryFloorClampServiceTests.cs` (TECH-383 archived) — Awake fixture approach (omit `GameNotificationManager` — NPE on null `notificationPanel`).

## 5. Proposed Design

### 5.2 Architecture

Test fixture: instantiate `BudgetAllocationService` + `TreasuryFloorClampService` + `EconomyManager` stub (bypass `cityStats` where possible). Seed `globalMonthlyCap` + `envelopePct` via public setters or reflection for determinism. v3 fixture: hand-authored `GameSaveData` JSON (schemaVersion = 3) under `Assets/Tests/EditMode/Economy/Fixtures/v3-save.json` (or inline string). Assert post-`MigrateLoadedSaveData`: `schemaVersion == 4`, `budgetAllocation != null`, `Math.Abs(sum(envelopePct) - 1.0) < 1e-6`, `stateServiceZones.Count == 0`.

## 7. Implementation Plan

### Phase 1 — Tests

- [ ] Author `BudgetAllocationServiceTests` 6 cases.
- [ ] Author `SaveMigrationV3ToV4Tests` round-trip case.
- [ ] Run `npm run unity:testmode-batch` → green.

### Phase 2 — Glossary + indexes

- [ ] Add 2 glossary rows (`BudgetAllocationService`, `IBudgetAllocator`).
- [ ] `npm run generate:ia-indexes`.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance | Check | Command | Notes |
|---|---|---|---|
| tests pass | Unity | `npm run unity:testmode-batch` | 7 tests total |
| glossary + indexes | Node | `npm run generate:ia-indexes && npm run validate:all` | Post-impl |

## 8. Acceptance Criteria

- [ ] 7 tests compile + pass.
- [ ] Both glossary rows present w/ correct spec refs.
- [ ] `glossary-index.json` + `glossary-graph-index.json` regenerated.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — behavior locked by TECH-420 / 421 / 423 / 424.
