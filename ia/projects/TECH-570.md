---
purpose: "TECH-570 — Integration test Example 3 end-to-end."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.6"
---
# TECH-570 — Integration test — Example 3 end-to-end

> **Issue:** [TECH-570](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Automate exploration **Example 3** bond flow: issue, treasury credit, **monthlyRepayment** math, month tick, save/load on **bondRegistry**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. EditMode (or PlayMode) harness with economy + ledger test doubles or scene fixture.
2. Assert issue + repayment math + registry state; assert save/load preserves **BondData** fields.
3. Cover `ProcessMonthlyRepayment` + `monthsRemaining` decrement.

### 2.2 Non-Goals (Out of Scope)

1. Full UI automation for modal (covered manually or separate test).
2. Load testing save files.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Regression guard for bond ledger | Automated test green |
| 2 | QA | Example 3 numbers reproducible | Assertions match doc |

## 4. Current State

### 4.1 Domain behavior

**BondLedgerService** + **GameSaveData.bondRegistry** implemented in Stage 4; Example 3 narrative in exploration doc.

### 4.2 Systems map

- `Assets/Tests/EditMode/Economy/`, `BondLedgerService`, `GameSaveManager` / save data path

### 4.3 Implementation investigation notes (optional)

Prefer EditMode with scripted `EconomyManager` + ledger if scene-free; else PlayMode with test scene asset.

## 5. Proposed Design

### 5.1 Target behavior (product)

Given treasury 1200, `TryIssueBond(city, 5000, 24)` → true, treasury 6200, `monthlyRepayment` 233; after repayment tick, treasury 5967, `monthsRemaining` 23.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

`BondIssuanceIntegrationTests` class; setup money + ledger; drive `ProcessMonthlyRepayment`; optional save round-trip using existing save test helpers if present.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Numbers from orchestrator Intent column | Single source | Re-read exploration (if drift, fix Intent) |

## 7. Implementation Plan

### Phase 1 — Harness + issue assertions

- [ ] Create test assembly + fixture; seed treasury 1200.
- [ ] Call `TryIssueBond`; assert balance + registry + monthlyRepayment.

### Phase 2 — Tick + save

- [ ] Simulate month tick / call `ProcessMonthlyRepayment`; assert treasury + monthsRemaining.
- [ ] Save/load round-trip asserts bond fields.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Example 3 regression | Unity EditMode | `dotnet test` / Unity Test Runner | Primary deliverable |
| Repo validators | Node | `npm run validate:all` | If touching IA |

## 8. Acceptance Criteria

- [ ] Test asserts issue + treasury credit + `monthlyRepayment` math per Example 3
- [ ] Month tick repayment updates treasury and `monthsRemaining`
- [ ] Save/load preserves bond registry fields

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Flaky timing on month tick — prefer direct **ProcessMonthlyRepayment** call after deterministic setup vs waiting full sim day. Mitigation: mirror **BondLedgerServiceTests** patterns from Stage 4.
- Save/load path must use same **GameSaveManager** migration as production **bondRegistry** field.
- Numbers locked to Example 3 in orchestrator Intent — if ledger rate differs, fix test expectation to match **BondLedgerService** constant.

### §Examples

| Step | Treasury | Registry |
|------|----------|----------|
| Start | 1200 | empty |
| After **TryIssueBond** | 6200 | entry, **monthlyRepayment** 233 |
| After one repayment | 5967 | **monthsRemaining** 23 |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| example3_issue | 1200 cash, issue 5000@24 | balance + registry per table | EditMode |
| example3_repay | After issue | treasury −233 | EditMode |
| save_roundtrip | Save + load | **BondData** identity | EditMode |

### §Acceptance

- [ ] Issue + repayment + **monthlyRepayment** match Intent; save/load preserves bond

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. City tier int constant for test — match production default city scale = 0 or 1; align with ledger API.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
