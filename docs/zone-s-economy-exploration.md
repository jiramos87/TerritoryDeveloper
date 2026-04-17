# Zone S + Economy — Exploration (stub)

> Pre-plan exploration stub for Bucket 3 of the polished-ambitious MVP (per `docs/full-game-mvp-exploration.md` + `ia/projects/full-game-mvp-master-plan.md`). Seeds a future `/design-explore` pass that expands Approaches + Architecture + Subsystem impact + Implementation points. **Scope is Zone S (state-owned fourth zone) + finance depth (per-service budgets, deficit spending, bonds, monthly maintenance extension). NOT city-sim signals (Bucket 2), NOT utility pools (Bucket 4), NOT CityStats read model (Bucket 8). Those land in sibling buckets.**

---

## Problem

Territory Developer has R / C / I zones + a flat city treasury + monthly maintenance. That pairs with happiness + tax + demand but leaves a structural gap:

- No **state-owned** zone type. Services (police, fire, education, healthcare, parks) have nowhere to live as zonable tiles — they currently exist only as conceptual placeholders.
- Treasury is one scalar. No per-service budget allocation, no deficit behaviour, no bond instrument. Player cannot tune spending priorities the way a genre tester expects.
- `ZoneManager` is hardcoded 3-channel (R / C / I). Adding a 4th channel is a breaking migration across zoning, demand, save schema, overlays, and CityStats producers.
- Monthly maintenance doesn't cover the new surface — Zone S spawns, utility contributors (Bucket 4), multi-tier roads (Bucket 2) — budget will drift once those land without an expanded maintenance contract.
- Landmark + big-project commissioning (Bucket 4) needs a committed-budget ledger that doesn't exist yet.

**Design goal (high-level):** Zone S ships as a fourth parallel zone type fully integrated with a deepened finance loop. S spawn costs money to the owning scale's budget; S buildings provide services to R / C / I via coverage + happiness + desirability contributions; per-service budget controls + deficit spending + bonds form the primary economy-depth surface for MVP.

## Approaches surveyed

_(To be expanded by `/design-explore` — seed list only.)_

- **Approach A — Zone S as 4th enum + flat treasury extension.** Add `S` to `ZoneType`, treat S tiles like R/C/I but with inverted cashflow (cost, not tax). Extend one treasury scalar. Minimal churn. Risk: per-service budget + deficit + bonds bolted on later with no coherent contract.
- **Approach B — Zone S as 4th channel + full budget surface first.** Define `BudgetAllocationService` + `BondService` contracts up front. S spawn draws from per-service budget envelope. Treasury splits into per-service accounts + general fund + bond ledger. Higher upfront design cost; clean extensibility across Buckets 4 + 2.
- **Approach C — Zone S as service deployment, not zone channel.** Keep RCI at 3. Services are placed buildings (like roads) with a dedicated `ServicePlacementTool`. Skips the 4-channel migration; breaks the "parallel zone type" UX promise in the exploration.
- **Approach D — Budget-first, Zone S second.** Land per-service budget + deficit + bonds against existing city treasury + maintenance first. Add Zone S as a consumer of the budget contract once it's stable. Risk: service buildings ship with no home (stuck as placeholders).
- **Approach E — Hybrid A + B.** S as 4th zone channel (A's structural answer) + budget-allocation service contract (B's finance answer) designed together. Save-schema bump covers both. Matches bucket's "merge Zone S + economy depth" framing.

## Recommendation

_TBD — `/design-explore` Phase 2 gate decides._ Author's prior lean: **Approach E** (hybrid 4th-channel + budget service contract). Matches the bucket framing (merge Zone S + economy depth), avoids A's bolt-on risk, avoids C's UX break, avoids D's orphaned-placeholder risk. One save-schema bump covers both surfaces.

## Open questions

- **Zone S sub-types + building archetypes.** S spawns what? Public housing (vivienda pública), public offices (sector público), police / fire / education / health stations, parks. Shared pool of S tiles or sub-typed at placement? Interaction with Bucket 2 service coverage computer.
- **Per-service budget semantics.** Envelope model (fixed monthly allocation, unspent rolls over or evaporates?) or accrual (service draws continuously, deficit triggers bond)? Player UI surface (slider per service, or absolute amounts)? Interaction with CityStats Bucket 8 budget panel.
- **Deficit spending rules.** Hard cap (at X% of monthly revenue → buildings decay / services degrade)? Soft cap (happiness penalty)? Does deficit unlock bond issuance automatically or is it player-triggered?
- **Bond instrument shape.** Principal + interest + term + monthly repayment. Fixed interest rate for MVP (advanced bond market deferred)? Max concurrent bonds? Rating-based interest unlock path (deferred)?
- **Cross-scale budget flow.** Country budget → region → city transfer mechanics (per-scale per-service allocation, or only at city scale for MVP with region + country treating S as aggregate cost)? Coordinate with Bucket 1 scale-switch rollup.
- **Monthly maintenance contract extension.** Unified maintenance function across RCI + S + utilities (Bucket 4) + multi-tier roads (Bucket 2) — single formula or per-surface registered contributors? Authority: glossary row **Monthly maintenance** or new `economy-system.md` spec?
- **Zone S tax rules.** S buildings pay zero tax (they ARE government). Do they still generate any revenue (fees, permits)? Or pure cost center?
- **Save schema migration.** `schemaVersion` bump for 4-channel RCI → RCIS + per-service budget ledger + bond registry. Legacy save defaults (all S missing, budget migrated from flat treasury, no bonds).
- **Demand model extension.** Does S consume R demand (public housing = R stock)? Does S generate employment (sector público = jobs)? Interaction with `DemandManager` + `EmploymentManager`.
- **Zone S UI surface.** Zoning tool adds 4th button; per-service budget panel; bond issuance dialog; monthly budget report. Which surfaces ship MVP? Coordinate with Bucket 6.
- **Invariant compliance.** `ZoneManager` touches carve-out pattern — S placement through zoning pipeline, not ad-hoc. No new singletons. `EconomyManager` extensions stay MonoBehaviour + Inspector-wired.
- **Bucket 2 dependency shape.** Signal contract defines `ServicePolice` / `ServiceFire` / etc. — S buildings are the producers. Confirm S spawn emits signal producer registration automatically; coverage radius authored where (S-specific ScriptableObject vs signal spec).
- **Consumer-count inventory.** Which surfaces read per-service budget / bond state (HUD, info panels, CityStats, web dashboard, save)? Decide at exploration time to inform Bucket 8 parity contract.

---

## Design Expansion

### Interview summary (Phase 0.5)

Five-question gate closed all blocking open questions before compare + select:

1. **Q1 — S sub-type surface.** Single shared S pool, 7 RCI-symmetric sub-types (police, fire, education, health, parks, public housing, public offices). Sub-type chosen at click via a picker, not at zoning-tool mode-switch. Keeps zoning-tool UX parallel to RCI (one S button + picker) and lets the 7-way split live in content + budget envelope, not enum explosion.
2. **Q2 — service effect timing.** MVP defers RCI service-coverage + happiness wiring to the scales / city-sim bucket. S buildings spawn with visuals + cost + budget draw only. No coverage radius computation, no happiness contribution, no desirability contribution in Bucket 3.
3. **Q3 — budget semantics.** Envelope model: 7 percentage sliders across S sub-types, must sum to 100%. Allocated pool = player-tunable monthly cap derived from envelope × global S allocation. Underfunded sub-type = visual decay placeholder, no mechanical penalty in Bucket 3.
4. **Q4 — deficit rules.** A1 hard cap. Balance (per scale tier) NEVER goes negative. All spend operations check-and-block BEFORE deducting. Player governments cannot go broke; only people and companies go broke in-sim.
5. **Q5 — bond shape.** B1: fixed term + fixed principal, max 1 concurrent bond per scale tier. Bond is a PROACTIVE cash injection lever, not a remedial overdraft mechanism. Fixed interest rate for MVP; rating / bond-market depth deferred.

### Compare (Phase 1)

| Criterion | A (4th enum + flat) | B (4th channel + full budget) | C (service deployment) | D (budget-first) | E (A+B hybrid) |
|---|---|---|---|---|---|
| Constraint fit (bucket framing = Zone S + economy depth merged) | partial — S present, economy thin | full | breaks parallel-zone UX promise | inverted — S orphaned | full |
| Effort (MVP window) | low | high | medium | medium | medium-high |
| Output control (clean contracts for Buckets 2/4/8) | weak — bolt-on later | strong | medium — services not zones | strong but S late | strong — both contracts author together |
| Maintainability (save + overlay + demand extensibility) | save bump twice | one bump | no bump, but service-ad-hoc | two bumps | one bump |
| Dependencies / risk | decoupled but debt-accruing | upfront design cost | Bucket 2 coverage redesign | orphaned placeholders | requires tight Phase-3 architecture |

### Select (Phase 2) — Approach E confirmed

Author lean (doc §Recommendation) matched Q1–Q5 answers. Q1 RCI-symmetric sub-type pool validates 4th-channel shape (A's answer). Q3 envelope + Q4 hard cap + Q5 bond shape lock the service-contract surface (B's answer). Single save-schema bump covers both. No override received; proceed to Phase 3.

### Chosen Approach (Phase 3) — Approach E: hybrid 4th-channel + budget service contract

Scope (MVP): Zone S as a 4th parallel zone channel with 7 sub-types under one shared enum extension, one envelope-based per-sub-type budget allocator, one floor-clamped treasury (per scale tier), one single-bond-per-tier ledger, one extended monthly-maintenance contract. Everything else (coverage, happiness contribution, multi-scale rollup mechanics, bond rating, bond-market depth) explicitly deferred.

Out-of-scope (Bucket 3): RCI service coverage computer, desirability signal, cross-scale treasury transfer mechanics, bond secondary market, bond rating, interest-rate tiering, S tax revenue (pure cost center).

### Architecture (Phase 4)

Four new concrete components + three contracts + one enum extension. All MonoBehaviour-hosted where invariant #4 applies. All helper services follow the `*Service.cs` carve-out (invariants #5, #6).

```
ZoneManager (existing)
  └─ ZoneType enum (existing, extend)
        └─ StateServiceLightBuilding / StateServiceMediumBuilding / StateServiceHeavyBuilding
        └─ StateServiceLightZoning / StateServiceMediumZoning / StateServiceHeavyZoning
        └─ (sub-type carried in ZoneMeta sidecar, NOT enum)
  └─ ZoneSubTypeRegistry (new, ScriptableObject catalogue)
        └─ 7 entries: police, fire, education, health, parks, public housing, public offices
        └─ fields: id, displayName, prefab, baseCost, monthlyUpkeep, icon

EconomyManager (existing)
  └─ BudgetAllocationService (new, helper under Managers/GameManagers/BudgetAllocationService.cs)
        └─ 7 envelope pct (float 0..1, sum == 1)
        └─ global S monthly cap (int)
        └─ per-sub-type monthly envelope = cap × pct
        └─ TryDraw(subType, amount) → bool (floor-clamp at 0, never negative)
  └─ BondLedgerService (new, helper under Managers/GameManagers/BondLedgerService.cs)
        └─ single active bond record per scale tier
        └─ fields: principal, termMonths, monthlyRepayment, fixedInterestRate, issuedOnDate
        └─ TryIssueBond(principal, term) → bool (fails if active bond exists)
        └─ ProcessMonthlyRepayment() (called from EconomyManager monthly tick)
  └─ TreasuryFloorClampService (new, helper under Managers/GameManagers/TreasuryFloorClampService.cs)
        └─ wraps SpendMoney to enforce balance >= 0 BEFORE deduction
        └─ re-implements CanAfford semantics as a hard precondition, not soft check

ZoneSService (new, helper under Managers/GameManagers/ZoneSService.cs)
  └─ PlaceStateServiceZone(cell, subTypeId)
        └─ calls BudgetAllocationService.TryDraw(subType, baseCost) first
        └─ if false → emit game notification "insufficient S envelope", no placement
        └─ if true → route through standard ZoneManager placement pipeline
  └─ RegisterMonthlyUpkeep(building) → EconomyManager maintenance registry

GameSaveData (existing, bump schemaVersion 1 → 2)
  └─ stateServiceZones: List<StateServiceZoneData> (cellRef, subTypeId, densityTier)
  └─ budgetAllocation: BudgetAllocationData (envelope pcts[7], globalCap, perScale tier)
  └─ bondRegistry: List<BondData> (one per scale tier)
  └─ MigrateLoadedSaveData extension: v1→v2 adds empty S list + default envelope (equal 14.28% × 7) + empty bond registry
```

**Contracts** (three interface surfaces, all author-time locked):

1. `IBudgetAllocator` — `TryDraw(subType, amount) → bool`, `GetEnvelope(subType) → int`, `SetEnvelopePct(subType, pct)`, `MonthlyReset()`. Consumer: `ZoneSService` (writes), `EconomyManager` (monthly reset), HUD (read).
2. `IBondLedger` — `TryIssueBond(scaleTier, principal, termMonths) → bool`, `GetActiveBond(scaleTier) → BondData?`, `ProcessMonthlyRepayment(scaleTier)`. Consumer: `EconomyManager` (monthly), UI bond-issue dialog (write), HUD (read).
3. `IMaintenanceContributor` — `GetMonthlyMaintenance() → int`, `GetContributorId() → string`. Implementer: each S building, existing road contributor, utility contributor (Bucket 4 consumer). `EconomyManager.ProcessMonthlyMaintenance()` iterates registered contributors instead of hardcoded `roadCount × rate + powerPlantCount × rate`.

### Subsystem Impact (Phase 5)

Eight subsystems touched. Invariant tags in brackets refer to numbered invariants from `ia/rules/invariants.md`.

| Subsystem | Touch | Invariants flagged | Notes |
|---|---|---|---|
| `ZoneManager` + `Zone.ZoneType` enum | extend (6 new enum values) | #6 (no new `GridManager` responsibilities — touches `ZoneManager` only), #12 (project spec under `ia/projects/`) | Extend enum in place; sub-type carried in ZoneMeta sidecar so the 7-way split stays content-driven. |
| `EconomyManager` | extend + refactor | #3, #4 | New `BudgetAllocationService` + `BondLedgerService` + `TreasuryFloorClampService` fields wired via `[SerializeField] private` + `FindObjectOfType` fallback in `Awake` (guardrail #1). Maintenance loop refactored to iterate `IMaintenanceContributor` registry instead of hardcoded road + power counts. |
| `GameSaveData` + `GameSaveManager` | schema bump v1 → v2 | — | Migration adds empty S list + default-equal envelope + empty bond registry. Legacy v1 saves load into v2 with zero S buildings, 14.28% × 7 default envelope, no active bonds. |
| `DemandManager` | no-op in Bucket 3 | — | Q2 defers RCI coverage wiring. S buildings do NOT consume R demand + do NOT generate employment in MVP. Hook site documented for scales bucket. |
| `CityStats` | read-only read of budget + bond state | — | HUD projected-income-minus-maintenance hint extended to subtract envelope cap (not per-sub-type draw). Bond balance surfaced as debt line. |
| `UIManager.ToolbarChrome` + `UIManager.Hud` | new S zoning button + sub-type picker + budget panel + bond dialog | — | Four new UI surfaces (see Implementation Points). |
| `AutoZoningManager` | no-op for S | — | S placement is player-only in MVP (no AUTO). Guard + comment documented at enum-switch site. |
| `MiniMapController` | new color channel for S | — | Palette entry for 4th channel; no logic change beyond color lookup. |

### Implementation Points (Phase 6)

**Implementation Point IP-1 — ZoneType enum extension.** Add `StateServiceLightBuilding` / `StateServiceMediumBuilding` / `StateServiceHeavyBuilding` / `StateServiceLightZoning` / `StateServiceMediumZoning` / `StateServiceHeavyZoning` to `Assets/Scripts/Managers/UnitManagers/Zone.cs`. Extend `EconomyManager.IsBuildingZone`, `IsZoningType`, and add `IsStateServiceZone(Zone.ZoneType)`. Add `ZoneSubTypeRegistry` ScriptableObject under `Assets/ScriptableObjects/` with 7 entries. Sub-type id stored on `Zone` component as new `subTypeId` int field (default -1 for RCI).

**Implementation Point IP-2 — BudgetAllocationService.** New helper under `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs`. MonoBehaviour, Inspector-wired on `EconomyManager` GameObject. Public API: `TryDraw(int subTypeId, int amount) → bool`, `GetMonthlyEnvelope(int subTypeId) → int`, `SetEnvelopePct(int subTypeId, float pct)` (auto-normalizes so sum == 1), `MonthlyReset()`. Internal state: `float[7] envelopePct`, `int globalMonthlyCap`, `int[7] currentMonthRemaining`. `TryDraw` checks `currentMonthRemaining[subTypeId] >= amount && TreasuryFloorClampService.CanAfford(amount)` BEFORE mutation (Q4 hard cap).

**Implementation Point IP-3 — TreasuryFloorClampService.** New helper under `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs`. Wraps existing `EconomyManager.currentMoney` writes. `TrySpend(int amount, string context) → bool` replaces raw `SpendMoney` at all call sites; returns `false` + emits game notification when `amount > currentMoney` instead of allowing negative balance. Existing `EconomyManager.SpendMoney(int, string, bool)` keeps signature for backward compat but delegates to `TrySpend`; insufficient-funds path no longer subtracts — balance floor-clamped at 0 per Q4/Q5.

**Implementation Point IP-4 — BondLedgerService.** New helper under `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs`. MonoBehaviour, Inspector-wired. Single `BondData?` field per scale tier (dictionary keyed by tier id, MVP = city tier only). `TryIssueBond(scaleTier, principal, termMonths) → bool` returns false if active bond exists (Q5 max 1 concurrent). On success: credit `principal` via `EconomyManager.AddMoney`, record `monthlyRepayment = (principal × (1 + fixedInterestRate)) / termMonths`, stamp `issuedOnDate`. `ProcessMonthlyRepayment(scaleTier)` called from `EconomyManager.ProcessDailyEconomy` on month-first day after tax + maintenance; deducts via `TreasuryFloorClampService.TrySpend` — if spend fails, bond enters arrears state (no mechanical penalty MVP, HUD flag only).

**Implementation Point IP-5 — Maintenance registry refactor.** `EconomyManager.ProcessMonthlyMaintenance()` refactored: new `List<IMaintenanceContributor>` registry replacing hardcoded `roadCount × rate + powerPlantCount × rate`. Existing roads register via `RoadMaintenanceContributor` adapter (preserves current formula). Each S building implements `IMaintenanceContributor` with `GetMonthlyMaintenance()` reading from `ZoneSubTypeRegistry[subTypeId].monthlyUpkeep`. Contributors emit to the owning sub-type envelope (not general maintenance pool), so underfunded envelope = visible decay placeholder (Q3).

**Implementation Point IP-6 — ZoneSService placement.** New helper under `Assets/Scripts/Managers/GameManagers/ZoneSService.cs`. Entry point `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool`:
1. Lookup `ZoneSubTypeRegistry[subTypeId].baseCost`.
2. Call `BudgetAllocationService.TryDraw(subTypeId, baseCost)` — if false, emit game notification + return false (Q4 block-before-deduct).
3. Route through existing `ZoneManager.PlaceZone` with corresponding `StateServiceLightZoning` enum value + `subTypeId` sidecar write.
4. On building spawn (existing growth pipeline), register the building as `IMaintenanceContributor`.
All `gridArray`/`cellArray` access goes through `GridManager.GetCell` (invariant #5).

**Implementation Point IP-7 — Save schema v1 → v2 migration.** `GameSaveData.CurrentSchemaVersion` bumped to 2. New fields: `List<StateServiceZoneData> stateServiceZones`, `BudgetAllocationData budgetAllocation` (float[7] envelopePct, int globalMonthlyCap, int[7] currentMonthRemaining), `Dictionary<int, BondData> bondRegistry` (scale-tier-keyed). `MigrateLoadedSaveData` extension: `if (schemaVersion < 2) { stateServiceZones = new List<StateServiceZoneData>(); budgetAllocation = BudgetAllocationData.Default(); bondRegistry = new Dictionary<int, BondData>(); schemaVersion = 2; }`. `Default()` = 14.28% × 7 equal envelope + globalCap derived from treasury snapshot at migration time.

**Implementation Point IP-8 — UI surfaces (four).**
1. **S zoning button** — 4th button in `UIManager.ToolbarChrome` zoning cluster (existing R/C/I sibling pattern).
2. **Sub-type picker** — modal/overlay that opens on S-click (7 buttons, icon + name + baseCost). Click commits sub-type to placement.
3. **Budget panel** — panel with 7 pct sliders (sum-locked to 100%) + global cap slider + per-sub-type current-month remaining readout. HUD summary badge for overspend-blocked events.
4. **Bond dialog** — modal: principal input + term selector (fixed options: 12 / 24 / 48 months) + preview of `monthlyRepayment` + issue button. Disabled when active bond exists.

### Examples (Phase 7)

**Example 1 — Player places a police station, envelope allows.**
```
Player → S button → click cell (5, 8) → sub-type picker opens → click "police"
ZoneSService.PlaceStateServiceZone(cell=(5,8), subTypeId=POLICE)
  BudgetAllocationService.TryDraw(POLICE, baseCost=500)
    currentMonthRemaining[POLICE] = 1200, cap OK
    TreasuryFloorClampService.CanAfford(500) → true (treasury = 10000)
    deduct: currentMonthRemaining[POLICE] = 700, treasury = 9500
    → true
  ZoneManager.PlaceZone(cell, StateServiceLightZoning, subTypeId=POLICE)
  → growth tick spawns PoliceStation building
  → EconomyManager registers PoliceStation as IMaintenanceContributor
  → monthly maintenance draws from POLICE envelope each month 1
```

**Example 2 — Player places a fire station, envelope exhausted.**
```
Player → S button → click cell → picker → "fire"
ZoneSService.PlaceStateServiceZone(cell, subTypeId=FIRE)
  BudgetAllocationService.TryDraw(FIRE, baseCost=600)
    currentMonthRemaining[FIRE] = 200 < 600
    → false
  GameNotification.Raise("Fire service envelope exhausted this month")
  return false (no placement, no treasury deduction) — Q4 block-before-deduct
```

**Example 3 — Player issues a bond proactively.**
```
Treasury = 1200, upcoming planned S expansion = 3500
Player → bond dialog → principal=5000 + term=24 months
BondLedgerService.TryIssueBond(scaleTier=city, principal=5000, termMonths=24)
  no active bond → proceed
  monthlyRepayment = (5000 × 1.12) / 24 = 233
  EconomyManager.AddMoney(5000) → treasury = 6200
  bondRegistry[city] = { principal=5000, term=24, monthlyRepayment=233, ... }
  → true
Month 1 tick:
  EconomyManager.ProcessMonthlyMaintenance() (iterates contributors)
  BondLedgerService.ProcessMonthlyRepayment(city) → TrySpend(233) → OK
```

**Example 4 — Load legacy v1 save.**
```
GameSaveManager.LoadGame(path) → GameSaveData { schemaVersion=1, ... }
MigrateLoadedSaveData(data)
  schemaVersion < 2 → add:
    stateServiceZones = []
    budgetAllocation = { envelopePct=[0.143, 0.143, 0.143, 0.143, 0.143, 0.143, 0.143], globalCap=derived, remaining=cap×pct }
    bondRegistry = {}
  schemaVersion = 2
Restore proceeds — zero S buildings, default envelope, no bonds.
```

### Review Notes (Phase 8)

Self-review run as Plan subagent (prompt: "critique architecture for invariant breaks, contract gaps, save-migration gaps, UI coverage gaps, deferred-scope leaks"). Two rounds.

**Round 1 — BLOCKING flagged (3), all resolved.**

- **B1 (resolved).** `TreasuryFloorClampService` originally defined as wrapper around `SpendMoney` but left the existing `SpendMoney(int, string, bool)` signature reachable by non-S call sites — legacy paths could still go negative. **Resolution:** re-route existing `SpendMoney` to delegate to `TrySpend` so the floor clamp is systemic, not opt-in. See IP-3.
- **B2 (resolved).** `BudgetAllocationService` envelope check originally did not consult `TreasuryFloorClampService` — envelope could have funds while treasury was empty (envelope is a cap, not a wallet). **Resolution:** `TryDraw` now requires BOTH envelope remaining AND treasury floor check before deduction. See IP-2.
- **B3 (resolved).** Bond repayment originally called raw `SpendMoney` — could breach zero. **Resolution:** repayment routed through `TrySpend`; failure enters arrears state (no crash, no negative balance). See IP-4.

**Round 2 — NON-BLOCKING carried into Review Notes.**

- **N1.** Default envelope `14.28% × 7` does not sum exactly to 1.0 (rounding residue 0.04%). Resolution deferred to implementation: normalize on load + on every `SetEnvelopePct` call.
- **N2.** `IMaintenanceContributor` registry needs deterministic iteration order for save-replay parity — deferred to implementation (sort by contributor id string at enumeration).
- **N3.** Sub-type picker UX when player cancels mid-placement: close picker + no cost + no cell mutation. Deferred to UI implementation stage.
- **N4.** `AutoZoningManager` S no-op comment must reference this exploration doc by path (authoring convention).
- **N5.** MiniMap palette for S should use a single color for the 4th channel in MVP; 7-way sub-type color split deferred (Q2-adjacent decision: visuals-only sub-type distinction via building prefab, not minimap).
- **N6.** Bond arrears state needs a later-bucket decision: does arrears compound interest, trigger happiness penalty, block new bonds? Deferred post-MVP.
- **N7.** Cross-scale budget transfer (region → city) stays out of MVP per doc scope. Bond scale-tier dictionary already supports multi-tier keys; consumer wiring deferred.

### Expansion metadata

- Date (ISO): 2026-04-17
- Model: claude-opus-4-7
- Approach selected: E (hybrid 4th-channel + budget service contract)
- Blocking items resolved: 3
- Non-blocking items carried: 7

---

_Next step._ Run `/master-plan-new docs/zone-s-economy-exploration.md` to author `ia/projects/zone-s-economy-master-plan.md` and decompose into 3 steps × 2–3 stages each (per Bucket 3 size estimate).
