# Zone S + Economy — Master Plan (MVP)

> **Last updated:** 2026-04-20
>
> **Status:** In Progress — Step 3 (Stage 9 Done)
>
> **Scope:** Zone S (state-owned 4th zone channel, 7 sub-types) + economy depth — per-sub-type envelope budget allocator, floor-clamped treasury (hard cap, no negative balance), single-bond-per-scale-tier ledger, extended monthly-maintenance contract via `IMaintenanceContributor` registry, save schema bump. Bucket 3 of full-game MVP umbrella (`ia/projects/full-game-mvp-master-plan.md`).
>
> **Exploration source:** `docs/zone-s-economy-exploration.md` (§Design Expansion — Interview summary, Chosen Approach, Architecture, Subsystem Impact, Implementation Points IP-1..IP-8, Examples, Review Notes).
>
> **Locked decisions (do NOT reopen in this plan):**
> - Approach E — 4th zone channel + budget service contract together, one save-schema bump.
> - 7 sub-types behind one shared enum extension (`StateServiceLight/Medium/Heavy` × Building/Zoning). Sub-type id stored in `Zone` sidecar field, NOT enum.
> - Envelope budget — 7 pct sliders sum-locked to 100% × global S monthly cap. `TryDraw` checks envelope remaining AND treasury floor BEFORE deduction.
> - Hard-cap treasury — balance NEVER negative across ALL spend call sites (systemic floor clamp, not opt-in).
> - Single concurrent bond per scale tier, fixed principal + fixed term + fixed interest rate. Proactive injection lever, not remedial overdraft.
> - Out-of-scope MVP: RCI service coverage, desirability, happiness contribution, cross-scale budget transfer, bond secondary market, bond rating, interest-rate tiering, S tax revenue.
>
> **Schema-version note:** exploration says v1→v2 but current repo `GameSaveData.CurrentSchemaVersion = 3` (see `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs:404`). Real bump is **v3→v4**; semantic migration payload is identical to the exploration spec. Locked.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Glossary gap (blocks umbrella column (e)):** new domain terms need glossary rows before any task files can close — `Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget)`. Land rows inside Stage 1.1 scaffolding.
>
> **Spec gap (blocks umbrella column (g)):** no `ia/specs/economy-system.md` exists yet. Author new reference spec covering Zone S + envelope budget + bond ledger + maintenance contributor registry. Task lands in Step 3 integration stage.
>
> **Read first if landing cold:**
> - `docs/zone-s-economy-exploration.md` — full design + IP breakdown + 4 worked examples. §Design Expansion is ground truth.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella orchestrator (Bucket 3 owner).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons), #5 (no direct `gridArray` outside `GridManager`), #6 (no new `GridManager` responsibilities — helper carve-out under `Managers/GameManagers/*Service.cs`), #12 (project spec under `ia/projects/`).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Stage 1 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / ZoneType enum extension + sub-type registry

**Status:** Final

**Backlog state (Stage 1.1):** 6 filed (TECH-278..283 all Done (archived))

**Objectives:** Extend `ZoneType` with 6 new enum values (3 densities × Building/Zoning). Author `ZoneSubTypeRegistry` SO with 7 entries. Add `subTypeId` sidecar to `Zone` component. Land glossary rows for the new domain vocabulary (Zone S, ZoneSubTypeRegistry, envelope). Scaffolding only — no runtime logic consumes the new values yet.

**Exit:**

- 6 new `ZoneType` enum values added + existing consumers (`IsBuildingZone`, `IsZoningType`) updated + new `IsStateServiceZone` predicate on `EconomyManager`.
- `Zone.subTypeId` field (default -1) + serializable.
- `ZoneSubTypeRegistry` MonoBehaviour + `Assets/Resources/Economy/zone-sub-types.json` with 7 seeded entries.
- Glossary rows added: `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)`.
- `npm run unity:compile-check` green.
- Phase 1 — `ZoneType` enum + predicates + `Zone.subTypeId` sidecar.
- Phase 2 — `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) + `zone-sub-types.json` config.
- Phase 3 — Glossary + spec-index refresh.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Extend `ZoneType` enum + predicates | **TECH-278** | Done (archived) | Add `StateServiceLightBuilding`, `StateServiceMediumBuilding`, `StateServiceHeavyBuilding`, `StateServiceLightZoning`, `StateServiceMediumZoning`, `StateServiceHeavyZoning` to `Zone.ZoneType` enum in `Assets/Scripts/Managers/UnitManagers/Zone.cs`. Extend `EconomyManager.IsBuildingZone` + `IsZoningType` to include the new values. Add `IsStateServiceZone(Zone.ZoneType)` predicate on `EconomyManager`. No caller change yet. |
| T1.2 | Add `Zone.subTypeId` sidecar field | **TECH-279** | Done (archived) | Add `[SerializeField] private int subTypeId = -1;` to `Zone.cs` + public getter/setter. Default -1 means "RCI, no sub-type". Persists via existing Unity serialization — no save plumbing yet (save bump lands in Stage 1.3). |
| T1.3 | Author `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) | **TECH-280** | Done (archived) | `ZoneSubTypeRegistry : MonoBehaviour` at `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs`. Loads `Assets/Resources/Economy/zone-sub-types.json` in `Awake` via `Resources.Load<TextAsset>` + `JsonUtility`. Entry fields: `int id`, `string displayName`, `string prefabPath`, `string iconPath`, `int baseCost`, `int monthlyUpkeep`. `GetById(int)` lookup. `LoadFromJson()` public for tests. |
| T1.4 | Seed `zone-sub-types.json` config file | **TECH-281** | Done (archived) | Author `Assets/Resources/Economy/zone-sub-types.json` with 7 entries (police=0…public offices=6). `prefabPath` + `iconPath` empty strings (art deferred). `baseCost` + `monthlyUpkeep` per exploration §IP-1. Human/agent edits costs directly in JSON; no Unity Editor required. |
| T1.5 | Glossary + spec-index refresh | **TECH-282** | Done (archived) | Add rows to `ia/specs/glossary.md` — `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)` — each with definition + authoritative spec link (points at forthcoming `ia/specs/economy-system.md`, cross-refs exploration doc for now). Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json`. |
| T1.6 | EditMode tests for enum + registry | **TECH-283** | Done (archived) | New test class `ZoneSubTypeRegistryTests` under `Assets/Tests/EditMode/Economy/` (new asmdef if needed). Cover: `GetById` returns correct entry for each of 7 ids, `GetById(-1)` returns null/throws, `IsStateServiceZone` true for 6 new enum values + false for R/C/I, `Zone.subTypeId` default -1 persists via serialization round-trip. |

### Stage 2 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `TreasuryFloorClampService` + systemic spend delegation

**Status:** Final

**Objectives:** Land hard-cap treasury (Q4 locked decision). Wrap ALL `EconomyManager` spend call sites so balance NEVER goes negative. Existing `SpendMoney(int, string, bool)` keeps signature for backward compat but delegates to new `TrySpend`. This stage is the single riskiest refactor — touches every current money-out path in `EconomyManager`.

**Exit:**

- `TreasuryFloorClampService.cs` under `Assets/Scripts/Managers/GameManagers/` — MonoBehaviour, Inspector-wired on `EconomyManager` GO, `FindObjectOfType` fallback in `Awake` (guardrail #1).
- Public API: `bool CanAfford(int amount)`, `bool TrySpend(int amount, string context)`, `int CurrentBalance`.
- Existing `EconomyManager.SpendMoney` retained for backward compat, internally delegates to `TrySpend` (insufficient → `false` return + game notification, no mutation).
- EditMode test proves balance cannot go below zero via any public EconomyManager spend path.
- Glossary row: `TreasuryFloorClampService`.
- `npm run unity:compile-check` green.
- Phase 1 — `TreasuryFloorClampService` skeleton + Inspector wiring.
- Phase 2 — Delegate existing `SpendMoney` + re-route call sites + EditMode coverage + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | `TreasuryFloorClampService` skeleton | **TECH-379** | Done (archived) | New MonoBehaviour at `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs`. `[SerializeField] private EconomyManager economy;` with `FindObjectOfType<EconomyManager>()` fallback in `Awake`. Public API: `CanAfford(int) → bool`, `TrySpend(int, string) → bool`, `CurrentBalance` property reading `economy.GetCurrentMoney()`. `TrySpend` checks `amount <= CurrentBalance` BEFORE mutation; success path calls `economy.cityStats.RemoveMoney(amount)`; failure path emits `GameNotificationManager.PostError`. |
| T2.2 | Wire service on `EconomyManager` GO | **TECH-380** | Done (archived) | Add `[SerializeField] private TreasuryFloorClampService treasuryFloorClamp;` field + `FindObjectOfType` fallback in `EconomyManager.Awake` (guardrail #1, invariant #4). Attach component to the `EconomyManager` GameObject in the main scene prefab. Document composition relationship in XML doc on the field. |
| T2.3 | Re-route `SpendMoney` through `TrySpend` | **TECH-381** | Done (archived) | Existing `EconomyManager.SpendMoney(int amount, string context, bool logToConsole)` keeps signature but body delegates to `treasuryFloorClamp.TrySpend(amount, context)`. On `false` return, log + emit notification; do NOT subtract `currentMoney` (previously allowed negative). Audit all internal call sites inside `EconomyManager.cs` that touch `currentMoney -= X` directly — rewrite via `TrySpend`. |
| T2.4 | Audit cross-file `SpendMoney` call sites | **TECH-382** | Done (archived) | Grep for `SpendMoney(` + `currentMoney -=` across `Assets/Scripts/**`. For each non-EconomyManager caller, confirm path now routes through `TrySpend`; update any direct `currentMoney` mutation to `TrySpend`. Document audit result in Decision Log section of spec stub. Zero remaining direct `currentMoney -=` outside `TreasuryFloorClampService`. |
| T2.5 | EditMode tests + glossary row | **TECH-383** | Done (archived) | `TreasuryFloorClampServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TrySpend(100)` when balance=200 succeeds + balance=100, (b) `TrySpend(300)` when balance=200 returns false + balance UNCHANGED + notification emitted, (c) `CanAfford(200)` true at balance=200, false at balance=199. Add `TreasuryFloorClampService` glossary row linking exploration + forthcoming economy-system spec. Regenerate MCP indexes. |

### Stage 3 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `BudgetAllocationService` + save-schema v3→v4 migration

**Status:** Final

**Objectives:** Land the envelope allocator (Q3 locked decision) + save-schema bump so fresh games persist state + legacy v3 saves migrate cleanly with default-equal envelope. `TryDraw` enforces Q4 block-before-deduct by checking BOTH envelope remaining AND `TreasuryFloorClampService.CanAfford` before mutation. This stage completes Step 1 — all structural primitives ready for Step 2 consumers.

**Exit:**

- `IBudgetAllocator` interface under `Assets/Scripts/Managers/GameManagers/IBudgetAllocator.cs`.
- `BudgetAllocationService.cs` implements `IBudgetAllocator` + MonoBehaviour + Inspector-wired. Internal state: `float[7] envelopePct`, `int globalMonthlyCap`, `int[7] currentMonthRemaining`.
- `TryDraw(int subTypeId, int amount)` checks envelope remaining AND treasury floor BEFORE deduction.
- `SetEnvelopePct(int, float)` auto-normalizes so sum == 1.0 (N1 non-blocking resolution).
- `MonthlyReset()` called from `EconomyManager.ProcessDailyEconomy` on month-first day.
- `GameSaveData.CurrentSchemaVersion = 4`. New fields: `List<StateServiceZoneData> stateServiceZones` + `BudgetAllocationData budgetAllocation` (float[7] envelopePct, int globalMonthlyCap, int[7] currentMonthRemaining).
- `MigrateLoadedSaveData` branch for v3→v4 seeds equal 14.28% × 7 envelope + empty S list. Normalizes envelope post-seed.
- Glossary rows added: `BudgetAllocationService`, `IBudgetAllocator`.
- EditMode + integration tests green; `npm run unity:compile-check` green.
- Phase 1 — `IBudgetAllocator` + `BudgetAllocationService` skeleton.
- Phase 2 — `TryDraw` logic + monthly reset + envelope normalization.
- Phase 3 — Save schema v3→v4 bump + migration + round-trip + tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | `IBudgetAllocator` interface + `BudgetAllocationService` skeleton | **TECH-418** | Done (archived) | Author `IBudgetAllocator.cs` with `TryDraw(int, int) → bool`, `GetMonthlyEnvelope(int) → int`, `SetEnvelopePct(int, float)`, `MonthlyReset()`. Author `BudgetAllocationService.cs` MonoBehaviour implementing the interface; `[SerializeField]` refs to `EconomyManager` + `TreasuryFloorClampService` + `ZoneSubTypeRegistry` with `FindObjectOfType` fallbacks in `Awake`. Fields inert in this task. |
| T3.2 | Wire service on `EconomyManager` GO | **TECH-419** | Done (archived) | Add `[SerializeField] private BudgetAllocationService budgetAllocation;` to `EconomyManager` + `FindObjectOfType` fallback. Attach `BudgetAllocationService` + `ZoneSubTypeRegistry` MonoBehaviours to the `EconomyManager` GO (same GO as `TreasuryFloorClampService`). Does not call them yet — consumer wiring lands in Step 2. |
| T3.3 | `TryDraw` + monthly reset logic | **TECH-420** | Done (archived) | Implement `TryDraw(subTypeId, amount)`: returns false if `subTypeId < 0 |  | subTypeId > 6`; returns false if `currentMonthRemaining[subTypeId] < amount`; returns false if `!treasuryFloorClamp.CanAfford(amount)`; otherwise `currentMonthRemaining[subTypeId] -= amount`, `treasuryFloorClamp.TrySpend(amount, "S envelope draw")` and return true. Implement `MonthlyReset`: for each i, `currentMonthRemaining[i] = (int)(globalMonthlyCap * envelopePct[i])`. |
| T3.4 | `SetEnvelopePct` auto-normalize | **TECH-421** | Done (archived) | Implement `SetEnvelopePct(int subTypeId, float pct)`: store raw value then normalize entire array so `sum == 1.0` (scale every entry by `1.0 / currentSum`). Handles N1 rounding residue from 14.28% × 7. Guard division-by-zero on all-zero envelope (reject + keep prior state). Also expose `SetEnvelopePctsBatch(float[7])` for player slider commits. |
| T3.5 | Save-schema v3→v4 bump | **TECH-422** | Done (archived) | Bump `GameSaveData.CurrentSchemaVersion` from 3 to 4 in `GameSaveManager.cs`. Add `[Serializable] public class BudgetAllocationData { public float[] envelopePct; public int globalMonthlyCap; public int[] currentMonthRemaining; public static BudgetAllocationData Default(int cap); }`. Add `public BudgetAllocationData budgetAllocation` + `public List<StateServiceZoneData> stateServiceZones` fields on `GameSaveData`. Add `[Serializable] public class StateServiceZoneData { public int cellX, cellY; public int subTypeId; public int densityTier; }`. |
| T3.6 | `MigrateLoadedSaveData` v3→v4 branch | **TECH-423** | Done (archived) | Null-coalesce branch in `MigrateLoadedSaveData` seeds `stateServiceZones` (empty list) + `budgetAllocation` (`BudgetAllocationData.Default(DEFAULT_S_CAP)`) before final `schemaVersion = CurrentSchemaVersion` line. Cap constant `DEFAULT_S_CAP = 10_000` on `GameSaveManager` (save-lifecycle owner). Idempotent on already-v4 data (non-null → skip). Integrity asserts appended (null post-migration → `InvalidOperationException`). `Default(cap)` envelope sums 1.0 ±1e-6. Decision Log persisted to `ia_project_spec_journal`. |
| T3.7 | Save/load round-trip wiring | **TECH-424** | Done (archived) | Wire `BudgetAllocationService.CaptureSaveData()` + `RestoreFromSaveData(BudgetAllocationData)` methods. `GameSaveManager.SaveGame` calls capture; `GameSaveManager.LoadGame` calls restore post-migration. Verify envelope + remaining + cap survive save → load → verify identity. No S zones restored yet (placement lands in Step 2). |
| T3.8 | EditMode tests + glossary + MCP reindex | **TECH-425** | Done (archived) | `BudgetAllocationServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TryDraw` blocks when envelope exhausted, (b) `TryDraw` blocks when treasury empty (envelope fat), (c) `TryDraw` succeeds when both OK + both decrement, (d) `MonthlyReset` restores `currentMonthRemaining` to `cap × pct`, (e) `SetEnvelopePct` normalizes sum to 1.0 within 1e-6, (f) legacy v3 save loads into v4 with equal envelope + empty S list. Add `BudgetAllocationService` + `IBudgetAllocator` glossary rows. Run `npm run validate:all`. |

---

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

### Stage 5 — Economy services: bond ledger + maintenance registry + Zone S placement / `IMaintenanceContributor` registry + deterministic iteration

**Status:** Final

**Objectives:** Refactor `EconomyManager.ProcessMonthlyMaintenance` from hardcoded formula to contributor-registry iteration. Existing roads + power plants preserved via adapter pattern (behavior-preserving). Registry iteration sorted by contributor id string for save-replay parity (N2 resolution). Contributors emit into owning sub-type envelope — underfunded envelope shows visual decay placeholder (Q3).

**Exit:**

- `IMaintenanceContributor` interface: `int GetMonthlyMaintenance()`, `string GetContributorId()`, `int GetSubTypeId()` (-1 = general maintenance pool).
- `EconomyManager.RegisterMaintenanceContributor(IMaintenanceContributor)` + `Unregister`.
- `ProcessMonthlyMaintenance` iterates registry sorted by `GetContributorId()` (deterministic).
- `RoadMaintenanceContributor` adapter preserves existing road maintenance formula bit-for-bit.
- `PowerPlantMaintenanceContributor` adapter confirmed present (utility line in monthly tick) and implemented.
- Glossary row: `IMaintenanceContributor`.
- Phase 1 — `IMaintenanceContributor` interface + `EconomyManager` registry plumbing.
- Phase 2 — Existing-contributor adapters + hardcoded-formula retirement + tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | `IMaintenanceContributor` interface + registry | **TECH-540** | Done (archived) | New `IMaintenanceContributor.cs` with `GetMonthlyMaintenance() → int`, `GetContributorId() → string`, `GetSubTypeId() → int`. Add `List<IMaintenanceContributor> maintenanceContributors` to `EconomyManager`. `RegisterMaintenanceContributor(c)` + `UnregisterMaintenanceContributor(c)` public methods. Registry reset on scene load / save restore. |
| T5.2 | Refactor `ProcessMonthlyMaintenance` to iterate registry | **TECH-541** | Done (archived) | In `EconomyManager.ProcessMonthlyMaintenance`: sort registry snapshot by `GetContributorId()` (stable, deterministic). For each contributor: if `GetSubTypeId() >= 0`, draw from `budgetAllocation.TryDraw(subTypeId, amount)` — failure flags decay on contributor (no penalty MVP, hook only). Else spend from general treasury via `treasuryFloorClamp.TrySpend`. Old hardcoded `roadCount × rate + powerPlantCount × rate` formula removed. |
| T5.3 | `RoadMaintenanceContributor` adapter | **TECH-542** | Done (archived) | New adapter class `RoadMaintenanceContributor` implementing `IMaintenanceContributor`. `GetMonthlyMaintenance()` returns the prior `roadCount × ratePerRoad` result (read from existing `RoadManager` or equivalent). `GetContributorId()` returns `"road-aggregate"`. `GetSubTypeId()` returns -1 (general pool). Register from `EconomyManager.Start` after dependencies wired. Result: month-to-month maintenance delta identical to pre-refactor. |
| T5.4 | Power-plant contributor audit + adapter | **TECH-543** | Done (archived) | Audit confirmed `ComputeMonthlyUtilityMaintenanceCost` present. `PowerPlantMaintenanceContributor` adapter added, contributor id `power-aggregate`, general pool. Decision Log updated. |
| T5.5 | EditMode tests + glossary | **TECH-544** | Done (archived) | `MaintenanceRegistryTests` under `Assets/Tests/EditMode/Economy/`. 7 test cases covering sorted iteration, register/unregister, clear, duplicates, projection sum, road parity, power plant parity. `IMaintenanceContributor` glossary row added. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-540"
  title: "`IMaintenanceContributor` interface + registry"
  priority: "high"
  notes: |
    New interface + contributor list on `EconomyManager` with register/unregister.
    Reset registry on load after Stage 4 bond maintenance context (`TECH-533`).
    Touches `EconomyManager`, new `IMaintenanceContributor.cs`.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Introduce `IMaintenanceContributor` and wire a deterministic registration surface on `EconomyManager`
      so monthly maintenance can move off a hardcoded formula in later tasks.
    goals: |
      - Add `IMaintenanceContributor` with `GetMonthlyMaintenance`, `GetContributorId`, `GetSubTypeId` (-1 = general pool).
      - Add `RegisterMaintenanceContributor` / `UnregisterMaintenanceContributor` on `EconomyManager` plus internal list storage.
      - Clear or rebuild contributor list on scene load and save restore (parity with prior maintenance baseline).
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/EconomyManager.cs`
      - New `Assets/Scripts/Managers/GameManagers/IMaintenanceContributor.cs`
      - `GameSaveManager` / load ordering if registry must snapshot with save (coordinate with Stage 4 save tasks).
    impl_plan_sketch: |
      Phase 1 — Add interface + fields + public register API. Phase 2 — hook reset on load paths used by bond/economy restore.

- reserved_id: "TECH-541"
  title: "Refactor `ProcessMonthlyMaintenance` to iterate registry"
  priority: "high"
  notes: |
    Replace road/power line item with sorted registry iteration. Sub-type path uses `budgetAllocation.TryDraw`; general pool uses treasury clamp.
    Depends on registry API from T5.1 (`TECH-533` chain prerequisite).
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Drive `ProcessMonthlyMaintenance` from a snapshot of contributors sorted by `GetContributorId()` for save/replay-stable ordering.
    goals: |
      - Snapshot and sort contributors by id string before spending.
      - Route `GetSubTypeId() >= 0` draws through `budgetAllocation.TryDraw` with decay hook on failure; else `treasuryFloorClamp.TrySpend`.
      - Remove legacy `roadCount × rate + powerPlantCount × rate` block once adapters land in T5.3–T5.4.
    systems_map: |
      - `EconomyManager.ProcessMonthlyMaintenance`
      - `BudgetAllocationService`, `TreasuryFloorClampService`
    impl_plan_sketch: |
      Phase 1 — Iterate sorted list. Phase 2 — delete hardcoded formula after contributor adapters match legacy totals.

- reserved_id: "TECH-542"
  title: "`RoadMaintenanceContributor` adapter"
  priority: "high"
  notes: |
    Preserve pre-refactor road maintenance math via adapter implementing `IMaintenanceContributor`.
    Register from `EconomyManager` after dependencies wired (`RoadManager` or equivalent).
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Encapsulate existing road maintenance calculation in `RoadMaintenanceContributor` so refactor stays behavior-identical month-to-month.
    goals: |
      - Implement `GetMonthlyMaintenance` using prior `roadCount × ratePerRoad` inputs.
      - Use `GetContributorId() == "road-aggregate"` and `GetSubTypeId() == -1`.
      - Register in `EconomyManager.Awake` after refs resolve.
    systems_map: |
      - `RoadManager` (or existing road count source)
      - `EconomyManager`
      - New `RoadMaintenanceContributor.cs`
    impl_plan_sketch: |
      Phase 1 — Adapter class + unit comparison against legacy formula. Phase 2 — register + EditMode guard in T5.5.

- reserved_id: "TECH-543"
  title: "Power-plant contributor audit + adapter"
  priority: "medium"
  notes: |
    Confirm whether power plants participate in monthly maintenance today. If yes, add `PowerPlantMaintenanceContributor`; if deferred, document Decision Log + skip implementation.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Audit `ProcessMonthlyMaintenance` for power-plant lines and either add a matching adapter or explicitly defer Bucket 4 with logged decision.
    goals: |
      - Grep/read current monthly tick for power plant charges.
      - If present: adapter analogous to roads with stable contributor id. If absent: Decision Log entry + no code churn.
    systems_map: |
      - `EconomyManager.ProcessMonthlyMaintenance`
      - Power / utility managers as referenced in codebase audit
    impl_plan_sketch: |
      Phase 1 — Audit + decision record. Phase 2 — implement adapter only if audit finds existing line item.

- reserved_id: "TECH-544"
  title: "EditMode tests + glossary"
  priority: "medium"
  notes: |
    `MaintenanceRegistryTests` covering sort order, envelope vs treasury paths, road parity regression, plus glossary row for `IMaintenanceContributor`.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Lock contributor registry behavior with tests and add canonical glossary entry for `IMaintenanceContributor`.
    goals: |
      - Deterministic iteration order test with three contributors registered out of order.
      - Sub-type vs general-pool spend paths exercised per Stage exit bullets.
      - Road cost before/after adapter parity assertion.
      - Glossary row + index regen where required by repo policy.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/MaintenanceRegistryTests.cs` (new)
      - `ia/specs/glossary.md`, MCP glossary indexes if touched
    impl_plan_sketch: |
      Phase 1 — Author tests with fakes/mocks for contributors. Phase 2 — glossary + `npm run validate:all` coordination.
```

### Stage 6 — Economy services: bond ledger + maintenance registry + Zone S placement / `ZoneSService` placement pipeline + `AutoZoningManager` guard

<!-- sizing-gate-waiver: H5+H6 WARN (GridManager + ZoneSService overlap across tasks); user /stage-file 2026-04-20 -->

**Status:** Final

**Objectives:** Land the single entry point for Zone S placement. `ZoneSService.PlaceStateServiceZone` wraps the envelope draw + `ZoneManager.PlaceZone` route + growth-pipeline contributor registration. Guard `AutoZoningManager` from placing S (manual-only in MVP). End of Step 2: exploration Examples 1 + 2 (place police OK, place fire blocked by envelope) reproducible as integration tests with no UI involvement.

**Exit:**

- `ZoneSService.cs` under `Assets/Scripts/Managers/GameManagers/`. MonoBehaviour. `[SerializeField]` refs to `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager`.
- `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool` implements Example 1 + 2 flow.
- Cell access goes through `GridManager.GetCell` (invariant #5).
- Growth pipeline hook: on S building spawn, `EconomyManager.RegisterMaintenanceContributor(StateServiceMaintenanceContributor)`.
- `AutoZoningManager` guard: early-return no-op when `IsStateServiceZone(targetType)` is true + comment: `// Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2`.
- Glossary row: `ZoneSService`.
- Integration tests: Example 1 (place succeeds, envelope + treasury decrement, building registers as contributor) + Example 2 (envelope exhausted blocks placement, no treasury mutation) green.
- Phase 1 — `ZoneSService` skeleton + manager refs wired.
- Phase 2 — `PlaceStateServiceZone` flow + growth-pipeline contributor registration.
- Phase 3 — `AutoZoningManager` guard + integration tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | `ZoneSService` skeleton class | **TECH-545** | Done (archived) | New `ZoneSService.cs` MonoBehaviour under `Assets/Scripts/Managers/GameManagers/`. `[SerializeField] private BudgetAllocationService budgetAllocation; [SerializeField] private TreasuryFloorClampService treasuryFloorClamp; [SerializeField] private ZoneSubTypeRegistry registry; [SerializeField] private ZoneManager zoneManager; [SerializeField] private GridManager grid;` — all with `FindObjectOfType` fallback in `Awake` (guardrail #1). Public API signatures declared (body empty / returns default). |
| T6.1.1 | Scene wiring + prefab attach | **TECH-546** | Done (archived) | Attach `ZoneSService` component to the `EconomyManager` GameObject (or dedicated GameManagers GO) in main scene prefab. Populate Inspector refs for all 5 serialized fields. Verify `FindObjectOfType` fallbacks still resolve when Inspector refs are cleared (loadable prefab safety). |
| T6.2 | `PlaceStateServiceZone` flow | **TECH-547** | Done (archived) | Implement `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool`: lookup `entry = registry.GetById(subTypeId)`; fail-fast on null. Fetch cell via `grid.GetCell(cell.x, cell.y)` (invariant #5). Call `budgetAllocation.TryDraw(subTypeId, entry.baseCost)` — on false, emit notification "insufficient {subType} envelope this month" and return false (Q4 block-before-deduct, no treasury mutation). On true, call `zoneManager.PlaceZone(cell, ZoneType.StateServiceLightZoning, subTypeId: subTypeId)` and return true. Sub-type id written to `Zone.subTypeId` sidecar inside `ZoneManager.PlaceZone` (extend signature). |
| T6.3 | `StateServiceMaintenanceContributor` component | **TECH-548** | Done (archived) | New `StateServiceMaintenanceContributor` MonoBehaviour attached to S building prefabs. Implements `IMaintenanceContributor`: `GetMonthlyMaintenance()` reads `registry.GetById(subTypeId).monthlyUpkeep`; `GetContributorId()` returns `$"s-{subTypeId}-{instanceId}"`; `GetSubTypeId()` returns `subTypeId`. `Start()` calls `economyManager.RegisterMaintenanceContributor(this)`; `OnDestroy()` calls `Unregister`. |
| T6.4 | Growth-pipeline hook for S buildings | **TECH-549** | Done (archived) | Inside existing building-spawn pipeline (grep `PlaceZone` → growth callback or `BuildingSpawner` equivalent), attach `StateServiceMaintenanceContributor` component on spawned S buildings. Read `Zone.subTypeId` from the owning cell + pass to the component. Guard: only applies when `IsStateServiceZone(cell.zoneType)`. No change for R/C/I pipelines. |
| T6.5 | `AutoZoningManager` S no-op guard | **TECH-550** | Done (archived) | Grep `AutoZoningManager.cs` for the zone-selection switch. Add early-return: `if (EconomyManager.IsStateServiceZone(candidate)) { /* Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2 */ continue; }`. Covers Review Note N4 (path reference in comment). No behavioral change for RCI. |
| T6.6 | Integration tests — Examples 1 + 2 | **TECH-551** | Done (archived) | `ZoneSServicePlacementTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode if grid scene needed). Example 1: envelope=1200, treasury=10000, `PlaceStateServiceZone(cell, POLICE(baseCost=500))` → returns true, envelope=700, treasury=9500, zone at cell is `StateServiceLightZoning` with `subTypeId=POLICE`. Example 2: envelope=200, baseCost=600 → returns false, envelope UNCHANGED (200), treasury UNCHANGED, zone at cell NOT placed. Add `ZoneSService` glossary row + MCP reindex. Run `npm run validate:all`. |

### §Plan Fix — resolved (2026-04-20)

> plan-review found 1 drift: Stage 6 **Exit** first bullet listed `EconomyManager` on `ZoneSService`; T6.1 + TECH-545 specify five refs only. Contributor path uses `EconomyManager` via `StateServiceMaintenanceContributor` (TECH-548/549), not on `ZoneSService`. **Exit** bullet updated in-repo to match. Original tuple had invalid anchor `L498` — repaired to full-line `target_anchor` before apply. No pending `plan-fix-apply` for this item.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-545"
  title: "`ZoneSService` skeleton class"
  priority: "high"
  notes: |
    New `ZoneSService` MonoBehaviour with serialized refs to budget, treasury clamp, registry, zone manager, grid.
    `FindObjectOfType` fallbacks in `Awake`. Public method stubs only — behavior lands in TECH-547.
  depends_on: []
  related: ["TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Introduce `ZoneSService` shell with guardrail wiring so later tasks can implement `PlaceStateServiceZone` without scene churn.
    goals: |
      - Add `ZoneSService.cs` under `Assets/Scripts/Managers/GameManagers/`.
      - Serialize `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager` with `Awake` fallbacks.
      - Declare `PlaceStateServiceZone` (or equivalent) signatures; bodies empty / default returns until TECH-547.
    systems_map: |
      - New `ZoneSService.cs`
      - `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager`
    impl_plan_sketch: |
      Phase 1 — New class + serialized fields + `Awake` resolution. Phase 2 — stub public API for placement (no logic).

- reserved_id: "TECH-546"
  title: "Scene wiring + prefab attach"
  priority: "high"
  notes: |
    Attach `ZoneSService` to Economy / GameManagers GO in main scene prefab; fill Inspector refs; verify fallbacks when refs cleared.
  depends_on: []
  related: ["TECH-545", "TECH-547", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Wire `ZoneSService` into shipped scene so runtime and tests resolve component without manual scene edits.
    goals: |
      - Add component to `EconomyManager` GameObject or dedicated GameManagers root in main scene prefab.
      - Populate all five serialized references in Inspector.
      - Document or verify `FindObjectOfType` path when Inspector fields empty (load safety).
    systems_map: |
      - Main scene / prefab under `Assets/` (exact path per repo convention)
      - `ZoneSService` component
    impl_plan_sketch: |
      Phase 1 — Prefab/scene edit. Phase 2 — smoke: enter play mode or EditMode load if applicable.

- reserved_id: "TECH-547"
  title: "`PlaceStateServiceZone` flow"
  priority: "high"
  notes: |
    Implement placement: registry lookup, `GridManager.GetCell`, `budgetAllocation.TryDraw`, `ZoneManager.PlaceZone` with `subTypeId` param.
    Example 1–2 semantics: block before deduct when draw fails; extend `PlaceZone` signature for sidecar write.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Implement manual Zone S placement through envelope draw then zone placement; honor invariant #5 (cell access via `GridManager`).
    goals: |
      - `PlaceStateServiceZone(GridCoord cell, int subTypeId)` returns false on bad registry id or failed `TryDraw` with notification.
      - On success call `ZoneManager.PlaceZone` with correct `ZoneType` and `subTypeId`; `Zone.subTypeId` set inside `PlaceZone`.
      - No treasury mutation when envelope draw fails (Q4).
    systems_map: |
      - `ZoneSService.cs`
      - `ZoneManager`, `BudgetAllocationService`, `ZoneSubTypeRegistry`, `GridManager`
      - `Zone.cs` sidecar if signature threading required
    impl_plan_sketch: |
      Phase 1 — Implement draw + place happy path. Phase 2 — failure paths + notifications + `PlaceZone` signature extension.

- reserved_id: "TECH-548"
  title: "`StateServiceMaintenanceContributor` component"
  priority: "medium"
  notes: |
    MonoBehaviour on S buildings implementing `IMaintenanceContributor`; register in `Start`, unregister in `OnDestroy`; id scheme `s-{subTypeId}-{instanceId}`.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Per-building maintenance contributor for Zone S using registry monthly upkeep and stable contributor id.
    goals: |
      - New `StateServiceMaintenanceContributor` under GameManagers (or appropriate folder).
      - Implement interface methods; `GetSubTypeId()` returns configured sub-type; upkeep from `ZoneSubTypeRegistry`.
      - Register with `EconomyManager` on `Start`; unregister on destroy.
    systems_map: |
      - New contributor class
      - `IMaintenanceContributor`, `EconomyManager`, `ZoneSubTypeRegistry`
    impl_plan_sketch: |
      Phase 1 — Component + interface impl. Phase 2 — wire subTypeId source (serialized or injected).

- reserved_id: "TECH-549"
  title: "Growth-pipeline hook for S buildings"
  priority: "medium"
  notes: |
    In building spawn path after `PlaceZone`, attach contributor when `IsStateServiceZone`; read `Zone.subTypeId` from cell; R/C/I unchanged.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Ensure spawned S buildings register maintenance contributor automatically from growth/build pipeline.
    goals: |
      - Locate spawn callback chain (grep `PlaceZone` / spawner).
      - Add `StateServiceMaintenanceContributor` only for state-service zone types.
      - Pass through `subTypeId` from zone sidecar.
    systems_map: |
      - Building spawn / growth pipeline scripts (paths from codebase search)
      - `StateServiceMaintenanceContributor`, `EconomyManager.IsStateServiceZone`
    impl_plan_sketch: |
      Phase 1 — Trace spawn hook. Phase 2 — attach component + configure fields.

- reserved_id: "TECH-550"
  title: "`AutoZoningManager` S no-op guard"
  priority: "medium"
  notes: |
    Early-return / continue when candidate type is state-service zone; comment cites exploration §Q2; RCI behavior unchanged.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-551"]
  stub_body:
    summary: |
      Block AUTO zoning from placing Zone S; manual-only MVP per locked design.
    goals: |
      - Find zone-selection loop in `AutoZoningManager`.
      - Skip state-service zone types with documented comment referencing `docs/zone-s-economy-exploration.md` §Q2.
      - No change to R/C/I AUTO paths.
    systems_map: |
      - `AutoZoningManager.cs`
      - `EconomyManager.IsStateServiceZone`
    impl_plan_sketch: |
      Phase 1 — Grep + insert guard. Phase 2 — regression sanity on AUTO RCI.

- reserved_id: "TECH-551"
  title: "Integration tests — Examples 1 + 2"
  priority: "medium"
  notes: |
    `ZoneSServicePlacementTests` EditMode (or PlayMode if scene required). Assert envelope + treasury + placement per Stage Exit; glossary `ZoneSService`; validate:all.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-550"]
  stub_body:
    summary: |
      Lock Examples 1 and 2 from exploration with automated tests and canonical glossary entry for `ZoneSService`.
    goals: |
      - Example 1: successful place decrements envelope and treasury; zone + subTypeId correct.
      - Example 2: insufficient envelope leaves balances and grid unchanged.
      - Add glossary row + index regen per repo policy; `npm run validate:all` green.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/ZoneSServicePlacementTests.cs` (new)
      - `ia/specs/glossary.md`, index regen scripts
      - `ZoneSService` + test doubles / scene setup
    impl_plan_sketch: |
      Phase 1 — Test harness + Example 1. Phase 2 — Example 2 + glossary + validators.
```

---

### Stage 7 — UI surfaces + CityStats integration + economy-system reference spec / Toolbar + sub-type picker + budget panel

**Status:** Final

**Objectives:** Player can click S button, pick a sub-type, place a building, tune envelope sliders. End-to-end flow with visible feedback on envelope draws + overspend blocks.

**Exit:**

- S zoning button visible in toolbar, correct icon, enters S placement mode on click.
- Sub-type picker opens over placement mode; 7 buttons; click commits sub-type + resumes cursor placement.
- Picker cancel (ESC or outside-click) closes picker without cost or placement (N3).
- Budget panel reachable from HUD; 7 sliders sum-locked + commit normalizes to 1.0; global cap slider + remaining readouts live-update.
- Overspend-blocked notification visible when `TryDraw` returns false.
- Phase 1 — S zoning button + placement mode entry.
- Phase 2 — Sub-type picker modal.
- Phase 3 — Budget panel with envelope sliders + global cap.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | S zoning button in `UIManager.ToolbarChrome` | **TECH-553** | Done (archived) | Add 4th button to zoning cluster in `UIManager.ToolbarChrome.cs` alongside R/C/I. Icon: placeholder "S" glyph. Click handler sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` + opens sub-type picker (next task). Toolbar layout respects existing theme spacing. |
| T7.2 | Placement-mode routing through `ZoneSService` | **TECH-554** | Done (archived) | When S placement active + user clicks on grid cell, route through `ZoneSService.PlaceStateServiceZone(cell, currentSubTypeId)` instead of direct `ZoneManager.PlaceZone`. `currentSubTypeId` carried in transient placement state (set by picker). Guard: if `currentSubTypeId < 0`, reopen picker. |
| T7.3 | Sub-type picker modal UI | **TECH-555** | Done (archived) | New `SubTypePickerModal.cs` under `Assets/Scripts/Managers/GameManagers/UI/` (or existing UI dir). Uses `UIManager.PopupStack` to present 7 buttons (icon + displayName + baseCost) sourced from `ZoneSubTypeRegistry`. Click commits `currentSubTypeId` + closes modal + signals placement mode ready. |
| T7.4 | Picker cancel UX (N3) | **TECH-556** | Done (archived) | ESC key or outside-click dismisses picker with no cost + no placement + `currentSubTypeId = -1` + exits placement mode. Documented in `SubTypePickerModal` XML docs referencing Review Note N3. |
| T7.5 | Budget panel UI with sliders | **TECH-557** | Done (archived) | New `BudgetPanel.cs` + Unity UI prefab. 7 horizontal sliders (one per sub-type, labeled from `ZoneSubTypeRegistry`), 1 global cap slider, 7 remaining-this-month readouts. Open via HUD button (add to `UIManager.Hud`). Slider commit calls `budgetAllocation.SetEnvelopePct(i, pct)` which auto-normalizes; UI re-reads values post-normalize so sliders reflect actual stored state. |
| T7.6 | Overspend-blocked notification wiring | **TECH-558** | Done (archived) | Hook `GameNotificationManager` event raised by `BudgetAllocationService.TryDraw` failure. Display a transient HUD badge: "{sub-type} envelope exhausted" for 3s. Matches Example 2 user-facing feedback. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-553"
  title: "S zoning button in `UIManager.ToolbarChrome`"
  priority: "medium"
  notes: |
    Fourth toolbar zoning control for **Zone S**; enters placement + opens sub-type picker. Touches `UIManager.ToolbarChrome`, theme spacing, `ZoneManager.activeZoneType`.
  depends_on: []
  related: ["TECH-554", "TECH-555", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Add **StateServiceLightZoning** (S) button to zoning cluster in `UIManager.ToolbarChrome`; click sets active zone type and opens sub-type picker path for next task.
    goals: |
      - Insert 4th button beside R/C/I with placeholder S glyph and existing theme spacing.
      - Click sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` and triggers picker flow (wired when T7.3 lands).
      - No regression to R/C/I toolbar behavior.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/UI/UIManager.ToolbarChrome.cs` (or path from repo)
      - `ZoneManager`, `ZoneType`, `UiTheme` / toolbar layout
    impl_plan_sketch: |
      Phase 1 — Locate zoning cluster + duplicate pattern for S. Phase 2 — Wire click → zone type + picker hook.

- reserved_id: "TECH-554"
  title: "Placement-mode routing through `ZoneSService`"
  priority: "medium"
  notes: |
    S placement clicks must use `ZoneSService.PlaceStateServiceZone(cell, subTypeId)` not raw `PlaceZone`. Transient `currentSubTypeId` from picker; guard reopen if -1.
  depends_on: []
  related: ["TECH-553", "TECH-555", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Route grid clicks during S placement through **ZoneSService** with committed sub-type id; invalid id reopens picker.
    goals: |
      - Hold `currentSubTypeId` in placement state (setter from picker).
      - On cell click: if id < 0 reopen picker; else call `PlaceStateServiceZone`.
      - Keep R/C/I placement paths unchanged.
    systems_map: |
      - `ZoneSService`, `ZoneManager`, placement-mode controller / input path
    impl_plan_sketch: |
      Phase 1 — Trace current zone placement click handler. Phase 2 — Branch for StateService zone type + service call.

- reserved_id: "TECH-555"
  title: "Sub-type picker modal UI"
  priority: "medium"
  notes: |
    New **SubTypePickerModal** on `UIManager.PopupStack`; 7 buttons from `ZoneSubTypeRegistry`; commit id + close + resume placement.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Modal lists seven **Zone S** sub-types with icon, name, base cost; selection commits `currentSubTypeId` and closes.
    goals: |
      - Implement `SubTypePickerModal` under GameManagers UI folder.
      - Populate buttons via registry; click sets id and dismisses.
      - Integrate with toolbar S button entry and placement state.
    systems_map: |
      - `ZoneSubTypeRegistry`, `UIManager.PopupStack`, new `SubTypePickerModal.cs`
    impl_plan_sketch: |
      Phase 1 — Modal shell + stack push. Phase 2 — Bind registry + selection callback.

- reserved_id: "TECH-556"
  title: "Picker cancel UX (N3)"
  priority: "medium"
  notes: |
    ESC + outside-click dismiss picker without spend; `currentSubTypeId = -1`; exit placement per Review Note N3.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Cancel paths clear sub-type selection and placement without charging player (exploration N3).
    goals: |
      - ESC closes modal and resets id + exits S placement mode.
      - Outside-click same behavior.
      - XML docs reference N3 on `SubTypePickerModal`.
    systems_map: |
      - `SubTypePickerModal`, input / graphic raycast for backdrop
    impl_plan_sketch: |
      Phase 1 — ESC + backdrop handlers. Phase 2 — Reset placement state via `ZoneManager` / coordinator.

- reserved_id: "TECH-557"
  title: "Budget panel UI with sliders"
  priority: "medium"
  notes: |
    **BudgetPanel** + prefab: 7 envelope sliders, global cap, remaining readouts; HUD open; commits via `budgetAllocation.SetEnvelopePct`; UI refresh post-normalize.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-556", "TECH-558"]
  stub_body:
    summary: |
      HUD-driven panel edits **envelope** percentages with sum-lock and global cap; reflects allocator state after normalize.
    goals: |
      - Add `BudgetPanel.cs` + prefab; wire open from `UIManager.Hud`.
      - Seven sliders + cap + monthly remaining labels from registry / allocator.
      - On commit call `SetEnvelopePct`; re-read model to sync sliders.
    systems_map: |
      - `BudgetAllocationService` / `IBudgetAllocator`, `ZoneSubTypeRegistry`, `UIManager.Hud`
    impl_plan_sketch: |
      Phase 1 — Layout + bind sliders. Phase 2 — Allocator round-trip + live readouts.

- reserved_id: "TECH-558"
  title: "Overspend-blocked notification wiring"
  priority: "medium"
  notes: |
    Surface `TryDraw` failure via **GameNotificationManager**; transient HUD badge ~3s; aligns Example 2 feedback.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-556", "TECH-557"]
  stub_body:
    summary: |
      When envelope draw fails, show short HUD message naming exhausted sub-type (Example 2).
    goals: |
      - Subscribe or hook notification raised from `BudgetAllocationService.TryDraw` false path.
      - Display badge text `"{displayName} envelope exhausted"` ~3s.
      - Match existing notification / HUD styling.
    systems_map: |
      - `GameNotificationManager`, `BudgetAllocationService`, HUD strip
    impl_plan_sketch: |
      Phase 1 — Event or callback from TryDraw failure. Phase 2 — HUD presenter + timer.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

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
| T8.1 | Bond issuance modal UI | **TECH-565** | Draft | New `BondIssuanceModal.cs` + Unity UI prefab via `UIManager.PopupStack`. Fields: principal `InputField` (int, min 100), term radio (12/24/48), live preview Text computing `(principal × 1.12) / termMonths`. Issue button calls `bondLedger.TryIssueBond(scaleTier: city, principal, termMonths)`. Disabled if `bondLedger.GetActiveBond(city) != null`. |
| T8.2 | Bond-active HUD flag + entry point | **TECH-566** | Draft | HUD badge showing "Active bond: {remainingMonths} mo, {monthlyRepayment}/mo" when active bond exists (city tier MVP). Click opens bond detail view (reuses `BondIssuanceModal` in read-only mode, showing current bond + disabled issue button). Arrears state shows red badge. |
| T8.3 | `CityStats` envelope + bond fields | **TECH-567** | Draft | Add fields to `CityStats.cs`: `int totalEnvelopeCap`, `int[] envelopeRemaining` (len 7), `int activeBondDebt`, `int monthlyBondRepayment`. Populate each tick from `budgetAllocation` + `bondLedger`. `CityStatsUIController` displays new fields in stats panel (label + value). |
| T8.4 | HUD income-minus-maintenance hint update | **TECH-568** | Draft | Update HUD projected-income-minus-maintenance readout in `UIManager.Hud` (or the existing formula site) to subtract `cityStats.totalEnvelopeCap` from the projected monthly surplus. Label text updated to "Est. monthly surplus (after S envelope + bond repayment)". |
| T8.5 | `MiniMapController` S palette | **TECH-569** | Draft | Extend color lookup in `MiniMapController.cs`: new case for each of 6 new `ZoneType` values returning a single S color (e.g. purple). N5 locks: no per-sub-type color split in MVP. RCI colors unchanged. |
| T8.6 | Integration test — Example 3 end-to-end | **TECH-570** | Draft | `BondIssuanceIntegrationTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode). Reproduces Example 3: treasury=1200, `TryIssueBond(city, 5000, 24)` → returns true, treasury=6200, registry has entry with `monthlyRepayment=233`. Month tick triggers `ProcessMonthlyRepayment` → treasury=5967, `monthsRemaining=23`. Save/load round-trip preserves bond state. |

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

### Stage 9 — UI surfaces + CityStats integration + economy-system reference spec / `economy-system.md` reference spec + closeout alignment

**Status:** Done

**Objectives:** Author the new `ia/specs/economy-system.md` reference spec (invariant #12 — covers permanent domain). Glossary rows authored in Steps 1/2 get proper authoritative spec cross-refs. Run full validation + confirm umbrella rollout tracker row ready to advance to column (f) "≥1 task filed" on first `/stage-file` call. Final stage — nothing player-visible added, but spec + docs + alignment land.

**Exit:**

- `ia/specs/economy-system.md` authored with sections: Overview, Zone S (enum + sub-type registry + placement pipeline), Budget envelope (`IBudgetAllocator` contract + `TryDraw` semantics + monthly reset), Treasury floor clamp (hard cap), Bond ledger (`IBondLedger` contract + single-concurrent rule + arrears state), Maintenance contributor registry (`IMaintenanceContributor` + deterministic iteration), Save schema v3→v4 migration, Glossary cross-refs.
- All glossary rows added in Steps 1/2 re-point to `ia/specs/economy-system.md` sections (replace placeholder exploration-doc links).
- `ia/rules/agent-router.md` table gets new row(s) for economy / Zone S domain → `economy-system.md` sections.
- `tools/mcp-ia-server/data/spec-index.json` regenerated (captures new spec).
- `npm run validate:all` green.
- Umbrella rollout tracker (`ia/projects/full-game-mvp-rollout-tracker.md`) Bucket 3 row columns (a)–(e) verified complete; column (g) align gate closed.
- Phase 1 — Author `economy-system.md` + glossary repointing + router table update.
- Phase 2 — Index regen + full validation + umbrella alignment.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Author `ia/specs/economy-system.md` | **TECH-593** | Done | New reference spec under `ia/specs/economy-system.md`. Sections per stage Exit list. Follows existing spec authoring conventions (frontmatter, ToC, glossary cross-refs). Cross-references `persistence-system.md` (save) + `managers-reference.md` (Zones). Caveman prose per `ia/rules/agent-output-caveman.md` §authoring. |
| T9.2 | Repoint glossary rows to new spec | **TECH-594** | Done | Update 10 glossary rows added in Steps 1/2 (`Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget)`) — replace exploration-doc placeholder links with `ia/specs/economy-system.md#{anchor}` links. Preserves cross-link integrity. |
| T9.3 | Router-table row for economy domain | **TECH-595** | Done | Update `ia/rules/agent-router.md` routing table: add row(s) mapping task-domain keywords ("zone s", "economy", "budget", "bond", "maintenance") to `economy-system.md` sections. Ensures MCP `router_for_task` dispatches correctly in future agent sessions. |
| T9.4 | Index regen + `validate:all` | **TECH-596** | Done | Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/spec-index.json` + `glossary-index.json` + `glossary-graph-index.json`. Run `npm run validate:all`; fix any frontmatter / dead-link issues. Confirm MCP tests pass (`tools/mcp-ia-server/tests`). |
| T9.5 | Umbrella rollout-tracker alignment check | **TECH-597** | Done | Read `ia/projects/full-game-mvp-rollout-tracker.md` Bucket 3 row. Verify columns (a)–(e) marked complete (design-explore → master-plan → stage-file → project-spec-kickoff → glossary rows landed). Verify column (g) align gate closed (spec + router + glossary all pointing to `economy-system.md`). Do NOT tick column (f) — that's `/stage-file` authoring, not this closeout stage. Document state in closeout notes. |

<!-- sizing-gate-waiver: Stage 9 IA-only (spec + glossary + router + index + tracker); multi-subsystem doc touch expected; accepted -->

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Author `ia/specs/economy-system.md`"
  priority: medium
  notes: |
    New **reference spec** under `ia/specs/economy-system.md` per Stage 9 Exit: Zone S, **BudgetAllocationService** / **IBudgetAllocator**, **TreasuryFloorClampService**, **BondLedgerService** / **IBondLedger**, **IMaintenanceContributor**, save v3→v4, glossary-style cross-refs to **persistence-system** + **managers-reference**. Caveman authoring per `agent-output-caveman.md`.
  depends_on: []
  related:
    - "TECH-594"
    - "TECH-595"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Land authoritative **economy-system** reference spec: Zone S enums, sub-type registry, placement pipeline, budget envelope semantics, treasury floor, bond ledger contracts, maintenance contributors, save migration notes — aligns glossary rows from prior stages.
    goals: |
      - `ia/specs/economy-system.md` exists with Overview + sections listed in Stage 9 Exit (Zone S, budget, bonds, maintenance, save).
      - Cross-refs to **persistence-system**, **managers-reference**, **isometric-geography** where relevant; no orphan anchors.
      - Frontmatter + ToC match existing `ia/specs/` conventions.
    systems_map: |
      - `ia/specs/economy-system.md` (new), `ia/specs/glossary.md`, `ia/specs/persistence-system.md`, `ia/specs/managers-reference.md`
    impl_plan_sketch: |
      Phase 1 — Outline sections from Stage Exit checklist. Phase 2 — Fill domain text + cross-refs + validate links locally.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Repoint glossary rows to new spec"
  priority: medium
  notes: |
    Update 10 glossary rows (**Zone S**, **BudgetAllocationService**, **BondLedgerService**, **TreasuryFloorClampService**, **ZoneSService**, **IMaintenanceContributor**, **ZoneSubTypeRegistry**, **IBudgetAllocator**, **IBondLedger**, **envelope (budget)**): replace exploration-doc placeholders with `ia/specs/economy-system.md#{anchor}` links; preserve table integrity.
  depends_on:
    - "TECH-593"
  related:
    - "TECH-593"
    - "TECH-595"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Point economy-related glossary rows at **economy-system** spec sections so MCP + humans resolve authoritative definitions.
    goals: |
      - Each listed row links to correct `economy-system.md` anchor; no stale exploration-only URLs.
      - Glossary table formatting unchanged; `npm run validate:all` glossary-index path green after edits.
    systems_map: |
      - `ia/specs/glossary.md`, `ia/specs/economy-system.md`
    impl_plan_sketch: |
      Phase 1 — Map row → section anchor. Phase 2 — Edit glossary + spot-check `glossary-index` / dead links.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Router-table row for economy domain"
  priority: medium
  notes: |
    Extend `ia/rules/agent-router.md` Task → Spec routing: keywords (zone s, economy, budget, bond, maintenance) → `economy-system.md` section slices. Keeps **router_for_task** + agent-router table aligned.
  depends_on:
    - "TECH-593"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Add router rows so IA routing sends economy/Zone S work to **economy-system** spec slices (MCP + human agent-router).
    goals: |
      - New table row(s) with keywords + target spec sections.
      - No duplicate or conflicting routes vs existing geography/zones rows.
    systems_map: |
      - `ia/rules/agent-router.md`, `ia/specs/economy-system.md`
    impl_plan_sketch: |
      Phase 1 — Keyword list from Stage Objectives. Phase 2 — Insert rows + verify `router_for_task` doc alignment.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Index regen + `validate:all`"
  priority: medium
  notes: |
    Run `npm run mcp-ia-index` (regen `spec-index.json`, glossary indexes). Run `npm run validate:all`; fix frontmatter/dead-link issues; confirm `tools/mcp-ia-server/tests` pass.
  depends_on:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
    - "TECH-597"
  stub_body:
    summary: |
      Regenerate MCP IA indexes after spec + glossary + router land; full repo validation green.
    goals: |
      - `tools/mcp-ia-server/data/spec-index.json` + glossary indexes updated.
      - `validate:all` exit 0; MCP package tests pass.
    systems_map: |
      - `tools/mcp-ia-server/`, `package.json` scripts, `ia/specs/`
    impl_plan_sketch: |
      Phase 1 — `npm run mcp-ia-index`. Phase 2 — `validate:all` + fix any IA drift.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Umbrella rollout-tracker alignment check"
  priority: medium
  notes: |
    Verify `ia/projects/full-game-mvp-rollout-tracker.md` Bucket 3 row: columns (a)–(e) complete; column (g) align gate closed. Document findings in spec closeout notes; do not tick column (f) here.
  depends_on:
    - "TECH-596"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
    - "TECH-596"
  stub_body:
    summary: |
      Close-the-loop check vs umbrella rollout tracker: Bucket 3 alignment before program calls column (f) done elsewhere.
    goals: |
      - Tracker row state documented; mismatches filed or noted for umbrella owner.
      - Explicit note that column (f) tick is out of scope for this task.
    systems_map: |
      - `ia/projects/full-game-mvp-rollout-tracker.md`, `ia/projects/zone-s-economy-master-plan.md`
    impl_plan_sketch: |
      Phase 1 — Read tracker + compare to repo. Phase 2 — Write findings into Task spec §Verification / Decision Log as needed.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/zone-s-economy-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/zone-s-economy-exploration.md` §Design Expansion.
- Keep this orchestrator synced with umbrella `ia/projects/full-game-mvp-master-plan.md` + rollout tracker per `project-spec-close` / `closeout` umbrella-sync rule.
- Land the 10 glossary rows inside Stages 1.1 / 1.2 / 1.3 / 2.1 / 2.2 / 2.3 / 3.3 (distributed per stage) BEFORE umbrella column (e) can tick.
- Author `ia/specs/economy-system.md` in Stage 3.3 BEFORE umbrella column (g) align gate can close.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (RCI service coverage, desirability, cross-scale transfer, bond market, bond rating, interest tiering, S tax revenue, per-sub-type mini-map color split, compounding arrears) into MVP stages — they belong in a future `docs/zone-s-economy-post-mvp-extensions.md` doc.
- Merge partial stage state — every stage must land on a green bar (`unity:compile-check` + `validate:all`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Skip the v3→v4 save-migration branch — legacy v3 saves must round-trip cleanly.
- Let any spend path bypass `TreasuryFloorClampService.TrySpend` — systemic floor clamp, not opt-in. Any direct `currentMoney -=` outside the service = invariant violation.

---
