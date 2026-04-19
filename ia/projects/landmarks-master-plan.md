# Landmarks — Master Plan (Bucket 4-b MVP)

> **Last updated:** 2026-04-17
>
> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Landmarks v1 — two parallel progression tracks. **Tier-defining landmarks** (free gift on scale-tier transition — Bucket 1 coupling) + **intra-tier reward landmarks** (designer-tuned pop milestones → commissioned "super-building" via bond-backed multi-month build). Catalog-driven (`StreamingAssets/landmark-catalog.yaml`). Sidecar `landmarks.json` = authoritative state; main-save cell-tag map = denormalized index. Super-utility buildings register into sibling Bucket 4-a `UtilityContributorRegistry` via narrow catalog interface. **OUT of scope:** utilities sim (sibling `docs/utilities-exploration.md`), Zone S + per-service budgets (Bucket 3 — consumed only as `IBondConsumer`), city-sim signals (Bucket 2), CityStats overhaul (Bucket 8), multi-scale core (Bucket 1 — consumed as scale-transition event source), heritage / cultural landmarks, landmark-specific tourism effects, destructible landmarks, mid-build cancellation, multi-cell footprints.
>
> **Exploration source:** `docs/landmarks-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points §A–§F, Examples, Review Notes).
>
> **Umbrella:** `ia/projects/full-game-mvp-master-plan.md` Bucket 4-b row. Sibling orchestrator `ia/projects/utilities-master-plan.md` (Bucket 4-a). Schema bump piggybacks on Bucket 3 v3 envelope (same rule as utilities — no mid-tier v2.x bump owned here).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach D — hybrid two-track, both pop-driven. Rejected A (registry only), B (scale-unlock only), C (commission only).
> - Tier-defining track = free gift on scale-tier transition (no commission cost, instant placement). Coupling to Bucket 1 `ScaleTierController`.
> - Intra-tier track = commissioned — bond-backed multi-month build, drawn against Bucket 3 per-service budget. Pause-able; NO mid-build cancellation (v1).
> - Deficit commission allowed (bond underwrites — no floor check beyond bond ceiling per Bucket 3 kickoff contract).
> - Catalog = hand-authored YAML at `StreamingAssets/landmark-catalog.yaml`. Schema: `id`, `name`, `tier`, `popGate`, `sprite`, `commissionCost`, `buildMonths`, `utilityContributorRef?`, `contributorScalingFactor?`.
> - Count target: 2 tier-defining (city→region, region→country) + 4 intra-tier = 6 rows v1.
> - Persistence: sidecar `landmarks.json` = truth; main-save per-scale cell-tag map = denormalized index. Reconciliation on load — sidecar wins; dangling cell tags cleared.
> - Placement is tile-sprite only — NO `HeightMap` mutation (invariant #1 safe). 1-cell footprint v1.
> - Super-utility bridge = narrow catalog interface. Landmark `utilityContributorRef` nullable — sibling Bucket 4-a `UtilityContributorRegistry.Register(landmarkId, contributorRef, scalingFactor)` called on `LandmarkBuildCompleted` when non-null.
> - Costs = placeholder constants. Migration to cost-catalog bucket (future Bucket 11) flagged at every commission-cost touch site.
> - UI = progress panel + commission dialog minimum viable. No tooltip / glossary polish (Bucket 6 scope). Bucket 6 `UiTheme` must land first.
> - Hard deferrals: heritage / cultural / tourism effects, destructible / decay, mid-build cancel, multi-cell footprints, in-game info panel polish.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Read first if landing cold:**
> - `docs/landmarks-exploration.md` — full design + architecture mermaid + sidecar/cell-tag reconciliation example. §Design Expansion is ground truth.
> - `ia/projects/utilities-master-plan.md` — sibling orchestrator; `UtilityContributorRegistry.Register` contract consumed by Step 4 of this plan.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella Bucket 4-b row + Bucket 3 v3 schema envelope rule.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — **#1** (no `HeightMap` mutation — placement is tile-sprite only), **#3** (no `FindObjectOfType` in hot loops — cache refs in `Awake`), **#4** (no new singletons — `LandmarkProgressionService` / `BigProjectService` / `LandmarkPlacementService` / `LandmarkCatalogStore` all MonoBehaviour + Inspector + `FindObjectOfType` fallback), **#5 + #6** (`LandmarkPlacementService` under `Assets/Scripts/Managers/GameManagers/*Service.cs` carve-out — no `GridManager` responsibility creep), **#12** (permanent domain → `ia/specs/landmarks-system.md` authored in Stage 4.2).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; `spec_section persistence-system load-pipeline` for sidecar restore ordering; `rule_content orchestrator-vs-spec` for permanence rule. Never full `BACKLOG.md` read.
> - **Umbrella parallel-work rule:** sequential filing only. No concurrent `/stage-file` run with sibling `ia/projects/utilities-master-plan.md` on same branch.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Stage 1 — Catalog + data model + glossary/spec seed / Data contracts + enums

**Status:** In Progress (TECH-335, TECH-336, TECH-337, TECH-338 filed)

**Objectives:** Define the row type + gate discriminator + tier enum. No runtime logic — typed scaffolding that Steps 2–4 consume. Same Stage 1.1 shape as utilities — data lands before services.

**Exit:**

- `LandmarkTier` enum (`City`, `Region`, `Country`) with XML doc per value (city = base tier, region = post city→region transition, country = post region→country transition).
- `LandmarkPopGate` polymorphic — abstract base + two concrete subclasses `ScaleTransitionGate` (carries `fromTier`) and `IntraTierGate` (carries `pop`). Tagged for YAML deserialization (`kind: scale_transition` / `kind: intra_tier`).
- `LandmarkCatalogRow` serializable class w/ all 9 fields.
- Files compile clean (`npm run unity:compile-check`); no references from runtime code yet.
- Phase 1 — Tier enum + gate discriminator.
- Phase 2 — Catalog row class + compile check.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | LandmarkTier enum | **TECH-335** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkTier.cs` — `City`, `Region`, `Country` enum values. XML doc each value explaining scale coupling (region = unlocked on city→region scale transition). No behavior. |
| T1.2 | LandmarkPopGate discriminator | **TECH-336** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkPopGate.cs` — abstract base + `ScaleTransitionGate { LandmarkTier fromTier }` + `IntraTierGate { int pop }`. YAML-deserializable via tag field `kind`. Unit test for YAML round-trip lands in T1.3.4. |
| T1.3 | LandmarkCatalogRow class | **TECH-337** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkCatalogRow.cs` — serializable class w/ `id`, `displayName`, `tier`, `popGate`, `spritePath`, `commissionCost`, `buildMonths`, `utilityContributorRef` (nullable), `contributorScalingFactor` (default 1.0). XML doc each field. |
| T1.4 | Compile check + asmdef alignment | **TECH-338** | Draft | Run `npm run unity:compile-check`; ensure new types land in correct assembly (main asm unless Landmarks asmdef exists). No runtime refs yet — just compile green. |

### Stage 2 — Catalog + data model + glossary/spec seed / Catalog YAML + validator rule

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author the 6-row catalog file + extend `validate:all` with landmark-catalog lint. Ensures authoring errors (duplicate id, dangling `utilityContributorRef`) fail CI before runtime loads.

**Exit:**

- `Assets/StreamingAssets/landmark-catalog.yaml` — 6 rows: 2 tier-defining (city→region: `regional_plocks`, region→country: `country_capital`) + 4 intra-tier (`big_power_plant` super-utility w/ `contributorScalingFactor: 10`, `state_university` non-utility, `grand_hospital` non-utility, `major_airport` non-utility). All commission-cost fields comment-flagged `// cost-catalog bucket 11 placeholder`.
- `tools/scripts/validate-landmark-catalog.ts` (OR equivalent Node script) — parses YAML, asserts id uniqueness, asserts `utilityContributorRef` non-null rows resolve against a placeholder allowlist (sibling utilities catalog not yet shipped — use a hard-coded allowlist + TODO-link to utilities Stage 2.1 archetype asset names), asserts `popGate.kind ∈ { scale_transition, intra_tier }`, asserts `tierCount` maps to a valid `LandmarkTier`.
- Validator wired into `package.json` `validate:all` chain + CI script.
- EditMode smoke — load YAML via upcoming Store (stubbed) + assert 6 rows parsed (reference check moved to Stage 1.3 once Store lands).
- Phase 1 — Author 6-row YAML.
- Phase 2 — Validator script + CI wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Author 6 catalog rows | _pending_ | _pending_ | Create `Assets/StreamingAssets/landmark-catalog.yaml` with 6 rows per Exploration Examples block. Tier-defining rows: `commissionCost: 0`, `buildMonths: 0`, `utilityContributorRef: null`. Intra-tier super-utility (`big_power_plant`): `utilityContributorRef: contributors/coal_plant`, `contributorScalingFactor: 10`. Comment every `commissionCost` line w/ cost-catalog bucket 11 marker. |
| T2.2 | StreamingAssets dir conventions | _pending_ | _pending_ | Add `Assets/StreamingAssets/.meta` + README stub under `Assets/StreamingAssets/README.md` (new dir — create if missing) documenting loading convention (Unity `Application.streamingAssetsPath` + YAML parser). Update `.gitignore` if needed. |
| T2.3 | Landmark-catalog validator script | _pending_ | _pending_ | Add `tools/scripts/validate-landmark-catalog.ts` — parse YAML, assert id uniqueness, assert `utilityContributorRef` resolves against placeholder allowlist (TODO-link sibling utilities archetype assets), assert `popGate.kind` enum, assert cost/buildMonths ≥ 0. Exit code nonzero on violations. |
| T2.4 | Wire validator into validate:all | _pending_ | _pending_ | Edit `package.json` — add `validate:landmark-catalog` script, chain into `validate:all`. Update CI matrix (if separate from validate:all root). Document in `docs/agent-led-verification-policy.md` validator list. |

### Stage 3 — Catalog + data model + glossary/spec seed / LandmarkCatalogStore + glossary + spec stub

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the runtime loader MonoBehaviour + seed glossary vocabulary + author `ia/specs/landmarks-system.md` stub. Vocabulary lands early so Step 2+ tasks can cite canonical terms in intent prose. Same Stage-1-seeds-vocab pattern as utilities.

**Exit:**

- `LandmarkCatalogStore.cs` MonoBehaviour — `Awake` loads `Application.streamingAssetsPath/landmark-catalog.yaml`, deserializes into `List<LandmarkCatalogRow>`, builds `Dictionary<string, LandmarkCatalogRow> byId` index. API: `GetAll()`, `GetById(string id)`. `FindObjectOfType` fallback pattern documented.
- Glossary rows added to `ia/specs/glossary.md`: **Landmark**, **Big project**, **Tier-defining landmark**, **Intra-tier reward landmark**, **Landmark catalog row**, **Landmark sidecar**, **Commission ledger**, **Super-utility building**. `specReference` = `landmarks-system §{section}` (stub sections exist).
- `ia/specs/landmarks-system.md` — new file, frontmatter per `ia/templates/spec-template.md`. Sections: §1 Overview (fill), §2 Catalog schema (fill), §3 Progression state machine (stub — "filled in Stage 4.2"), §4 Commission pipeline (stub), §5 Placement + reconciliation (stub), §6 Landmarks↔Utilities bridge (stub), §7 BUG-20 interaction (stub), §8 Save schema (stub).
- MCP regen — `npm run validate:all` updates `tools/mcp-ia-server/data/glossary-index.json` + `spec-index.json` including new spec + glossary rows.
- EditMode test — instantiate Store in test scene w/ fixture YAML, assert 6 rows loaded, assert `GetById("big_power_plant").contributorScalingFactor == 10`.
- Phase 1 — `LandmarkCatalogStore` MonoBehaviour + YAML parse.
- Phase 2 — Glossary rows + spec stub + MCP regen.
- Phase 3 — Store EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | LandmarkCatalogStore scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/LandmarkCatalogStore.cs` — MonoBehaviour, `[SerializeField] private string catalogRelativePath = "landmark-catalog.yaml"`, internal `List<LandmarkCatalogRow>` + `Dictionary<string, LandmarkCatalogRow>`. Invariant #4 pattern — Inspector + `FindObjectOfType` fallback. |
| T3.2 | YAML load + GetById/GetAll | _pending_ | _pending_ | Implement `Awake` YAML load via existing Unity YAML parser (check `UtilityContributorRegistry` serialization stack in sibling Bucket 4-a OR add minimal YamlDotNet dep). Build `byId` index; expose `GetAll()` + `GetById(id)`. |
| T3.3 | Glossary rows seed | _pending_ | _pending_ | Edit `ia/specs/glossary.md` — add 8 rows per Step 1 exit criteria (**Landmark**, **Big project**, **Tier-defining landmark**, **Intra-tier reward landmark**, **Landmark catalog row**, **Landmark sidecar**, **Commission ledger**, **Super-utility building**). `specReference` = `landmarks-system §{stub id}`. |
| T3.4 | landmarks-system.md spec stub | _pending_ | _pending_ | Create `ia/specs/landmarks-system.md` — frontmatter per `ia/templates/spec-template.md`, §1 Overview + §2 Catalog schema populated (copy schema table from LandmarkCatalogRow XML docs), §3–§8 stub headings with "filled in Stage 4.2" placeholder + exploration-doc link. |
| T3.5 | MCP index regen | _pending_ | _pending_ | Run `npm run validate:all` → regenerates glossary + spec indexes. Commit regenerated JSON artifacts alongside spec + glossary edits. |
| T3.6 | CatalogStore EditMode test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/LandmarkCatalogStoreTests.cs` — fixture test scene w/ Store component + fixture YAML under `Assets/Tests/Fixtures/landmark-catalog.yaml`. Assert 6 rows, assert tier-defining rows have `commissionCost == 0`, assert `big_power_plant.contributorScalingFactor == 10`. |
| T3.7 | GetById miss + duplicate id test | _pending_ | _pending_ | Add tests — `GetById("unknown")` returns null + logs warning once; fixture YAML w/ duplicate id fails validator (T1.2.3) before Store load (sanity cross-check the validator catches what Store would otherwise silently dedupe). |

### Stage 4 — LandmarkProgressionService (unlock-only) / Service scaffold + unlock flags

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Scaffold the MonoBehaviour + seed unlock flags from catalog. No tick logic yet — just structure + Inspector wiring.

**Exit:**

- `LandmarkProgressionService` MonoBehaviour exists with serialized refs + `FindObjectOfType` fallback.
- `Awake` populates `unlockedById` with one `false` entry per catalog row.
- `IsUnlocked(string id)` read API returns the flag value.
- Compile clean; no runtime side effects yet.
- Phase 1 — MonoBehaviour scaffold + ref wiring + unlock dictionary seed.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Service MonoBehaviour scaffold | _pending_ | _pending_ | Add `LandmarkProgressionService.cs` MonoBehaviour. `[SerializeField] private ScaleTierController scaleTier`, `[SerializeField] private PopulationAggregator population`, `[SerializeField] private LandmarkCatalogStore catalog`. `Awake` applies `FindObjectOfType` fallback per invariant #4; log error if any remain null. |
| T4.2 | Unlock dictionary seed | _pending_ | _pending_ | In `Awake` (after catalog load + ref fallback), populate `Dictionary<string, bool> unlockedById` — one false entry per `catalog.GetAll()` row. Guard against duplicate ids (catalog validator catches but runtime defensive). |
| T4.3 | IsUnlocked read API | _pending_ | _pending_ | Add `public bool IsUnlocked(string id)` — returns dict flag or false on miss. XML doc clarifies that miss = unknown landmark (log warning once). Used by `BigProjectService.TryCommission` in Stage 3.2. |

### Stage 5 — LandmarkProgressionService (unlock-only) / Gate evaluation + tick loop

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the `Tick()` gate evaluator per `LandmarkPopGate` discriminator. Raise `LandmarkUnlocked` event on false→true transitions. Idempotent — once unlocked stays unlocked.

**Exit:**

- `Tick()` walks catalog rows; dispatches per-gate via pattern match on `ScaleTransitionGate` vs `IntraTierGate`.
- `ScaleTransitionGate` check — `scaleTier.CurrentTier` compares > `gate.fromTier` (Region > City, Country > Region).
- `IntraTierGate` check — `population.GetPopForCurrentScale() >= gate.pop`.
- On false→true transition, fires `event Action<string> LandmarkUnlocked`. Once true, the per-row evaluator skips re-evaluation (idempotent short-circuit).
- EditMode test — synthetic `ScaleTierController` fake + `PopulationAggregator` fake; drive tick stream across scale transition + pop crossing; assert unlock event order + idempotency.
- Phase 1 — Gate pattern-match evaluator.
- Phase 2 — Tick loop + event emission.
- Phase 3 — EditMode tests for unlock order.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Gate evaluator per discriminator | _pending_ | _pending_ | Add `private bool EvaluateGate(LandmarkPopGate gate)` — pattern match: `ScaleTransitionGate g` → `scaleTier.CurrentTier > g.fromTier`; `IntraTierGate g` → `population.GetPopForCurrentScale() >= g.pop`. XML doc both branches. |
| T5.2 | Tick loop + event | _pending_ | _pending_ | Implement `public void Tick()` — foreach catalog row, skip if `unlockedById[row.id] == true`; else `if (EvaluateGate(row.popGate)) { unlockedById[row.id] = true; LandmarkUnlocked?.Invoke(row.id); }`. Add `public event Action<string> LandmarkUnlocked`. |
| T5.3 | Unlock order EditMode tests | _pending_ | _pending_ | Add `LandmarkProgressionServiceTests.cs` — fake ScaleTierController + PopulationAggregator; drive ticks: pop rises below threshold (no event), pop crosses intra-tier threshold (1 event), scale transitions (tier-defining event), re-tick (no re-emit = idempotent). |
| T5.4 | Catalog re-entry guard test | _pending_ | _pending_ | Test case: call `Tick()` 100× after unlock; assert event fires exactly once per row. Guard against future edits that might accidentally re-evaluate. |

### Stage 6 — LandmarkProgressionService (unlock-only) / Tick ordering + bootstrap integration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Register `LandmarkProgressionService.Tick` into `GameManager` / `SimulationManager` tick bus AFTER `ScaleTierController.Tick`. Per Review Notes NON-BLOCKING item — tier-defining unlock fires one tick late if ordering wrong.

**Exit:**

- `GameManager` (or `SimulationManager.Tick` if that's the canonical bus) calls `scaleTier.Tick()` → `population.Tick()` → `landmarkProgression.Tick()` in that order. Code comment at touch site cites Review Notes.
- `LandmarkProgressionService` ref cached in `Awake` per invariant #3.
- Integration EditMode test — run one tick where scale transition + intra-tier threshold cross happen simultaneously; assert tier-defining fires THIS tick (not next).
- Phase 1 — Bootstrap tick registration + integration tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Bootstrap tick wiring | _pending_ | _pending_ | Edit `GameManager.cs` (or `SimulationManager.cs` — check canonical tick bus at stage-file time). Cache `LandmarkProgressionService` ref in `Awake` (invariant #3); invoke `Tick()` AFTER `ScaleTierController.Tick()`. Code comment cites Review Notes NON-BLOCKING. |
| T6.2 | Same-tick ordering integration test | _pending_ | _pending_ | Add integration test fixture w/ real `GameManager` bootstrap; drive tick where scale crosses AND intra-tier pop crosses same tick; assert both events fire same tick in scale-first order (tier-defining before intra-tier). |
| T6.3 | Boot-null fallback test | _pending_ | _pending_ | Add test — bootstrap scene missing a service ref (scaleTier null); assert `LandmarkProgressionService.Awake` logs error + subsequent `Tick()` short-circuits (no NPE). Guard against boot-order drift. |

### Stage 7 — BigProjectService + LandmarkPlacementService + sidecar save / LandmarkPlacementService + cell-tag write

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the placement helper (under `GameManagers/*Service.cs` carve-out). `Place(id, cell, scale)` writes sprite tile + cell-tag. No sidecar yet (Stage 3.3), no commission yet (Stage 3.2) — just the placement atom that both tracks call.

**Exit:**

- `LandmarkPlacementService.cs` MonoBehaviour — `[SerializeField] private GridManager grid`, `[SerializeField] private LandmarkCatalogStore catalog`. `FindObjectOfType` fallback per invariant #4.
- `Place(string landmarkId, CellCoord cell, ScaleTag scale)` — looks up row, writes sprite tile at `(cell.x, cell.y)` via existing tile API, sets `grid.GetCell(cell.x, cell.y).landmarkId = landmarkId` (new `Cell.landmarkId` string field, nullable). Invariant #1 safe — no height write.
- `Cell.landmarkId` field added to existing `Cell` struct (check file at stage-file time — likely `Assets/Scripts/Grid/Cell.cs`).
- `event Action<string, CellCoord, ScaleTag> LandmarkPlaced` fires after write (consumed by sidecar + future CityStats feed).
- EditMode test — drive `Place("regional_plocks", (42,88), Region)`, assert cell-tag set, assert sprite tile registered, assert `LandmarkPlaced` event fired once.
- Phase 1 — Service scaffold + `Cell.landmarkId` field.
- Phase 2 — Place method + event emission + EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | LandmarkPlacementService scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/LandmarkPlacementService.cs` MonoBehaviour. `[SerializeField]` GridManager + LandmarkCatalogStore refs + `FindObjectOfType` fallback. XML doc cites invariant #5 + #6 carve-out (service under GameManagers allowed cellArray access; no GridManager responsibility creep). |
| T7.2 | Cell.landmarkId field | _pending_ | _pending_ | Edit `Assets/Scripts/Grid/Cell.cs` (or canonical cell struct location — verify at stage-file time). Add `public string landmarkId` nullable field. XML doc: "denormalized index of sidecar row; null when no landmark placed; rebuilt from sidecar on load." |
| T7.3 | Place method + invariant #1 guard | _pending_ | _pending_ | Implement `Place(landmarkId, cell, scale)` — catalog lookup, sprite tile write via existing BuildingPlacementService tile API pattern, `grid.GetCell(cell.x, cell.y).landmarkId = landmarkId`. Code comment: `// Invariant #1 — placement is tile-sprite only, no HeightMap mutation`. |
| T7.4 | LandmarkPlaced event | _pending_ | _pending_ | Add `public event Action<string, CellCoord, ScaleTag> LandmarkPlaced`; invoke after cell-tag write in `Place`. Consumers wired in Stage 3.3 (sidecar) + Step 4 (utilities bridge). |
| T7.5 | Placement EditMode test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/LandmarkPlacementServiceTests.cs` — fixture scene w/ GridManager + Store + PlacementService. Call `Place("regional_plocks", (42,88), Region)`; assert `grid.GetCell(42,88).landmarkId == "regional_plocks"`, assert event fired once, assert height untouched (invariant #1 check). |

### Stage 8 — BigProjectService + LandmarkPlacementService + sidecar save / BigProjectService commission pipeline

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the commission pipeline — `TryCommission` opens bond + ledger row; `Tick()` advances monthly progress; completion fires `LandmarkBuildCompleted` which invokes placement. Pause / resume supported; cancel NOT supported (v1 locked).

**Exit:**

- `BigProjectService.cs` MonoBehaviour — refs to `ServiceBudgetService`, `LandmarkCatalogStore`, `LandmarkProgressionService`, `LandmarkPlacementService`, `TimeManager`. `FindObjectOfType` fallback per invariant #4.
- `CommissionLedgerRow` class — `string landmarkId`, `int principal`, `int monthsElapsed`, `int buildMonths`, `bool paused`, `BondRef bondRef`, `CellCoord targetCell`, `ScaleTag targetScale`.
- `CommissionResult` enum — `Ok`, `NotUnlocked`, `AlreadyCommissioned`, `BondDeclined`, `UnknownLandmark`.
- `TryCommission(string id, CellCoord cell, ScaleTag scale)` — checks unlock flag via `progression.IsUnlocked`, checks no existing row for same id, calls `serviceBudget.OpenBond(row.commissionCost, this)` (this = `IBondConsumer` impl), appends ledger row. Returns result.
- `OnGameMonth` handler advances `monthsElapsed` on non-paused rows; on `monthsElapsed >= buildMonths` fires `event Action<string, CellCoord, ScaleTag> LandmarkBuildCompleted` + removes row from active ledger.
- Tier-defining bypass — subscribes to `progression.LandmarkUnlocked`; when row has `buildMonths == 0 && commissionCost == 0`, fires `LandmarkBuildCompleted` immediately at default cell (scale-capital cell lookup via `ScaleTierController` OR fallback grid center).
- `Pause(string id)` / `Resume(string id)` flip `paused` flag on matching ledger row.
- EditMode tests — commission flow, pause blocks progress, tier-defining bypass, bond decline path, not-unlocked reject.
- Phase 1 — Data scaffolds + `BigProjectService` MonoBehaviour + `TryCommission`.
- Phase 2 — Monthly tick + completion event.
- Phase 3 — Tier-defining bypass + pause/resume.
- Phase 4 — EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | CommissionLedgerRow + CommissionResult | _pending_ | _pending_ | Add `Assets/Scripts/Data/Landmarks/CommissionLedgerRow.cs` serializable class (fields listed in Stage exit) + `CommissionResult.cs` enum. |
| T8.2 | BigProjectService scaffold + refs | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/BigProjectService.cs` MonoBehaviour. `[SerializeField]` slots for ServiceBudgetService, LandmarkCatalogStore, LandmarkProgressionService, LandmarkPlacementService, TimeManager. Inventory `List<CommissionLedgerRow>` (active). `FindObjectOfType` fallback per invariant #4. |
| T8.3 | TryCommission + bond open | _pending_ | _pending_ | Implement `TryCommission(id, cell, scale)` — validate unlock via `progression.IsUnlocked(id)`, check no duplicate ledger row, call `serviceBudget.OpenBond(row.commissionCost, this)`. On success append ledger row. Return `CommissionResult`. `IBondConsumer` impl = this service (Bucket 3 contract stub). |
| T8.4 | Monthly tick + completion event | _pending_ | _pending_ | Add `OnGameMonth` handler — iterate non-paused rows, `monthsElapsed++`. On `monthsElapsed >= buildMonths` fire `event Action<string, CellCoord, ScaleTag> LandmarkBuildCompleted` + remove row. Subscribe to `TimeManager.OnGameMonth`. |
| T8.5 | LandmarkBuildCompleted → placement wiring | _pending_ | _pending_ | Subscribe `placement.Place` to `LandmarkBuildCompleted` event in `OnEnable` / unsubscribe in `OnDisable`. Callback — `placement.Place(id, cell, scale)`. |
| T8.6 | Tier-defining bypass | _pending_ | _pending_ | Subscribe to `progression.LandmarkUnlocked` — if `catalog.GetById(id).buildMonths == 0 && commissionCost == 0`, skip commission, fire `LandmarkBuildCompleted` immediately. Default cell = scale-capital cell (add `ScaleTierController.GetCapitalCell(tier)` or fallback grid-center helper). |
| T8.7 | Pause + resume API | _pending_ | _pending_ | Add `public void Pause(string id)` / `Resume(string id)` — look up ledger row by id, flip `paused` flag. No-op on unknown id. |
| T8.8 | Commission flow EditMode tests | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/BigProjectServiceTests.cs` — fake ServiceBudget + fixture catalog. Core flow: commission→tick×18→complete→placement invoked; pause→tick×N→no progress; resume→tick→progress resumes. |
| T8.9 | Commission reject-path tests | _pending_ | _pending_ | Extend test suite — `TryCommission` returns `NotUnlocked` when progression flag false; returns `AlreadyCommissioned` on duplicate id; returns `BondDeclined` when fake budget refuses bond; returns `UnknownLandmark` on catalog miss. Tier-defining bypass test — `LandmarkUnlocked` event w/ `buildMonths==0` fires `LandmarkBuildCompleted` same tick. |

### Stage 9 — BigProjectService + LandmarkPlacementService + sidecar save / Sidecar save + reconciliation on load

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Persist placed landmarks + commission ledger to `landmarks.json` sidecar; extend main-save per-scale cell-tag map w/ `landmarkId`; atomic write with main save; reconcile on load (sidecar wins). Schema piggybacks on Bucket 3 v3 envelope — no mid-tier bump here.

**Exit:**

- `LandmarkSidecarDto` serializable class mirroring sidecar schema (`schemaVersion: 1`, `landmarks[]`, `commissionLedger[]`).
- `GameSaveManager` writes + reads sidecar — `landmarks.json` path = `{persistentDataPath}/{saveSlot}/landmarks.json`. Atomic write: temp file + atomic rename, paired with main-save temp+rename per Review Notes Phase C.
- Main-save `GameSaveData` v3 envelope additions — `regionCells[].landmarkId` + `cityCells[].landmarkId` (nullable). Code comment: `// schemaVersion bump owned by Bucket 3 (zone-s-economy); landmarks adds fields to v3 envelope only`.
- Write path — `GameSaveManager.Save` extension: walk `LandmarkPlacementService` sidecar row inventory + `BigProjectService.activeLedger`; serialize to DTO; atomic write.
- Read path — `GameSaveManager.Load` extension, after grid cell restore (existing step): (1) load sidecar first (sidecar = truth); (2) walk sidecar `landmarks[]` — for each row, call `placement.RestoreCellTag(row)` idempotent helper; (3) walk main-save cell tags; for each tag w/o matching sidecar row, clear tag + log diagnostic (dangling); (4) restore `BigProjectService.activeLedger` from `commissionLedger[]`.
- Load guard — when `saveData.schemaVersion < 3`, skip cell-tag read; when sidecar missing, treat as empty list (new save OR pre-landmark save).
- Diagnostic channel — `Debug.Log` reconciliation delta (count of restored tags, count of dangling tags cleared).
- PlayMode round-trip test — place `regional_plocks` + commission `big_power_plant` mid-build (months=5/18), save, reload, assert sidecar + cell-tag restored + ledger progress preserved + pause flag preserved.
- Divergence test — hand-craft save where sidecar has `big_power_plant @ (17,33)` but main-save cell-tag absent → reload → assert tag restored + diagnostic logged.
- Dangling test — hand-craft save where main-save has cell-tag but sidecar row missing → reload → assert tag cleared + diagnostic logged.
- Phase 1 — Sidecar DTO + write path + main-save cell-tag field.
- Phase 2 — Load + reconciliation pipeline + atomic-write pairing.
- Phase 3 — PlayMode round-trip + divergence tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | LandmarkSidecarDto | _pending_ | _pending_ | Add `Assets/Scripts/Data/Landmarks/LandmarkSidecarDto.cs` — `[Serializable]` class w/ `int schemaVersion = 1`, `List<LandmarkSidecarRow> landmarks`, `List<CommissionLedgerRow> commissionLedger`. Nested `LandmarkSidecarRow` w/ `id`, `cell`, `footprint`, `placedTick`, `active`, `counters`. |
| T9.2 | GameSaveManager sidecar write | _pending_ | _pending_ | Edit `GameSaveManager.cs` — add `WriteSidecar(saveSlot)` private method. Walks `LandmarkPlacementService` inventory + `BigProjectService.activeLedger`; serializes to DTO; writes to `{persistentDataPath}/{saveSlot}/landmarks.json.tmp`. Called from Save pipeline after main-save serialization. |
| T9.3 | Main-save cell-tag field | _pending_ | _pending_ | Edit main-save `GameSaveData` DTO (verify exact location at stage-file time). Add nullable `string landmarkId` to `regionCells[]` + `cityCells[]` DTO. Comment: `// v3 envelope — Bucket 3 owns schemaVersion bump; landmarks additive`. |
| T9.4 | Sidecar read + reconciliation | _pending_ | _pending_ | Edit `GameSaveManager.Load` — after grid cell restore: (1) load sidecar if exists; (2) foreach sidecar row call `placement.RestoreCellTag(row)`; (3) walk cell-tags, clear dangling + log diagnostic; (4) restore `BigProjectService.activeLedger` from `commissionLedger`. Guard `if (saveData.schemaVersion < 3) skip`. |
| T9.5 | LandmarkPlacementService.RestoreCellTag | _pending_ | _pending_ | Add `public void RestoreCellTag(LandmarkSidecarRow row)` — idempotent write to `grid.GetCell(row.cell.x, row.cell.y).landmarkId = row.id`. Does NOT re-emit `LandmarkPlaced` event (load-path, not place-path). |
| T9.6 | Atomic write pairing | _pending_ | _pending_ | Refactor `GameSaveManager.Save` — write both main-save + sidecar to `.tmp`, then atomic-rename both as a pair (File.Move). Fail-safe: if rename fails mid-pair, leave `.tmp` files for next save recovery. Doc cites Review Notes Phase C sidecar bundling. |
| T9.7 | Save round-trip PlayMode test | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Landmarks/LandmarkSaveRoundTripTests.cs` — place `regional_plocks`, commission `big_power_plant` to months=5/18, pause, save, reload. Assert: sidecar row restored, cell-tag restored, ledger row restored w/ `paused == true` + `monthsElapsed == 5`. |
| T9.8 | Reconciliation divergence tests | _pending_ | _pending_ | Add tests: (a) sidecar has row, main-save cell-tag absent → reload → tag restored + diagnostic count 1; (b) main-save has tag, sidecar missing row → reload → tag cleared + diagnostic count 1; (c) both present + matching → no diagnostic. |

### Stage 10 — Super-utility bridge + UI surface + spec closeout / Super-utility contributor bridge

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire the narrow catalog bridge to sibling Bucket 4-a `UtilityContributorRegistry`. On `LandmarkBuildCompleted` with non-null `utilityContributorRef`, call `Register`. On load-path re-registration via sidecar restore, same call. **Hard sequencing dep:** utilities Stage 1.3 closed before this stage files.

**Exit:**

- `LandmarkPlacementService` adds optional `[SerializeField] private UtilityContributorRegistry utilityRegistry` (Bucket 4-a type). `Awake` fallback + nullable handling (bucket-4-a not always loaded in test scenes).
- `Place` method — after cell-tag write, if `row.utilityContributorRef != null && utilityRegistry != null`, call `utilityRegistry.Register(row.utilityContributorRef, row.contributorScalingFactor)`. Log if registry missing but row has non-null ref (misconfiguration).
- `RestoreCellTag` (load-path) — same conditional re-registration. Ensures re-register on load so utility pools rebuild correctly.
- `Unregister` path — v1 has no in-game landmark destruction, but scaffolded for future: new `Demolish(string id)` method clears cell-tag + sidecar row + if super-utility calls `utilityRegistry.Unregister(id)`. Exposed but not invoked by any UI in v1.
- EditMode bridge test — fake `UtilityContributorRegistry`, place `big_power_plant`, assert `Register("contributors/coal_plant", 10.0f)` called once. Place `regional_plocks` (null ref) — assert `Register` NOT called.
- Load-path bridge test — restore sidecar w/ 1 super-utility row, assert `Register` called during `RestoreCellTag`.
- Phase 1 — Registry ref + place-path bridge.
- Phase 2 — Load-path bridge + Demolish scaffold.
- Phase 3 — Bridge EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | UtilityContributorRegistry ref | _pending_ | _pending_ | Edit `LandmarkPlacementService.cs` — `[SerializeField] private UtilityContributorRegistry utilityRegistry` (nullable). `Awake` — `FindObjectOfType` fallback; log info if still null (OK for test scenes). |
| T10.2 | Place-path super-utility register | _pending_ | _pending_ | In `Place` method, after cell-tag write: `if (row.utilityContributorRef != null) { if (utilityRegistry != null) utilityRegistry.Register(row.utilityContributorRef, row.contributorScalingFactor); else Debug.LogWarning($"Landmark {row.id} has utilityContributorRef but no registry wired"); }`. |
| T10.3 | Load-path re-register | _pending_ | _pending_ | `RestoreCellTag` — same conditional registry.Register call. Ensures load-path rebuilds utility pool contributors. XML doc note: "idempotent — utility registry dedupes by id." |
| T10.4 | Demolish scaffold | _pending_ | _pending_ | Add `public void Demolish(string id)` — look up sidecar row, clear cell-tag at `(row.cell.x, row.cell.y).landmarkId = null`, remove sidecar row, if `row.utilityContributorRef != null` call `utilityRegistry.Unregister(id)`. Not invoked by UI in v1; scaffolded for post-MVP destructibility. |
| T10.5 | Place-path bridge test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/SuperUtilityBridgeTests.cs` — fake `UtilityContributorRegistry`, place `big_power_plant`, assert `Register("contributors/coal_plant", 10.0f)` called once; place `regional_plocks` (null ref), assert no register call. |
| T10.6 | Load-path bridge test | _pending_ | _pending_ | Add test: construct sidecar DTO w/ 1 super-utility row + 1 non-utility row. Call `RestoreCellTag` for each. Assert fake registry has exactly one `Register` call w/ correct ref + multiplier. |

### Stage 11 — Super-utility bridge + UI surface + spec closeout / UI surface (progress panel + commission dialog)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship minimum-viable UI — progress panel listing state + commission dialog. No tooltip / onboarding polish (Bucket 6 owns). **Hard dep:** Bucket 6 `UiTheme` must land (Tier B' exit).

**Exit:**

- `LandmarkProgressPanel.cs` MonoBehaviour — constructs UGUI list, rows categorized (Unlocked-available / In-progress / Locked). Row shows `displayName`, cost, build months, state badge. Click-to-open commission dialog for available rows.
- Refresh triggers: `LandmarkProgressionService.LandmarkUnlocked`, `BigProjectService.LandmarkBuildCompleted`, per-game-month (progress bar for in-progress rows).
- `CommissionDialog.cs` — modal confirms cost + build months + target cell (default = player-selected cell via existing placement-mode UI OR scale-capital fallback); on confirm invokes `BigProjectService.TryCommission`. Renders result enum.
- Toolbar entry — new "Landmarks" button in existing UI toolbar opens progress panel. Reuse `UIManager.Toolbar.cs` pattern.
- `Awake` caches all service refs per invariant #3 (no per-frame `FindObjectOfType`).
- PlayMode smoke — open progress panel, confirm commission, advance months via debug hook, assert landmark placed + panel reflects state.
- Phase 1 — `LandmarkProgressPanel` layout + list rendering.
- Phase 2 — `CommissionDialog` modal + confirm flow.
- Phase 3 — Toolbar entry + live-binding refresh + PlayMode smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | LandmarkProgressPanel layout | _pending_ | _pending_ | Add `Assets/Scripts/UI/LandmarkProgressPanel.cs` MonoBehaviour. `Awake` caches LandmarkCatalogStore, LandmarkProgressionService, BigProjectService refs (invariant #3). Build UGUI vertical list w/ three sections (Available / In progress / Locked). |
| T11.2 | Row rendering + state badge | _pending_ | _pending_ | Row prefab shows `displayName`, commission cost, build months, state badge (colour per section). In-progress rows show progress bar (`monthsElapsed / buildMonths`). Uses existing `UiTheme` palette (Bucket 6 dep). |
| T11.3 | CommissionDialog modal | _pending_ | _pending_ | Add `Assets/Scripts/UI/CommissionDialog.cs` MonoBehaviour — modal w/ cost + months + target-cell readout + Confirm/Cancel. Confirm invokes `BigProjectService.TryCommission(id, cell, scale)`; renders `CommissionResult` outcome (toast or inline status). |
| T11.4 | Target-cell selection | _pending_ | _pending_ | Commission dialog — integrates with existing placement-mode cell-pick flow OR falls back to scale-capital cell. Add `ScaleTierController.GetCapitalCell(tier)` helper if missing (see Stage 3.2 T3.2.6). |
| T11.5 | Toolbar entry | _pending_ | _pending_ | Edit `UIManager.Toolbar.cs` — add "Landmarks" button opening `LandmarkProgressPanel`. Icon = placeholder (Bucket 5 coordination). |
| T11.6 | Live-binding refresh | _pending_ | _pending_ | `LandmarkProgressPanel` subscribes to `LandmarkProgressionService.LandmarkUnlocked` + `BigProjectService.LandmarkBuildCompleted` + `TimeManager.OnGameMonth` (for progress bar). Unsubscribes in `OnDisable`. |
| T11.7 | PlayMode commission smoke | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Landmarks/LandmarkCommissionSmoke.cs` — open panel, confirm commission on `big_power_plant`, advance 18 game-months via debug hook, assert landmark placed at cell, panel row moved from In-progress to Unlocked section. |

### Stage 12 — Super-utility bridge + UI surface + spec closeout / landmarks-system.md §3–§8 prose + glossary specRef update

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill spec sections 3–8 w/ full prose (state machine, commission pipeline, placement + reconciliation, landmarks↔utilities bridge, BUG-20 interaction, save schema). Update glossary rows added in Stage 1.3 to point at specific §. Cross-link sibling utilities doc. Same end-of-plan spec-closeout pattern as utilities Stage 4.2.

**Exit:**

- `ia/specs/landmarks-system.md` §3 Progression state machine — unlock gate discriminator, idempotency rule, tick ordering, example flow for both tracks.
- §4 Commission pipeline — `TryCommission` contract, bond open, monthly tick, pause/resume, tier-defining bypass, `CommissionResult` matrix.
- §5 Placement + reconciliation — `LandmarkPlacementService.Place` + `RestoreCellTag`, atomic-save pairing, sidecar-wins rule, dangling-tag clear, diagnostic channel.
- §6 Landmarks↔Utilities bridge — `Register` / `Unregister` call contract, nullable `utilityContributorRef` semantics, sibling Bucket 4-a ownership. Marked authoritative; sibling utilities doc consumes.
- §7 BUG-20 interaction — orthogonal note; landmark placement is tile-sprite only, invariant #1 compliant, does not fix or reopen BUG-20 (which concerns visual-restore of zone buildings).
- §8 Save schema — sidecar JSON schema, v3 envelope cell-tag extract, Bucket 3 coordination note.
- Glossary `specReference` fields updated to precise §N anchors (e.g. **Commission ledger** → `landmarks-system §4`).
- Sibling `docs/landmarks-exploration.md` closing link → `ia/specs/landmarks-system.md` noted as canonical landing doc.
- Sibling `ia/projects/utilities-master-plan.md` Stage 4.2 sibling-contract section cross-linked (coordinate at stage-file time — may require edit to utilities doc).
- `npm run validate:all` green — spec index regen + glossary graph index regen.
- Phase 1 — §3 + §4 prose authoring.
- Phase 2 — §5 + §6 + §7 prose authoring.
- Phase 3 — §8 save schema + glossary specRef update.
- Phase 4 — Cross-links + MCP regen.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | §3 Progression state machine prose | _pending_ | _pending_ | Edit `ia/specs/landmarks-system.md` §3 — document `LandmarkPopGate` discriminator, `Tick()` evaluation order, idempotency guard, tick-ordering rule (after ScaleTierController). Include scale-transition + intra-tier example flow. |
| T12.2 | §4 Commission pipeline prose | _pending_ | _pending_ | §4 — `TryCommission` contract (unlock check, dedupe, bond open), `OnGameMonth` tick, `LandmarkBuildCompleted` event, pause/resume semantics, tier-defining bypass rule, `CommissionResult` enum matrix. |
| T12.3 | §5 Placement + reconciliation prose | _pending_ | _pending_ | §5 — `LandmarkPlacementService.Place` + `RestoreCellTag`, invariant #1 compliance note, atomic-save pairing (main + sidecar temp+rename), sidecar-wins reconciliation rule, dangling-tag clear, diagnostic log format. |
| T12.4 | §6 Landmarks↔Utilities bridge prose | _pending_ | _pending_ | §6 — `UtilityContributorRegistry.Register(ref, multiplier)` call contract, load-path re-register via `RestoreCellTag`, nullable `utilityContributorRef` semantics. Mark section authoritative; note sibling Bucket 4-a consumes. |
| T12.5 | §7 BUG-20 interaction prose | _pending_ | _pending_ | §7 — short section documenting that landmark placement is orthogonal to BUG-20 (visual-restore of zone buildings). Landmark placement = tile-sprite, invariant #1 safe, cell-tag rebuilt from sidecar on load. Does not fix or reopen BUG-20. |
| T12.6 | §8 Save schema prose | _pending_ | _pending_ | §8 — sidecar JSON schema table (fields + types), v3 envelope `regionCells[].landmarkId` + `cityCells[].landmarkId` extract, Bucket 3 ownership note (no mid-tier bump from this plan). |
| T12.7 | Glossary specReference updates | _pending_ | _pending_ | Edit `ia/specs/glossary.md` — update 8 rows added in Stage 1.3 to precise `specReference` (e.g. **Commission ledger** → `landmarks-system §4`, **Landmark sidecar** → `landmarks-system §5`). |
| T12.8 | Sibling doc cross-links + exploration closing note | _pending_ | _pending_ | Edit `docs/landmarks-exploration.md` — add closing note linking `ia/specs/landmarks-system.md` as canonical landing doc. Edit `ia/projects/utilities-master-plan.md` Stage 4.2 section — cross-link the landmarks-system §6 bridge contract. |
| T12.9 | MCP index regen + validate:all | _pending_ | _pending_ | Run `npm run validate:all` — regenerates glossary + spec indexes. Commit regen artifacts. Green signal required for stage close. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/landmarks-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/landmarks-exploration.md` §Design Expansion.
- Keep this orchestrator synced with `ia/projects/full-game-mvp-master-plan.md` Bucket 4-b row — per `project-spec-close` / `closeout` umbrella-sync rule.
- Respect sibling Bucket 4-a hard-dep — Stage 4.1 (super-utility bridge) files ONLY after utilities Stage 1.3 (`RegisterWithMultiplier`) closes.
- Respect Bucket 6 hard-dep — Stage 4.2 (UI) files ONLY after Bucket 6 `UiTheme` Tier B' exit lands.
- Coordinate schema bump with Bucket 3 — never introduce mid-tier `schemaVersion` bump; Bucket 3 owns v3.
- Flag every `commissionCost` placeholder touch with `// cost-catalog bucket 11` marker until migration lands.
- **Umbrella parallel-work rule** — never run `/stage-file` on this plan concurrent with sibling `ia/projects/utilities-master-plan.md` on same branch. Sequential filing only.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only terminal step landing triggers a final `Status: Final`; file stays.
- Silently promote post-MVP items (heritage / cultural landmarks, tourism effects, destructible landmarks, mid-build cancel + partial refund, multi-cell footprints, info-panel polish) into MVP stages — flag to a future `docs/landmarks-post-mvp-extensions.md` stub.
- Mutate `HeightMap` from placement path (invariant #1) — landmark placement is tile-sprite only. Any proposed height change requires a separate master-plan decision.
- Add responsibilities to `GridManager` (invariant #6). Cell-tag write belongs on `LandmarkPlacementService` under `GameManagers/*Service.cs` carve-out (invariant #5).
- Add singletons (invariant #4). All four services (`LandmarkCatalogStore`, `LandmarkProgressionService`, `BigProjectService`, `LandmarkPlacementService`) = MonoBehaviour + Inspector + `FindObjectOfType` fallback.
- Use `FindObjectOfType` in `Update` / per-frame loops (invariant #3). Cache in `Awake`.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` + `npm run unity:compile-check`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Resolve BUG-20 in this plan — orthogonal to landmark placement; track separately.

---
