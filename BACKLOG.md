# Backlog — Territory Developer

> Single source of truth for project issues. Reference via `@BACKLOG.md` in agent conversation. Closed work → [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Use **`mcp__territory-ia__backlog_issue`** for slice access.
>
> **Lane order (highest first):** § Compute-lib program → § Agent ↔ Unity & MCP context lane → § IA evolution lane → § UI-as-code program → § Economic depth lane → § Gameplay & simulation lane → § Multi-scale simulation lane → § Blip audio program → § Sprite gen lane → § Web platform lane → § High / § Medium / § Code Health / § Low. **Gameplay blockers** in § High Priority stay **interrupt** work — stop play / corrupt saves.
>
> **Closed program charters** (trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) + glossary): **Spec-pipeline** (territory-ia spec-pipeline program; exploration [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md)) · **UI-as-code program** umbrella (UI-as-code program; **`ui-design-system.md`** Codebase inventory (uGUI)) · **TECH-39 computational MCP suite** (Computational MCP tools (TECH-39)).
>
> **Active programs:** **§ Compute-lib program** (TECH-38 + TECH-32 / TECH-35 research) · **§ IA evolution lane** TECH-77–TECH-83 + TECH-552 (FTS, skill chaining, agent memory, bidirectional IA, knowledge graph, gameplay entity model, sim parameter tuning, Unity Agent Bridge — [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md); bridge program [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md)) · **§ UI-as-code program** open FEAT-51 · **§ Economic depth lane** FEAT-52 → FEAT-53 → FEAT-09 (economy, services, districts; monthly maintenance, tax→demand feedback, happiness + pollution shipped) · **§ Gameplay & simulation lane** player-facing AUTO / density.

---

## Compute-lib program

**Dependency order.** Pilot compute-lib + World ↔ Grid MCP shipped ([`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) + glossary `territory-compute-lib`). TECH-39 (computational MCP suite) closed (glossary `Computational MCP tools (TECH-39)`). **TECH-38** (C# pure modules + harnesses) extends `Utilities/Compute/` + `tools/reports/`. Research **TECH-32** + **TECH-35** marked `Depends on: none` but run after TECH-38 surfaces exist (compare vs UrbanGrowthRingMath / RNG notes).

- [ ] **TECH-38** — **Core** **computational** modules (Unity **utilities** + **`tools/`** harnesses)
  - Type: code health / performance enablement
  - Files: `Assets/Scripts/Utilities/Compute/`; `GridManager.cs` (**CoordinateConversionService**), `GridPathfinder.cs`, `UrbanCentroidService.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterManager.cs`, `DemandManager.cs` / `CityStats.cs` (as extractions land); `tools/reports/`; **UTF** tests
  - Spec: none — unity-development-context §11; `tools/reports/compute-utilities-inventory.md`, `tools/reports/compute-utilities-rng-derivation.md`
  - Notes: **Behavior-preserving** extractions; **UrbanGrowthRingMath** **multipolar**-ready for **FEAT-47**; **stochastic** **geography initialization** documentation; **no** second **pathfinding** authority. Prepare **batchmode** hooks for **TECH-66** follow-ups. **Context:** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) (TECH-37/TECH-39 archived).
  - Acceptance: inventory doc + **≥ 3** **pure** modules with tests or **golden** **JSON**; **RNG** derivation doc; **invariants** respected — see `tools/reports/compute-utilities-inventory.md` and bullets above
  - Depends on: none (pilot milestone in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **TECH-32** — **Urban growth rings** / centroid recompute what-if (research tooling)
  - Type: tooling / research
  - Files: `tools/` or Unity Editor batch; parameters from **FEAT-43** / **FEAT-36** notes as inputs
  - Notes: Compare full **UrbanCentroidService** recompute every tick vs throttled/approximate strategies; report desync or behavior risk vs glossary **sim §Rings**. Non-player-facing evidence for tuning. `docs/agent-tooling-verification-priority-tasks.md` task 24. **Order:** Prefer running against **TECH-38** **UrbanGrowthRingMath** / harness **JSON** once **Phase B** exists; until then, baseline against current **MonoBehaviour** code.
  - Depends on: none (coordinates with **FEAT-43**; soft: **TECH-38** for **pure** module parity)

- [ ] **TECH-35** — Research spike: property-based / random mutation **invariant** fuzzing (optional)
  - Type: research / test harness
  - Files: TBD test assembly or `tools/` prototype
  - Notes: High setup cost; only if geometric / ordering bugs justify. Predicates from **invariants** (HeightMap/**cell** sync, **road cache**, **shore band**, etc.). `docs/agent-tooling-verification-priority-tasks.md` task 38. **Non-goals:** production fuzz in player builds. **Order:** Easiest once **TECH-38** exposes stable **pure** surfaces + documented **RNG** derivation.
  - Depends on: none (soft: **TECH-38**)

## Agent ↔ Unity & MCP context lane

Ordered for **closed-loop agent ↔ Unity** — **Close Dev Loop** orchestration shipped (glossary **IDE agent bridge** — [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); Play Mode bridge **`kind`** values, **`debug_context_bundle`**, **`close-dev-loop`** Skill, **dev environment preflight** archived [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**). Remaining lane order: **JSON / reports** plumbing → **MCP platform** → **agent workflow & CI helpers** → **research tooling**. (**§ Compute-lib program** above: **TECH-38** + **TECH-32**/**TECH-35**.) **Prerequisites for later items:** **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30** (existing `ia/projects/*.md`); **TECH-38** + archived **TECH-39** (**§ Compute-lib program** / [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). **Spec-pipeline** charter: **glossary** **territory-ia spec-pipeline program** + archive.

- [ ] **TECH-53** — **Schema validation history** (Postgres extension **E2** track)
  - Type: technical / CI / data
  - Files: `.github/workflows/` (e.g. extend **ia-tools**), `docs/schemas/`, `docs/schemas/fixtures/`; optional **Postgres** table (IA schema milestone in archive)
  - Spec: none (backlog-only — no `ia/projects/` spec)
  - Notes: Persist per-CI-run outcomes of **`npm run validate:fixtures`** / **JSON Schema** checks so regressions on **Interchange JSON** and fixtures are visible over time. Align row shape with [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **B1** if stored in **Postgres**. Program pointer: same doc **Program extension mapping (E1–E3)**; Postgres umbrella trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: agreed storage (artifact file, DB rows, or workflow summary) + documented query or review path; English **Notes** updated when implementation choice is fixed
  - Depends on: none (soft: IA **Postgres** milestone + JSON infra in archive)

- [ ] **TECH-54** — **Agent patch proposal staging** (Postgres extension **E3** track)
  - Type: tooling / agent workflow
  - Files: optional **Postgres** migrations; `tools/` or thin HTTP handler; `docs/`
  - Spec: none (backlog-only — no `ia/projects/` spec)
  - Notes: Queue **B3**-style idempotent patch envelopes ([`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md)) with explicit lifecycle (**pending** / **approved** / **rejected**) before humans merge changes to git; **`natural_key`** for deduplication. **Not** player **Save data**. Program pointer: same doc **Program extension mapping (E1–E3)**; Postgres umbrella trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: documented state machine + at least one insert/list path (script, SQL, or API); conflict policy recorded in issue **Notes** or [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) / [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) when implementing
  - Depends on: none (soft: IA **Postgres** milestone + interchange patterns doc in archive)

- [ ] **TECH-43** — Append-only **JSON** line **event log** (telemetry / sim anomalies) — **backlog placeholder**
  - Type: technical / observability (future)
  - Files: TBD (`tools/`, optional **Postgres** table, ship pipeline)
  - Spec: none (promote to `ia/projects/TECH-43.md` when scheduled)
  - Notes: Idea from **JSON interchange program** brainstorm **B2** (`projects/json-use-cases-brainstorm.md`); **schema_version** per line; same validator family as shipped JSON infra (archive). **Schema** pipeline exists under `docs/schemas/` + **`npm run validate:fixtures`**.
  - Acceptance: issue refined with concrete consumer + storage choice; optional schema + sample sink
  - Depends on: none (soft: JSON infra milestone in archive)

- [ ] **TECH-18** — Migrate Information Architecture from Markdown to PostgreSQL (MCP evolution)
  - Type: infrastructure / tooling
  - Files: All `ia/specs/*.md`, `ia/rules/agent-router.md`, `ia/rules/invariants.md`, `ARCHITECTURE.md`; MCP server (file-backed **territory-ia** — shipped, see archive); schema / migrations / seed from IA **Postgres** milestone (archive); `tools/mcp-ia-server/src/index.ts`, `docs/mcp-ia-server.md`
  - Notes: **Goal:** After file-backed MCP and IA **Postgres** tables, **migrate authoritative IA content** into PostgreSQL and evolve the **same MCP** so **primary** retrieval is DB-backed. Markdown becomes **generated or secondary** for human reading. **Explicit dependency:** This work **extends the MCP built first on Markdown** — same tool contracts where possible, swapping implementation to query the IA database. **Scope:** (1) Parse and ingest spec sections (`isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, etc.) into `spec_sections`. (2) Populate `relationships` (e.g. HeightMap↔Cell.height, PathTerraformPlan→Phase-1→Apply). (3) Populate `invariants` from `invariants.md`. (4) Extend tools: `what_do_i_need_to_know(task_description)`, `search_specs(query)`, `dependency_chain(term)`. (5) Script to regenerate `.md` from DB for review. (6) Update `agent-router.md` — MCP tools first, Markdown fallback second. **Acceptance:** Agent resolves a multi-spec task (e.g. “bridge over multi-level lake”) via MCP reading ≤ ~500 tokens of context instead of many full-file reads. **Phased MCP tools** (bundles, `backlog_search`, **`unity_context_section`** after **unity-development-context** spec, etc.): see `ia/projects/TECH-18.md` and `docs/agent-tooling-verification-priority-tasks.md` (tasks 12–20, 28–32, 35). **Deferred unless reopened:** `findobjectoftype_scan`, `find_symbol` MCP tools (prefer **TECH-26** script).
  - Depends on: none (soft: MCP baseline + IA **Postgres** milestone — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **TECH-15** — New Game / **geography initialization** performance
  - Type: performance / optimization
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, `InterstateManager.cs`, `ForestManager.cs`, `RegionalMapManager.cs`, `ProceduralRiverGenerator.cs` (as applicable)
  - Notes: Reduce wall-clock time and frame spikes when starting a **New Game** (**geography initialization**): **HeightMap**, lakes, procedural **rivers** (shipped — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)), **interstate**, **forests**, **map border** signs, **sorting order** passes, etc. **Priority:** Land the **Editor/batch JSON profiler** under `tools/reports/` (see spec) *before* or in parallel with deep optimization — agents need **measurable** phase breakdowns. **Related:** **Load Game** / **water map** persist work is archived — this issue targets **geography initialization** cost only. **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 3, 22).

- [ ] **TECH-16** — **Simulation tick** performance v2 (per-tick **AUTO systems** pipeline)
  - Type: performance / optimization
  - Files: `SimulationManager.cs`, `TimeManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`, `DemandManager.cs`, `CityStats.cs` (as applicable)
  - Notes: Second-pass optimization of the **simulation tick** after early **Simulation optimization** work (completed). **Priority:** Ship **spec-labeled tick harness** JSON + **ProfilerMarker** names (see spec) so agents and CI can read **AUTO** pipeline cost *before* micro-optimizing allocations. **Related:** **BUG-14** (per-frame UI `FindObjectOfType`); **TECH-01** (manager decomposition may help profiling and hotspots). **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 4, 25); drift detection **TECH-29**.

- [ ] **TECH-33** — Asset introspection: **prefab** manifest + scene **MonoBehaviour** listing
  - Type: tooling
  - Files: `tools/` (Unity `-batchmode` or Editor script), `Assets/Prefabs/`, agreed scene path (e.g. `MainScene.unity`)
  - Notes: List prefabs with missing script references; list MonoBehaviour types/paths in scene for **toolbar** layout work. `docs/agent-tooling-verification-priority-tasks.md` tasks 26, 27.
  - Depends on: none

- [ ] **TECH-23** — Agent workflow: MCP **invariant preflight** for issue kickoff
  - Type: documentation / process
  - Files: `AGENTS.md`, optional `ia/templates/` or **How to Use This Backlog** section in this file, `docs/mcp-ia-server.md` (short pointer)
  - Notes: Document that implementation chats for **BUG-**/**FEAT-**/**TECH-** work should record **territory-ia** **`invariants_summary`**, **`router_for_task`**, and at least one **`spec_section`** (or equivalent slice) before substantive code edits—reduces **road preparation family**, **HeightMap**/**cell** sync, and per-frame **`FindObjectOfType`** mistakes. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-45** — **Cursor Skill:** **road** modification guardrails (**road stroke**, **road preparation family**, cache)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `ia/skills/` (TBD subfolder + `SKILL.md`); optional one-line pointer in `AGENTS.md`
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: **road placement** only through **road preparation family** ending in **`PathTerraformPlan`** + Phase-1 + **`Apply`** — never **`ComputePathPlan`** alone; call **`InvalidateRoadCache()`** after **road** changes; pull normative detail via **territory-ia** (`router_for_task` → **roads** / **geo**) — do not duplicate **`roads-system`** in the skill body. **Pattern:** [ia/skills/README.md](ia/skills/README.md) (thin skill + **Tool recipe** + MCP pointers).
  - Acceptance: **Skill** file committed; **`description`** names **road stroke**, **wet run**, **interstate**/**bridge** touchpoints where relevant
  - Depends on: none (soft: [ia/skills/README.md](ia/skills/README.md) conventions)

- [ ] **TECH-46** — **Cursor Skill:** **terrain** / **HeightMap** / **water** / **shore** edit guardrails
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `ia/skills/` (TBD subfolder + `SKILL.md`); optional pointer in `AGENTS.md`
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: keep **`HeightMap[x,y]`** and **`Cell.height`** in sync; **water** placement/removal → **`RefreshShoreTerrainAfterWaterUpdate`**; **shore band** and **river** monotonicity per **invariants**; use **`spec_section`** / **`router_for_task`** for **water-terrain** and **geo** slices — no spec paste. **Pattern:** [ia/skills/README.md](ia/skills/README.md).
  - Acceptance: **Skill** file committed; **`description`** triggers on **terraform**, **water map**, **cliff**, **shore** edits
  - Depends on: none (soft: [ia/skills/README.md](ia/skills/README.md))

- [ ] **TECH-47** — **Cursor Skill:** new **`MonoBehaviour`** **manager** wiring pattern
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `ia/skills/` (TBD subfolder + `SKILL.md`); optional pointer in `AGENTS.md` or `ia/specs/unity-development-context.md` **Decision Log**
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: scene **component**, never `new`; **`[SerializeField] private`** refs + **`FindObjectOfType`** fallback in **`Awake`**; **no new singletons**; do not add responsibilities to **`GridManager`** — extract helpers; align with **`ia/specs/unity-development-context.md`** via MCP slice when needed. **Pattern:** [ia/skills/README.md](ia/skills/README.md).
  - Acceptance: **Skill** file committed; **`description`** states “new manager / **MonoBehaviour** service” triggers
  - Depends on: none (soft: [ia/skills/README.md](ia/skills/README.md))

- [ ] **TECH-48** — **territory-ia** MCP: discovery from **project specs** (terms, domains, spec slices)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new or extended handlers, parsers); `tools/mcp-ia-server/README.md`; `docs/mcp-ia-server.md`; optional fixtures under `tools/mcp-ia-server/`; align notes with `ia/projects/TECH-18.md` when **search**/**bundle** tools land
  - Spec: none (promote to `ia/projects/TECH-48.md` when design stabilizes)
  - Notes: **Goal:** Make **project-spec-kickoff** and similar workflows cheaper and safer by improving how MCP turns **implementation**-oriented text (project **spec** body, backlog **Files**) into **glossary** matches and **`spec_section`** targets. **Candidate directions:** (1) Path-based tool: input `ia/projects/{ISSUE}.md` → ranked **glossary** candidates + suggested **`router_for_task`** **domain** strings + ordered **`spec_section`** queue with **max_chars** budget. (2) Improve **`glossary_discover`** ranking using tokens extracted from **`backlog_issue`** **Files**/**Notes** when `issue_id` is bundled in the same turn. (3) Optional composite read helper (defer if **TECH-18** `search_specs` / bundles subsume). **Does not** replace **`ia/skills/project-spec-kickoff/SKILL.md`** prose until tools are **shipped** and **`npm run verify`** green. **Related:** closeout helpers shipped (**`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:*`**, **`project-spec-closeout-parse.ts`**) — trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: ≥1 **measurable** improvement merged (new tool **or** clear ranking/UX win on existing tools) + docs updated; **`npm run verify`** green
  - Depends on: none (soft: dogfood with **project-spec-kickoff**; **TECH-18** for long-term search architecture)

- [ ] **TECH-24** — territory-ia MCP: parser regression policy (tests/fixtures when parsers change)
  - Type: tooling / code health
  - Files: `tools/mcp-ia-server/` (tests, fixtures, `scripts/verify-mcp.ts` or equivalent), `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Notes: When changing markdown parsers, fuzzy matching, or glossary ranking, extend **`node:test`** fixtures and keep **`npm run verify`** green (same pattern as **`glossary_discover`** / parser fixtures — see archive). No Unity. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-30** — Validate **BACKLOG** issue IDs referenced in `ia/projects/*.md`
  - Type: tooling / doc hygiene
  - Files: `tools/` (Node script), optional `package.json` `npm run` at repo root or under `tools/`
  - Notes: Every `[BUG-XX]` / `[TECH-XX]` / etc. front matter or link in active project specs must exist in `BACKLOG.md` (open rows) or `BACKLOG-ARCHIVE.md` when cited as historical. `docs/agent-tooling-verification-priority-tasks.md` task 9. **Related:** `npm run validate:dead-project-specs` (repo-wide missing `ia/projects/*.md` paths) — shipped; coordinate shared **Node** helpers when implementing **TECH-30**.
  - Depends on: none

- [ ] **TECH-29** — CI / script: **simulation tick** call-order drift detector
  - Type: tooling / CI
  - Files: `tools/` (Node or shell), checked-in ordered manifest (derived from `ia/specs/simulation-system.md` **Tick execution order**), optional `.github/workflows/`; `SimulationManager.cs` as truth source to diff
  - Notes: Fail CI (or print advisory) when `ProcessSimulationTick` step order diverges from manifest without matching spec update. `docs/agent-tooling-verification-priority-tasks.md` task 5. Phase labels should stay aligned with **TECH-16** harness.
  - Depends on: **TECH-16** (stable spec-labeled phase names in harness — soft dependency for naming parity)

- [ ] **TECH-31** — **Agent scenario generator** (**test mode**, **32×32** **test map**, loadable **save**-shaped scenarios, MCP tool)
  - Type: tooling / test infrastructure
  - Files: `tools/`, `tools/fixtures/scenarios/`, `tools/scripts/unity-testmode-batch.sh`, `tools/scripts/unity-quit-project.sh` (or agreed `Assets/` test paths), Unity test assembly or Editor scripts, scenario descriptors + committed save artifacts; `GameSaveManager` / **`GameSaveData`** integration (**persistence-system**); bridge- or export-friendly hooks (glossary **IDE agent bridge**); `tools/mcp-ia-server/src/` (final phase MCP tool); `docs/mcp-ia-server.md`
  - Notes: **Program tracker** (stages, progress, lessons): [`projects/TECH-31-agent-scenario-generator-program.md`](projects/TECH-31-agent-scenario-generator-program.md) — **31a** → **31a2** → **31a3** → **31b** → **31c** → **31d** → **31e**. **Shipped through 31d (2026-04-10):** includes everything through **31c** (see prior archive notes) plus **TECH-82** Phase 1: Postgres **`city_metrics_history`**, Unity **`MetricsRecorder`**, **territory-ia** **`city_metrics_query`**, **test mode** **`scenario_id`** correlation — **glossary** **City metrics history**, [`ia/projects/TECH-82.md`](ia/projects/TECH-82.md), [`tools/fixtures/scenarios/README.md`](tools/fixtures/scenarios/README.md) (**optional Postgres** section), **agent-test-mode-verify** skill. **Remaining:** **31e** = **territory-ia** **MCP** scenario resolver tool + **`docs/mcp-ia-server.md`**. **Cursor stub** (Open Questions + aggregate test contracts): `ia/projects/TECH-31.md`. **Agent-facing** intent → **`GameSaveData`-compatible** artifact + **scenario id** (**kebab-case**); **32×32** **test mode**; bounded **simulation**; **UTF** / **`debug_context_bundle`** / golden JSON; optional **glossary** **City metrics history** when **Postgres** is configured. **Core builder** validation per stub **Open Questions**; **minimal UI** (**TEST-MODE**); release builds cannot enable **test mode**. Keep expected values aligned across stub **Test contracts**, **31c** normative table, committed **`agent-testmode-golden-*.json`**, and **Acceptance** below. `docs/agent-tooling-verification-priority-tasks.md`. **Acceptance roll-up:** full **Acceptance** line is the **issue closeout** bar; **31e** (**MCP** scenario tool) still required before removing this row (**TECH-82** Phases 2–4 remain on the **TECH-82** backlog row).
  - Acceptance: at least one automated Unity run (**Edit Mode** or **Play Mode**) on a clean tree with a committed scenario; documented **test mode** launch (**scenario id** or path, **32×32**); builder docs for **AUTO** and at least one non-**AUTO** pattern; **close-dev-loop** and **TECH-82** **city history** composition documented (**close-dev-loop** + Path A/B driver matrix: **31c** + stub; **city history**: **31d** / **TECH-82**); **`npm run unity:compile-check`** after tooling changes; **MCP** tool registered and described in **`docs/mcp-ia-server.md`** (**31e**); project stub **Test contracts** + **31c** normative table list chosen batch driver, args, **CI** simulation tick bound, and optional golden assert; optional **BUG-52** **Notes** link when first **AUTO** scenario lands
  - Depends on: **TECH-82** (soft: Phase 1 **metrics** for time-series **city history** verification; load/build milestones may ship first using **save** + **`debug_context_bundle`** only). Soft: stable bridge **`kind`** values and export shapes (**unity-development-context**, **close-dev-loop** skill). Soft: **TECH-15** / **TECH-16** for **UTF**/**batchmode** vs bridge and harness labels when defining **CI** vs dev drivers.

- [ ] **TECH-34** — Generate **`gridmanager-regions.json`** from `GridManager.cs` `#region` blocks
  - Type: tooling / IA
  - Files: `tools/` (Node or C# extractor), output e.g. `tools/mcp-ia-server/data/gridmanager-regions.json`; `GridManager.cs`
  - Notes: Supports **TECH-01** extraction planning and optional future MCP `gridmanager_region_map`. `docs/agent-tooling-verification-priority-tasks.md` task 28. Coordinate MCP registration with **TECH-18** when applicable.
  - Depends on: none (MCP wiring: **TECH-18**)

- [ ] **TECH-27** — **BACKLOG.md** glossary alignment pass (**Depends on** / **Spec** / **Files** / **Notes**)
  - Type: documentation / IA hygiene
  - Files: `BACKLOG.md`, `ia/specs/glossary.md`, optional `tools/` link-check script
  - Notes: Audit open issues so **Depends on**, **Spec**, **Files**, and **Notes** use vocabulary from **`ia/specs/glossary.md`** and linked **reference specs** where practical—improves **`backlog_issue`** usefulness and cross-agent consistency. **Optional automation:** script verifying glossary “Spec” column paths (and optional heading anchors) exist (`docs/agent-tooling-verification-priority-tasks.md` task 10). Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-26** — Repo scripts / CI: mechanical checks (**FindObjectOfType** in **Update**; optional **`gridArray`** gate)
  - Type: tooling / CI
  - Files: new script under `tools/` (Node or shell), optional CI workflow; align wording with `ia/rules/invariants.md`
  - Notes: Implement scanner for **`FindObjectOfType`** inside **`Update`/`LateUpdate`/`FixedUpdate`** (supports **BUG-14** prevention) and optional **`rg`** gate blocking new **`gridArray`/`cellArray`** use outside **`GridManager`** (**TECH-04**). **Phase 2:** hot-path static scan manifest from `ARCHITECTURE.md` / managers-reference to prioritize files in AUTO or per-frame paths (`docs/agent-tooling-verification-priority-tasks.md` tasks 1, 6). Priority order: `docs/agent-tooling-verification-priority-tasks.md`. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

## Backlog YAML ↔ MCP alignment program

Orchestrator: [`ia/projects/backlog-yaml-mcp-alignment-master-plan.md`](projects/backlog-yaml-mcp-alignment-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Aligns **MCP territory-ia** parser + tool surface + validator + skill docs w/ per-issue yaml backlog refactor. Step 1 = HIGH band (IP1–IP5). Stage 1.1 closed 2026-04-17 (TECH-295..TECH-301 all archived — type extension + loader field mapping + soft-dep marker preservation + `proposed_solution` decision + MCP payload surfacing + round-trip test). Stage 1.2 opened 2026-04-17 — 7 tasks filed below (TECH-323..TECH-329: shared lint core + `backlog_record_validate` + `reserve_backlog_ids` + concurrency test + `backlog_list` + filter tests). Step 2 = MEDIUM/LOW band (IP6–IP9); file rows when Stage 1.x → `In Progress`.

### Stage 1.1 — Types + yaml loader (IP1 + IP2)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — MCP tools batch 1 (IP3 + IP4 + IP5)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.2 — Template frontmatter + backfill script

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

- [ ] **TECH-858 — Cross-check `related` ids exist** — In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive).
  - Acceptance — In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive).
  - Spec — [`ia/projects/TECH-858.md`](ia/projects/TECH-858.md)

- [ ] **TECH-860 — Fixtures for `related` existence check** — Add to `tools/scripts/test-fixtures/` — `related-exists-pass/` (two records, one refers to the other), `related-exists-fail/` (record refers to nonexistent id).
  - Acceptance — Add to `tools/scripts/test-fixtures/` — `related-exists-pass/` (two records, one refers to the other), `related-exists-fail/` (record refers to nonexistent id).
  - Spec — [`ia/projects/TECH-860.md`](ia/projects/TECH-860.md)

- [ ] **TECH-861 — Enforce `depends_on_raw` non-empty** — In `validate-backlog-yaml.mjs`, reject records where `depends_on: []` is non-empty AND `depends_on_raw` is empty / missing.
  - Acceptance — In `validate-backlog-yaml.mjs`, reject records where `depends_on: []` is non-empty AND `depends_on_raw` is empty / missing.
  - Spec — [`ia/projects/TECH-861.md`](ia/projects/TECH-861.md)

- [ ] **TECH-862 — Warn on `depends_on_raw` drift** — Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`.
  - Acceptance — Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`.
  - Spec — [`ia/projects/TECH-862.md`](ia/projects/TECH-862.md)

- [ ] **TECH-863 — Fixtures for `depends_on_raw` checks** — Add fixtures — `depends-raw-pass/`, `depends-raw-empty-fail/`, `depends-raw-drift-warn/`.
  - Acceptance — Add fixtures — `depends-raw-pass/`, `depends-raw-empty-fail/`, `depends-raw-drift-warn/`.
  - Spec — [`ia/projects/TECH-863.md`](ia/projects/TECH-863.md)

## MCP lifecycle tools — Opus 4.7 audit program

Orchestrator: [`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`](projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Reshapes 32-tool MCP surface from 4.6-era sequential-call shape to 4.7-era composite-bundle + structured-envelope architecture. Step 1 closed — Stage 1.1 + Stage 1.2 archived (glossary bulk-`terms`, structured `invariants_summary`, v0.6.0 release). Step 2 In Progress — Stage 2.1 archived (TECH-388..TECH-391: envelope + caller allowlist + unit tests). Stage 2.2 opened 2026-04-18 — 8 tasks filed below (TECH-398..TECH-405: wrap all 32 handlers in `wrapTool` by family — spec / rule+router / glossary / invariant / backlog / DB-coupled / bridge / Unity analysis). Step 2 exit ships as breaking release v1.0.0.

### Stage 2.1 — Envelope Infrastructure + Auth

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.2 — Rewrite 32 Tool Handlers

## IA evolution lane

Evolve **Information Architecture** from doc retrieval → learning, bidirectional, graph-queryable platform. **TECH-77** (FTS) + **TECH-78** (skill chaining) independent. **TECH-79** (agent memory) + **TECH-80** (bidirectional IA) need Postgres tables (independent). **TECH-81** (knowledge graph) long-term — benefits from **TECH-77** index + **TECH-79** session data. **TECH-82** (gameplay entity model) bridges IA tooling + game data. **TECH-83** (sim param tuning) uses bridge + optional **TECH-82** metrics tables. **TECH-552** (Unity Agent Bridge program — MCP + Editor queue hardening / transport / depth tiers per [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §10). **Context:** [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md). **Overview:** [`docs/information-architecture-overview.md`](docs/information-architecture-overview.md).

- [ ] **TECH-77** — **Unified semantic search** across all IA surfaces (FTS in Postgres)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new tool + ingest); `db/migrations/`; `docs/mcp-ia-server.md`
  - Notes: Single `ia_search(query, scope?)` MCP tool backed by Postgres FTS that returns ranked results across glossary, spec sections, invariants, rules, backlog issues, and journal entries. Extends the `body_tsv` GIN pattern from `ia_project_spec_journal`. Does not replace existing precise tools (`spec_section`, `glossary_lookup`).
  - Acceptance: `ia_search` registered; searches across all IA surfaces with source attribution; `npm run verify` green
  - Depends on: none

- [ ] **TECH-78** — **Skill chaining engine** (`suggest_skill_chain` MCP tool)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new tool + SKILL.md parser); `ia/skills/*/SKILL.md` (read-only); `docs/mcp-ia-server.md`
  - Notes: MCP tool that reads all SKILL.md files, matches trigger conditions against a task description, and returns an ordered skill chain with pre-populated MCP tool call sequences. Understands skill lifecycle dependencies (kickoff → implement → close-dev-loop → close). When given an `issue_id`, enriches the chain with `backlog_issue` data.
  - Acceptance: `suggest_skill_chain` registered; returns correct chains for known task descriptions; `npm run verify` green
  - Depends on: none

- [ ] **TECH-79** — **Agent memory across sessions** (persistent agent context)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (logging middleware + new tool); `db/migrations/`; `docs/mcp-ia-server.md`
  - Notes: Log MCP tool calls per issue in `agent_session_log` Postgres table. New `ia_recommend(issue_id?, domain?)` MCP tool uses historical patterns to recommend spec sections, glossary terms, and tool sequences. Fire-and-forget logging — never blocks tool responses.
  - Acceptance: tool calls transparently logged; `ia_recommend` returns recommendations based on historical data; graceful `db_unconfigured` degradation
  - Depends on: none

- [ ] **TECH-80** — **Bidirectional IA**: agents propose **glossary** additions and flag **spec** ambiguity
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (3 new tools); `db/migrations/`; `docs/mcp-ia-server.md`
  - Notes: `suggest_ia_improvement(kind, content, context?)` for agents to propose glossary additions, flag spec ambiguity, or suggest invariant additions. `ia_suggestions_pending` and `ia_suggestion_resolve` for human review lifecycle (`proposed` → `accepted` / `rejected`). Human review mandatory.
  - Acceptance: three tools registered; full lifecycle (propose → list → resolve) works; `npm run verify` green
  - Depends on: none

- [ ] **TECH-81** — **Knowledge graph**: evolve IA from document retrieval to entity-relationship model
  - Type: tooling / agent enablement (long-term)
  - Files: `tools/mcp-ia-server/src/` (graph ingest + 2 new tools); `db/migrations/`; `docs/mcp-ia-server.md`
  - Notes: Postgres-backed entity-relationship graph (managers, data structures, invariants, glossary terms, spec sections as nodes; "depends on"/"modifies"/"validates" as edges). `dependency_chain(entity)` and `impact_analysis(entity)` MCP tools for transitive queries. Ingest from glossary cross-references, ARCHITECTURE.md dependencies, invariant entity mentions, spec cross-links.
  - Acceptance: graph tables populated from current IA; `dependency_chain` and `impact_analysis` return correct transitive relationships; visualization JSON export
  - Depends on: none (soft: TECH-77 for FTS infrastructure; TECH-79 for usage-based edge enrichment)

- [ ] **TECH-82** — **Entity model** for gameplay database (time-series, events, snapshots, building identity)
  - Type: tooling / gameplay infrastructure
  - Files: `db/migrations/`; new C# `MetricsRecorder` helper; `tools/postgres-ia/` (bridge scripts); `tools/mcp-ia-server/src/` (query tools); `docs/postgres-ia-dev-setup.md`; `SimulationManager.cs`, `EconomyManager.cs`, `ZoneManager.cs` (integration hooks)
  - Notes: Four phases: (1) `city_metrics_history` — per-tick city metric snapshots for FEAT-51 dashboard and **TECH-31** **test mode** **city history** assertions — **program stage 31d** in [`projects/TECH-31-agent-scenario-generator-program.md`](projects/TECH-31-agent-scenario-generator-program.md) tracks ordering vs **TECH-31** **31a**–**31c**. (2) `city_events` — financial event sourcing for **monthly maintenance** and other treasury movements (see **glossary** **Monthly maintenance**). (3) `grid_snapshots` — periodic grid state for diffing/analysis. (4) `buildings` table — individual building identity for FEAT-08 density evolution. All fire-and-forget; game fully playable without Postgres. Scenarios remain **save**-authoritative; DB rows are observability, not the scenario file.
  - Acceptance: Phase 1 at minimum: metrics recorded per tick, MCP query tool returns time-series; game playable without DB
  - Depends on: none (soft: FEAT-51 as primary consumer of Phase 1; **TECH-31** for **scenario** run correlation / **test mode** recording expectations; Phase 2 aligns with **EconomyManager** chokepoints; FEAT-08 for Phase 4)

- [ ] **TECH-83** — **Agent-driven simulation parameter tuning**
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (3 new tools); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (new bridge commands); `db/migrations/` (experiment results); `docs/mcp-ia-server.md`
  - Notes: MCP tools to read (`sim_params_read`), modify (`sim_params_write`), and evaluate (`sim_experiment`) simulation parameters at runtime. Agents can A/B test parameter changes (growth budget, demand rates, ring fractions) by running N ticks and measuring outcomes. State snapshot/restore for experiment isolation. Results persisted in Postgres.
  - Acceptance: parameter catalog complete; write→read roundtrip works; experiment runs N ticks and returns metric comparison; game state restored after experiment
  - Depends on: none (soft: TECH-82 Phase 1 for richer metric collection)

- [ ] **TECH-552** — **Unity Agent Bridge** program — file-based command queue + MCP tools for agent-triggered exports
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (existing `unity_bridge_command` / `unity_bridge_get` + sugar wrappers); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (existing; extended); `.claude/skills/` (new Cursor skills per analysis doc §6); `docs/mcp-ia-server.md` (tool docs); `ia/skills/ide-bridge-evidence/SKILL.md` (evidence contract)
  - Notes: Phase 1 (file-based command queue + `unity_bridge_command` / `unity_bridge_get`) already shipped per analysis doc §8.1. Program tracks analysis doc §10 tiers B (hardening — parameterized exports, sugar tools, Cursor skills), C (HTTP transport, log streaming, screenshot automation), D (deterministic replay, visual diff). Explicitly excludes `-batchmode` / headless CI per analysis doc §4.1 + §11.
  - Acceptance: master plan authored at `ia/projects/unity-agent-bridge-master-plan.md` with ≥3 Steps landing on green-bar boundaries; at minimum Step 1 = analysis §10-B hardening; each stage has 2–6 tasks per phase (cardinality gate); all analysis doc §10 items mapped into a step OR explicitly deferred to post-MVP extensions doc.
  - Depends on: none hard. Soft: existing `unity_bridge_command` MCP tool contract (do not break); `ia/specs/unity-development-context.md` §10 Reports contract alignment.
  - Related: TECH-83 (consumer — sim params uses bridge); TECH-78 (sibling agent tooling); TECH-251 (Opus 4.7 touches `ide-bridge-evidence`).

- [ ] **TECH-322** — Ship-stage chain shipper — stateful subagent + skill + command (Approach B)
  - Type: tech (IA infrastructure / lifecycle tooling)
  - Files: `.claude/commands/ship-stage.md` (new), `.claude/agents/ship-stage.md` (new), `ia/skills/ship-stage/SKILL.md` (new), `.claude/agents/verify-loop.md`, `ia/skills/verify-loop/SKILL.md`, `docs/agent-lifecycle.md`, `ia/rules/agent-lifecycle.md`, `CLAUDE.md`, `AGENTS.md`, `ia/specs/glossary.md`; design source `docs/ship-stage-exploration.md`
  - Notes: New `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` dispatcher — chains `spec-kickoff → spec-implementer → verify-loop → closeout` across every filed task row of one Stage X.Y in a master plan. **Approach B** per `/design-explore` interview (Q1=B stateful chain w/ cached MCP context; Q2=A resolved-as-none-needed — per-spec `project-stage-close` unchanged + chain-level stage digest NEW; Q3=A auto-resolve next-stage across 4 cases; Q4=C hybrid verify — per-task Path A fail-fast, batched Path B at stage end via `--skip-path-b`; Q5=C hybrid parser — narrow regex v1 + follow-up MCP `spec_stage_table` slice). Hard-depends on TECH-302 Stage 2 (`domain-context-load` + `term-anchor-verify` shared subskills). `/ship-step` + `/ship-plan` deferred. Tooling only — no runtime C# invariants.
  - Acceptance: `/ship-stage` chains all stage tasks sequentially; stops on first per-task failure w/ structured digest; chain-level stage digest distinct from per-spec `project-stage-close`; `Next:` auto-resolves 4 cases (filed / pending / skeleton / umbrella-done); hybrid verify — Path A per-task, Path B batched via `--skip-path-b`; regex parser fails loud on schema drift w/ fixtures for 2-3 master plans; smoke run on real stage w/ ≥2 open tasks passes; follow-up issue filed for `spec_stage_table` MCP slice; docs + glossary updated; `npm run validate:all` clean.
  - Depends on: TECH-302 (Stage 2 `domain-context-load` + `term-anchor-verify` — hard gate)

## UI-as-code program (exploration)

**Charter (§ Completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**):** Reduce **manual Unity Editor** work for **HUD** / **menus** / **panels** / **toolbars** — make **UI** composable from **IDE** (Cursor) + **AI agents**. Shipped: **reference spec** (**`ui-design-system.md`**), **runtime** **`UiTheme`** + **`UIManager` partials** + prefab **v0**, **Editor** menus (**`unity-development-context.md`** **§10**), **Cursor Skills**, optional **territory-ia** affordances. **UI** spans **multiple scenes**; **UI** inventory export + spec prose **per scene**. **As-built baseline:** **`ui-design-system.md`** + committed [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json). **Codebase inventory (uGUI):** **`ui-design-system.md`** **Related files**. **Ongoing:** refresh **inventory** + baseline JSON when hierarchies shift; optional **`ui_theme_tokens` MCP** — new **BACKLOG** row if product wants it.

- [ ] **FEAT-51** — **Game data dashboard**: **time-series** **simulation** metrics, charts, dense **HUD**-style **cards** (**uGUI**)
  - Type: feature / UX + **simulation** observability
  - Files: [`docs/ui-data-dashboard-exploration.md`](docs/ui-data-dashboard-exploration.md) (mechanisms and dependency graph); [`ia/projects/FEAT-51.md`](ia/projects/FEAT-51.md); `ia/specs/ui-design-system.md` (**modal**, **scroll**, **UiTheme**); `ia/specs/simulation-system.md` (**simulation tick** sampling — read-only); `ia/specs/persistence-system.md` (if **Save**/**Load** of history); `Assets/Scripts/Managers/GameManagers/` (**CityStats**, **EconomyManager**, **DemandManager**, **StatisticsManager**, **TimeManager**); new **UI** prefabs / partials as implemented
  - Spec sections: `ia/specs/ui-design-system.md` — **§1** **Foundations**, **§3** patterns, **§5.3** polish patterns; `ia/specs/simulation-system.md`; `ia/specs/persistence-system.md` (optional persistence); [`docs/ui-data-dashboard-exploration.md`](docs/ui-data-dashboard-exploration.md)
  - Notes: Delivers **exploration** **§2.1–§2.5** (history → derived metrics → chart engine → **dashboard** layout). Reuse **UI-as-code** **tokens** (**`ui-design-system.md`**); **map** **info view** (**§2.6**) is **out of scope** — separate **FEAT-** when prioritized. **Spike** chart library (**XCharts** or equivalent) per **Decision Log**. Add chart-specific **`UiTheme`** fields in this issue or a follow-up **TECH-** row when scoped.
  - Acceptance: per `ia/projects/FEAT-51.md` **§8**; chart choice and persistence stance recorded in spec **Decision Log**
  - Depends on: none (soft constraint: no per-frame **`FindObjectOfType`** in dashboard UI — see `BACKLOG-ARCHIVE.md` BUG-14)
  - Related: see `BACKLOG-ARCHIVE.md` BUG-14

- [ ] **TECH-72** — **HUD** / **uGUI** scene hygiene for agents (**UI** inventory alignment)
  - Type: code health / **UI**-as-code enablement
  - Files: `Assets/Scenes/MainScene.unity`; `Assets/Scenes/MainMenu.unity` (if matching issues appear); `UIManager.cs` + **`UIManager.*.cs`** partials; `CityStatsUIController.cs`; **`ProposalUIController.cs`**, **`UrbanizationProposalManager.cs`** (if removing obsolete **Proposal** chrome); `ia/specs/ui-design-system.md` — **§1.3.1**; `docs/reports/ui-inventory-as-built-baseline.json` (refresh after scene edits)
  - Spec sections: `ia/specs/ui-design-system.md` — **§1.3.1** **HUD and uGUI hygiene**; `ia/specs/unity-development-context.md` **§10** when re-exporting **UI** inventory
  - Notes: Remediate **as-built** drift flagged against **Postgres** **`editor_export_ui_inventory`** **id** **8** / committed baseline: **`CommercialTaxText `** trailing space; **`RoadGrowthLabel (1)`** auto-rename; **`Canvas/DataPanelButtons/NewGameButton`** name collision vs **MainMenu**; **`GameManager`** on **`LoadGameMenuPanel`** root; **`StatsPanel`** **UIDocument** + **uGUI** boundary documentation; **`NotificationPanel`** **TMP** + legacy mix policy; **`ProposalUI`** vs glossary **Urbanization proposal** (**obsolete**)—confirm inert then remove or disconnect. **No** **simulation** rule changes. **Id policy:** **TECH-60** is **archived** for the **spec pipeline program** — do not reuse; this row uses the next **TECH** id (**TECH-72** after **TECH-71** in archive).
  - Acceptance: per `ia/projects/TECH-72.md` **§8**; baseline JSON re-exported after scene changes; **§1.3.1** violations in scope either fixed or explicitly documented in spec **Decision Log**
  - Depends on: none
  - Related: **FEAT-51**

- [ ] **TECH-309** — **StudioRackBlock schema** — extend UiTheme token ring w/ studio-rack block
  - Type: technical / UI infrastructure
  - Files: `Assets/Scripts/Managers/GameManagers/UiTheme.cs`
  - Notes: Add `[Serializable] class StudioRackBlock` to `UiTheme.cs` w/ studio-rack fields: `ledHues` (`Color[]`), `vuGradientStops` (`Gradient`), `knobDetentColor` (`Color`), `faderTrackGradient` (`Gradient`), `oscilloscopeTrace` (`Color`), `oscilloscopeGlowColor` (`Color`), `shadowDepthStops` (`float[3]`), `glowRadius` (`float`), `glowColor` (`Color`), `sparklePalette` (`Color[]`). Expose via `public StudioRackBlock studioRack` on `UiTheme`. Additive only — existing fields untouched. Serializable defaults inert (asset defaults land in TECH-311). Stage 1.1 T1.1.1 of `ia/projects/ui-polish-master-plan.md`. Foundation for all downstream rings.
  - Acceptance: `StudioRackBlock` nested class present w/ all 10 named fields; `UiTheme.studioRack` exposed; existing fields untouched; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: none (stage-internal: TECH-311 consumes these fields as asset defaults)

- [ ] **TECH-310** — **MotionBlock schema + curves** — extend UiTheme w/ semantic motion block
  - Type: technical / UI infrastructure
  - Files: `Assets/Scripts/Managers/GameManagers/UiTheme.cs`
  - Notes: Add `[Serializable] class MotionBlock` to `UiTheme.cs` w/ semantic motion fields: `moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`. Each a `[Serializable] struct MotionEntry { float durationSeconds; AnimationCurve easing; }`. Expose via `public MotionBlock motion` on `UiTheme`. Additive only. Consumer rings (primitives Step 2, studio controls Step 3, juice Step 4) read duration + easing from these entries — no hard-coded values downstream. Stage 1.1 T1.1.2 of `ia/projects/ui-polish-master-plan.md`.
  - Acceptance: `MotionBlock` + `MotionEntry` types present; `UiTheme.motion` exposed w/ 6 semantic entries; existing fields untouched; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: none (stage-internal: TECH-311 consumes these entries as asset defaults)

- [ ] **TECH-311** — **DefaultUiTheme.asset defaults** — populate studio-rack + motion Inspector values
  - Type: technical / UI asset authoring
  - Files: `Assets/UI/Theme/DefaultUiTheme.asset`
  - Notes: Populate `Assets/UI/Theme/DefaultUiTheme.asset` Inspector values for every new `studioRack` + `motion` field per `ia/specs/ui-design-system.md §7.1` dark-first palette. LED hues: green / amber / red triad. VU gradient: green → amber → red. Motion defaults: `moneyTick.durationSeconds = 0.28f`; `needleAttack = 0.08f`; `needleRelease = 0.40f`; `alertPulse`, `sparkleDuration`, `panelElevate` per exploration §Design Expansion. Stage 1.1 T1.1.3 of `ia/projects/ui-polish-master-plan.md`. Consumes schema from TECH-309 + TECH-310.
  - Acceptance: Every `studioRack` + `motion` field on `DefaultUiTheme.asset` has non-default Inspector value matching §7.1 dark-first palette; LED hues triad present; VU gradient correct; motion durations per above; `npm run validate:all` green.
  - Depends on: TECH-309 (StudioRackBlock schema); TECH-310 (MotionBlock schema)

- [ ] **TECH-312** — **ui-design-system §1 + §1.5 token catalog** — normative studio-rack + motion rows
  - Type: documentation / reference spec
  - Files: `ia/specs/ui-design-system.md`
  - Notes: Extend `ia/specs/ui-design-system.md` §1 (palette / spacing) w/ studio-rack token names + role. Extend §1.5 (motion) w/ motion token catalog. Every field in `StudioRackBlock` + `MotionBlock` cited normatively. Normative token names match `UiTheme` field names exactly (consumer rings read these by name). Link from §2 to anchor primitives-to-tokens mapping. Stage 1.1 T1.1.4 of `ia/projects/ui-polish-master-plan.md`. Source of record = `docs/ui-polish-exploration.md` §Design Expansion.
  - Acceptance: §1 extended w/ studio-rack token catalog subsection; §1.5 extended w/ motion token catalog; every `StudioRackBlock` + `MotionBlock` field present normatively; §2 anchor link added; `npm run validate:all` green.
  - Depends on: TECH-309 (StudioRackBlock schema — spec rows match field names); TECH-310 (MotionBlock schema)

- [ ] **TECH-313** — **Glossary rows** — UiTheme token ring / Studio-rack token / Motion token
  - Type: documentation / glossary
  - Files: `ia/specs/glossary.md`
  - Notes: Add three rows to `ia/specs/glossary.md`: `UiTheme token ring` (extended token catalog under `UiTheme` SO covering surface / accent / studio-rack / motion blocks), `Studio-rack token` (LED / VU / knob / fader / oscilloscope visual params), `Motion token` (semantic named duration + easing curve entry under `UiTheme.motion`). Each row cites `ia/specs/ui-design-system.md` §1 / §1.5 per terminology-consistency rule. Stage 1.1 T1.1.5 of `ia/projects/ui-polish-master-plan.md`. Locks vocabulary downstream steps reference.
  - Acceptance: Three glossary rows present + spec-referenced; terminology-consistency rule satisfied (glossary + authoritative spec section both carry term); `npm run validate:all` green; `npm run test:ia` green (glossary-index regenerate).
  - Depends on: TECH-312 (ui-design-system §1 + §1.5 catalog — glossary rows cite those sections)

## Economic depth lane

Transform economy from "money goes up forever" → genuine city-builder sim w/ tension, feedback loops, player-visible consequences. **Sequential dependency order:** dynamic happiness (done — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)) → **monthly maintenance** (shipped — **glossary** **Monthly maintenance**) → **tax→demand feedback** (shipped — **managers-reference** **Demand (R / C / I)**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)) → **FEAT-09** (trade/production — deep economy, moved from § Low Priority). **FEAT-52** (city services coverage) + **FEAT-53** (districts) extend spatial economic depth. **Context:** [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md) §4.

- [ ] **FEAT-52** — **City services coverage** model (fire, police, education, health)
  - Type: feature (new system)
  - Files: new `ServiceCoverageManager.cs`; `CityStats.cs`; `DemandManager.cs`; `GridManager.cs`; `GridPathfinder.cs`; `MiniMapController.cs`
  - Notes: Generic **service coverage** system: each service **building** has a coverage **radius** computed from the **road network**. **Cells** within coverage receive **happiness** and **desirability** bonuses; **cells** outside suffer penalties. Coverage gaps visible on **minimap** as danger zones. Framework for FEAT-11 (education), FEAT-12 (police), FEAT-13 (fire). Ships with at least one concrete service type (fire station).
  - Acceptance: per `ia/projects/FEAT-52.md` §8; coverage affects happiness and desirability; minimap layer shows coverage heatmap
  - Depends on: none (happiness system shipped — see [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **FEAT-53** — **District / neighborhood** system
  - Type: feature (new system)
  - Files: new `DistrictManager.cs`; `Cell.cs` / `CellData.cs`; `CityStats.cs`; `EconomyManager.cs`; `MiniMapController.cs`; `UIManager.cs`; `GameSaveManager.cs`
  - Notes: Player-defined **districts** (contiguous **cell** regions with name and color). Per-**district** statistics: **population**, **happiness**, **zone** distribution, **density**, **tax** revenue. Optional per-**district** **tax** policy overrides. **Minimap** district overlay. Coordinates with FEAT-47 (**multipolar** **urban centroids**) — each **urban pole** naturally becomes a **district**.
  - Acceptance: per `ia/projects/FEAT-53.md` §8; districts persist across save/load; per-district stats and tax overrides functional; minimap district layer
  - Depends on: none (soft: FEAT-47 for multipolar coordination; **tax→demand** loop shipped — **managers-reference** **Demand (R / C / I)**)

- [ ] **FEAT-09** — Trade / Production / Salaries (deep economy)
  - Type: feature (new system)
  - Files: `EconomyManager.cs`, `CityStats.cs` (+ new managers)
  - Notes: Economic system of production, trade between **RCI** **zones** and salaries. Long-term lane goal: full economic loop from production through trade to consumption.
  - Depends on: none (**tax→demand** feedback shipped — **managers-reference** **Demand (R / C / I)**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

## Gameplay & simulation lane

Player-facing **simulation**, **AUTO** growth, **urban growth rings** / **zone density** depth. **Economic** issues → **§ Economic depth lane** above. **§ High Priority** still holds map/render/save **interrupt** bugs.

- [ ] **FEAT-43** — **Urban growth rings**: tune **AUTO** road/zoning weights for a gradual center → edge gradient
  - Type: feature (simulation / balance)
  - Files: `UrbanCentroidService.cs` (**growth ring** boundaries, **urban centroid** distance), `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `GrowthBudgetManager.cs` if per-ring **growth budgets** apply; `GridManager.cs` / `DemandManager.cs` only if **desirability** or placement must align with **growth rings**
  - Notes: **Observed:** In **AUTO** simulation, cities tend toward a **dense core**, **under-developed middle growth rings**, and **outer rings that are more zoned than the middle** — not a smooth radial gradient. **Expected:** Development should fall off **gradually from the urban centroid**: **highest** **street** density and **AUTO** zoning pressure **near the centroid**, **moderate** in **mid growth rings**, and **lowest** in **outer growth rings**. Revisit **growth ring** radii/thresholds, per-ring weights for **AUTO** road growth vs zoning, and any caps or priorities that invert mid vs outer activity. **Related:** earlier **AUTO** road/**desirability**/**zone density** features and perpendicular-stub fixes — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Depends on: none

- [ ] **FEAT-08** — **Zone density** and **desirability** simulation: evolution to larger **buildings**
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`, `Cell.cs`
  - Notes: Existing **buildings** evolve to larger versions based on **zone density** and **desirability**. Includes spatial **pollution** → **desirability** penalty: **cells** near polluting sources (industrial **buildings**, power plants) receive a per-cell **desirability** malus via radius-based diffusion, discouraging residential evolution and **AUTO** zoning near polluters. Extends the city-wide **pollution** aggregate (shipped) into a per-cell spatial model. (**TECH-15** / **TECH-16** — performance + harness work — live under **§ Agent ↔ Unity & MCP context lane**.)
  - Depends on: none (**pollution** model shipped — see [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

## Multi-scale simulation lane

Orchestrator: [`ia/projects/multi-scale-master-plan.md`](projects/multi-scale-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = parent-scale conceptual stubs (code + save surfaces only; no playable parent scales). Stage 1.1 = parent-scale identity fields — archived. Stage 1.2 = cell-type split — archived. Stage 1.3 = neighbor-city stub + interstate-border semantics — filed below.

### Stage 1.3 — Neighbor-city stub + interstate-border semantics

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

## Distribution program — Full-Game MVP Bucket 10

Orchestrator: [`ia/projects/distribution-master-plan.md`](projects/distribution-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 10 of full-game-mvp umbrella (Tier E — unsigned installer tier for curated 20–50 testers; signing / Linux / WebGL / patch deltas / Steam / public itch deferred). Exploration: [`docs/distribution-exploration.md`](docs/distribution-exploration.md) §Design Expansion. Step 1 = Unity build pipeline + versioning manifest. Step 2 = unsigned packaging + `/download` publication + in-game notifier. Stage 1.1 opened 2026-04-18 — 4 tasks filed below (TECH-347..TECH-350: BuildInfo SO type + asset + SemverCompare helper + distribution glossary rows).

### Stage 1.1 — BuildInfo SO + semver compare helper

- [ ] **TECH-347** — BuildInfo ScriptableObject type (Stage 1.1 T1.1.1)
  - Type: feature
  - Files: `Assets/Scripts/Runtime/Distribution/BuildInfo.cs`
  - Notes: Author BuildInfo SO per Design Expansion IP-3 — `[CreateAssetMenu]`, private serialized `version` / `gitSha` / `buildTimestamp` w/ defaults, public getters, editor-gated `WriteFields` under `#if UNITY_EDITOR`. Inert data model.
  - Acceptance: compiles; menu populates; `WriteFields` gated; `unity:compile-check` green.
  - Depends on: none
  - Related: TECH-348, TECH-349, TECH-350

- [ ] **TECH-348** — BuildInfo asset instance under Assets/Resources (Stage 1.1 T1.1.2)
  - Type: feature
  - Files: `Assets/Resources/BuildInfo.asset`, `Assets/Resources/BuildInfo.asset.meta`
  - Notes: Create `.asset` via Territory/BuildInfo menu (from T1.1.1 `[CreateAssetMenu]`); commit `.asset` + `.meta`; verify `Resources.Load<BuildInfo>("BuildInfo")` non-null. Defaults stay until Stage 1.2 writer stamps.
  - Acceptance: asset + meta committed; `Resources.Load` non-null; defaults match; `unity:compile-check` green.
  - Depends on: TECH-347
  - Related: TECH-347, TECH-349, TECH-350

- [ ] **TECH-349** — SemverCompare helper + EditMode truth-table tests (Stage 1.1 T1.1.3)
  - Type: feature
  - Files: `Assets/Scripts/Runtime/Distribution/SemverCompare.cs`, `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs`
  - Notes: Author static `Compare(string, string) → int` per IP-8 subset (MAJOR.MINOR.PATCH + optional `-PRERELEASE`). No external lib. Truth-table ≥6 cases — equal, major >, minor >, patch >, prerelease ordering, malformed → 0 fallback.
  - Acceptance: `Compare` handles M.m.p + prerelease; ≥6 EditMode cases green; malformed → 0; `unity:compile-check` + EditMode green.
  - Depends on: none
  - Related: TECH-347, TECH-348, TECH-350

- [ ] **TECH-350** — Distribution glossary rows in ia/specs/glossary.md (Stage 1.1 T1.1.4)
  - Type: documentation
  - Files: `ia/specs/glossary.md`
  - Notes: Append rows — **BuildInfo ScriptableObject**, **Release manifest (`latest.json`)**, **Update notifier**, **Unsigned installer tier**. Forward-ref Stage 2.1 / 2.2 / 2.3. Follow `ia/rules/terminology-consistency-authoring.md`.
  - Acceptance: four rows in alpha order; spec refs + forward-refs present; `validate:all` green.
  - Depends on: TECH-347
  - Related: TECH-347, TECH-348, TECH-349

## CityStats overhaul program

Orchestrator: [`ia/projects/citystats-overhaul-master-plan.md`](projects/citystats-overhaul-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 8 of full-game-mvp umbrella (Tier D — execution gated on downstream triggers; filing now for IA alignment). Replace `CityStats` god-class w/ typed read-model facade (`CityStatsFacade`) + columnar ring-buffer store (`ColumnarStatsStore`); migrate consumers; add region/country rollup facades; surface web stats route. **Stage 1.1 (Core types) closed 2026-04-21** — TECH-303, TECH-304 archived. Next: `/stage-file ia/projects/citystats-overhaul-master-plan.md` Stage 2 (CityStatsFacade + tick bracket; tasks still `_pending_`).

### Stage 1.1 — Core types (IStatsReadModel, StatKey, ColumnarStatsStore) — **closed**

## City-Sim Depth program

Orchestrator: [`ia/projects/city-sim-depth-master-plan.md`](projects/city-sim-depth-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 2 of full-game-mvp umbrella. Shared 12-signal simulation contract + district aggregation + `HappinessComposer` / `DesirabilityComposer` migration + 7 new simulation sub-surfaces + signal overlays + HUD/district panel parity. Step 1 = Signal Layer Foundation. Stage 1.1 opened 2026-04-17 — 4 tasks filed below (TECH-305..TECH-308: `SimulationSignal` enum + producer/consumer interfaces + `SignalField` + `SignalMetadataRegistry` SO + `SignalFieldRegistry` MonoBehaviour + `ia/specs/simulation-signals.md` reference spec).

### Stage 1.1 — Signal Contract Primitives

- [ ] **TECH-305** — Add `SimulationSignal` enum + `ISignalProducer`/`ISignalConsumer` interfaces (Stage 1.1 T1.1.1)
  - Type: infrastructure / simulation signal types
  - Files: `Assets/Scripts/Simulation/Signals/SimulationSignal.cs` (new), `Assets/Scripts/Simulation/Signals/ISignalProducer.cs` (new), `Assets/Scripts/Simulation/Signals/ISignalConsumer.cs` (new)
  - Notes: `SimulationSignal` enum w/ exactly 12 locked entries — `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`. `ISignalProducer.EmitSignals(SignalFieldRegistry)` + `ISignalConsumer.ConsumeSignals(SignalFieldRegistry, DistrictSignalCache)` interfaces. Type surface only — no runtime wiring. City-sim depth Bucket 2 foundation.
  - Acceptance: All 3 files compile; enum has exactly 12 entries; interfaces match spec; `npm run unity:compile-check` clean; `npm run validate:all` clean.
  - Depends on: none

- [ ] **TECH-306** — Add `SignalField` + `SignalMetadataRegistry` ScriptableObject (Stage 1.1 T1.1.2)
  - Type: infrastructure / simulation signal types
  - Files: `Assets/Scripts/Simulation/Signals/SignalField.cs` (new), `Assets/Scripts/Simulation/Signals/SignalMetadataRegistry.cs` (new); consumes `Assets/Scripts/Simulation/Signals/SimulationSignal.cs`
  - Notes: `SignalField` — `float[,]` backing store; `Get`/`Set`/`Add`/`Snapshot`; clamp floor 0 on all writes. `SignalMetadataRegistry` SO — per-signal `diffusionRadius`, `decayPerStep`, `Vector2 anisotropy`, `rollupRule (Mean/P90)`. Consumes TECH-305 enum.
  - Acceptance: Both compile; floor clamp verified; `Snapshot()` returns independent copy; `npm run unity:compile-check` clean; `npm run validate:all` clean.
  - Depends on: TECH-305 (SimulationSignal enum)

- [ ] **TECH-307** — Add `SignalFieldRegistry` MonoBehaviour (Stage 1.1 T1.1.3)
  - Type: infrastructure / simulation signal MonoBehaviour
  - Files: `Assets/Scripts/Simulation/Signals/SignalFieldRegistry.cs` (new); consumes `SignalField.cs`, `SimulationSignal.cs`, `Assets/Scripts/Managers/GameManagers/GridManager.cs`
  - Notes: MonoBehaviour; allocates one `SignalField` per `SimulationSignal` in `Awake` sized from `GridManager.gridWidth`/`gridHeight`; `GetField(SimulationSignal)` accessor; `[SerializeField] GridManager grid` + `FindObjectOfType` fallback (invariant #4); resize method for map reload. Invariant #3 (no hot-path `FindObjectOfType`) + #6 (new MonoBehaviour, not added to `GridManager`).
  - Acceptance: Compiles; 12 fields allocated in Awake; `GetField` works; fallback present; no hot-path `FindObjectOfType`; `npm run unity:compile-check` + `validate:all` clean.
  - Depends on: TECH-305 (enum), TECH-306 (SignalField + SignalMetadataRegistry)

- [ ] **TECH-308** — Author `ia/specs/simulation-signals.md` reference spec (Stage 1.1 T1.1.4)
  - Type: reference spec / simulation signal contract
  - Files: `ia/specs/simulation-signals.md` (new), `ia/specs/simulation-system.md` (link update), `ia/specs/glossary.md` (new rows)
  - Notes: Signal inventory (12 entries: source / sink / rollup rule / cadence), diffusion physics (separable Gaussian, anisotropy, decay, clamp-floor-0), `ISignalProducer`/`ISignalConsumer` contract, rollup rule table (P90 for Crime + TrafficLevel; mean for rest), spec-gap closure note. Link from `ia/specs/simulation-system.md` §Tick execution order. Invariant #12 (permanent domain).
  - Acceptance: New spec present w/ 5 sections; 12 signal rows; rollup table correct; cross-link added; glossary updated; `npm run validate:all` clean.
  - Depends on: TECH-305 (enum), TECH-306 (SignalField + registry types)

## Utilities program

Orchestrator: [`ia/projects/utilities-master-plan.md`](projects/utilities-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 4a of full-game-mvp umbrella. Country-pool-first water / power / sewage w/ local contributor buildings feeding per-scale pools (city / region / country). EMA soft warning → cliff-edge deficit (freeze + happiness decay + desirability decay). Stage 1.1 opened 2026-04-17 — 4 tasks filed below (TECH-331..TECH-334: `UtilityKind` / `ScaleTag` / `PoolStatus` enums + `PoolState` struct + `IUtilityContributor` / `IUtilityConsumer` interfaces + assembly compile-check).

### Stage 1.1 — Data contracts + enums

- [ ] **TECH-331** — Add `UtilityKind` / `ScaleTag` / `PoolStatus` enums (Stage 1.1 T1.1.1)
  - Type: infrastructure / data model
  - Files: `Assets/Scripts/Data/Utilities/UtilityKind.cs` (new), `ScaleTag.cs` (new), `PoolStatus.cs` (new)
  - Notes: Three plain enums w/ XML doc per value. No runtime refs yet. Stage 1.1 Phase 1 of utilities-master-plan.
  - Acceptance: files compile clean via `unity:compile-check`; `validate:all` green.
  - Related: TECH-332, TECH-333, TECH-334

- [ ] **TECH-332** — Add `PoolState` struct (Stage 1.1 T1.1.2)
  - Type: infrastructure / data model
  - Files: `Assets/Scripts/Data/Utilities/PoolState.cs` (new)
  - Notes: Blittable struct (`net`, `ema`, `status`, two hysteresis counters). Default → Healthy + zeros. Stage 1.1 Phase 1.
  - Acceptance: compiles clean; blittable; `validate:all` green.
  - Depends on: TECH-331 (PoolStatus enum — hard gate)
  - Related: TECH-331, TECH-333, TECH-334

- [ ] **TECH-333** — Add `IUtilityContributor` + `IUtilityConsumer` interfaces (Stage 1.1 T1.1.3)
  - Type: infrastructure / data model
  - Files: `Assets/Scripts/Data/Utilities/IUtilityContributor.cs` (new), `IUtilityConsumer.cs` (new)
  - Notes: Two read-only interfaces — Kind, rate, Scale. Consumed by service + registry in later stages. Stage 1.1 Phase 2.
  - Acceptance: compile clean; XML doc; `validate:all` green.
  - Depends on: TECH-331 (enums — hard gate)
  - Related: TECH-331, TECH-332, TECH-334

- [ ] **TECH-334** — Utilities assembly + compile-check green (Stage 1.1 T1.1.4)
  - Type: infrastructure / build
  - Files: `Assets/Scripts/Data/Utilities/Utilities.asmdef` (new, if used)
  - Notes: Asmdef wiring OR confirm main-asm inclusion. Closes Stage 1.1 exit criteria. Stage 1.1 Phase 2.
  - Acceptance: `unity:compile-check` green; types visible to consumers; `validate:all` green.
  - Depends on: TECH-331, TECH-332, TECH-333 (all types exist — hard gate)
  - Related: TECH-331, TECH-332, TECH-333

## Skill training program

Orchestrator: [`ia/projects/skill-training-master-plan.md`](projects/skill-training-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Approach A two-skill split — structured JSON self-report emitter at Phase-N-tail of 13 lifecycle skills + `skill-train` consumer subagent (Opus, on-demand) that synthesizes recurring friction into patch proposals for SKILL.md bodies, gated by user review. Exploration: [`docs/skill-training-exploration.md`](docs/skill-training-exploration.md) §Design Expansion. Stage 1.1 opened 2026-04-18 — 4 tasks filed below (TECH-367..TECH-370: glossary rows × 4 + agent-lifecycle surface-map row + CLAUDE.md §3 pointer + AGENTS.md retrospective paragraph). Satisfies invariant #12 — terminology lands before Stage 1.2 or Step 2 authors cross-refs.

### Stage 1.1 — Glossary + Docs Foundation

## Blip audio program

Orchestrator: [`ia/projects/blip-master-plan.md`](projects/blip-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = DSP foundations + audio infra (all four stages archived). Step 2 in progress — Stage 2.1 archived. Stage 2.2 archived 2026-04-15 (TECH-169..TECH-174). Stage 2.3 closed 2026-04-15 (TECH-188..TECH-191 all archived). Stage 2.4 closed 2026-04-15 (TECH-196..TECH-199 all archived). Step 3 opened 2026-04-15 — Stage 3.1 closed 2026-04-15 (TECH-209..TECH-212 all archived). Stage 3.2 closed 2026-04-15 (TECH-215..TECH-218 all archived). Stage 3.3 closed 2026-04-16 (TECH-219..TECH-222 all archived). Stage 3.4 closed 2026-04-16 (TECH-227..TECH-230 archived). Step 4 opened 2026-04-16 — Stage 4.1 closed 2026-04-16 (TECH-235..TECH-238 all archived). Stage 4.2 closed 2026-04-16 (TECH-243..TECH-246 all archived — `BlipVolumeController` logic bodies + `SfxMutedKey` boot-time restore + glossary update). Step 5 = DSP kernel v2 (post-MVP FX chain + LFOs + biquad BP + param smoothing). Stage 5.1 opened 2026-04-16 — 5 tasks filed below (FX data model + memoryless cores: BitCrush / RingMod / SoftClip / DcBlocker; delay-line kinds stubbed to passthrough until Stage 5.2). Stage 5.2 opened 2026-04-16 — 6 tasks filed below (TECH-270..TECH-275: `BlipDelayPool` service + `Render` delay-buffer overload + `BlipBaker` lease-on-bake + comb / allpass / chorus / flanger kernels + NoAlloc chorus gate).

### Stage 3.1 — Patch authoring + catalog wiring

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.2 — UI + Eco + Sys call sites

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.3 — World lane call sites

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.4 — Golden fixtures + spec promotion + glossary

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_


### Stage 1.1 — Audio infrastructure + persistent bootstrap

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — Patch data model

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.3 — Voice DSP kernel

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.4 — EditMode DSP tests

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.1 — Bake-to-clip pipeline

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.2 — Catalog + mixer router + cooldown registry + player pool

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.3 — BlipEngine facade + main-thread gate

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.4 — PlayMode smoke test

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 4.1 — Options panel UI (slider + mute toggle + controller stub)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 4.2 — Settings controller + persistence + mute semantics

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.1 — FX data model + memoryless cores

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.2 — Delay-line FX + BlipDelayPool

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.3 — LFOs + routing matrix + param smoothing

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

## Music audio program

Orchestrator: [`ia/projects/music-player-master-plan.md`](projects/music-player-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = audio infra + playlist pipeline. Stage 1.1 opened 2026-04-17 — 6 tasks filed below (TECH-316..TECH-321: Blip-Music mixer group + Master/Music param exposure + `MusicBootstrap` consts + Awake shape + prefab + MainMenu placement). Stages 1.2 / 2.x / 3.x remain in master plan; file rows when parent stage → `In Progress`.

### Stage 1.1 — Mixer extension + persistent bootstrap

- [ ] **TECH-316** — **Music** — Add Blip-Music mixer group (Stage 1.1 Phase 1)
  - Type: audio / mixer asset
  - Files: `Assets/Audio/BlipMixer.mixer`
  - Notes: Stage 1.1 Phase 1 T1.1.1 of music-player orchestrator. Extend `BlipMixer.mixer` — add `Blip-Music` group under Master. Do NOT touch existing Blip-UI/World/Ambient groups.
  - Acceptance: New `Blip-Music` group visible under Master in Audio Mixer window; existing groups untouched.

- [ ] **TECH-317** — **Music** — Expose MusicVolume + MasterVolume params (Stage 1.1 Phase 1)
  - Type: audio / mixer asset
  - Files: `Assets/Audio/BlipMixer.mixer`
  - Notes: Stage 1.1 Phase 1 T1.1.2 of music-player orchestrator. Expose `MusicVolume` (→ Blip-Music) + `MasterVolume` (→ Master) dB params. Defaults 0 dB. `SfxVolume` untouched.
  - Acceptance: Exposed Parameters panel shows 3 entries: `MasterVolume`, `MusicVolume`, `SfxVolume`. Bindings correct.
  - Depends on: TECH-316 (Blip-Music group must exist before exposing MusicVolume param)

- [ ] **TECH-318** — **Music** — Author MusicBootstrap constants (Stage 1.1 Phase 2)
  - Type: audio / C#
  - Files: `Assets/Scripts/Audio/Music/MusicBootstrap.cs`
  - Notes: Stage 1.1 Phase 2 T1.1.3 of music-player orchestrator. 7 string consts (Master/Music volume params + keys; last-track id key; enabled key; first-run done key) + 2 default floats. No re-declaration of Blip `SfxVolume*` keys.
  - Acceptance: Consts compile; no key collision w/ Blip; `npm run unity:compile-check` green.

- [ ] **TECH-319** — **Music** — MusicBootstrap.Awake shape (Stage 1.1 Phase 2)
  - Type: audio / C#
  - Files: `Assets/Scripts/Audio/Music/MusicBootstrap.cs`
  - Notes: Stage 1.1 Phase 2 T1.1.4 of music-player orchestrator. MB scaffold mirror of `BlipBootstrap.cs`. `Instance` accessor, `DontDestroyOnLoad` Awake, PlayerPrefs → mixer SetFloat binding for Master + Music params. Invariant #4 — Inspector-placed.
  - Acceptance: Compiles; Awake binds 2 mixer params headless; null-mixer warn present; OnDestroy clears Instance.
  - Depends on: TECH-317 (mixer params must exist before SetFloat calls bind); TECH-318 (consts authored first)

- [ ] **TECH-320** — **Music** — MusicBootstrap prefab creation (Stage 1.1 Phase 3)
  - Type: audio / prefab
  - Files: `Assets/Prefabs/Audio/MusicBootstrap.prefab`
  - Notes: Stage 1.1 Phase 3 T1.1.5 of music-player orchestrator. New prefab w/ `MusicBootstrap` component; Inspector wires `blipMixer` → `BlipMixer.mixer`. No scene diff yet.
  - Acceptance: `.prefab` + `.meta` committed; Inspector ref wired.
  - Depends on: TECH-319 (MusicBootstrap component must exist before prefab can attach it)

- [ ] **TECH-321** — **Music** — MainMenu scene placement + compile verify (Stage 1.1 Phase 3)
  - Type: audio / scene
  - Files: `Assets/Scenes/MainMenu.unity`
  - Notes: Stage 1.1 Phase 3 T1.1.6 of music-player orchestrator. Place `MusicBootstrap.prefab` at `MainMenu.unity` root (sibling to `BlipBootstrap`). Compile verify + manual smoke — `Instance != null`, no null-mixer warn.
  - Acceptance: Scene diff committed; `unity:compile-check` green; smoke passes; invariant #4 satisfied.
  - Depends on: TECH-320 (prefab must exist before scene placement)

## Sprite gen lane

Orchestrator: [`ia/projects/sprite-gen-master-plan.md`](projects/sprite-gen-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = Geometry MVP. Stages 1.1–1.2 archived (TECH-123..TECH-128, TECH-147..TECH-152 in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). Stage 1.3 opened 2026-04-15 — 6 tasks filed below (K-means extractor + palette CLI + apply_ramp + compose wiring + palette tests + bootstrap residential JSON + Tier 1 `.gpl` round-trip). T1.3.3+T1.3.4 merged into TECH-155 (apply_ramp API + compose wiring — tight coupling, single commit unit); T1.3.7+T1.3.8+T1.3.9 merged into TECH-158 (GPL export + import + round-trip test — must land atomic for symmetry). Stage 1.4 opened 2026-04-15 — 9 tasks filed below (slopes.yaml + iso_stepped_foundation + compose auto-insert + slope regression tests + Unity meta writer + promote/reject CLI + Aseprite bin resolver + layered .aseprite emit + promote --edit round-trip). Steps 2–3 remain in master plan; file rows when parent stage → `In Progress`.

### Stage 1.1 — Scaffolding + Primitive Renderer (Layer 1)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — Composition + YAML Schema + CLI Skeleton (Layer 2)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.3 — Palette System (Layer 3)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.4 — Slope-Aware Foundation + Curation CLI (Layer 5)

## Web platform lane

Orchestrator: [`ia/projects/web-platform-master-plan.md`](projects/web-platform-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = Scaffold + design system foundation. Stage 1.1 closed (see BACKLOG-ARCHIVE.md). Stage 1.2 closed 2026-04-14 — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips + HeatmapCell/AnnotatedMap + `/design` review route + README §Tokens all archived (see BACKLOG-ARCHIVE.md). Step 2 closed 2026-04-15 — Stage 2.1 (MDX pipeline + public pages + SEO — TECH-163…TECH-168), Stage 2.2 (wiki + glossary auto-index + search — TECH-184…TECH-187), Stage 2.3 (devlog + RSS + origin story — TECH-192…TECH-195) all archived. Step 3 Stage 3.1 closed 2026-04-15 — plan loader + typed schema (TECH-200…TECH-203 archived). Stage 3.2 closed 2026-04-15 — dashboard RSC + filters (T3.2.1 + T3.2.2 + T3.2.3 + T3.2.4 archived). Stage 3.3 closed 2026-04-15 — legacy handoff + E2E smoke + deprecation log (TECH-213 + TECH-214 archived). Step 4 Stage 4.1 closed 2026-04-16 — nav sidebar + icon system (TECH-223 + TECH-224 + TECH-225 + TECH-226 all archived). Stage 4.2 closed 2026-04-16 — UI primitives polish + dashboard percentages (TECH-231 + TECH-232 + TECH-233 + TECH-234 all archived 2026-04-16). Stage 4.3 closed 2026-04-16 — D3 PlanChart grouped-bar chart (TECH-239 + TECH-240 + TECH-241 + TECH-242 all archived 2026-04-16). Stage 4.4 closed 2026-04-16 — multi-select dashboard filtering (TECH-247 + TECH-248 + TECH-249 + TECH-250 all archived 2026-04-16). Stage 5.1 closed 2026-04-16 — Postgres provider + auth library selection. TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived 2026-04-16 (Neon free + roll-own JWT + sessions + `web/lib/db/client.ts` lazy driver wiring + `web/README.md §Portal` contributor doc landed). Stage 5.2 opened 2026-04-16 — 4 tasks filed (drizzle schema + `db:generate` script + 4 stub auth route handlers; no migrations run, no real auth flow — architecture-only per orchestrator §Step 5). Stage 5.3 closed 2026-04-17 — Phase 0 (TECH-269), Phase 1 (TECH-265 + TECH-266), Phase 2 (TECH-267 + TECH-268) all archived; presence-only cookie check w/ `DASHBOARD_AUTH_SKIP=1` local-dev bypass, no signature verify — architecture-only per orchestrator §Step 5. Next.js 16 middleware → proxy rename absorbed during TECH-268 smoke (see `ia/projects/web-platform-master-plan.md` §Step 5 Status).

### Stage 5.2 — Auth API stubs + schema draft

### Stage 5.3 — Dashboard auth middleware migration

### Stage 6.1 — Playwright e2e harness: install + config + CI wiring

### Stage 6.2 — Baseline route coverage

### Stage 6.3 — Dashboard e2e (SSR filter flows)

### Stage 7.1 — Registry + pure shapers


### Grid asset visual registry — Step 1 Stage 1.2 (hand-written DTOs, no Drizzle)

Orchestrator: [`ia/projects/grid-asset-visual-registry-master-plan.md`](../ia/projects/grid-asset-visual-registry-master-plan.md). **SQL** in `db/migrations/` is authoritative; `web/types/api/catalog*.ts` hand DTOs per **architecture audit 2026-04-22**; depends on **TECH-612** (0011) / **TECH-615** (0012) migrations archived.


### Web platform — Stage 24 (CD bundle extraction + transcription pipeline)

- [ ] **TECH-1349** — **Users + capability migration** (asset-pipeline Stage 2.1 T2.1.1)
  - Acceptance — migration applies clean; seeds admin / author / viewer + capability rows per DEC-A33; `npm run db:migrate` exit 0.
  - Spec — [`ia/projects/TECH-1349.md`](ia/projects/TECH-1349.md)

- [ ] **TECH-1350** — **NextAuth + middleware wiring** (asset-pipeline Stage 2.1 T2.1.2)
  - Acceptance — every `/api/catalog/*` declares `requires`; forbidden envelope shape matches DEC-A48; dev-cookie fallback works locally.
  - Spec — [`ia/projects/TECH-1350.md`](ia/projects/TECH-1350.md)

- [ ] **TECH-1351** — **Audit log emitter + library** (asset-pipeline Stage 2.1 T2.1.3)
  - Acceptance — every mutating route emits one audit_log row; response envelope carries `audit_id` per DEC-A48.
  - Spec — [`ia/projects/TECH-1351.md`](ia/projects/TECH-1351.md)

- [ ] **TECH-1352** — **validate:capability-coverage validator** (asset-pipeline Stage 2.1 T2.1.4)
  - Acceptance — validator asserts every route's `requires` exists in `capability` table; wired into `validate:all`; exit clean on green tree.
  - Spec — [`ia/projects/TECH-1352.md`](ia/projects/TECH-1352.md)

## High Priority

<!-- zone-s-economy master plan — Stage 1.1 (orchestrator: `ia/projects/zone-s-economy-master-plan.md`; Bucket 3 of full-game MVP umbrella) -->

- [ ] **BUG-31** — Wrong prefabs at interstate entry/exit (border)
  - Type: fix
  - Files: `RoadPrefabResolver.cs`, `RoadManager.cs`
  - Notes: **Interstate** must be able to enter/exit at **map border** in any direction. Incorrect prefab selection at entry/exit cells. Isolated from slope prefab fixes (archive) for separate work.

- [ ] **BUG-28** — **Sorting order** between **slope** cell and **interstate** cell
  - Type: fix
  - Files: `GridManager.cs` (**Sorting order** region), `TerrainManager.cs`, `RoadManager.cs`
  - Notes: **Slope** cells and **interstate** cells render in wrong **sorting order**; one draws over the other incorrectly.

- [ ] **BUG-20** — **Utility buildings** (power plant, 3×3/2×2 multi-cell **buildings**) load incorrectly in LoadGame: end up under **grass cells** (**visual restore**)
  - Type: fix
  - Files: `GeographyManager.cs` (GetMultiCellBuildingMaxSortingOrder, ReCalculateSortingOrderBasedOnHeight), `BuildingPlacementService.cs` (LoadBuildingTile, RestoreBuildingTile), `GridManager.cs` (RestoreGridCellVisuals)
  - Notes: Load-game **grass**/**sorting** fixes landed in archive (2026-03). Re-verify in Unity whether multi-cell **utility buildings** still sort under terrain after those fixes; close if resolved.

- [ ] **TECH-01** — Extract responsibilities from large files (focus: **GridManager** decomposition next)
  - Type: refactor
  - Files: `GridManager.cs` (~2070 lines), `TerrainManager.cs` (~3500), `CityStats.cs` (~1200), `ZoneManager.cs` (~1360), `UIManager.cs` (~1240), `RoadManager.cs` (~1730)
  - Notes: Helpers already extracted (`GridPathfinder`, `GridSortingOrderService`, `ChunkCullingSystem`, `RoadCacheService`, `BuildingPlacementService`, etc.). **Next candidates from GridManager:** `BulldozeHandler` (~200 lines), `GridInputHandler` (~130 lines), `CoordinateConversionService` (~230 lines). Prioritize this workstream; see `ARCHITECTURE.md` (GridManager hub trade-off).

## Medium Priority
<!-- zone-s-economy master plan — Stage 1.1 (orchestrator: `ia/projects/zone-s-economy-master-plan.md`; Bucket 3 of full-game MVP umbrella) -->

- [ ] **BUG-49** — Manual **street** drawing: preview builds the **road stroke** cell-by-cell (animated); should show full path at once
  - Type: bug (UX / preview)
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, preview placement / ghost or temp prefab updates per frame), `GridManager.cs` if road mode input drives incremental preview; any coroutine or per-tick preview extension of the **road stroke**
  - Spec: `ia/specs/isometric-geography-system.md` §14 (manual **streets** — preview behavior)
  - Notes: **Observed:** While drawing a **street**, **preview mode** visually **extends the road stroke one cell at a time**, like an animation, instead of updating the full proposed **road stroke** in one step. **Expected:** **No** step-by-step or staggered preview animation. The game should **compute the full valid road stroke** (same rules as commit / **road validation pipeline** / `TryPrepareRoadPlacementPlan` or equivalent) for the current **stroke**, **then** instantiate or refresh **preview** prefabs for that complete **road stroke** in a single update — or batch updates without visible per-cell delay. **Related:** street commit vs terrain refresh fixes in archive — keep preview/commit paths consistent.
  - Acceptance: **Street** preview shows the full computed **road stroke** in one visual update; no visible cell-by-cell animation during drag
  - Depends on: none

- [ ] **BUG-48** — Minimap stays stale until toggling a layer (e.g. data-visualization / **desirability** / **urban centroid**)
  - Type: bug
  - Files: `MiniMapController.cs` (`RebuildTexture`, `Update`; layer toggles call `RebuildTexture` but nothing runs on **simulation tick**), `TimeManager.cs` / `SimulationManager.cs` if wiring refresh to the **simulation tick** or a shared event
  - Notes: **Observed:** The procedural minimap **does not refresh** as the city changes unless the player **toggles a minimap layer** (or other actions that call `RebuildTexture`, such as opening the panel). **Expected:** The minimap should track **zones**, **streets**, **open water**, **forests**, etc. **without** requiring layer toggles. **Implementation:** Rebuild at least **once per simulation tick** while the minimap is visible, **or** a **performance-balanced** approach (throttled full rebuild, dirty rect / incremental update, or event-driven refresh when grid/**zone**/**street**/**water body** data changes) — profile full `RebuildTexture` cost first (see project spec; measurement tooling **task 8** in `docs/agent-tooling-verification-priority-tasks.md`). Class summary in code states rebuilds on **geography initialization** completion, grid restore, panel open, and layer changes **not** on a fixed timer — that gap is this bug. **Related:** water layer alignment shipped in archive; **FEAT-42** (optional **HeightMap** layer).
  - Depends on: none

- [ ] **FEAT-36** — Expand **AUTO** zoning and **AUTO** road candidates to include **forests** and cells meeting **land slope eligibility**
  - Type: feature
  - Files: `GridManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`
  - Notes: Treat **grass cells**, **forest (coverage)** cells, and cardinal-ramp **slopes** (per **land slope eligibility**) as valid candidates for **AUTO** zoning and **AUTO** road expansion. Capture any design notes in this issue or in `ia/specs/isometric-geography-system.md` if rules become stable.

- [ ] **FEAT-35** — Area demolition tool (bulldozer drag-to-select)
  - Type: feature
  - Files: `GridManager.cs`, `UIManager.cs`, `CursorManager.cs`
  - Notes: Manual tool to demolish all **buildings** and **zoning** in a rectangular area at once. Use the same area selection mechanism as **zoning**: hold mouse button, drag to define rectangle, release to demolish. Reuse **zoning**'s start/end position logic (zoningStartGridPosition, zoningEndGridPosition pattern). Demolish each **cell** in the selected area via DemolishCellAt. **Interstate** cells must remain non-demolishable. Consider preview overlay (e.g. red tint) during drag.

- [ ] **FEAT-03** — **Forest (coverage)** mode hold-to-place
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`
  - Notes: Currently requires click per **cell**. Allow continuous drag.

- [ ] **FEAT-04** — Random **forest (coverage)** spray tool
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`, `CursorManager.cs`
  - Notes: Place **forest (coverage)** in area with random spray/brush distribution.

- [ ] **FEAT-06** — **Forest (coverage)** that grows over **simulation ticks**: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: **Forest (coverage)** maturation system over **simulation ticks**.

- [ ] **TECH-251** — Adopt Claude Opus 4.7 across agent lifecycle
  - Type: tech (agent tooling / IA)
  - Files: `.claude/agents/*.md` (11 subagents), `.claude/commands/*.md`, `.claude/output-styles/{verification-report,closeout-digest}.md`, `ia/skills/{master-plan-new,stage-decompose,project-spec-implement,project-spec-kickoff,verify-loop,close-dev-loop,ide-bridge-evidence}/SKILL.md`, `CLAUDE.md` §1 + §3, `docs/agent-led-verification-policy.md`, `ia/rules/agent-lifecycle.md`
  - Notes: Opus 4.7 released 2026-04-16. Pricing flat ($5/$25 per 1M). Gains: +13% coding bench, 3× Rakuten-SWE-Bench prod task resolution, +10% review recall, -21% OfficeQA Pro errors, vision up to 2576px, loop resistance + tool-failure recovery + output self-verification. New `xhigh` effort level + `/ultrareview` slash cmd. Stricter literal instruction following — prompts may need retune. Tokenizer produces 1.0–1.35× more tokens. Model id `claude-opus-4-7`. **Scope:** smoke-test gate on one low-blast flow; doc drift fixes (`CLAUDE.md` §3 says verify-loop = Sonnet but frontmatter = Opus; "10 native subagents" vs 11 actual); wire `/ultrareview` into `/verify-loop` terminal step; adopt `xhigh` for `closeout` + `master-plan-new`; prompt retune pass on skill bodies with soft instructions; `spec-implementer` Opus 4.7 opt-in behind effort flag (default Sonnet); vision evidence extension in `ide-bridge-evidence`; 2-week cost monitoring window. **Out of scope:** global bulk frontmatter bump (alias `model: opus` auto-upgrades); 4.7 file-system memory API wiring; task budgets GA. Alias resolution at dispatch makes blanket version pin unnecessary — only pin versioned `claude-opus-4-7` where early lock-in required.
  - Acceptance: Smoke-test gate passes on pilot flow w/ caveman preamble + cardinality hints intact; `CLAUDE.md` §3 model column matches `.claude/agents/*.md` frontmatter + subagent count (11 not 10); `/ultrareview` terminal step wired into `/verify-loop`; `xhigh` effort applied to `closeout` + `master-plan-new`; skill body retune pass landed for `master-plan-new` + `stage-decompose` + `project-spec-implement`; `spec-implementer` 4.7 opt-in flag documented + default Sonnet preserved; `ide-bridge-evidence` SKILL captures 2576px evidence; 2-week cost log surfaces per-agent token deltas
  - Depends on: none

## Code Health (technical debt)

- [ ] **TECH-13** — Remove obsolete **urbanization proposal** system (dead code, UI, models)
  - Type: refactor (cleanup)
  - Files: `UrbanizationProposalManager.cs`, `ProposalUIController.cs`, `UrbanizationProposal.cs` (and related), `SimulationManager.cs`, `UIManager.cs`, scene references, **save data** if any
  - Notes: The **urbanization proposal** feature is **obsolete** and intentionally **disabled**; the game is stable without it. **Keep** `UrbanizationProposalManager` disconnected from the simulation — do **not** re-enable proposals. **Keep** `UrbanCentroidService` / **urban growth rings** for **AUTO** roads and zoning. This issue tracks **full removal** of proposal-specific code and UI after a safe audit (no **save data** breakage). Supersedes older proposal bugs — see archive.

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access to the **cell** grid. Several classes violate this. Risk of subtle bugs when grid or **HeightMap** changes.

- [ ] **TECH-02** — Change public fields to `[SerializeField] private` in managers
  - Type: refactor
  - Files: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`
  - Notes: Dependencies and prefabs exposed as `public` allow accidental access from any class. Use `[SerializeField] private` to encapsulate.

- [ ] **TECH-03** — Extract magic numbers to constants or ScriptableObjects
  - Type: refactor
  - Files: multiple (GridManager, CityStats, RoadManager, UIManager, TimeManager, TerrainManager, WaterManager, EconomyManager, ForestManager, InterstateManager, etc.)
  - Notes: **Building** costs, economic balance, **height generation** parameters, **sorting order** offsets (**type offsets**, **DEPTH_MULTIPLIER**, **HEIGHT_MULTIPLIER**), **pathfinding cost model** weights, initial dates, probabilities — all hardcoded. Extract to named constants or configuration ScriptableObject for easier tuning.

- [ ] **TECH-05** — Extract duplicated dependency resolution pattern
  - Type: refactor
  - Files: ~25+ managers with `if (X == null) X = FindObjectOfType<X>()` block
  - Notes: Consider helper method, base class, or extension method to reduce duplication of Inspector + FindObjectOfType fallback pattern.

*(Umbrella programs (**spec-pipeline**, **JSON**/**Postgres** interchange, **compute-lib**, **Cursor Skills**) and **Editor export registry** are archived under [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) with **glossary** pointers. **§ IA evolution lane** holds **TECH-77**–**TECH-83** + **TECH-552** (FTS, skill chaining, agent memory, bidirectional IA, knowledge graph, gameplay entity model, sim parameter tuning, Unity Agent Bridge). **§ Economic depth lane** holds **monthly maintenance** (shipped — **glossary**) → **tax→demand feedback** (shipped — **managers-reference** **Demand**) → **FEAT-52** → **FEAT-53** → **FEAT-09** (happiness + pollution shipped). **§ Gameplay & simulation lane** lists **BUG-52**, **FEAT-43**, **FEAT-08**. **§ Compute-lib program** above holds **TECH-38** + **TECH-32**/**TECH-35**; **TECH-39** **MCP** suite is archived.)*

## Low Priority

- [ ] **FEAT-18** — **Height generation** (improved terrain generator)
  - Type: feature
  - Files: `TerrainManager.cs`, `GeographyManager.cs`, `HeightMap.cs`
  - Notes: Improved **height generation** with more control and variety over the **HeightMap**.

- [ ] **FEAT-10** — **Regional map** contribution: monthly bonus for belonging to the region
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `RegionalMapManager.cs`
  - Notes: Additional monthly income for belonging to the **regional map** network.

- [ ] **FEAT-19** — Map rotation / prefabs
  - Type: feature
  - Files: `CameraController.cs`, `GridManager.cs`, all rendering managers
  - Notes: Isometric view rotation. High impact on **sorting order** (**sorting formula**, **cliff face visibility**) and rendering.

- [ ] **TECH-14** — Remove residual placeholder / test scripts
  - Type: refactor (cleanup)
  - Files: `CityManager.cs` (namespace-only stub), `TestScript.cs` (compile smoke test)
  - Notes: Delete or replace with real content only if nothing references them; verify no scene/Inspector references.

- [ ] **FEAT-11** — Education level / Schools
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`, `DemandManager.cs`
  - Notes: Education system affecting **demand (R / C / I)** and growth.

- [ ] **FEAT-12** — Security / Order / Police
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`
  - Notes: Public security system.

- [ ] **FEAT-13** — Fire / Fire risk / Firefighters
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`
  - Notes: Fire risk and firefighter service system.

- [ ] **FEAT-14** — Vehicle traffic system / traffic animations
  - Type: feature (new system)
  - Files: new manager + `RoadManager.cs`, `GridManager.cs`
  - Notes: Vehicles circulating on **streets** and **interstate**.

- [ ] **FEAT-15** — Port system / cargo ship animations
  - Type: feature (new system)
  - Files: new manager + `WaterManager.cs`
  - Notes: Requires **water body** system with defined **sea** (**water body kind**). Depends on lake generation / water map foundations (archive).

- [ ] **FEAT-16** — Train system / train animations
  - Type: feature (new system)
  - Files: new manager + `GridManager.cs`
  - Notes: Railway network and animations.

- [ ] **FEAT-39** — Sea / **shore band**: **map border** region, infinite reservoir, tide direction (data)
  - Type: feature
  - Files: `WaterManager.cs`, `WaterMap.cs`, `TerrainManager.cs`, `GeographyManager.cs`
  - Notes: Define **sea** as a **water body kind** at the **map border** with **surface height (S)** and **shore band** rules. Coordinate with **FEAT-15** (ports). **Water map** persist work — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-40** — Water sources & drainage (snowmelt, rain, overflow) — simulation
  - Type: feature
  - Files: new helpers + `WaterMap.cs`, `WaterManager.cs`, `SimulationManager.cs`
  - Notes: Not full fluid simulation; data-driven flow affecting **water bodies**, **surface height (S)**, and **depression-fill** dynamics. Prior **water map** / procedural **rivers** work — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-41** — **Water body** terrain tools (manual paint/modify, **AUTO** terraform) — extended
  - Type: feature
  - Files: `GridManager.cs`, `WaterManager.cs`, `UIManager.cs`, `TerraformingService.cs` (as needed)
  - Notes: Beyond legacy paint-at-**sea level**. Tools to create/modify **water bodies** with proper **surface height (S)**, **shore band**, and **water map** registration. **Water map** persist shipped — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-42** — Minimap: optional **HeightMap** / relief shading layer
  - Type: feature (UI)
  - Files: `MiniMapController.cs`, `HeightMap` / `GridManager` read access as needed
  - Notes: Visualize terrain elevation (**HeightMap**) on the minimap (distinct from **zones**/**streets**/**open water** layers). Does not replace logical **water map** / **zone** data; base layer reliability follows prior minimap / water layer work (archive).
  - Depends on: none

- [ ] **FEAT-46** — **Geography** authoring: **territory** / **urban** area **map** editor + parameter dashboard
  - Type: feature (tools / **New Game** flow)
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `ForestManager.cs`, `UIManager.cs` (or dedicated **Editor** / in-game **wizard**); **JSON** / **ScriptableObject** templates (align `ARCHITECTURE.md` §Interchange JSON)
  - Notes: In-game or **Editor** flow to author **city** / **territory** **maps** with **isometric** terrain controls: **map** size, **water** / **forest** / **height** mix, **sea** / **river** / **lake** proportions, etc. Reuse the same parameter pipeline for future **player** **terraform**, **basin** / **elevation** tools, **water body** placement in **depressions**, and **AUTO** **geography**-driven tools. **Spec:** canonical **geography initialization** + **water-terrain** + **geo** when implemented (no `ia/projects/` spec until scheduled).
  - Depends on: none (coordinates **FEAT-18**, **FEAT-41**; soft: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) TECH-21/TECH-36 docs)

- [ ] **FEAT-47** — **Multipolar** **urban centroid** model, per-pole **urban growth rings**, **connurbation**
  - Type: feature (**simulation** / **AUTO** architecture)
  - Files: `UrbanCentroidService.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs`, `GrowthBudgetManager.cs` (as applicable)
  - Notes: Evolve **sim** §Rings from a single **urban centroid** to **multiple** **centroids** (**desirability** / employment **poles**), each with **ring** fields; preserve coherent **AUTO** **street** / **zoning** patterns across the **map**; long-term **connurbation** between distinct urban masses. **Desirability** **scoring** may use **grid** decay; **committed** **streets** remain **road preparation family** + **geo** §10. Coordinates **FEAT-43** (gradient tuning). **Spec:** **simulation-system** §Rings + **managers-reference** when implemented (no project spec until scheduled).
  - Depends on: none (soft: **FEAT-43**; **UrbanGrowthRingMath** via **TECH-38**)

- [ ] **FEAT-48** — **Water body** volume budget: **basin** expand → **surface height (S)** adjusts; **Moore**-adjacent **dig** **fill**
  - Type: feature (**water** / **terraform**)
  - Files: `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `TerraformingService.cs`, water prefabs / **sorting order** (per **geo** §7, **water-terrain**)
  - Notes: **Not** full 3D **fluid** simulation. **Gameplay:** excavating a **cell** **Moore**-adjacent to **open water** fills the **depression**; **basin** volume conservation lowers or raises **surface height (S)**; **render** water prefabs at new **S** (may expose or cover **terrain** / **islands**). Optional **isometric** directional **fill** **animation**; **S** step changes not animated. Expands across **terraform** / **water** interactions per product plan. Coordinates **FEAT-40**, **FEAT-41**, **FEAT-39**. **Spec:** **isometric-geography-system** / **water-terrain** amendments when implemented (no project spec until scheduled).
  - Depends on: none (soft: **FEAT-41**, **glossary** **C# compute utilities (TECH-38)** for **pure** **volume** helpers; **water map** persist — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **ART-01** — Missing prefabs: **forest (coverage)** on SE, NE, SW, NW **slope types**
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ForestManager.cs`

- [ ] **ART-02** — Missing prefabs: residential **buildings** (2 heavy 1×1/2×2, light 2×2, medium 1×1 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-03** — Missing prefabs: commercial **buildings** (2 heavy 2×2/1×1, light 2×2, medium 2×2 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-04** — Missing prefabs: industrial **buildings** (2 heavy 2×2/1×1, light 1×1, 2 medium 1×1/2×2 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

*(Program history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Open lanes: **§ Compute-lib program**, **§ Agent ↔ Unity & MCP context lane**, **§ IA evolution lane**, **§ Economic depth lane**, **§ Gameplay & simulation lane**, **§ Multi-scale simulation lane**, **§ Blip audio program**, **§ Sprite gen lane**, **§ Web platform lane**, then standard priority sections.)*

- [ ] **AUDIO-01** — Audio FX: demolition, placement, **zoning**, **forest (coverage)**, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and **height** (**HeightMap**) over the map.

---

## How to Use This Backlog

1. **Work on issue:** Open chat in Cursor, reference `@BACKLOG.md`, request analysis / implementation by ID (e.g. "Analyze BUG-01, propose plan").
2. **Reprioritize:** Move row up/down within section, or change section.
3. **Add new issue:** Next available ID per category, place in correct priority section.
4. **Complete issue:** Remove row from **BACKLOG.md**; append **`[x]`** row w/ date to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) only (**no** "Completed" section in **BACKLOG.md**). After closure, **strip citations** to that issue id from durable docs (glossary, reference specs, rules, skills, `docs/`, code comments) per **project-spec-close** — **BACKLOG.md** (open rows), **BACKLOG-ARCHIVE.md**, new archived row may still name id.
5. **In progress:** Move to "In progress" section when starting.
6. **Dependencies:** `Depends on: ID` when open issue waits on another. **Convention:** every ID in `Depends on:` must appear **above** the dependent in this file (earlier in same section / higher-priority section), **or** be **completed** in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) — then write `Depends on: none` + cite archived id in **Notes**. Check deps before starting.

### ID Convention
| Prefix | Category |
|--------|----------|
| `BUG-XX` | Bugs / broken functionality |
| `FEAT-XX` | Features / enhancements |
| `TECH-XX` | Technical debt, refactors, code health |
| `ART-XX` | Art assets, prefabs, sprites |
| `AUDIO-XX` | Audio assets / audio system |

### Issue Fields
- **Type:** fix, feature, refactor, art/assets, audio/feature, etc.
- **Files:** main files involved
- **Notes:** context, problem description, expected solution
- **Acceptance** (optional): concrete pass/fail criteria
- **Depends on** (optional): IDs that must complete first

### Section Order
1. **Compute-lib program** (**TECH-38** open; **TECH-39** archived; pilot **compute-lib** archived; related **TECH-32**, **TECH-35**; charter — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))
2. **Agent ↔ Unity & MCP context lane** (Unity exports, MCP, CI, perf harnesses, adjacent tooling)
3. In progress (active — insert above **High priority** when used)
4. High priority (critical bugs, core gameplay blockers)
5. Medium priority (important features, balance, improvements)
6. **Multi-scale simulation lane** (orchestrator [`ia/projects/multi-scale-master-plan.md`](projects/multi-scale-master-plan.md); file rows only when parent stage → `In Progress`)
7. **Blip audio program** (orchestrator [`ia/projects/blip-master-plan.md`](projects/blip-master-plan.md); file rows only when parent stage → `In Progress`)
8. **Sprite gen lane** (orchestrator [`ia/projects/sprite-gen-master-plan.md`](projects/sprite-gen-master-plan.md); file rows only when parent stage → `In Progress`)
9. **Web platform lane** (orchestrator [`ia/projects/web-platform-master-plan.md`](projects/web-platform-master-plan.md); file rows only when parent stage → `In Progress`)
9. Code Health (technical debt, refactors, performance)
9. Low priority (new systems, polish, content)
8. **Archive** — completed work lives only in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)
