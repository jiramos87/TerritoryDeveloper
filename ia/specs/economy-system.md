---
purpose: "Reference spec for Economy System — Zone S channel, budget envelope, treasury floor, bonds, maintenance registry, save fields."
audience: agent
loaded_by: router
slices_via: spec_section
---
# Economy System — Reference Spec

> **Zone S** (4th **zone** channel) + finance helpers: per-sub-type **envelope (budget sense)**, **TreasuryFloorClampService**, **BondLedgerService**, extended **monthly maintenance** via **IMaintenanceContributor**, and **GameSaveData** fields migrated at schema v3→v4.
> Grid math, **sorting order** for visuals, and **road**/**water** rules stay in [`isometric-geography-system.md`](isometric-geography-system.md) — cite only when placement touches **Cell** / **GridManager** access patterns (invariant #5: use `GetCell`, not raw `gridArray`).

## Overview

- **Zone S** = state-owned zonable channel parallel to R/C/I: seven **Zone S** sub-types × three **zone density** tiers, manual placement in MVP (no AUTO zoning for S).
- **Sub-type** id (0…6) lives on **`Zone`** sidecar data, not as seven separate `ZoneType` enum values for sub-types — registry-driven content (**ZoneSubTypeRegistry**).
- **Spend order:** **envelope** `TryDraw` checks sub-type monthly remainder **and** **TreasuryFloorClampService** before any treasury debit — hard non-negative floor (no government overdraft).
- **Bonds:** at most one active bond per scale tier; issuance injects cash proactively; repayment uses `TrySpend`; failed repayment sets **arrears** (HUD signal only in MVP).
- **Maintenance:** **`EconomyManager`** aggregates **`IMaintenanceContributor`** instances in deterministic **`GetContributorId()`** order instead of only hard-coded road/plant counts.
- **Persistence:** schema **v4** adds **`stateServiceZones`**, **`budgetAllocation`**, **`bondRegistry`**; migration null-coalesces missing collections for older saves. Authoritative load/restore order remains [`persistence-system.md`](persistence-system.md).

## Zone S

- **Channel:** fourth **`ZoneType`** family alongside R/C/I — light/medium/heavy **zoning** + matching **building** enum values for state service (implementation names follow `StateService*` pattern in code).
- **Manual placement:** player picks **Zone S** tool, then **sub-type** (picker UI), then cell — **`ZoneSService`** (or equivalent orchestration) runs **`BudgetAllocationService.TryDraw(subTypeId, baseCost)`** before **`ZoneManager`** placement. Failure → **game notification**, no tile mutation.
- **AUTO:** **`AutoZoningManager`** does not place S in MVP — guard at enum switch documented in code.
- **Deferred (out of MVP bucket):** RCI **service coverage**, **happiness** contribution from S buildings, cross-scale treasury transfers, bond market depth — see exploration doc for bucket boundaries; this spec does not define those behaviors.

### Zone sub-type registry

- **`ZoneSubTypeRegistry`:** catalog of seven entries (police, fire, education, health, parks, public housing, public offices). Fields include id, display name, prefab reference, **baseCost**, **monthlyUpkeep**, icon — aligns with glossary **ZoneSubTypeRegistry**.
- **JSON / ScriptableObject:** implementation may load from Resources JSON or SO asset; costs are data-driven so agents/humans can edit without C# churn.

### ZoneSService placement

- Entry path validates **baseCost** from registry, calls **`TryDraw`**, then routes through standard **zone** placement pipeline with correct **`ZoneType`** + **subTypeId** sidecar write.
- **Grid access:** only via **`GridManager.GetCell`** (invariant #5).
- On building spawn, registrants may implement **`IMaintenanceContributor`** so **monthly maintenance** picks up **Zone S** upkeep.

## Budget envelope

- **Term:** glossary **envelope (budget sense)** — per-sub-type monthly spending pool under a global **Zone S** monthly cap.
- **`IBudgetAllocator` / `BudgetAllocationService`:** seven percentage sliders (sum normalized to 100%); **`GetMonthlyEnvelope`**, **`SetEnvelopePct`**, **`SetEnvelopePctsBatch`**; **`MonthlyReset()`** refills per-sub-type remainder from cap × pct.
- **`TryDraw(int subTypeId, int amount)`:** returns false when **`currentMonthRemaining[subTypeId] < amount`** **or** treasury cannot afford via **`TreasuryFloorClampService`** — blocks spend even if another envelope still has headroom (envelope is the gate for S capital spend).
- **Save:** **`BudgetAllocationData`** round-trips in schema v4 (`CaptureSaveData` / `RestoreFromSaveData` on the owning manager path).

## Treasury floor clamp

- **`TreasuryFloorClampService`:** single authorised **`TrySpend` / `CanAfford`** path for treasury debits post-audit (invariant #6 carve-out). On failure: **game notification**, balance unchanged, no negative **`CityStats`** money.
- **Composition:** **`BudgetAllocationService`** and **bond repayment** call into this service rather than raw **`SpendMoney`**.

## Bond ledger

- **`IBondLedger` / `BondLedgerService`:** **`TryIssueBond(scaleTier, principal, termMonths)`** fails when tier already has active bond. Success credits treasury via **`EconomyManager`** add path, records principal, term, computed **monthlyRepayment**, issue date.
- **`ProcessMonthlyRepayment` / `ProcessAllMonthlyRepayments`:** month-boundary hooks debit via **`TreasuryFloorClampService.TrySpend`**; insufficient funds → **arrears** flag on bond record (HUD), no extra simulation penalty in MVP.
- **Save:** **`bondRegistry`** list (or tier-keyed structure) in **`GameSaveData`** schema v4; migration seeds empty list when absent.

## Maintenance contributor registry

- **`IMaintenanceContributor`:** **`GetMonthlyMaintenance()`**, **`GetContributorId()`** (stable sort key), **`GetSubTypeId()`** (−1 = general pool, 0…6 = **Zone S** sub-type for envelope attribution where applicable).
- **`EconomyManager.ProcessMonthlyMaintenance`:** iterates registered contributors in **`GetContributorId()`** ordinal order — deterministic across sessions.
- **Built-in adapters:** e.g. road aggregate, power-plant aggregate — preserve legacy formulas while unifying the registry pattern.

## Save schema and migration (v3 → v4)

- **`GameSaveData.CurrentSchemaVersion`:** v4 in code; **`MigrateLoadedSaveData`** (see **`GameSaveManager`**) is idempotent post-deserialize.
- **v3 → v4:** if **`stateServiceZones`**, **`budgetAllocation`**, or **`bondRegistry`** null → seed empty list / **`BudgetAllocationData.Default(cap)`** / empty bond list; then set **`schemaVersion`** to current.
- **Narrative detail** for field shapes and ordering relative to **neighbor** lists lives in **`GameSaveManager`** XML docs and [`persistence-system.md`](persistence-system.md) — do not duplicate full **`GameSaveData`** member list here unless behavior is normative for designers.

## Glossary alignment

- Canonical table rows: **Zone S**, **ZoneSubTypeRegistry**, **envelope (budget sense)**, **TreasuryFloorClampService**, **BudgetAllocationService**, **IBudgetAllocator**, **BondLedgerService**, **IBondLedger**, **IMaintenanceContributor**, **ZoneSService** — definitions in [`glossary.md`](glossary.md); **spec wins** on conflict per glossary header.

## Related specs

| Topic | Spec |
|-------|------|
| Save / load pipeline | [`persistence-system.md`](persistence-system.md) |
| **Zone** lifecycle, managers | [`managers-reference.md`](managers-reference.md) |
| Grid / **sorting** (placement visuals only) | [`isometric-geography-system.md`](isometric-geography-system.md) §7 |
| UI patterns | [`ui-design-system.md`](ui-design-system.md) |

## Key implementation files (non-normative)

| File | Role |
|------|------|
| `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs` | Envelope + `TryDraw` |
| `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs` | Floor-clamped spend |
| `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs` | Bond issue + repayment |
| `Assets/Scripts/Managers/GameManagers/ZoneSService.cs` | S placement orchestration |
| `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` | Migration v3→v4 |
