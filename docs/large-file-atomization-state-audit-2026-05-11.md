---
purpose: "Audit current state of prior large-file atomization + cutover plans — measure thin-hub coverage, list remaining fat hubs, seed next master plan."
audience: agent
loaded_by: on-demand
created_at: 2026-05-11
related_plans:
  - large-file-atomization-refactor (v2, closed 2026-05-08)
  - large-file-atomization-cutover-refactor (v1, closed 2026-05-09)
related_docs:
  - docs/large-file-atomization-componentization-strategy.md (Strategy γ — LOCKED)
  - docs/post-atomization-architecture.md (target architecture — LOCKED)
  - docs/explorations/large-file-atomization-refactor.md
  - docs/explorations/large-file-atomization-cutover-refactor.md
---

# Large-file atomization — state audit (2026-05-11)

## §Goal

Inventory current state of hub-thinning effort. Two prior plans landed:
- **v1 extraction plan** (`large-file-atomization-refactor`): created `Assets/Scripts/Domains/{X}/` folders + services + facades. Body **ported** to services but **NOT removed** from hub managers (duplicate). 20 stages done.
- **v2 cutover plan** (`large-file-atomization-cutover-refactor`): trimmed 6 specific hubs to thin pass-through delegates per Approach 1b (file/class/namespace/SerializeField preserved). 6 stages done.

Gap: most hubs still carry inline body even when a Domain port exists. New plan needed to finish cutover sweep.

## §Method

Per-file LOC scan against `Assets/Scripts/Managers/**`, `Assets/Scripts/Controllers/**`, `Assets/Scripts/Editor/**`, `Assets/Scripts/Core/**`. For each fat hub: count public methods, count `Domains.{X}.Services` injections, classify state.

States:
- `THIN` — file ≤200 LOC, every public method = single-line delegate (cutover'd)
- `PARTIAL` — Domain service instantiated, some delegates wired, body still inline for other concerns
- `FAT` — zero or near-zero Domain injection, full body still inline (despite v1 port existing)
- `NO-PORT` — Domain folder doesn't exist yet; needs both extraction AND cutover

## §Cutover plan v2 — landed states

| Hub | LOC now | Was | State |
|---|---|---|---|
| `InterstateManager.cs` | 115 | 1149 | THIN ✓ |
| `RoadPrefabResolver.cs` | 35 | 1121 | THIN ✓ |
| `UIManager.Theme.cs` | 147 | 1004 | THIN ✓ |
| `TerraformingService.cs` (Manager copy) | — | 1095 | THIN ✓ (Domain port at 926 LOC) |
| `AgentBridgeCommandRunner.Mutations.cs` | 420 | ~? | THIN ✓ (partial of family) |
| `GeographyManager.cs` | 696 | 1160 | PARTIAL — only sorting delegated; init / load / clear / report bodies still inline |

## §Fat hubs — pending cutover

Sorted by LOC (largest first). Public-method count = pass-through surface to delegate.

| Hub | LOC | Public methods | Domain port status | State |
|---|---:|---:|---|---|
| `Managers/GameManagers/TerrainManager.cs` | **4260** | 40 | `Domains/Terrain/` exists (HeightMap 309, Terraforming 926 LOC) | FAT — 0 Domain svc injection |
| `Managers/GameManagers/RoadManager.cs` | **3226** | 28 | `Domains/Roads/` exists (PrefabResolver 922, AutoBuild 1283, InterstateService 1180, Stroke 67) | FAT — 0 Domain svc injection |
| `Editor/Bridge/UiBakeHandler.Archetype.cs` | 2532 | — | `Domains/UI/Editor/UiBake/` minimal (34 LOC stub) | FAT — port absent for archetype concern |
| `Editor/Bridge/UiBakeHandler.cs` | 2336 | — | same | FAT |
| `Core/Terrain/WaterMap.cs` | 2325 | — | `Domains/Water/` exists (WaterMap 190, Shore 122) | FAT — port covers fragment only |
| `Managers/GameManagers/GridManager.cs` | **2321** | 49 | `Domains/Grid/` exists (CellAccess 135, GridSortingOrder 463, Grid.cs 171) | PARTIAL — only `cellAccessService` instantiated (line 261); body for 48/49 methods still inline |
| `Editor/AgentBridgeCommandRunner.cs` | 1919 | — | `Domains/Bridge/` thin (Mutation 33, Conformance 33) | FAT — core file untouched (Mutations partial cutover'd separately) |
| `Managers/GameManagers/ZoneManager.cs` | 1449 | 21 | `Domains/Zones/` exists (ZonePlacement 114, ZonePrefabRegistry 45, ZoneSection 154) | FAT — 0 Domain svc injection |
| `Managers/GameManagers/CityStats.cs` | 1328 | 90 | `Domains/Economy/` exists (CityStatsService 354) | FAT — 0 Domain svc injection |
| `Editor/AgentTestModeBatchRunner.cs` | 1221 | — | `Domains/Testing/` exists (small slices) | FAT |
| `Editor/AgentBridgeCommandRunner.Conformance.cs` | 1219 | — | `Domains/Bridge/Services/ConformanceService.cs` = 33 LOC stub | FAT — port empty |
| `Editor/Bridge/UiBakeHandler.Frame.cs` | 1036 | — | UiBake port stub | FAT |
| `Managers/GameManagers/ForestManager.cs` | 847 | 10 | **`Domains/Forests/` MISSING** | NO-PORT |
| `Managers/GameManagers/EconomyManager.cs` | 842 | 35 | `Domains/Economy/` exists; no EconomyService port | FAT — needs port + cutover |
| `Managers/GameManagers/ProceduralRiverGenerator.cs` | 837 | — | no clear Domain target | NO-PORT |
| `Audio/Blip/BlipVoice.cs` | 716 | — | **`Domains/Audio/` MISSING** | NO-PORT |
| `Managers/GameManagers/GeographyManager.cs` | 696 | ~20 | `Domains/Geography/Services/` (49 LOC) | PARTIAL (cutover stage 4 only delegated sorting) |
| `Managers/GameManagers/WaterManager.cs` | 686 | 24 | `Domains/Water/` (WaterMap 190, Shore 122) | FAT — 0 Domain svc injection |
| `Managers/GameManagers/GameSaveManager.cs` | 668 | — | **`Domains/Save/` MISSING** | NO-PORT |
| `Editor/AgentDiagnosticsReportsMenu.cs` | 640 | — | no Domain target | NO-PORT |
| `Core/Terrain/PathTerraformPlan.cs` | 631 | — | `Domains/Terrain/` (POCO candidate) | FAT |
| `Controllers/GameControllers/MiniMapController.cs` | 559 | — | UI Domain candidate | NO-PORT |
| `Managers/GameManagers/DemandManager.cs` | 558 | — | **`Domains/Demand/` MISSING** | NO-PORT |
| `Editor/Bridge/UiBakeHandler.Button.cs` | 551 | — | UiBake port stub | FAT |
| `UI/Toolbar/ToolbarDataAdapter.cs` | 543 | — | UI Domain candidate | NO-PORT |
| `Managers/GameManagers/GameNotificationManager.cs` | 519 | — | **`Domains/Notifications/` MISSING** | NO-PORT |
| `Core/Core/CityCell.cs` | 512 | — | Core type, partial-class candidate | (Strategy α — partial split, not service extraction) |
| `Managers/GameManagers/UIManager.Utilities.cs` | 508 | — | `Domains/UI/` exists | FAT |

## §Domain services that themselves grew large (>500 LOC)

Atomization target — these need internal split per Strategy γ once their hub cutover lands.

| Service | LOC | Domain | Suggested split |
|---|---:|---|---|
| `Domains/Roads/Services/AutoBuildService.cs` | 1283 | Roads | Per-strategy services (auto-build vs sim-rules vs candidate-scoring) |
| `Domains/Roads/Services/InterstateService.cs` | 1180 | Roads | gen + conformance + flow tracker |
| `Domains/Terrain/Services/TerraformingService.cs` | 926 | Terrain | Plan / apply / smooth / clamp |
| `Domains/Roads/Services/PrefabResolverService.cs` | 922 | Roads | Lookup vs variant-pick vs cache |
| `Domains/UI/Services/ThemeService.cs` | 530 | UI | Token resolve vs style apply vs cache |

## §Missing Domain folders

Need scaffolding (`I{X}.cs` + `{X}.cs` facade + `{X}.asmdef` + `Services/`) before cutover:
- `Domains/Forests/` — ForestManager + ForestMap (UnitManagers)
- `Domains/Buildings/` — building lifecycle scattered across GridManager + ZoneManager
- `Domains/Audio/` — BlipVoice + audio managers
- `Domains/Notifications/` — GameNotificationManager
- `Domains/Save/` — GameSaveManager
- `Domains/Camera/` — CameraController family
- `Domains/Cursor/` — CursorManager
- `Domains/Demand/` — DemandManager + UrbanizationProposalManager + GrowthManager

## §Pattern reference — what THIN looks like

Reference: `Assets/Scripts/Managers/GameManagers/InterstateManager.cs` (115 LOC).
- File path UNCHANGED (preserves Unity GO inspector script ref).
- Class name + namespace UNCHANGED.
- Every `[SerializeField]` UNCHANGED (dependency wiring on prefab survives).
- `Awake()` instantiates `_service = new Domains.{X}.Services.{Y}Service(deps...)`.
- Every public method body = single-line `return _service.Method(args);`.
- Class implements `Domains.{X}.I{X}` facade interface for cross-asmdef consumers.

## §Constraints (locked from prior plans)

From cutover plan Q1–Q5 + Strategy γ doc:
1. DO NOT move hub file path.
2. DO NOT rename hub class / namespace.
3. DO NOT modify any `[SerializeField]` field.
4. Preserve every public method signature + Unity lifecycle hook + coroutine attachment.
5. Body absorbed by `Domains/{X}/Services/{Concern}Service.cs` POCO (not MonoBehaviour).
6. Cross-asmdef refs flow through `I{X}` facade interface only.
7. Dead-public sweep flagged in `§Open Questions`, NOT auto-deleted (Q5=5b).
8. Hard cap: hub ≤200 LOC, services ≤500 LOC after second-pass split (target).
9. Verification per stage: `validate:all` + `unity:compile-check` + scene-load smoke + 1 PlayMode smoke per cutover concern.

## §Seed — next master plan

Working title: **`large-file-atomization-hub-thinning-sweep`** (v1).

**Goal.** Take every hub in §Fat hubs table to THIN state, scaffold missing Domain folders, split Domain services >500 LOC into per-concern sub-services. End state: ZERO file >500 LOC across `Assets/Scripts/**` (excluding generated + `.archive/`).

**Approach.** Same as cutover plan v2 — Approach 1b pass-through delegation, Strategy γ folder shape. Stages ordered by:
1. **Tier A — scaffold missing Domains** (Forests, Buildings, Audio, Notifications, Save, Camera, Cursor, Demand): one stage per Domain, just `I{X}.cs` + `{X}.cs` + `{X}.asmdef` + folder, no body move.
2. **Tier B — hubs with port ready** (TerrainManager, RoadManager, GridManager, ZoneManager, CityStats, WaterManager, EconomyManager, UIManager.Utilities, GeographyManager-finish, WaterMap-Core, etc.): one stage per hub, pass-through cutover.
3. **Tier C — hubs needing extract+cutover** (ForestManager, GameSaveManager, BlipVoice, ProceduralRiverGenerator, MiniMapController, DemandManager, GameNotificationManager): two-task stage (extract POCO service → cutover hub).
4. **Tier D — partial-class consolidation** (UiBakeHandler family, AgentBridgeCommandRunner family, AgentBridgeCommandRunner.Conformance): per-family stage; extract concern services from each partial, hub partial becomes thin.
5. **Tier E — second-pass split** (AutoBuildService, InterstateService, TerraformingService, PrefabResolverService, ThemeService): per-service internal split into sub-services.
6. **Tier F — gate hardening**: add `validate:no-hub-fat` lint (any `Assets/Scripts/Managers/**/*.cs` or registered hub file with >200 LOC = red); add `validate:no-service-fat` (>500 LOC = red).

**Acceptance.** `find Assets/Scripts -name '*.cs' ! -path '*/.archive/*' -exec wc -l {} \; | awk '$1>500'` returns ZERO lines.

**Risks / open questions for design-explore phase:**
- Q1: Hubs with serialized field cross-references (e.g. ZoneManager ↔ GridManager) — extract shared interface or keep concrete?
- Q2: Partial classes (UIManager.*, UiBakeHandler.*) — service-extract or partial-only? Strategy γ favors service; partial preserved for trivial splits only.
- Q3: Tests — extend cumulative stage test or one-per-hub? Prior plans used cumulative per stage.
- Q4: Stage size — one hub per stage (slow, safe) vs sibling-batch (e.g. all Water hubs in one stage)?
- Q5: Tier-A scaffold stages — fold into Tier B cutover stages (scaffold + cutover in one) or keep separate to compile-gate scaffold first?

**Pre-conditions.**
- BUG-63 (.meta capture in stage commit) — landed (per cutover plan note); confirm still green.
- `validate:no-domain-game-cycle` — landed Stage 20 of v1 plan; confirm green.

**Hand-off.** Run `/design-explore docs/explorations/large-file-atomization-hub-thinning-sweep.md` to expand this seed, resolve Q1–Q5, then `/master-plan-new`.
