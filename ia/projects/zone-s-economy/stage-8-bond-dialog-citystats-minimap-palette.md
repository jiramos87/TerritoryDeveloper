### Stage 8 — UI surfaces + CityStats integration + economy-system reference spec / Bond dialog + `CityStats` + `MiniMap` palette

**Status:** In Progress

**Objectives:** Land bond issuance UI + read-model integration. Player can issue a bond, see debt aggregated in CityStats + HUD, see S cells distinct from RCI on the mini-map.

**Exit:**

- Bond issuance modal reachable from HUD or budget panel. Principal input (int) + term selector (radio: 12/24/48). Live `monthlyRepayment` preview. Issue button calls `bondLedger.TryIssueBond(cityTier, principal, term)`. Disabled when `GetActiveBond(cityTier) != null`.
- `CityStats` read-model fields: `totalEnvelopeCap`, `envelopeRemainingPerSubType[7]`, `activeBondDebt`, `monthlyBondRepayment`. `CityStatsUIController` displays these in the stats panel.
- HUD income-minus-maintenance hint extended: subtracts `totalEnvelopeCap` (not per-draw) per exploration §Subsystem Impact.
- `MiniMapController` palette: new color for S (all sub-types same color MVP, per N5). RCI unchanged.
- Integration test reproducing Example 3 (issue bond, treasury credited, month tick repays).
- Phase 1 — Bond issuance modal.
- Phase 2 — `CityStats` + HUD read-model extension.
- Phase 3 — `MiniMap` palette + integration test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Bond issuance modal UI | **TECH-565** | Done | New `BondIssuanceModal.cs` + Unity UI prefab via `UIManager.PopupStack`. Fields: principal `InputField` (int, min 100), term radio (12/24/48), live preview Text computing `(principal × 1.12) / termMonths`. Issue button calls `bondLedger.TryIssueBond(scaleTier: city, principal, termMonths)`. Disabled if `bondLedger.GetActiveBond(city) != null`. |
| T8.2 | Bond-active HUD flag + entry point | **TECH-566** | Done | HUD badge showing "Active bond: {remainingMonths} mo, {monthlyRepayment}/mo" when active bond exists (city tier MVP). Click opens bond detail view (reuses `BondIssuanceModal` in read-only mode, showing current bond + disabled issue button). Arrears state shows red badge. |
| T8.3 | `CityStats` envelope + bond fields | **TECH-567** | Done | Add fields to `CityStats.cs`: `int totalEnvelopeCap`, `int[] envelopeRemaining` (len 7), `int activeBondDebt`, `int monthlyBondRepayment`. Populate each tick from `budgetAllocation` + `bondLedger`. `CityStatsUIController` displays new fields in stats panel (label + value). |
| T8.4 | HUD income-minus-maintenance hint update | **TECH-568** | Done | Update HUD projected-income-minus-maintenance readout in `UIManager.Hud` (or the existing formula site) to subtract `cityStats.totalEnvelopeCap` from the projected monthly surplus. Label text updated to "Est. monthly surplus (after S envelope + bond repayment)". |
| T8.5 | `MiniMapController` S palette | **TECH-569** | Done | Extend color lookup in `MiniMapController.cs`: new case for each of 6 new `ZoneType` values returning a single S color (e.g. purple). N5 locks: no per-sub-type color split in MVP. RCI colors unchanged. |
| T8.6 | Integration test — Example 3 end-to-end | **TECH-570** | Done | `BondIssuanceIntegrationTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode). Reproduces Example 3: treasury=1200, `TryIssueBond(city, 5000, 24)` → returns true, treasury=6200, registry has entry with `monthlyRepayment=233`. Month tick triggers `ProcessMonthlyRepayment` → treasury=5967, `monthsRemaining=23`. Save/load round-trip preserves bond state. |

<!-- sizing-gate-waiver: H1/H6 — bond modal + HUD + CityStats + MiniMap + test span multiple UI files; incremental MVP surfaces; accepted 2026-04-20 -->

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  reserved_id: "TECH-565"
  issue_type: TECH
  title: "Bond issuance modal UI"
  priority: "medium"
  notes: |
    New **BondIssuanceModal** on `UIManager.PopupStack`. Principal **InputField**, term radios 12/24/48, live **monthlyRepayment** preview. Issue calls `BondLedgerService.TryIssueBond` at city scale tier; disabled when active bond on tier. Touches HUD/budget entry path from Stage 7.
  depends_on: []
  related: ["TECH-566", "TECH-567", "TECH-568", "TECH-569", "TECH-570"]
  stub_body:
    summary: |
      Player-facing bond issuance flow: enter principal + term, preview repayment, issue via **IBondLedger**; block duplicate tier bond per locked ledger rules.
    goals: |
      - Modal prefab + `BondIssuanceModal.cs` wired to `UIManager.PopupStack`.
      - Preview text matches `(principal × (1 + fixedRate)) / termMonths` with ledger default rate.
      - Issue button invokes `TryIssueBond(cityTier, principal, termMonths)`; guard when `GetActiveBond(cityTier) != null`.
    systems_map: |
      - `UIManager.PopupStack`, `BondLedgerService` / `IBondLedger`, `EconomyManager` (tier read)
      - `docs/zone-s-economy-exploration.md` Example 3
    impl_plan_sketch: |
      Phase 1 — Modal layout + stack push + ledger refs. Phase 2 — Validation + preview + issue wiring + disabled states.

- operation: file_task
  reserved_id: "TECH-566"
  issue_type: TECH
  title: "Bond-active HUD flag + entry point"
  priority: "medium"
  notes: |
    HUD strip badge when city-tier bond active: remaining months + monthly repayment; click opens bond modal read-only. **Arrears** → red styling. Entry from HUD or budget panel per Stage 8 Exit.
  depends_on: []
  related: ["TECH-565", "TECH-567", "TECH-568", "TECH-569", "TECH-570"]
  stub_body:
    summary: |
      Persistent HUD indicator for active bond; drill-down reuses issuance modal in read-only mode with issue disabled.
    goals: |
      - Badge text: active bond summary + arrears flag when ledger marks arrears.
      - Click opens `BondIssuanceModal` read-only path (no issue) showing current **BondData**.
      - Wire HUD / budget entry points without breaking Stage 7 layout.
    systems_map: |
      - `UIManager.Hud`, `BondLedgerService`, `BondIssuanceModal`
    impl_plan_sketch: |
      Phase 1 — Badge presenter + ledger subscription or poll. Phase 2 — Modal dual mode (issue vs detail) + arrears color.

- operation: file_task
  reserved_id: "TECH-567"
  issue_type: TECH
  title: "`CityStats` envelope + bond fields"
  priority: "medium"
  notes: |
    Extend **CityStats** read model: envelope cap, per-sub-type remaining array, bond debt + monthly repayment aggregates. **CityStatsUIController** renders new rows in stats panel.
  depends_on: []
  related: ["TECH-565", "TECH-566", "TECH-568", "TECH-569", "TECH-570"]
  stub_body:
    summary: |
      Surface allocator + ledger state on city read model for stats UI and downstream HUD hint.
    goals: |
      - Add `totalEnvelopeCap`, `envelopeRemaining[7]`, `activeBondDebt`, `monthlyBondRepayment` to `CityStats`.
      - Populate from `BudgetAllocationService` + `BondLedgerService` on economy tick.
      - Stats panel labels + values for each field.
    systems_map: |
      - `CityStats.cs`, `CityStatsUIController`, `BudgetAllocationService`, `BondLedgerService`
    impl_plan_sketch: |
      Phase 1 — Fields + tick population. Phase 2 — UI controller wiring + formatting.

- operation: file_task
  reserved_id: "TECH-568"
  issue_type: TECH
  title: "HUD income-minus-maintenance hint update"
  priority: "medium"
  notes: |
    Subtract **`totalEnvelopeCap`** from projected monthly surplus line in HUD readout; label copy matches exploration §Subsystem Impact (after S envelope + bond repayment wording).
  depends_on: []
  related: ["TECH-565", "TECH-566", "TECH-567", "TECH-569", "TECH-570"]
  stub_body:
    summary: |
      Align projected surplus hint with envelope budget ceiling so player sees post-S envelope picture.
    goals: |
      - Locate existing projected income-minus-maintenance HUD formula site.
      - Incorporate `cityStats.totalEnvelopeCap` (and bond repayment already in model if separate).
      - Update label to Stage 8 Exit string.
    systems_map: |
      - `UIManager.Hud` (or dedicated HUD presenter), `CityStats`
    impl_plan_sketch: |
      Phase 1 — Trace current formula. Phase 2 — Subtract cap + verify copy.

- operation: file_task
  reserved_id: "TECH-569"
  issue_type: TECH
  title: "`MiniMapController` S palette"
  priority: "medium"
  notes: |
    One **Zone S** color for all six **StateService** zone types + zoning variants; **N5** — no per-sub-type tint in MVP. R/C/I unchanged.
  depends_on: []
  related: ["TECH-565", "TECH-566", "TECH-567", "TECH-568", "TECH-570"]
  stub_body:
    summary: |
      Mini-map distinguishes **Zone S** cells from R/C/I using single shared tint per exploration N5.
    goals: |
      - Extend `MiniMapController` color lookup for `StateService*` **ZoneType** values.
      - Single purple (or chosen theme token) for all S; no per-sub-type split.
    systems_map: |
      - `MiniMapController.cs`, `Zone.ZoneType`, `UiTheme` if applicable
    impl_plan_sketch: |
      Phase 1 — Map enum cases to S color. Phase 2 — Visual sanity vs R/C/I.

- operation: file_task
  reserved_id: "TECH-570"
  issue_type: TECH
  title: "Integration test — Example 3 end-to-end"
  priority: "medium"
  notes: |
    **BondIssuanceIntegrationTests**: treasury 1200 → issue 5000 @ 24 mo → **TryIssueBond** true; treasury 6200; **monthlyRepayment** 233; month tick repayment; save/load round-trip on bond registry.
  depends_on: []
  related: ["TECH-565", "TECH-566", "TECH-567", "TECH-568", "TECH-569"]
  stub_body:
    summary: |
      Automate exploration Example 3 bond flow + persistence check for Stage 8 exit gate.
    goals: |
      - EditMode (or PlayMode) test harness with economy + ledger test doubles or scene.
      - Assert issue + repayment math + registry state; assert save/load preserves bond fields.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/`, `BondLedgerService`, `GameSaveManager` / save data path
    impl_plan_sketch: |
      Phase 1 — Harness + issue assertions. Phase 2 — Tick repayment + save round-trip.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.
