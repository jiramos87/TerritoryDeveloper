---
purpose: "TECH-217 — EconomyManager money earn/spend Blip call sites."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-217 — EconomyManager money earn/spend Blip call sites

> **Issue:** [TECH-217](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Wire `BlipEngine.Play(BlipId.EcoMoneyEarned)` + `BlipEngine.Play(BlipId.EcoMoneySpent)` at `EconomyManager.AddMoney` + success branch of `SpendMoney`. Monthly-maintenance non-interactive charge excluded. Satisfies Blip master plan Stage 3.2 Exit bullet 2 (Eco lane). Orchestrator: [`ia/projects/blip-master-plan.md`](blip-master-plan.md) Stage 3.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Player-facing earn → `EcoMoneyEarned` SFX.
2. Player-facing spend (successful `SpendMoney` w/ notification) → `EcoMoneySpent` SFX.
3. Monthly-maintenance charge path (`notifyInsufficientFunds: false`) stays silent (non-interactive budget).
4. No new fields, no singletons (invariant #4).

### 2.2 Non-Goals

1. Failed-spend denied SFX — covered by `ToolBuildingDenied` at placement site, not here.
2. Separate per-context volume attenuation — patch SO already carries `gainJitterDb`.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Earn money, hear uplift tone | `AddMoney(amount > 0)` fires `EcoMoneyEarned` |
| 2 | Player | Spend money interactively, hear spend tone | `SpendMoney(...)` success branch w/ `notifyInsufficientFunds == true` fires `EcoMoneySpent` |
| 3 | Player | Monthly maintenance charge stays silent | `ChargeMonthlyMaintenance` path does NOT fire SFX |

## 4. Current State

### 4.1 Domain behavior

`EconomyManager.AddMoney` line ~205 + `SpendMoney` success branch line ~169 currently silent. `ChargeMonthlyMaintenance` line ~101 calls `SpendMoney(total, "Monthly maintenance", notifyInsufficientFunds: false)` — must stay silent (non-interactive).

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — `AddMoney(int)` line ~191, `SpendMoney(int, string, bool)` line ~153, `ChargeMonthlyMaintenance` line ~101.
- `cityStats.AddMoney` line ~205, `cityStats.RemoveMoney` line ~169.
- `BlipId.EcoMoneyEarned` + `BlipId.EcoMoneySpent` enum values (Stage 1.2).

## 5. Proposed Design

### 5.1 Target behavior

- `AddMoney(amount)` → after `cityStats.AddMoney(amount)` → `BlipEngine.Play(BlipId.EcoMoneyEarned)`.
- `SpendMoney(..., notifyInsufficientFunds = true)` success branch → after `cityStats.RemoveMoney(amount)` → `BlipEngine.Play(BlipId.EcoMoneySpent)`.
- `SpendMoney(..., notifyInsufficientFunds = false)` (monthly maintenance) → NO Blip fire.

### 5.2 Architecture

Gate the spend Blip on the existing `notifyInsufficientFunds` parameter — it already marks "interactive call" semantics. Single `if (notifyInsufficientFunds)` wrapping `BlipEngine.Play(BlipId.EcoMoneySpent)`. AddMoney fires unconditionally (negative-amount guard already rejects + the no-op `amount == 0` early-return guard stays in place — only fire when value actually changed).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-15 | Reuse `notifyInsufficientFunds` as interactive flag | Already marks player-facing spend; no new param needed | Add `bool fireBlip = true` overload (rejected — param explosion) |

## 7. Implementation Plan

### Phase 1 — Earn + spend call sites

- [ ] `AddMoney(int amount)` — after `cityStats.AddMoney(amount)` line ~205, add `BlipEngine.Play(BlipId.EcoMoneyEarned)`. Guard behind existing `amount > 0` (negative-reject guard already precedes; zero-amount passes through silently).
- [ ] `SpendMoney(...)` success branch — after `cityStats.RemoveMoney(amount)` line ~169, add `if (notifyInsufficientFunds) BlipEngine.Play(BlipId.EcoMoneySpent);`.
- [ ] Verify `ChargeMonthlyMaintenance` line ~101 still silent (passes `notifyInsufficientFunds: false`).
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity compile | `npm run unity:compile-check` | |
| Interactive spend audible; monthly silent | Manual | Play Mode: build road (spend SFX); tick sim to month-end (silent maintenance) | |
| IA validation | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `AddMoney` fires `EcoMoneyEarned` after ledger write.
- [ ] Interactive `SpendMoney` fires `EcoMoneySpent` after ledger write.
- [ ] Monthly-maintenance `SpendMoney` stays silent (`notifyInsufficientFunds == false` guard).
- [ ] No new fields / singletons (invariant #4).
- [ ] `npm run unity:compile-check` + `npm run validate:all` green.

## Open Questions

1. None — game rule is clear from orchestrator Stage 3.2 (interactive vs non-interactive spend); `notifyInsufficientFunds` already carries that semantic.
