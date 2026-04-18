# Zone S + Economy — Master Plan (MVP)

> **Last updated:** 2026-04-17
>
> **Status:** In Progress — Step 1 / Stage 1.3
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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema

**Status:** In Progress — Stage 1.3 (TECH-418..421 Done (archived); TECH-422..425 Draft)

**Backlog state (Step 1):** 19 filed (Stage 1.1 — TECH-278..283 Done (archived); Stage 1.2 — TECH-379..383 Done (archived); Stage 1.3 — TECH-418..421 Done (archived); TECH-422..425 Draft)

**Objectives:** Land the structural primitives Zone S depends on. Extend `ZoneType` enum + add `ZoneSubTypeRegistry` SO catalogue. Introduce `TreasuryFloorClampService` so balance NEVER goes negative across ALL spend call sites (systemic, not opt-in). Stand up `BudgetAllocationService` with 7-envelope + global cap + `TryDraw` that checks BOTH envelope remaining AND treasury floor before deducting. Bump save schema v3→v4 with default-equal envelope migration. Nothing player-visible yet; foundations only.

**Exit criteria:**

- 6 new `ZoneType` enum values (`StateServiceLight/Medium/Heavy` × `Building`/`Zoning`) in `Assets/Scripts/Managers/UnitManagers/Zone.cs` + `IsStateServiceZone` helper on `EconomyManager`.
- `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) + `Assets/Resources/Economy/zone-sub-types.json` with 7 entries (police, fire, education, health, parks, public housing, public offices) — fields: `id`, `displayName`, `prefabPath`, `baseCost`, `monthlyUpkeep`, `iconPath`. Human/agent edits costs directly in the JSON; no Unity Editor required.
- `Zone` component carries `subTypeId` int field (default -1 for RCI).
- `TreasuryFloorClampService` under `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs` — MonoBehaviour, Inspector-wired on `EconomyManager` GO. `TrySpend(int, string) → bool` floor-clamps at 0; existing `SpendMoney` delegates through it.
- `BudgetAllocationService` under `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs` implementing `IBudgetAllocator`. `TryDraw`, `GetMonthlyEnvelope`, `SetEnvelopePct` (auto-normalize), `MonthlyReset`.
- `GameSaveData.CurrentSchemaVersion = 4` with `budgetAllocation` + `stateServiceZones` fields. `MigrateLoadedSaveData` v3→v4 seeds equal 14.28% × 7 envelope + empty S zone list.
- Glossary rows land for `Zone S`, `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `envelope (budget sense)`.
- EditMode tests: `TryDraw` blocks when envelope exhausted, blocks when treasury empty, succeeds when both OK; `TrySpend` blocks when `amount > balance` (no mutation); `MigrateLoadedSaveData` round-trip v3 → v4 preserves existing data + adds defaults.
- `npm run unity:compile-check` green. `npm run validate:all` green.

**Art:** None. Code + SOs + tests only.

**Relevant surfaces (load when step opens):**
- `docs/zone-s-economy-exploration.md` §Architecture, §Implementation Points IP-1 / IP-2 / IP-3 / IP-7.
- `Assets/Scripts/Managers/UnitManagers/Zone.cs` — `ZoneType` enum extension site.
- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — helper wiring + delegation.
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `GameSaveData` class + `MigrateLoadedSaveData` (§277+, `CurrentSchemaVersion` line 404).
- `Assets/Resources/Economy/zone-sub-types.json` (new config file for `ZoneSubTypeRegistry`).
- `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs` *(new)*.
- `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs` *(new)*.
- `Assets/Tests/EditMode/Economy/` *(new test asmdef dir)*.
- `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons — helper services are MonoBehaviour Inspector-wired), #6 (helper carve-out pattern).
- `ia/specs/persistence-system.md` §Save (save-migration pattern ref).
- `ia/specs/managers-reference.md` §Zones (`ZoneType` extension ref).

#### Stage 1.1 — ZoneType enum extension + sub-type registry

**Status:** Final

**Backlog state (Stage 1.1):** 6 filed (TECH-278..283 all Done (archived))

**Objectives:** Extend `ZoneType` with 6 new enum values (3 densities × Building/Zoning). Author `ZoneSubTypeRegistry` SO with 7 entries. Add `subTypeId` sidecar to `Zone` component. Land glossary rows for the new domain vocabulary (Zone S, ZoneSubTypeRegistry, envelope). Scaffolding only — no runtime logic consumes the new values yet.

**Exit:**

- 6 new `ZoneType` enum values added + existing consumers (`IsBuildingZone`, `IsZoningType`) updated + new `IsStateServiceZone` predicate on `EconomyManager`.
- `Zone.subTypeId` field (default -1) + serializable.
- `ZoneSubTypeRegistry` MonoBehaviour + `Assets/Resources/Economy/zone-sub-types.json` with 7 seeded entries.
- Glossary rows added: `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)`.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — `ZoneType` enum + predicates + `Zone.subTypeId` sidecar.
- [ ] Phase 2 — `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) + `zone-sub-types.json` config.
- [ ] Phase 3 — Glossary + spec-index refresh.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Extend `ZoneType` enum + predicates | 1 | **TECH-278** | Done (archived) | Add `StateServiceLightBuilding`, `StateServiceMediumBuilding`, `StateServiceHeavyBuilding`, `StateServiceLightZoning`, `StateServiceMediumZoning`, `StateServiceHeavyZoning` to `Zone.ZoneType` enum in `Assets/Scripts/Managers/UnitManagers/Zone.cs`. Extend `EconomyManager.IsBuildingZone` + `IsZoningType` to include the new values. Add `IsStateServiceZone(Zone.ZoneType)` predicate on `EconomyManager`. No caller change yet. |
| T1.1.2 | Add `Zone.subTypeId` sidecar field | 1 | **TECH-279** | Done (archived) | Add `[SerializeField] private int subTypeId = -1;` to `Zone.cs` + public getter/setter. Default -1 means "RCI, no sub-type". Persists via existing Unity serialization — no save plumbing yet (save bump lands in Stage 1.3). |
| T1.1.3 | Author `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) | 2 | **TECH-280** | Done (archived) | `ZoneSubTypeRegistry : MonoBehaviour` at `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs`. Loads `Assets/Resources/Economy/zone-sub-types.json` in `Awake` via `Resources.Load<TextAsset>` + `JsonUtility`. Entry fields: `int id`, `string displayName`, `string prefabPath`, `string iconPath`, `int baseCost`, `int monthlyUpkeep`. `GetById(int)` lookup. `LoadFromJson()` public for tests. |
| T1.1.4 | Seed `zone-sub-types.json` config file | 2 | **TECH-281** | Done (archived) | Author `Assets/Resources/Economy/zone-sub-types.json` with 7 entries (police=0…public offices=6). `prefabPath` + `iconPath` empty strings (art deferred). `baseCost` + `monthlyUpkeep` per exploration §IP-1. Human/agent edits costs directly in JSON; no Unity Editor required. |
| T1.1.5 | Glossary + spec-index refresh | 3 | **TECH-282** | Done (archived) | Add rows to `ia/specs/glossary.md` — `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)` — each with definition + authoritative spec link (points at forthcoming `ia/specs/economy-system.md`, cross-refs exploration doc for now). Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json`. |
| T1.1.6 | EditMode tests for enum + registry | 3 | **TECH-283** | Done (archived) | New test class `ZoneSubTypeRegistryTests` under `Assets/Tests/EditMode/Economy/` (new asmdef if needed). Cover: `GetById` returns correct entry for each of 7 ids, `GetById(-1)` returns null/throws, `IsStateServiceZone` true for 6 new enum values + false for R/C/I, `Zone.subTypeId` default -1 persists via serialization round-trip. |

#### Stage 1.2 — `TreasuryFloorClampService` + systemic spend delegation

**Status:** Final

**Objectives:** Land hard-cap treasury (Q4 locked decision). Wrap ALL `EconomyManager` spend call sites so balance NEVER goes negative. Existing `SpendMoney(int, string, bool)` keeps signature for backward compat but delegates to new `TrySpend`. This stage is the single riskiest refactor — touches every current money-out path in `EconomyManager`.

**Exit:**

- `TreasuryFloorClampService.cs` under `Assets/Scripts/Managers/GameManagers/` — MonoBehaviour, Inspector-wired on `EconomyManager` GO, `FindObjectOfType` fallback in `Awake` (guardrail #1).
- Public API: `bool CanAfford(int amount)`, `bool TrySpend(int amount, string context)`, `int CurrentBalance`.
- Existing `EconomyManager.SpendMoney` retained for backward compat, internally delegates to `TrySpend` (insufficient → `false` return + game notification, no mutation).
- EditMode test proves balance cannot go below zero via any public EconomyManager spend path.
- Glossary row: `TreasuryFloorClampService`.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — `TreasuryFloorClampService` skeleton + Inspector wiring.
- [ ] Phase 2 — Delegate existing `SpendMoney` + re-route call sites + EditMode coverage + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | `TreasuryFloorClampService` skeleton | 1 | **TECH-379** | Done (archived) | New MonoBehaviour at `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs`. `[SerializeField] private EconomyManager economy;` with `FindObjectOfType<EconomyManager>()` fallback in `Awake`. Public API: `CanAfford(int) → bool`, `TrySpend(int, string) → bool`, `CurrentBalance` property reading `economy.GetCurrentMoney()`. `TrySpend` checks `amount <= CurrentBalance` BEFORE mutation; success path calls `economy.cityStats.RemoveMoney(amount)`; failure path emits `GameNotificationManager.PostError`. |
| T1.2.2 | Wire service on `EconomyManager` GO | 1 | **TECH-380** | Done (archived) | Add `[SerializeField] private TreasuryFloorClampService treasuryFloorClamp;` field + `FindObjectOfType` fallback in `EconomyManager.Awake` (guardrail #1, invariant #4). Attach component to the `EconomyManager` GameObject in the main scene prefab. Document composition relationship in XML doc on the field. |
| T1.2.3 | Re-route `SpendMoney` through `TrySpend` | 2 | **TECH-381** | Done (archived) | Existing `EconomyManager.SpendMoney(int amount, string context, bool logToConsole)` keeps signature but body delegates to `treasuryFloorClamp.TrySpend(amount, context)`. On `false` return, log + emit notification; do NOT subtract `currentMoney` (previously allowed negative). Audit all internal call sites inside `EconomyManager.cs` that touch `currentMoney -= X` directly — rewrite via `TrySpend`. |
| T1.2.4 | Audit cross-file `SpendMoney` call sites | 2 | **TECH-382** | Done (archived) | Grep for `SpendMoney(` + `currentMoney -=` across `Assets/Scripts/**`. For each non-EconomyManager caller, confirm path now routes through `TrySpend`; update any direct `currentMoney` mutation to `TrySpend`. Document audit result in Decision Log section of spec stub. Zero remaining direct `currentMoney -=` outside `TreasuryFloorClampService`. |
| T1.2.5 | EditMode tests + glossary row | 2 | **TECH-383** | Done (archived) | `TreasuryFloorClampServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TrySpend(100)` when balance=200 succeeds + balance=100, (b) `TrySpend(300)` when balance=200 returns false + balance UNCHANGED + notification emitted, (c) `CanAfford(200)` true at balance=200, false at balance=199. Add `TreasuryFloorClampService` glossary row linking exploration + forthcoming economy-system spec. Regenerate MCP indexes. |

#### Stage 1.3 — `BudgetAllocationService` + save-schema v3→v4 migration

**Status:** In Progress — Phase 2 (TECH-418..421 Done (archived); TECH-422..425 Draft)

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

**Phases:**

- [ ] Phase 1 — `IBudgetAllocator` + `BudgetAllocationService` skeleton.
- [ ] Phase 2 — `TryDraw` logic + monthly reset + envelope normalization.
- [ ] Phase 3 — Save schema v3→v4 bump + migration + round-trip + tests + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | `IBudgetAllocator` interface + `BudgetAllocationService` skeleton | 1 | **TECH-418** | Done (archived) | Author `IBudgetAllocator.cs` with `TryDraw(int, int) → bool`, `GetMonthlyEnvelope(int) → int`, `SetEnvelopePct(int, float)`, `MonthlyReset()`. Author `BudgetAllocationService.cs` MonoBehaviour implementing the interface; `[SerializeField]` refs to `EconomyManager` + `TreasuryFloorClampService` + `ZoneSubTypeRegistry` with `FindObjectOfType` fallbacks in `Awake`. Fields inert in this task. |
| T1.3.2 | Wire service on `EconomyManager` GO | 1 | **TECH-419** | Done (archived) | Add `[SerializeField] private BudgetAllocationService budgetAllocation;` to `EconomyManager` + `FindObjectOfType` fallback. Attach `BudgetAllocationService` + `ZoneSubTypeRegistry` MonoBehaviours to the `EconomyManager` GO (same GO as `TreasuryFloorClampService`). Does not call them yet — consumer wiring lands in Step 2. |
| T1.3.3 | `TryDraw` + monthly reset logic | 2 | **TECH-420** | Done (archived) | Implement `TryDraw(subTypeId, amount)`: returns false if `subTypeId < 0 || subTypeId > 6`; returns false if `currentMonthRemaining[subTypeId] < amount`; returns false if `!treasuryFloorClamp.CanAfford(amount)`; otherwise `currentMonthRemaining[subTypeId] -= amount`, `treasuryFloorClamp.TrySpend(amount, "S envelope draw")` and return true. Implement `MonthlyReset`: for each i, `currentMonthRemaining[i] = (int)(globalMonthlyCap * envelopePct[i])`. |
| T1.3.4 | `SetEnvelopePct` auto-normalize | 2 | **TECH-421** | Done (archived) | Implement `SetEnvelopePct(int subTypeId, float pct)`: store raw value then normalize entire array so `sum == 1.0` (scale every entry by `1.0 / currentSum`). Handles N1 rounding residue from 14.28% × 7. Guard division-by-zero on all-zero envelope (reject + keep prior state). Also expose `SetEnvelopePctsBatch(float[7])` for player slider commits. |
| T1.3.5 | Save-schema v3→v4 bump | 3 | **TECH-422** | In Review | Bump `GameSaveData.CurrentSchemaVersion` from 3 to 4 in `GameSaveManager.cs`. Add `[Serializable] public class BudgetAllocationData { public float[] envelopePct; public int globalMonthlyCap; public int[] currentMonthRemaining; public static BudgetAllocationData Default(int cap); }`. Add `public BudgetAllocationData budgetAllocation` + `public List<StateServiceZoneData> stateServiceZones` fields on `GameSaveData`. Add `[Serializable] public class StateServiceZoneData { public int cellX, cellY; public int subTypeId; public int densityTier; }`. |
| T1.3.6 | `MigrateLoadedSaveData` v3→v4 branch | 3 | **TECH-423** | Draft | Add `if (data.schemaVersion < 4)` branch in `MigrateLoadedSaveData`: `data.stateServiceZones = new List<StateServiceZoneData>(); data.budgetAllocation = BudgetAllocationData.Default(cap: derive from treasury snapshot); data.schemaVersion = 4;`. `Default(cap)` seeds `envelopePct = new float[]{0.1428f, ...×7}` then normalizes sum to 1.0. Seed `currentMonthRemaining[i] = cap * envelopePct[i]`. |
| T1.3.7 | Save/load round-trip wiring | 3 | **TECH-424** | Draft | Wire `BudgetAllocationService.CaptureSaveData()` + `RestoreFromSaveData(BudgetAllocationData)` methods. `GameSaveManager.SaveGame` calls capture; `GameSaveManager.LoadGame` calls restore post-migration. Verify envelope + remaining + cap survive save → load → verify identity. No S zones restored yet (placement lands in Step 2). |
| T1.3.8 | EditMode tests + glossary + MCP reindex | 3 | **TECH-425** | Draft | `BudgetAllocationServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TryDraw` blocks when envelope exhausted, (b) `TryDraw` blocks when treasury empty (envelope fat), (c) `TryDraw` succeeds when both OK + both decrement, (d) `MonthlyReset` restores `currentMonthRemaining` to `cap × pct`, (e) `SetEnvelopePct` normalizes sum to 1.0 within 1e-6, (f) legacy v3 save loads into v4 with equal envelope + empty S list. Add `BudgetAllocationService` + `IBudgetAllocator` glossary rows. Run `npm run validate:all`. |

---

### Step 2 — Economy services: bond ledger + maintenance registry + Zone S placement

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Stand up `BondLedgerService` (Q5 locked — single concurrent bond per scale tier, fixed principal + term + interest). Refactor `EconomyManager.ProcessMonthlyMaintenance` from hardcoded `roadCount × rate + powerPlantCount × rate` to `IMaintenanceContributor` registry iteration with deterministic order (N2 resolution). Land `ZoneSService` as the single entry point for Zone S placement — wraps `BudgetAllocationService.TryDraw` → `ZoneManager.PlaceZone` → register building as `IMaintenanceContributor`. End of Step 2 = full runtime loop for S placement + monthly upkeep + bond issuance, headless (no UI surfaces yet).

**Exit criteria:**

- `IBondLedger` + `BondLedgerService` under `Assets/Scripts/Managers/GameManagers/`. MonoBehaviour + Inspector-wired. `TryIssueBond`, `GetActiveBond`, `ProcessMonthlyRepayment`. Single-concurrent-bond-per-tier enforced.
- Save schema carries `Dictionary<int, BondData>` keyed by scale tier; round-trip through save/load.
- `IMaintenanceContributor` interface + `RoadMaintenanceContributor` adapter + per-S-building `StateServiceMaintenanceContributor` component.
- `EconomyManager.ProcessMonthlyMaintenance` iterates registry sorted by contributor id string (deterministic replay parity — N2).
- `ZoneSService` under `Assets/Scripts/Managers/GameManagers/` — `PlaceStateServiceZone(GridCoord, int subTypeId) → bool` entry point. Calls `BudgetAllocationService.TryDraw` FIRST, routes through `ZoneManager.PlaceZone` on success, registers building on growth-pipeline spawn.
- `AutoZoningManager` guard: no-op for S enum values (Q3 deferred — manual only in MVP) + comment referencing `docs/zone-s-economy-exploration.md`.
- Glossary rows: `BondLedgerService`, `IBondLedger`, `IMaintenanceContributor`, `ZoneSService`.
- EditMode + integration tests: headless S placement simulated end-to-end (exploration Examples 1 + 2 reproducible as test fixtures); bond issuance + monthly repayment (Example 3) reproducible.
- `npm run unity:compile-check` green.

**Art:** Placeholder S building prefabs (light/medium/heavy × 7 sub-types = 21 prefabs). Art refinement deferred post-MVP — placeholders can be colored cubes at MVP bar.

**Relevant surfaces (load when step opens):**
- Step 1 outputs: `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneType` enum, `ZoneSubTypeRegistry`, save schema v4.
- `docs/zone-s-economy-exploration.md` §Implementation Points IP-4 / IP-5 / IP-6 + §Examples 1–3.
- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — `ProcessMonthlyMaintenance` + `ProcessDailyEconomy` tick site.
- `Assets/Scripts/Managers/UnitManagers/ZoneManager.cs` (path per exploration Architecture; confirm at stage open via Grep) — `PlaceZone` pipeline re-entry point.
- `Assets/Scripts/Managers/GameManagers/AutoZoningManager.cs` — guard site.
- `Assets/Scripts/Managers/GameManagers/BondLedgerService.cs` *(new)*.
- `Assets/Scripts/Managers/GameManagers/ZoneSService.cs` *(new)*.
- `ia/rules/invariants.md` — #5 (no direct `gridArray` outside `GridManager` — `ZoneSService` uses `GridManager.GetCell`), #6 (helper carve-out).

#### Stage 2.1 — `BondLedgerService` + save-schema bond registry

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the single-bond-per-scale-tier ledger. Proactive injection only (not remedial). Save-schema already bumped to v4 in Step 1 — extend `GameSaveData` with `bondRegistry` field (same migration branch) without a second version bump.

**Exit:**

- `IBondLedger` interface + `BondLedgerService` MonoBehaviour.
- `TryIssueBond(scaleTier, principal, termMonths) → bool` rejects if active bond exists on tier; on success credits via `EconomyManager.AddMoney`, computes `monthlyRepayment = (principal × (1 + fixedInterestRate)) / termMonths`.
- `ProcessMonthlyRepayment(scaleTier)` routes repayment through `TreasuryFloorClampService.TrySpend` — failure flags arrears (HUD flag only, no mechanical penalty per Review Note N6).
- `GameSaveData.bondRegistry` = `Dictionary<int, BondData>` with save/load round-trip. Migration v3→v4 already adds empty registry (extend Stage 1.3 migration branch).
- Glossary rows: `BondLedgerService`, `IBondLedger`.

**Phases:**

- [ ] Phase 1 — `IBondLedger` + `BondLedgerService` skeleton + save schema extension.
- [ ] Phase 2 — Issue + repayment logic wired to `EconomyManager` tick + tests + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | `IBondLedger` + `BondLedgerService` skeleton | 1 | _pending_ | _pending_ | New interface `IBondLedger` with `TryIssueBond(int scaleTier, int principal, int termMonths) → bool`, `GetActiveBond(int scaleTier) → BondData?`, `ProcessMonthlyRepayment(int scaleTier)`. New `BondLedgerService.cs` MonoBehaviour implementing interface. `[SerializeField]` constants: `fixedInterestRate` (default 0.12). Internal state: `Dictionary<int, BondData> active`. |
| T2.1.2 | Extend save schema with `bondRegistry` | 1 | _pending_ | _pending_ | Add `[Serializable] public class BondData { public int scaleTier; public int principal; public int termMonths; public int monthlyRepayment; public float fixedInterestRate; public string issuedOnDate; public int monthsRemaining; public bool arrears; }`. Add `public Dictionary<int, BondData> bondRegistry` to `GameSaveData` (serialize as list-of-entries if Dictionary JSON not supported). Extend v3→v4 migration branch from Stage 1.3 to seed empty registry. |
| T2.1.3 | `TryIssueBond` logic | 2 | _pending_ | _pending_ | Implement `TryIssueBond(scaleTier, principal, termMonths)`: return false if `active.ContainsKey(scaleTier)`; else compute `monthlyRepayment = (int)((principal * (1 + fixedInterestRate)) / termMonths)`, call `economyManager.AddMoney(principal)`, create `BondData` stamped with today's date + `monthsRemaining = termMonths`, insert into `active`, return true. Reject non-positive principal/term. |
| T2.1.4 | `ProcessMonthlyRepayment` + tick wiring | 2 | _pending_ | _pending_ | Implement `ProcessMonthlyRepayment(scaleTier)`: lookup active bond; if none, return. Call `treasuryFloorClamp.TrySpend(bond.monthlyRepayment, "bond repayment")`. On false → `bond.arrears = true` (no crash, HUD flag only). On true → `bond.monthsRemaining--`; if `monthsRemaining == 0`, remove from `active`. Wire call from `EconomyManager.ProcessDailyEconomy` on month-first day AFTER tax + maintenance. |
| T2.1.5 | Save/load round-trip wiring | 2 | _pending_ | _pending_ | `BondLedgerService.CaptureSaveData() → Dictionary<int, BondData>` + `RestoreFromSaveData(Dictionary<int, BondData>)`. Wire into `GameSaveManager.SaveGame` / `LoadGame` post-migration. Verify save → load → identity on all fields including `monthsRemaining` + `arrears`. |
| T2.1.6 | EditMode tests + glossary rows | 2 | _pending_ | _pending_ | `BondLedgerServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TryIssueBond` succeeds when no active + balance credited + registry populated, (b) second `TryIssueBond` same tier returns false, (c) `ProcessMonthlyRepayment` decrements `monthsRemaining` + spends, (d) repayment when balance insufficient flags `arrears = true` + balance stays at 0, (e) bond clears from registry when `monthsRemaining` reaches 0, (f) save/load round-trip preserves all bond fields. Add `BondLedgerService` + `IBondLedger` glossary rows + MCP reindex. |

#### Stage 2.2 — `IMaintenanceContributor` registry + deterministic iteration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Refactor `EconomyManager.ProcessMonthlyMaintenance` from hardcoded formula to contributor-registry iteration. Existing roads + power plants preserved via adapter pattern (behavior-preserving). Registry iteration sorted by contributor id string for save-replay parity (N2 resolution). Contributors emit into owning sub-type envelope — underfunded envelope shows visual decay placeholder (Q3).

**Exit:**

- `IMaintenanceContributor` interface: `int GetMonthlyMaintenance()`, `string GetContributorId()`, `int GetSubTypeId()` (-1 = general maintenance pool).
- `EconomyManager.RegisterMaintenanceContributor(IMaintenanceContributor)` + `Unregister`.
- `ProcessMonthlyMaintenance` iterates registry sorted by `GetContributorId()` (deterministic).
- `RoadMaintenanceContributor` adapter preserves existing road maintenance formula bit-for-bit.
- `PowerPlantMaintenanceContributor` adapter (if power plants currently participate in monthly tick — confirm at impl time).
- Glossary row: `IMaintenanceContributor`.

**Phases:**

- [ ] Phase 1 — `IMaintenanceContributor` interface + `EconomyManager` registry plumbing.
- [ ] Phase 2 — Existing-contributor adapters + hardcoded-formula retirement + tests + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | `IMaintenanceContributor` interface + registry | 1 | _pending_ | _pending_ | New `IMaintenanceContributor.cs` with `GetMonthlyMaintenance() → int`, `GetContributorId() → string`, `GetSubTypeId() → int`. Add `List<IMaintenanceContributor> maintenanceContributors` to `EconomyManager`. `RegisterMaintenanceContributor(c)` + `UnregisterMaintenanceContributor(c)` public methods. Registry reset on scene load / save restore. |
| T2.2.2 | Refactor `ProcessMonthlyMaintenance` to iterate registry | 1 | _pending_ | _pending_ | In `EconomyManager.ProcessMonthlyMaintenance`: sort registry snapshot by `GetContributorId()` (stable, deterministic). For each contributor: if `GetSubTypeId() >= 0`, draw from `budgetAllocation.TryDraw(subTypeId, amount)` — failure flags decay on contributor (no penalty MVP, hook only). Else spend from general treasury via `treasuryFloorClamp.TrySpend`. Old hardcoded `roadCount × rate + powerPlantCount × rate` formula removed. |
| T2.2.3 | `RoadMaintenanceContributor` adapter | 2 | _pending_ | _pending_ | New adapter class `RoadMaintenanceContributor` implementing `IMaintenanceContributor`. `GetMonthlyMaintenance()` returns the prior `roadCount × ratePerRoad` result (read from existing `RoadManager` or equivalent). `GetContributorId()` returns `"road-aggregate"`. `GetSubTypeId()` returns -1 (general pool). Register from `EconomyManager.Awake` after dependencies wired. Result: month-to-month maintenance delta identical to pre-refactor. |
| T2.2.4 | Power-plant contributor audit + adapter | 2 | _pending_ | _pending_ | Audit current `EconomyManager.ProcessMonthlyMaintenance` for power-plant maintenance line. If present, author `PowerPlantMaintenanceContributor` adapter analogous to road adapter. If absent (utility pool deferred to Bucket 4), skip this task + document in Decision Log. |
| T2.2.5 | EditMode tests + glossary | 2 | _pending_ | _pending_ | `MaintenanceRegistryTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) contributors iterated in sorted id order (feed 3 contributors out-of-order, assert deterministic call order), (b) sub-type contributor draws from envelope, (c) general-pool contributor spends through `TrySpend`, (d) pre-refactor road cost == post-refactor road cost (regression guard). Add `IMaintenanceContributor` glossary row. |

#### Stage 2.3 — `ZoneSService` placement pipeline + `AutoZoningManager` guard

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the single entry point for Zone S placement. `ZoneSService.PlaceStateServiceZone` wraps the envelope draw + `ZoneManager.PlaceZone` route + growth-pipeline contributor registration. Guard `AutoZoningManager` from placing S (manual-only in MVP). End of Step 2: exploration Examples 1 + 2 (place police OK, place fire blocked by envelope) reproducible as integration tests with no UI involvement.

**Exit:**

- `ZoneSService.cs` under `Assets/Scripts/Managers/GameManagers/`. MonoBehaviour. `[SerializeField]` refs to `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `EconomyManager`, `GridManager`.
- `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool` implements Example 1 + 2 flow.
- Cell access goes through `GridManager.GetCell` (invariant #5).
- Growth pipeline hook: on S building spawn, `EconomyManager.RegisterMaintenanceContributor(StateServiceMaintenanceContributor)`.
- `AutoZoningManager` guard: early-return no-op when `IsStateServiceZone(targetType)` is true + comment: `// Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2`.
- Glossary row: `ZoneSService`.
- Integration tests: Example 1 (place succeeds, envelope + treasury decrement, building registers as contributor) + Example 2 (envelope exhausted blocks placement, no treasury mutation) green.

**Phases:**

- [ ] Phase 1 — `ZoneSService` skeleton + manager refs wired.
- [ ] Phase 2 — `PlaceStateServiceZone` flow + growth-pipeline contributor registration.
- [ ] Phase 3 — `AutoZoningManager` guard + integration tests + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | `ZoneSService` skeleton class | 1 | _pending_ | _pending_ | New `ZoneSService.cs` MonoBehaviour under `Assets/Scripts/Managers/GameManagers/`. `[SerializeField] private BudgetAllocationService budgetAllocation; [SerializeField] private TreasuryFloorClampService treasuryFloorClamp; [SerializeField] private ZoneSubTypeRegistry registry; [SerializeField] private ZoneManager zoneManager; [SerializeField] private GridManager grid;` — all with `FindObjectOfType` fallback in `Awake` (guardrail #1). Public API signatures declared (body empty / returns default). |
| T2.3.1b | Scene wiring + prefab attach | 1 | _pending_ | _pending_ | Attach `ZoneSService` component to the `EconomyManager` GameObject (or dedicated GameManagers GO) in main scene prefab. Populate Inspector refs for all 5 serialized fields. Verify `FindObjectOfType` fallbacks still resolve when Inspector refs are cleared (loadable prefab safety). |
| T2.3.2 | `PlaceStateServiceZone` flow | 2 | _pending_ | _pending_ | Implement `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool`: lookup `entry = registry.GetById(subTypeId)`; fail-fast on null. Fetch cell via `grid.GetCell(cell.x, cell.y)` (invariant #5). Call `budgetAllocation.TryDraw(subTypeId, entry.baseCost)` — on false, emit notification "insufficient {subType} envelope this month" and return false (Q4 block-before-deduct, no treasury mutation). On true, call `zoneManager.PlaceZone(cell, ZoneType.StateServiceLightZoning, subTypeId: subTypeId)` and return true. Sub-type id written to `Zone.subTypeId` sidecar inside `ZoneManager.PlaceZone` (extend signature). |
| T2.3.3 | `StateServiceMaintenanceContributor` component | 2 | _pending_ | _pending_ | New `StateServiceMaintenanceContributor` MonoBehaviour attached to S building prefabs. Implements `IMaintenanceContributor`: `GetMonthlyMaintenance()` reads `registry.GetById(subTypeId).monthlyUpkeep`; `GetContributorId()` returns `$"s-{subTypeId}-{instanceId}"`; `GetSubTypeId()` returns `subTypeId`. `Start()` calls `economyManager.RegisterMaintenanceContributor(this)`; `OnDestroy()` calls `Unregister`. |
| T2.3.4 | Growth-pipeline hook for S buildings | 2 | _pending_ | _pending_ | Inside existing building-spawn pipeline (grep `PlaceZone` → growth callback or `BuildingSpawner` equivalent), attach `StateServiceMaintenanceContributor` component on spawned S buildings. Read `Zone.subTypeId` from the owning cell + pass to the component. Guard: only applies when `IsStateServiceZone(cell.zoneType)`. No change for R/C/I pipelines. |
| T2.3.5 | `AutoZoningManager` S no-op guard | 3 | _pending_ | _pending_ | Grep `AutoZoningManager.cs` for the zone-selection switch. Add early-return: `if (EconomyManager.IsStateServiceZone(candidate)) { /* Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2 */ continue; }`. Covers Review Note N4 (path reference in comment). No behavioral change for RCI. |
| T2.3.6 | Integration tests — Examples 1 + 2 | 3 | _pending_ | _pending_ | `ZoneSServicePlacementTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode if grid scene needed). Example 1: envelope=1200, treasury=10000, `PlaceStateServiceZone(cell, POLICE(baseCost=500))` → returns true, envelope=700, treasury=9500, zone at cell is `StateServiceLightZoning` with `subTypeId=POLICE`. Example 2: envelope=200, baseCost=600 → returns false, envelope UNCHANGED (200), treasury UNCHANGED, zone at cell NOT placed. Add `ZoneSService` glossary row + MCP reindex. Run `npm run validate:all`. |

---

### Step 3 — UI surfaces + CityStats integration + economy-system reference spec

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Player-facing surface lands. Four UI screens (S zoning button, sub-type picker, budget envelope panel, bond issuance dialog) per IP-8. `CityStats` read-model extended with envelope + bond + maintenance aggregates (HUD projected-income-minus-maintenance now subtracts envelope cap; bond debt line visible). `MiniMap` palette gains 4th color channel for S (single color MVP, sub-type split deferred per N5). Author new reference spec `ia/specs/economy-system.md` so glossary cross-refs resolve + umbrella column (g) advances. End of Step 3: full MVP loop — player clicks S button, picks sub-type, places building, sees monthly draw + bond dialog, save/load round-trips cleanly.

**Exit criteria:**

- S zoning button in `UIManager.ToolbarChrome` zoning cluster (4th sibling to R/C/I).
- Sub-type picker modal: 7 entries (icon + name + baseCost), click commits sub-type + enters placement mode.
- Budget panel: 7 pct sliders (sum-locked to 100% via auto-normalize on commit) + global cap slider + per-sub-type remaining readout + overspend-blocked badge.
- Bond issuance modal: principal input + term selector (12/24/48 months fixed) + `monthlyRepayment` preview + issue button. Disabled when active bond exists on current scale tier.
- `CityStats` extended: total envelope cap shown, per-sub-type remaining shown, active bond debt shown, HUD projected-income-minus-maintenance subtracts envelope cap.
- `MiniMapController` palette gains 4th channel color for S (all 7 sub-types same color MVP).
- New reference spec `ia/specs/economy-system.md` authored; covers Zone S + envelope + bond ledger + maintenance contributor registry. Glossary rows reference this spec.
- Picker cancel UX per N3: cancel closes picker, no cost, no placement.
- `npm run validate:all` + `npm run unity:compile-check` green. Umbrella rollout tracker row for Bucket 3 ready to advance to column (f).

**Art:** Icons for 7 sub-types (placeholder sprites acceptable MVP). Budget panel + bond dialog chrome — reuse existing `UIManager.Theme` patterns.

**Relevant surfaces (load when step opens):**
- Step 1 + 2 outputs: all services + enum + save schema + registry.
- `docs/zone-s-economy-exploration.md` §IP-8 + §Examples 1 + 3.
- `Assets/Scripts/Managers/GameManagers/UIManager.ToolbarChrome.cs` — zoning cluster.
- `Assets/Scripts/Managers/GameManagers/UIManager.Hud.cs` — HUD income/maintenance readout.
- `Assets/Scripts/Managers/GameManagers/UIManager.PopupStack.cs` — modal stack (picker, bond dialog).
- `Assets/Scripts/Managers/GameManagers/CityStats.cs` + `Assets/Scripts/Controllers/GameControllers/CityStatsUIController.cs`.
- `Assets/Scripts/Controllers/GameControllers/MiniMapController.cs` — palette extension.
- `ia/specs/glossary.md` — cross-ref authoritative `economy-system.md` once landed.
- `ia/rules/invariants.md` — #12 (reference spec under `ia/specs/`).

#### Stage 3.1 — Toolbar + sub-type picker + budget panel

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Player can click S button, pick a sub-type, place a building, tune envelope sliders. End-to-end flow with visible feedback on envelope draws + overspend blocks.

**Exit:**

- S zoning button visible in toolbar, correct icon, enters S placement mode on click.
- Sub-type picker opens over placement mode; 7 buttons; click commits sub-type + resumes cursor placement.
- Picker cancel (ESC or outside-click) closes picker without cost or placement (N3).
- Budget panel reachable from HUD; 7 sliders sum-locked + commit normalizes to 1.0; global cap slider + remaining readouts live-update.
- Overspend-blocked notification visible when `TryDraw` returns false.

**Phases:**

- [ ] Phase 1 — S zoning button + placement mode entry.
- [ ] Phase 2 — Sub-type picker modal.
- [ ] Phase 3 — Budget panel with envelope sliders + global cap.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | S zoning button in `UIManager.ToolbarChrome` | 1 | _pending_ | _pending_ | Add 4th button to zoning cluster in `UIManager.ToolbarChrome.cs` alongside R/C/I. Icon: placeholder "S" glyph. Click handler sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` + opens sub-type picker (next task). Toolbar layout respects existing theme spacing. |
| T3.1.2 | Placement-mode routing through `ZoneSService` | 1 | _pending_ | _pending_ | When S placement active + user clicks on grid cell, route through `ZoneSService.PlaceStateServiceZone(cell, currentSubTypeId)` instead of direct `ZoneManager.PlaceZone`. `currentSubTypeId` carried in transient placement state (set by picker). Guard: if `currentSubTypeId < 0`, reopen picker. |
| T3.1.3 | Sub-type picker modal UI | 2 | _pending_ | _pending_ | New `SubTypePickerModal.cs` under `Assets/Scripts/Managers/GameManagers/UI/` (or existing UI dir). Uses `UIManager.PopupStack` to present 7 buttons (icon + displayName + baseCost) sourced from `ZoneSubTypeRegistry`. Click commits `currentSubTypeId` + closes modal + signals placement mode ready. |
| T3.1.4 | Picker cancel UX (N3) | 2 | _pending_ | _pending_ | ESC key or outside-click dismisses picker with no cost + no placement + `currentSubTypeId = -1` + exits placement mode. Documented in `SubTypePickerModal` XML docs referencing Review Note N3. |
| T3.1.5 | Budget panel UI with sliders | 3 | _pending_ | _pending_ | New `BudgetPanel.cs` + Unity UI prefab. 7 horizontal sliders (one per sub-type, labeled from `ZoneSubTypeRegistry`), 1 global cap slider, 7 remaining-this-month readouts. Open via HUD button (add to `UIManager.Hud`). Slider commit calls `budgetAllocation.SetEnvelopePct(i, pct)` which auto-normalizes; UI re-reads values post-normalize so sliders reflect actual stored state. |
| T3.1.6 | Overspend-blocked notification wiring | 3 | _pending_ | _pending_ | Hook `GameNotificationManager` event raised by `BudgetAllocationService.TryDraw` failure. Display a transient HUD badge: "{sub-type} envelope exhausted" for 3s. Matches Example 2 user-facing feedback. |

#### Stage 3.2 — Bond dialog + `CityStats` + `MiniMap` palette

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land bond issuance UI + read-model integration. Player can issue a bond, see debt aggregated in CityStats + HUD, see S cells distinct from RCI on the mini-map.

**Exit:**

- Bond issuance modal reachable from HUD or budget panel. Principal input (int) + term selector (radio: 12/24/48). Live `monthlyRepayment` preview. Issue button calls `bondLedger.TryIssueBond(cityTier, principal, term)`. Disabled when `GetActiveBond(cityTier) != null`.
- `CityStats` read-model fields: `totalEnvelopeCap`, `envelopeRemainingPerSubType[7]`, `activeBondDebt`, `monthlyBondRepayment`. `CityStatsUIController` displays these in the stats panel.
- HUD income-minus-maintenance hint extended: subtracts `totalEnvelopeCap` (not per-draw) per exploration §Subsystem Impact.
- `MiniMapController` palette: new color for S (all sub-types same color MVP, per N5). RCI unchanged.
- Integration test reproducing Example 3 (issue bond, treasury credited, month tick repays).

**Phases:**

- [ ] Phase 1 — Bond issuance modal.
- [ ] Phase 2 — `CityStats` + HUD read-model extension.
- [ ] Phase 3 — `MiniMap` palette + integration test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | Bond issuance modal UI | 1 | _pending_ | _pending_ | New `BondIssuanceModal.cs` + Unity UI prefab via `UIManager.PopupStack`. Fields: principal `InputField` (int, min 100), term radio (12/24/48), live preview Text computing `(principal × 1.12) / termMonths`. Issue button calls `bondLedger.TryIssueBond(scaleTier: city, principal, termMonths)`. Disabled if `bondLedger.GetActiveBond(city) != null`. |
| T3.2.2 | Bond-active HUD flag + entry point | 1 | _pending_ | _pending_ | HUD badge showing "Active bond: {remainingMonths} mo, {monthlyRepayment}/mo" when active bond exists (city tier MVP). Click opens bond detail view (reuses `BondIssuanceModal` in read-only mode, showing current bond + disabled issue button). Arrears state shows red badge. |
| T3.2.3 | `CityStats` envelope + bond fields | 2 | _pending_ | _pending_ | Add fields to `CityStats.cs`: `int totalEnvelopeCap`, `int[] envelopeRemaining` (len 7), `int activeBondDebt`, `int monthlyBondRepayment`. Populate each tick from `budgetAllocation` + `bondLedger`. `CityStatsUIController` displays new fields in stats panel (label + value). |
| T3.2.4 | HUD income-minus-maintenance hint update | 2 | _pending_ | _pending_ | Update HUD projected-income-minus-maintenance readout in `UIManager.Hud` (or the existing formula site) to subtract `cityStats.totalEnvelopeCap` from the projected monthly surplus. Label text updated to "Est. monthly surplus (after S envelope + bond repayment)". |
| T3.2.5 | `MiniMapController` S palette | 3 | _pending_ | _pending_ | Extend color lookup in `MiniMapController.cs`: new case for each of 6 new `ZoneType` values returning a single S color (e.g. purple). N5 locks: no per-sub-type color split in MVP. RCI colors unchanged. |
| T3.2.6 | Integration test — Example 3 end-to-end | 3 | _pending_ | _pending_ | `BondIssuanceIntegrationTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode). Reproduces Example 3: treasury=1200, `TryIssueBond(city, 5000, 24)` → returns true, treasury=6200, registry has entry with `monthlyRepayment=233`. Month tick triggers `ProcessMonthlyRepayment` → treasury=5967, `monthsRemaining=23`. Save/load round-trip preserves bond state. |

#### Stage 3.3 — `economy-system.md` reference spec + closeout alignment

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author the new `ia/specs/economy-system.md` reference spec (invariant #12 — covers permanent domain). Glossary rows authored in Steps 1/2 get proper authoritative spec cross-refs. Run full validation + confirm umbrella rollout tracker row ready to advance to column (f) "≥1 task filed" on first `/stage-file` call. Final stage — nothing player-visible added, but spec + docs + alignment land.

**Exit:**

- `ia/specs/economy-system.md` authored with sections: Overview, Zone S (enum + sub-type registry + placement pipeline), Budget envelope (`IBudgetAllocator` contract + `TryDraw` semantics + monthly reset), Treasury floor clamp (hard cap), Bond ledger (`IBondLedger` contract + single-concurrent rule + arrears state), Maintenance contributor registry (`IMaintenanceContributor` + deterministic iteration), Save schema v3→v4 migration, Glossary cross-refs.
- All glossary rows added in Steps 1/2 re-point to `ia/specs/economy-system.md` sections (replace placeholder exploration-doc links).
- `ia/rules/agent-router.md` table gets new row(s) for economy / Zone S domain → `economy-system.md` sections.
- `tools/mcp-ia-server/data/spec-index.json` regenerated (captures new spec).
- `npm run validate:all` green.
- Umbrella rollout tracker (`ia/projects/full-game-mvp-rollout-tracker.md`) Bucket 3 row columns (a)–(e) verified complete; column (g) align gate closed.

**Phases:**

- [ ] Phase 1 — Author `economy-system.md` + glossary repointing + router table update.
- [ ] Phase 2 — Index regen + full validation + umbrella alignment.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | Author `ia/specs/economy-system.md` | 1 | _pending_ | _pending_ | New reference spec under `ia/specs/economy-system.md`. Sections per stage Exit list. Follows existing spec authoring conventions (frontmatter, ToC, glossary cross-refs). Cross-references `persistence-system.md` (save) + `managers-reference.md` (Zones). Caveman prose per `ia/rules/agent-output-caveman.md` §authoring. |
| T3.3.2 | Repoint glossary rows to new spec | 1 | _pending_ | _pending_ | Update 10 glossary rows added in Steps 1/2 (`Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget)`) — replace exploration-doc placeholder links with `ia/specs/economy-system.md#{anchor}` links. Preserves cross-link integrity. |
| T3.3.3 | Router-table row for economy domain | 1 | _pending_ | _pending_ | Update `ia/rules/agent-router.md` routing table: add row(s) mapping task-domain keywords ("zone s", "economy", "budget", "bond", "maintenance") to `economy-system.md` sections. Ensures MCP `router_for_task` dispatches correctly in future agent sessions. |
| T3.3.4 | Index regen + `validate:all` | 2 | _pending_ | _pending_ | Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/spec-index.json` + `glossary-index.json` + `glossary-graph-index.json`. Run `npm run validate:all`; fix any frontmatter / dead-link issues. Confirm MCP tests pass (`tools/mcp-ia-server/tests`). |
| T3.3.5 | Umbrella rollout-tracker alignment check | 2 | _pending_ | _pending_ | Read `ia/projects/full-game-mvp-rollout-tracker.md` Bucket 3 row. Verify columns (a)–(e) marked complete (design-explore → master-plan → stage-file → project-spec-kickoff → glossary rows landed). Verify column (g) align gate closed (spec + router + glossary all pointing to `economy-system.md`). Do NOT tick column (f) — that's `/stage-file` authoring, not this closeout stage. Document state in closeout notes. |

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
