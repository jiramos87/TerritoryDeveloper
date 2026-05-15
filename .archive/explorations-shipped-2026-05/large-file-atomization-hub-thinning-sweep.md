---
slug: large-file-atomization-hub-thinning-sweep
target_version: 1
extends_existing_plan: false
parent_plan_id: null
audience: agent
loaded_by: on-demand
created_at: 2026-05-11
related_audit: docs/large-file-atomization-state-audit-2026-05-11.md
parent_plans:
  - large-file-atomization-refactor (v2, closed 2026-05-08) — extracted Domain services
  - large-file-atomization-cutover-refactor (v1, closed 2026-05-09) — trimmed 6 hubs
locked_strategy_docs:
  - docs/large-file-atomization-componentization-strategy.md (Strategy γ)
  - docs/post-atomization-architecture.md (target architecture)
---

# Large-file atomization — hub-thinning sweep (exploration seed)

## §Grilling protocol (read first)

When `/design-explore` runs on this doc, every clarification poll MUST use the **`AskUserQuestion`** format and MUST use **simple product language** — no class names, no namespaces, no paths, no asmdef terms, no stage numbers in the question wording. Translate every technical question into player/designer terms ("the Grid script", "the Build menu", "the in-game economy panel", "the save system"). The body of this exploration doc + the resulting design doc stay in **technical caveman-tech voice** (class names, paths, glossary slugs welcome) — only the user-facing poll questions get translated.

Example translation:
- ❌ tech voice: "Should ZoneManager's `[SerializeField] gridManager` cross-ref be replaced by `IGrid` facade injection?"
- ✓ product voice: "Should the Zone tool keep its direct link to the Grid script, or talk to it through a shared interface?"

Persist until every Q1..QN is resolved.

## §Goal

Finish the hub-thinning effort started in the two parent plans. End state: **zero source file >500 LOC** across `Assets/Scripts/**` (excluding generated + `.archive/`). Every Unity GO-inspector hub script trimmed to a thin pass-through delegate, no file moves, no class renames, no `[SerializeField]` mutations — prefab + scene wiring preserved by construction.

## §State of work (from audit)

See `docs/large-file-atomization-state-audit-2026-05-11.md` for full table. Compressed roll-up:

**Landed (THIN, ≤200 LOC):**
- `InterstateManager.cs` (115)
- `RoadPrefabResolver.cs` (35)
- `UIManager.Theme.cs` (147)
- `TerraformingService.cs` (Manager copy, cutover)
- `AgentBridgeCommandRunner.Mutations.cs` (420 — partial of family)

**PARTIAL — port exists, hub still fat:**
- `GridManager.cs` (2321 LOC, 49 publics, only `cellAccessService` injected)
- `GeographyManager.cs` (696 LOC, only sorting body delegated)

**FAT — port exists in `Domains/{X}/Services/`, 0 Domain svc injection on hub:**
- `TerrainManager.cs` (4260), `RoadManager.cs` (3226), `WaterMap.cs` (2325 Core), `ZoneManager.cs` (1449), `CityStats.cs` (1328), `WaterManager.cs` (686), `EconomyManager.cs` (842), `UIManager.Utilities.cs` (508), `Core/Terrain/PathTerraformPlan.cs` (631)

**FAT — partial-class families (Editor):**
- `AgentBridgeCommandRunner.cs` (1919) + `.Conformance.cs` (1219) — core still fat
- `UiBakeHandler.cs` (2336) + `.Archetype.cs` (2532) + `.Frame.cs` (1036) + `.Button.cs` (551)
- `AgentTestModeBatchRunner.cs` (1221)
- `AgentDiagnosticsReportsMenu.cs` (640)

**NO-PORT — Domain folder missing, needs scaffold + extract + cutover:**
- `ForestManager.cs` (847) → `Domains/Forests/`
- `GameSaveManager.cs` (668) → `Domains/Save/`
- `BlipVoice.cs` (716) → `Domains/Audio/`
- `ProceduralRiverGenerator.cs` (837) → `Domains/Water/` (extend) or new sub-Domain
- `DemandManager.cs` (558) → `Domains/Demand/`
- `GameNotificationManager.cs` (519) → `Domains/Notifications/`
- `MiniMapController.cs` (559) → `Domains/UI/` (extend) or `Domains/Map/`
- `ToolbarDataAdapter.cs` (543) → `Domains/UI/`
- `CursorManager.cs` (352) → `Domains/Cursor/` (under cap but tied to picking concern)
- (Buildings + Camera Domains also missing — see audit for full list)

**Domain services already >500 LOC — need second-pass internal split:**
- `Domains/Roads/Services/AutoBuildService.cs` (1283)
- `Domains/Roads/Services/InterstateService.cs` (1180)
- `Domains/Terrain/Services/TerraformingService.cs` (926)
- `Domains/Roads/Services/PrefabResolverService.cs` (922)
- `Domains/UI/Services/ThemeService.cs` (530)

## §Locked constraints (from cutover plan v1)

1. DO NOT move hub file path.
2. DO NOT rename hub class / namespace.
3. DO NOT modify any `[SerializeField]` field.
4. Preserve every public method signature + Unity lifecycle hook + coroutine attachment.
5. Body absorbed by `Domains/{X}/Services/{Concern}Service.cs` POCO (not MonoBehaviour).
6. Cross-asmdef refs flow through `I{X}` facade interface only.
7. Dead-public sweep flagged in `§Open Questions`, NOT auto-deleted.
8. Hard cap target: hub ≤200 LOC, services ≤500 LOC.
9. Per-stage verification: `validate:all` + `unity:compile-check` + scene-load smoke + ≥1 PlayMode smoke per cutover concern.

## §Reference shape — what THIN looks like

`Assets/Scripts/Managers/GameManagers/InterstateManager.cs` (115 LOC):
- File path + class name + namespace UNCHANGED.
- Every `[SerializeField]` UNCHANGED.
- `Awake()` instantiates `_service = new Domains.{X}.Services.{Y}Service(deps...)`.
- Every public method body = single-line `=> _service.Method(args);`.
- Class implements `Domains.{X}.I{X}` facade.
- Private helpers (formerly inline) → moved into the service POCO, no forwarder needed.

## §Proposed tier structure (to be challenged in design-explore)

- **Tier A — scaffold missing Domains.** One stage per new Domain folder: `I{X}.cs` + `{X}.cs` facade + `{X}.asmdef`. No body move. Compile-gate only.
- **Tier B — hubs with port ready.** One stage per hub. Pass-through cutover. Reference pattern = InterstateManager.
- **Tier C — hubs needing extract+cutover.** Two-task stage: extract POCO service first, then cutover hub.
- **Tier D — partial-class families.** UiBakeHandler.*, AgentBridgeCommandRunner.*, AgentTestModeBatchRunner — extract concern services from each partial; hub partials become thin.
- **Tier E — second-pass service split.** Per-service internal split into sub-services (AutoBuild, Interstate, Terraforming, PrefabResolver, Theme).
- **Tier F — gate hardening.** `validate:no-hub-fat` (any Manager/Controller .cs >200 LOC = red), `validate:no-service-fat` (Domain service >500 LOC = red).

## §Acceptance gate

```
find Assets/Scripts -name '*.cs' ! -path '*/.archive/*' -exec wc -l {} \; | awk '$1>500'
```
returns ZERO lines after final stage. Plus: scene loads, all PlayMode smokes green, `validate:all` green.

## §Pre-conditions

- BUG-63 (.meta capture in stage commit) — confirm still green.
- `validate:no-domain-game-cycle` (landed Stage 20 of v1 plan) — confirm still green.
- Branch hygiene: confirm `feature/asset-pipeline` (or successor) is stable before sweep begins.

## §Open questions (to grill in product voice via AskUserQuestion)

Each question below has a **tech statement** for the design doc and a **product wording** for the poll. Grill loop in `/design-explore` must use the product wording.

### Q1 — Shared interface vs. direct script link
- **Tech:** Hubs reference each other today via concrete fields (e.g. `ZoneManager.gridManager`, `TerrainManager.waterManager`). Should cutover swap these to `I{X}` facade-interface injection now, or defer to a later cross-cutting pass?
- **Product:** Today the game scripts call each other directly by name (Zone tool talks to Grid script, Terrain talks to Water, etc.). Should we keep these direct links during this cleanup, or replace them with a shared interface so scripts only know about each other through a contract?
- **Options:** (a) keep direct links — faster cutover (b) switch to interface — cleaner but bigger diff (c) hybrid: interface only for new cross-Domain calls, direct for existing.

### Q2 — Partial-class handling
- **Tech:** `UIManager.*`, `UiBakeHandler.*`, `AgentBridgeCommandRunner.*`, `WaterManager.*` use partial-class splits. Strategy γ favors service extraction; partial preserved only for trivial splits. Per family, decide: full service-extract or keep partial-only?
- **Product:** Some scripts are already split into multiple files of the same script (like UI and Bridge tooling). Should we replace those splits with proper service files (cleaner), or keep them as-is when they look tidy enough?
- **Options:** (a) full service-extract everywhere (b) keep partial when each piece <500 LOC (c) case-by-case per family.

### Q3 — Test discipline
- **Tech:** Parent plans used "one composed test per stage, extended task by task". Should the new plan inherit this, or one test file per cutover hub?
- **Product:** When we trim a script, the safety net is a test that proves the game still behaves the same. Do we want one growing test file per stage covering all scripts in that stage, or a fresh test file for each script trimmed?
- **Options:** (a) cumulative per stage (b) one file per hub (c) cumulative for Tier B/C, per-hub for Tier D/E.

### Q4 — Stage granularity
- **Tech:** One hub per stage = safe + slow. Sibling-batch (e.g. all Water hubs in one stage) = faster but larger diff. Pick policy.
- **Product:** Do we want each script trimmed in its own checkpoint (safest, slowest), or group scripts that belong to the same game system together in one checkpoint?
- **Options:** (a) one hub per stage (b) one game system per stage (c) hybrid by system size.

### Q5 — Scaffold ordering
- **Tech:** Tier A (scaffold missing Domains) — fold into the matching Tier B/C stage (scaffold + cutover same stage) or keep separate to compile-gate scaffold first?
- **Product:** For game systems that don't yet have their clean folder (Forests, Save, Audio, Notifications, etc.), should we create the empty folder first and only later move the logic, or do it all in one go?
- **Options:** (a) separate scaffold + cutover stages (safer) (b) fold scaffold into cutover stage (faster).

### Q6 — Order of operations
- **Tech:** Suggested order: Tier A → B → C → D → E → F. Should hubs with most consumers (GridManager, TerrainManager) ship first to flush facade-interface ripples, or last (highest risk last)?
- **Product:** The Grid and Terrain scripts are the foundation other scripts depend on. Should we trim them first (to surface any breakage early), or last (so smaller scripts go smoothly first)?
- **Options:** (a) foundation-first (b) leaves-first (c) by LOC descending.

### Q7 — Dead-public sweep
- **Tech:** Parent plan Q5=5b → flag zero-caller publics in `§Open Questions`, no auto-delete. Continue this policy or upgrade to auto-delete with reviewer gate?
- **Product:** While trimming, we sometimes find script methods nothing uses anymore. Keep flagging them in a list for human review, or delete them automatically when the test passes?
- **Options:** (a) flag-only (status quo) (b) auto-delete with green test (c) flag during cutover, batch-delete in a final stage.

### Q8 — Tier E (Domain service second-split) — scope in or out
- **Tech:** Domain services >500 LOC (AutoBuild 1283, Interstate 1180, Terraforming 926, PrefabResolver 922, Theme 530) — split inside this plan or defer to a follow-on?
- **Product:** Some of the new "clean" service files have already grown big themselves. Split them inside this same cleanup, or queue them for a follow-up so this plan stays focused on hub trimming?
- **Options:** (a) include in this plan (b) defer to follow-up (c) include only the ≥1000 LOC ones.

### Q9 — Acceptance LOC ceiling
- **Tech:** Audit proposed `>500 LOC = red` as acceptance gate. Confirm 500 as the line, or tune (e.g. 400, 600, separate hub-cap 200 + service-cap 500)?
- **Product:** What's the maximum number of lines a single code file should have at the end? (Smaller = stricter cleanup, more stages; bigger = laxer but faster.)
- **Options:** (a) 200 hub / 500 service (b) flat 500 across all (c) 400 stricter (d) 600 looser.

### Q10 — Buildings + Camera Domains
- **Tech:** `Domains/Buildings/` and `Domains/Camera/` don't exist; building lifecycle is spread across GridManager + ZoneManager; camera logic across CameraController + GridManager `cameraController`. Scaffold both as Tier A, or fold into the hub stage that surfaces them?
- **Product:** "Buildings" and "Camera" logic is currently scattered across other scripts. Should we create dedicated folders for them as part of this cleanup, or leave them inside the hosting scripts and tackle separately later?
- **Options:** (a) scaffold both as Tier A (b) defer Buildings, scaffold Camera (c) defer both.

## §Hand-off after grill

After Q1–Q10 resolved via `AskUserQuestion` polling, `/design-explore` writes the expanded design (§Architecture, §Subsystem Impact, §Implementation Points, §Examples, §Subagent Review) into this same file, then `/master-plan-new large-file-atomization-hub-thinning-sweep` seeds the plan in DB.
