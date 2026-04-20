---
purpose: "TECH-565 — Bond issuance modal UI."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.1"
---
# TECH-565 — Bond issuance modal UI

> **Issue:** [TECH-565](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Player-facing bond issuance flow: enter principal + term, preview repayment, issue via **IBondLedger**; block duplicate tier bond per locked ledger rules.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Modal prefab + `BondIssuanceModal.cs` wired to `UIManager.PopupStack`.
2. Preview text matches `(principal × (1 + fixedRate)) / termMonths` with ledger default rate.
3. Issue button invokes `TryIssueBond(cityTier, principal, termMonths)`; guard when `GetActiveBond(cityTier) != null`.

### 2.2 Non-Goals (Out of Scope)

1. Bond rating, secondary market, multiple concurrent bonds per tier (locked out-of-scope).
2. Per-sub-type bond products (single ledger contract MVP).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | As mayor, I want to issue a bond with visible repayment so I can fund the city | Modal issues bond and closes on success |
| 2 | Developer | As implementer, I want stack-based modal wiring consistent with Stage 7 pickers | Uses `UIManager.PopupStack` patterns |

## 4. Current State

### 4.1 Domain behavior

**BondLedgerService** implements issuance + repayment; HUD lacks dedicated issuance UI. Stage 7 landed budget panel + Zone S placement.

### 4.2 Systems map

- `UIManager.PopupStack`, `BondLedgerService` / `IBondLedger`, `EconomyManager` (city tier read)
- `docs/zone-s-economy-exploration.md` Example 3

### 4.3 Implementation investigation notes (optional)

Confirm city **scale tier** source on `EconomyManager` or `CityStats` for `TryIssueBond` first argument.

## 5. Proposed Design

### 5.1 Target behavior (product)

Modal collects principal (min 100), term (12/24/48), shows live monthly repayment preview, issues on button press when no active bond on tier.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

New `BondIssuanceModal` MonoBehaviour; serialized refs to ledger + economy; push via `UIManager` popup stack; disable issue when `GetActiveBond` returns non-null.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Stack modal vs full-screen | Match SubTypePicker / budget panel | Inline panel (rejected — less reuse for read-only mode in TECH-566) |

## 7. Implementation Plan

### Phase 1 — Layout + stack integration

- [ ] Create `BondIssuanceModal.cs` + prefab; register open path from HUD or budget entry (stub hook if entry lands in sibling task).
- [ ] Wire `InputField`, term toggles, preview label, issue button.

### Phase 2 — Ledger + validation

- [ ] Bind preview to `fixedInterestRate` from ledger; call `TryIssueBond` on issue; handle failure notifications.
- [ ] Disable issue when `GetActiveBond(cityTier) != null`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Modal compiles + opens | Unity compile | `npm run unity:compile-check` | C# + prefab |
| Issue path calls ledger | Agent / manual | Play Mode or EditMode harness | Full bond flow in TECH-570 |

## 8. Acceptance Criteria

- [ ] Modal prefab + `BondIssuanceModal.cs` on `UIManager.PopupStack`
- [ ] Preview matches ledger repayment formula; issue calls `TryIssueBond`; disabled when tier bond active

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Issuance must go through **BondLedgerService.TryIssueBond** only — no direct **EconomyManager** money mutation from UI. Mitigation: modal holds **IBondLedger** ref; no `AddMoney` from UI.
- Risk: **Invariant** no new singletons — modal is **MonoBehaviour** + **UIManager** stack. Mitigation: match **SubTypePickerModal** / **BudgetPanel** wiring.
- Ambiguity: **city** / **scaleTier** argument for **TryIssueBond** — resolve exact field name during impl (see Open Questions).

### §Examples

| Case | Principal | Term (mo) | Preview (12% rate) | Issue enabled |
|------|-------------|-----------|----------------------|---------------|
| Happy | 5000 | 24 | `(5000 * 1.12) / 24` → 233 | yes if no active bond |
| Blocked | 5000 | 24 | same | no if **GetActiveBond**(tier) non-null |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| modal_stack_push | Open from HUD | **PopupStack** depth +1 modal visible | manual / bridge screenshot |
| issue_calls_ledger | Click issue | **TryIssueBond** invoked once with tier, principal, term | EditMode with test double |

### §Acceptance

- [ ] **BondIssuanceModal** + prefab registered; **PopupStack** push/pop clean
- [ ] Preview matches ledger **fixedInterestRate**; issue uses **TryIssueBond**; disabled when active bond on tier

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. Exact **city tier** int used by `TryIssueBond` — confirm single city scale field name on `EconomyManager` / `CityStats`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
