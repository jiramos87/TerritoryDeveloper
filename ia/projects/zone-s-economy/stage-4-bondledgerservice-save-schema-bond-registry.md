### Stage 4 — Economy services: bond ledger + maintenance registry + Zone S placement / `BondLedgerService` + save-schema bond registry

**Status:** Final

**Objectives:** Land the single-bond-per-scale-tier ledger. Proactive injection only (not remedial). Save-schema already bumped to v4 in Step 1 — extend `GameSaveData` with `bondRegistry` field (same migration branch) without a second version bump.

**Exit:**

- `IBondLedger` interface + `BondLedgerService` MonoBehaviour.
- `TryIssueBond(scaleTier, principal, termMonths) → bool` rejects if active bond exists on tier; on success credits via `EconomyManager.AddMoney`, computes `monthlyRepayment = (principal × (1 + fixedInterestRate)) / termMonths`.
- `ProcessMonthlyRepayment(scaleTier)` routes repayment through `TreasuryFloorClampService.TrySpend` — failure flags arrears (HUD flag only, no mechanical penalty per Review Note N6).
- `GameSaveData.bondRegistry` = `Dictionary<int, BondData>` with save/load round-trip. Migration v3→v4 already adds empty registry (extend Stage 1.3 migration branch).
- Glossary rows: `BondLedgerService`, `IBondLedger`.
- Phase 1 — `IBondLedger` + `BondLedgerService` skeleton + save schema extension.
- Phase 2 — Issue + repayment logic wired to `EconomyManager` tick + tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | `IBondLedger` + `BondLedgerService` skeleton | **TECH-528** | Done (archived) | New interface `IBondLedger` with `TryIssueBond(int scaleTier, int principal, int termMonths) → bool`, `GetActiveBond(int scaleTier) → BondData?`, `ProcessMonthlyRepayment(int scaleTier)`. New `BondLedgerService.cs` MonoBehaviour implementing interface. `[SerializeField]` constants: `fixedInterestRate` (default 0.12). Internal state: `Dictionary<int, BondData> active`. |
| T4.2 | Extend save schema with `bondRegistry` | **TECH-529** | Done (archived) | Add `[Serializable] public class BondData { public int scaleTier; public int principal; public int termMonths; public int monthlyRepayment; public float fixedInterestRate; public string issuedOnDate; public int monthsRemaining; public bool arrears; }`. Add `public Dictionary<int, BondData> bondRegistry` to `GameSaveData` (serialize as list-of-entries if Dictionary JSON not supported). Extend v3→v4 migration branch from Stage 1.3 to seed empty registry. |
| T4.3 | `TryIssueBond` logic | **TECH-530** | Done (archived) | Implement `TryIssueBond(scaleTier, principal, termMonths)`: return false if `active.ContainsKey(scaleTier)`; else compute `monthlyRepayment = (int)((principal * (1 + fixedInterestRate)) / termMonths)`, call `economyManager.AddMoney(principal)`, create `BondData` stamped with today's date + `monthsRemaining = termMonths`, insert into `active`, return true. Reject non-positive principal/term. |
| T4.4 | `ProcessMonthlyRepayment` + tick wiring | **TECH-531** | Done (archived) | Implement `ProcessMonthlyRepayment(scaleTier)`: lookup active bond; if none, return. Call `treasuryFloorClamp.TrySpend(bond.monthlyRepayment, "bond repayment")`. On false → `bond.arrears = true` (no crash, HUD flag only). On true → `bond.monthsRemaining--`; if `monthsRemaining == 0`, remove from `active`. Wire call from `EconomyManager.ProcessDailyEconomy` on month-first day AFTER tax + maintenance. |
| T4.5 | Save/load round-trip wiring | **TECH-532** | Done (archived) | `BondLedgerService.CaptureSaveData() → Dictionary<int, BondData>` + `RestoreFromSaveData(Dictionary<int, BondData>)`. Wire into `GameSaveManager.SaveGame` / `LoadGame` post-migration. Verify save → load → identity on all fields including `monthsRemaining` + `arrears`. |
| T4.6 | EditMode tests + glossary rows | **TECH-533** | Done (archived) | `BondLedgerServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TryIssueBond` succeeds when no active + balance credited + registry populated, (b) second `TryIssueBond` same tier returns false, (c) `ProcessMonthlyRepayment` decrements `monthsRemaining` + spends, (d) repayment when balance insufficient flags `arrears = true` + balance stays at 0, (e) bond clears from registry when `monthsRemaining` reaches 0, (f) save/load round-trip preserves all bond fields. Add `BondLedgerService` + `IBondLedger` glossary rows + MCP reindex. |

<!-- sizing-gate-waiver: H6 triggered by incremental construction on one new service class (`BondLedgerService`) across T4.1/T4.3/T4.4/T4.5; accepted 2026-04-20 -->

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-528"
  title: "`IBondLedger` + `BondLedgerService` skeleton"
  priority: "medium"
  notes: |
    Create bond ledger seam. Add interface and MonoBehaviour shell.
    Wire serialized defaults for fixed interest and in-memory active registry map.
    Keep logic inert here; execution behavior lands in follow-up tasks.
  depends_on: []
  related:
    - "TECH-529"
    - "TECH-530"
    - "TECH-531"
    - "TECH-532"
    - "TECH-533"
  stub_body:
    summary: |
      Build Stage 4 base ledger surface for single-bond-per-tier flow. Define contract and service shell so issuance, repayment, and persistence tasks can stack without signature churn.
    goals: |
      - Define `IBondLedger` methods for issue/read/repayment lifecycle.
      - Create `BondLedgerService` MonoBehaviour with serialized `fixedInterestRate` default and active-bond registry state.
      - Keep behavior non-functional until Stage 4 logic tasks wire economy and treasury calls.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/IBondLedger.cs`
      - `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs`
      - `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` (composition reference only in this task)
      - Stage context: economy services, treasury floor clamp, save schema v4 extension path.
    impl_plan_sketch: |
      Phase 1 — Author interface + service shell with serialized fields, fallback dependency wiring pattern, and internal registry container.
- reserved_id: "TECH-529"
  title: "Extend save schema with `bondRegistry`"
  priority: "medium"
  notes: |
    Extend v4 payload. Add bond data DTO and save-field storage for active bonds.
    Keep v3->v4 migration branch in same schema version line; seed empty bond registry.
  depends_on: []
  related:
    - "TECH-528"
    - "TECH-530"
    - "TECH-531"
    - "TECH-532"
    - "TECH-533"
  stub_body:
    summary: |
      Add durable save representation for active bonds with no extra schema bump. Keep migration deterministic and compatible with existing v3-to-v4 path.
    goals: |
      - Add serializable `BondData` model with repayment and arrears fields.
      - Add `bondRegistry` field on `GameSaveData` using JSON-safe shape if dictionary serialization is unsafe.
      - Extend existing migration branch to seed empty bond registry for legacy saves.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
      - `Assets/Scripts/Managers/UnitManagers/GameSaveData.cs` (or equivalent save DTO file)
      - `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs` (save model coupling)
      - Persistence surface: v3->v4 migration and round-trip contracts.
    impl_plan_sketch: |
      Phase 1 — Introduce bond save DTO + registry field. Patch migration defaults and keep version semantics stable at v4.
- reserved_id: "TECH-530"
  title: "`TryIssueBond` logic"
  priority: "medium"
  notes: |
    Implement issuance guard and principal injection flow.
    Reject duplicate active tier and invalid input values.
    Compute fixed monthly repayment deterministically from principal, rate, and term.
  depends_on: []
  related:
    - "TECH-528"
    - "TECH-529"
    - "TECH-531"
    - "TECH-532"
    - "TECH-533"
  stub_body:
    summary: |
      Ship proactive bond issuance path with one-active-bond-per-tier constraint. Credit treasury on success and store normalized bond state for monthly repayment processing.
    goals: |
      - Validate `scaleTier`, `principal`, and `termMonths` inputs before mutation.
      - Block issuance when tier already has active bond.
      - Add principal through `EconomyManager.AddMoney` and persist `monthlyRepayment`, dates, and remaining-term fields.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs`
      - `Assets/Scripts/Managers/GameManagers/EconomyManager.cs`
      - `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs` (read-side constraint context)
      - Stage exit anchor: single concurrent bond per scale tier.
    impl_plan_sketch: |
      Phase 1 — Implement `TryIssueBond` path end-to-end with guard checks, repayment math, treasury credit, and active-registry insert.
- reserved_id: "TECH-531"
  title: "`ProcessMonthlyRepayment` + tick wiring"
  priority: "medium"
  notes: |
    Implement monthly repayment behavior and arrears flagging.
    Route repayment through treasury floor clamp spend API.
    Wire invocation on month-first economy tick after tax and maintenance order.
  depends_on: []
  related:
    - "TECH-528"
    - "TECH-529"
    - "TECH-530"
    - "TECH-532"
    - "TECH-533"
  stub_body:
    summary: |
      Add bond repayment lifecycle integration into monthly economy tick. Enforce non-negative treasury rule via floor clamp and capture arrears state without hard gameplay penalty.
    goals: |
      - Implement `ProcessMonthlyRepayment` with no-op when no active bond.
      - Spend via `TreasuryFloorClampService.TrySpend` and set `arrears` on failed spend.
      - Decrement `monthsRemaining` and remove completed bond records.
      - Wire call ordering into `EconomyManager.ProcessDailyEconomy` month-first sequence.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs`
      - `Assets/Scripts/Managers/GameManagers/EconomyManager.cs`
      - `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs`
      - Tick-order surface: tax + maintenance + bond repayment ordering.
    impl_plan_sketch: |
      Phase 1 — Implement repayment method in ledger service. Phase 2 — wire month-first call site and verify ordering relative to existing economy tick steps.
- reserved_id: "TECH-532"
  title: "Save/load round-trip wiring"
  priority: "medium"
  notes: |
    Bind bond ledger runtime state to save capture and restore paths.
    Ensure all bond fields persist, including arrears and remaining months.
    Keep migration-safe load order with post-migration restore call.
  depends_on: []
  related:
    - "TECH-528"
    - "TECH-529"
    - "TECH-530"
    - "TECH-531"
    - "TECH-533"
  stub_body:
    summary: |
      Connect ledger runtime registry with save pipeline so active bonds survive reload exactly. Preserve deterministic state shape across capture, migration, and restore.
    goals: |
      - Add `CaptureSaveData` and `RestoreFromSaveData` methods to `BondLedgerService`.
      - Call capture during `GameSaveManager.SaveGame`.
      - Call restore during `GameSaveManager.LoadGame` after migration.
      - Verify field identity for principal, repayment, remaining term, date, and arrears.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs`
      - `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
      - Save DTO surface carrying `bondRegistry`.
      - Stage 4 migration path introduced in prior tasks.
    impl_plan_sketch: |
      Phase 1 — Add capture/restore API. Phase 2 — wire save/load hooks and keep null-safe defaults on empty registry restores.
- reserved_id: "TECH-533"
  title: "EditMode tests + glossary rows"
  priority: "medium"
  notes: |
    Add bond ledger regression coverage and glossary updates.
    Validate issue, duplicate block, repayment success/failure, completion removal, and save round-trip.
    Reindex IA glossary after term additions.
  depends_on: []
  related:
    - "TECH-528"
    - "TECH-529"
    - "TECH-530"
    - "TECH-531"
    - "TECH-532"
  stub_body:
    summary: |
      Lock Stage 4 behavior with automated tests and vocabulary alignment. Prove bond ledger logic and persistence behavior are stable before UI tasks consume it.
    goals: |
      - Add `BondLedgerServiceTests` covering six Stage-exit scenarios.
      - Confirm arrears semantics keep treasury at floor with no crash.
      - Validate save/load round-trip for full bond payload.
      - Add glossary rows for `BondLedgerService` and `IBondLedger`, then regenerate indexes.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/BondLedgerServiceTests.cs`
      - `ia/specs/glossary.md`
      - `tools/mcp-ia-server/data/glossary-index.json` (regen target)
      - Runtime files exercised: `BondLedgerService`, `GameSaveManager`, `EconomyManager`.
    impl_plan_sketch: |
      Phase 1 — Author test fixtures and scenario assertions. Phase 2 — add glossary rows and run IA index regeneration.
```
