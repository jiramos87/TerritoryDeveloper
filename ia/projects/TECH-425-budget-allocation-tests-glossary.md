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
> **Status:** In Review
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

Test fixture: instantiate `BudgetAllocationService` + `TreasuryFloorClampService` + `EconomyManager` stub (bypass `cityStats` where possible) via sibling `TreasuryFloorClampServiceTests` Awake-fixture pattern — omit `GameNotificationManager` (null `notificationPanel` NPE). Seed `globalMonthlyCap` + `envelopePct` via public setters (`SetEnvelopePctsBatch`) + reflection for `currentMonthRemaining` when no public setter exists. v3 fixture: hand-authored `GameSaveData` JSON (schemaVersion = 3) inline string — no on-disk fixture file (keeps tests hermetic). Assert post-`MigrateLoadedSaveData`: `schemaVersion == 4`, `budgetAllocation != null`, `Math.Abs(sum(envelopePct) - 1.0) < 1e-6`, `stateServiceZones.Count == 0`, pre-existing v3 fields preserved (sample: `money`, `currentMonth`).

### 5.3 Glossary row shape

Both rows follow sibling `TreasuryFloorClampService` template (same category `City systems`, same forward-ref + fallback pattern):
- `BudgetAllocationService` — *Helper service extracted from `EconomyManager` (invariant #6) owning the per-S-sub-type monthly envelope. API: `TryDraw(int subTypeId, int amount) → bool`, `GetMonthlyEnvelope(int) → int`, `SetEnvelopePct(int, float)`, `SetEnvelopePctsBatch(float[7])`, `MonthlyReset()`. Composes `TreasuryFloorClampService` for the treasury-floor check + mutation. Save round-trip via `CaptureSaveData` / `RestoreFromSaveData`.* Ref: `docs/zone-s-economy-exploration.md` §IP-2 (forward-ref `ia/specs/economy-system.md#budget-envelope`).
- `IBudgetAllocator` — *Interface contract for `BudgetAllocationService` (Stage 1.3 Phase 1). Allows future alternative allocators (e.g. debug/test stubs) without touching call sites. Same API surface as impl.* Same refs.

## 7. Implementation Plan

### Phase 1 — `BudgetAllocationServiceTests`

- [ ] Scaffold test class + `[SetUp]` Awake fixture (copy `TreasuryFloorClampServiceTests` shape; omit `GameNotificationManager`).
- [ ] Case (a) envelope exhausted → `TryDraw == false`, treasury unchanged.
- [ ] Case (b) treasury empty + envelope fat → `TryDraw == false`, envelope unchanged.
- [ ] Case (c) both OK → `TryDraw == true`, BOTH `currentMonthRemaining[i]` + treasury decrement by `amount`.
- [ ] Case (d) `MonthlyReset` → `currentMonthRemaining[i] == (int)(globalMonthlyCap × envelopePct[i])` for all i.
- [ ] Case (e) `SetEnvelopePct(i, v)` → `|sum(envelopePct) − 1.0| < 1e-6`.
- [ ] Case (f) `SetEnvelopePctsBatch(all-zero)` rejected → prior state preserved (compare snapshot).

### Phase 2 — `SaveMigrationV3ToV4Tests`

- [ ] Author inline v3 JSON fixture (schemaVersion = 3, no `budgetAllocation`, no `stateServiceZones`, sample `money` + `currentMonth`).
- [ ] Deserialize → `MigrateLoadedSaveData` → assert v4 post-state (see §5.2).
- [ ] Run `npm run unity:testmode-batch` → all 7 tests green.

### Phase 3 — Glossary + indexes

- [ ] Add `BudgetAllocationService` + `IBudgetAllocator` rows to `ia/specs/glossary.md` (shape in §5.3) under `City systems` category, alphabetical.
- [ ] `npm run generate:ia-indexes` → regenerate `glossary-index.json` + `glossary-graph-index.json`.
- [ ] `npm run validate:all` → green.

## 7b. Test Contracts

| Acceptance | Check | Command | Notes |
|---|---|---|---|
| `BudgetAllocationServiceTests` green | Unity | `npm run unity:testmode-batch` | 6 cases (a)–(f) |
| `SaveMigrationV3ToV4Tests` green | Unity | `npm run unity:testmode-batch` | 1 case v3→v4 round-trip |
| glossary rows present | Grep | `grep -E "BudgetAllocationService\|IBudgetAllocator" ia/specs/glossary.md` | 2 rows, `City systems` category |
| indexes regenerated | Node | `npm run generate:ia-indexes` | Diff `tools/mcp-ia-server/data/glossary-*.json` |
| full validation | Node | `npm run validate:all` | Post-impl gate |

## 8. Acceptance Criteria

- [ ] 7 tests compile + pass.
- [ ] Both glossary rows present w/ correct spec refs.
- [ ] `glossary-index.json` + `glossary-graph-index.json` regenerated.
- [ ] `npm run validate:all` green.

## Open Questions

1. N/A — tooling/tests-only issue; all behavior locked by TECH-420 / 421 / 423 / 424. Per `ia/projects/PROJECT-SPEC-STRUCTURE.md`, Open Questions cover game logic / definitions only.

## Decision Log

- **v3 fixture inline (no JSON file under `Fixtures/`)** — keeps test hermetic + diffable; no I/O dependency in EditMode. (Kickoff 2026-04-18.)
- **Fixture pattern = `TreasuryFloorClampServiceTests` Awake shape, omit `GameNotificationManager`** — null `notificationPanel` NPE on that manager in EditMode; direct reuse of archived TECH-383 lesson.
- **Glossary refs mirror sibling `TreasuryFloorClampService`** — forward-ref `economy-system.md#budget-envelope` (Stage 3.3 authors target) + fallback `docs/zone-s-economy-exploration.md §IP-2`; `economy-system.md` not yet authored — forward-ref pattern already in use for 3 Zone-S rows.
