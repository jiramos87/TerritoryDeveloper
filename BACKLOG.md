# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first): **§ Compute-lib program**, then **§ Agent ↔ Unity & MCP context lane**, then **High** / **Medium** / **Code Health** / **Low**.
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.
>
> **Priority:** **§ Spec pipeline & verification program** (**TECH-60**–**TECH-63**) is **§ Completed** — **glossary** **territory-ia spec-pipeline program (TECH-60)**; exploration [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md). **§ Compute-lib program** (**TECH-36**; **TECH-37** **§ Completed**; **TECH-38** → **TECH-39**) covers **pure** math, **golden** **JSON**, and **UTF**-ready surfaces that **prerequisite** rows (**TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30**, **TECH-38**) still advance. **Gameplay** blockers in **§ High Priority** remain **interrupt** work when they **stop play** or **corrupt saves**.

---

## Compute-lib program (first priority)

**Dependency order:** **TECH-37** **§ Completed** — **TECH-38** and **TECH-39** follow. **TECH-38** supplies **batchmode** / **golden** **JSON** for **TECH-39** “heavy” tools; **TECH-39** may ship tool shells with honest **NOT_AVAILABLE** until **TECH-38** lands. **TECH-36** is the umbrella — progress is tracked on **TECH-38**–**TECH-39**. **Related research** (**TECH-32**, **TECH-35**) is listed here after the phased rows; both remain **`Depends on: none`** but should **follow** **TECH-38** in practice when comparing to extracted **pure** modules or **RNG**/**invariant** surfaces.

- [ ] **TECH-36** — **Computational program** (umbrella): **geometry**, **stochastics**, **algorithms** + **territory-ia** tools
  - Type: tooling / code health / agent enablement
  - Files: umbrella only — **TECH-37** **§ Completed** (**glossary** **territory-compute-lib (TECH-37)**); see **TECH-38**, **TECH-39**; charter `.cursor/projects/TECH-36.md`; reference specs: `.cursor/specs/isometric-geography-system.md`, `.cursor/specs/simulation-system.md`, `.cursor/specs/managers-reference.md`
  - Spec: `.cursor/projects/TECH-36.md`
  - Notes: **Program charter** with resolved product/tooling decisions. **Phased delivery:** **TECH-37** **§ Completed** (**glossary** **territory-compute-lib (TECH-37)** — **`tools/compute-lib/`** + pilot **`isometric_world_to_grid`**), **TECH-38** (Unity **pure** **compute** + **`tools/`** harnesses), **TECH-39** (computational **MCP** suite). **Coordination:** **TECH-60** **§ Completed** (**glossary** **territory-ia spec-pipeline program (TECH-60)**) listed **TECH-37**/**TECH-38** as **prerequisites** for **test contracts** / **golden** **JSON** / **invariant** checks — [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-60**. **Related:** **JSON program (TECH-21)** **§ Completed** (**TECH-40** / **TECH-41** / **TECH-44a** — JSON DTOs/schemas; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); **glossary**), **TECH-28** (completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)), **TECH-32**, **TECH-35**; product follow-ups **FEAT-46** (geography authoring UI), **FEAT-47** (**multipolar** **urban growth rings**), **FEAT-48** (**water body** volume / **surface height (S)**).
  - Acceptance: **TECH-37** **§ Completed**; **TECH-38** and **TECH-39** each satisfy their own **Acceptance** lines in this file (program **complete** when **TECH-38** and **TECH-39** **complete**)
  - Depends on: none (child issues **TECH-38** → **TECH-39** track remaining implementation order)

- [ ] **TECH-38** — **Core** **computational** modules (Unity **utilities** + **`tools/`** harnesses)
  - Type: code health / performance enablement
  - Files: `Assets/Scripts/Utilities/Compute/`; `GridManager.cs` (**CoordinateConversionService**), `GridPathfinder.cs`, `UrbanCentroidService.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterManager.cs`, `DemandManager.cs` / `CityStats.cs` (as extractions land); `tools/reports/`; **UTF** tests
  - Spec: `.cursor/projects/TECH-38.md`
  - Notes: **Phase B** of **TECH-36**. **Behavior-preserving** extractions; **UrbanGrowthRingMath** **multipolar**-ready for **FEAT-47**; **stochastic** **geography initialization** documentation; **no** second **pathfinding** authority. Prepare **batchmode** hooks for **TECH-39**.
  - Acceptance: inventory doc + **≥ 3** **pure** modules with tests or **golden** **JSON**; **RNG** derivation doc; **invariants** respected — see `.cursor/projects/TECH-38.md` §8
  - Depends on: **TECH-37** **§ Completed**

- [ ] **TECH-39** — **territory-ia** **computational** **MCP** tool suite
  - Type: tooling / agent enablement
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/src/` (compute handlers); `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Spec: `.cursor/projects/TECH-39.md`
  - Notes: **Phase C** of **TECH-36**. **`growth_ring_classify`**, **`grid_distance`**, **`pathfinding_cost_preview`**, **`geography_init_params_validate`**, **`desirability_top_cells`** (honest **NOT_AVAILABLE** until **TECH-38** **batchmode**); **many** **`snake_case`** tools, shared **compute-lib** core.
  - Acceptance: **≥ 4** new tools beyond **TECH-37** pilot (or **Decision Log** consolidation); **`npm run verify`** green; docs updated — see `.cursor/projects/TECH-39.md` §8
  - Depends on: **TECH-37** **§ Completed** (soft: **TECH-38** for **heavy** tools)

- [ ] **TECH-32** — **Urban growth rings** / centroid recompute what-if (research tooling)
  - Type: tooling / research
  - Files: `tools/` or Unity Editor batch; parameters from **FEAT-43** / **FEAT-36** notes as inputs
  - Spec: `.cursor/projects/TECH-32.md`
  - Notes: Compare full **UrbanCentroidService** recompute every tick vs throttled/approximate strategies; report desync or behavior risk vs glossary **sim §Rings**. Non-player-facing evidence for tuning. `docs/agent-tooling-verification-priority-tasks.md` task 24. **Order:** Prefer running against **TECH-38** **UrbanGrowthRingMath** / harness **JSON** once **Phase B** exists; until then, baseline against current **MonoBehaviour** code.
  - Depends on: none (coordinates with **FEAT-43**; soft: **TECH-38** for **pure** module parity)

- [ ] **TECH-35** — Research spike: property-based / random mutation **invariant** fuzzing (optional)
  - Type: research / test harness
  - Files: TBD test assembly or `tools/` prototype
  - Spec: `.cursor/projects/TECH-35.md`
  - Notes: High setup cost; only if geometric / ordering bugs justify. Predicates from **invariants** (HeightMap/**cell** sync, **road cache**, **shore band**, etc.). `docs/agent-tooling-verification-priority-tasks.md` task 38. **Non-goals:** production fuzz in player builds. **Order:** Easiest once **TECH-38** exposes stable **pure** surfaces + documented **RNG** derivation.
  - Depends on: none (soft: **TECH-38**)

## Agent ↔ Unity & MCP context lane

Ordered for **MCP Unity context** → **JSON / reports from Unity** → **MCP platform** → **agent workflow & CI helpers** → **research tooling**. (**TECH-36** program rows live in **§ Compute-lib program** above.) **Prerequisites** mapped by **TECH-60** **§ Completed** (**glossary** **territory-ia spec-pipeline program (TECH-60)**): **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30** (this lane — existing `.cursor/projects/*.md`); **TECH-37** **§ Completed**, **TECH-38** (**§ Compute-lib program**).

- [ ] **TECH-59** — **territory-ia** MCP: stage **Editor** export registry payload (**BACKLOG** issue id + JSON documents)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new **`registerTool`** handler + validation); `tools/mcp-ia-server/tests/`; `tools/mcp-ia-server/README.md`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `Assets/Scripts/Editor/` (menu: read staged file → apply **EditorPrefs** / optional one-shot register); **`.gitignore`** (staging path); optional JSON schema under `docs/schemas/` or fixture under `tools/mcp-ia-server/`; [`.cursor/skills/README.md`](.cursor/skills/README.md) pointer if agent recipe updates
  - Spec: `.cursor/projects/TECH-59.md`
  - Notes: **Problem:** Agents cannot set **Unity** **EditorPrefs**; developers should not re-type **`backlog_issue_id`** and JSON shapes by hand every time. **Direction:** MCP tool accepts **`issue_id`** + one or more **JSON** objects (typed by **export kind** or free-form envelope per **Decision Log**), writes a **gitignored** **staging file** under the repo; Unity menu **Apply MCP-staged registry…** loads file, validates, sets **EditorPrefs** (**`TerritoryDeveloper.EditorExportRegistry.BacklogIssueId`**), and optionally triggers the existing **Node** / **Postgres** path so the dev **clicks once**. **Non-goals:** MCP opens **TCP** to **Postgres**; no secrets in MCP arguments (connection string stays **EditorPrefs** / env). **Overlap:** **TECH-48** (MCP discovery — different scope); **TECH-55b** **§ Completed** (**Editor export registry** — staging may align when implemented).
  - Acceptance: per `.cursor/projects/TECH-59.md` §8; **`npm run verify`** / **`npm run test:ia`** green when MCP code ships
  - Depends on: none (soft: **TECH-55** / **TECH-55b** **§ Completed** for end-to-end UX; **TECH-24** for parser/test policy)
  - Related: **TECH-55** / **TECH-55b** **§ Completed** (glossary **Editor export registry**), **TECH-48**, **TECH-18** (long-term DB-backed IA)

- [ ] **TECH-53** — **Schema validation history** (former **TECH-44** **E2**)
  - Type: technical / CI / data
  - Files: `.github/workflows/` (e.g. extend **ia-tools**), `docs/schemas/`, `docs/schemas/fixtures/`; optional **Postgres** table via **TECH-44b** migrations
  - Spec: none (backlog-only — no `.cursor/projects/` spec)
  - Notes: Persist per-CI-run outcomes of **`npm run validate:fixtures`** / **JSON Schema** checks so regressions on **Interchange JSON** and fixtures are visible over time. Align row shape with [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **B1** if stored in **Postgres**. Program pointer: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**.
  - Acceptance: agreed storage (artifact file, DB rows, or workflow summary) + documented query or review path; English **Notes** updated when implementation choice is fixed
  - Depends on: **TECH-44b** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [ ] **TECH-54** — **Agent patch proposal staging** (former **TECH-44** **E3**)
  - Type: tooling / agent workflow
  - Files: optional **Postgres** migrations; `tools/` or thin HTTP handler; `docs/`
  - Spec: none (backlog-only — no `.cursor/projects/` spec)
  - Notes: Queue **B3**-style idempotent patch envelopes ([`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md)) with explicit lifecycle (**pending** / **approved** / **rejected**) before humans merge changes to git; **`natural_key`** for deduplication. **Not** player **Save data**. Program pointer: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**.
  - Acceptance: documented state machine + at least one insert/list path (script, SQL, or API); conflict policy recorded in issue **Notes** or [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) / [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) when implementing
  - Depends on: **TECH-44b** **§ Completed** (soft: **TECH-44a** **§ Completed**)

- [ ] **TECH-43** — Append-only **JSON** line **event log** (telemetry / sim anomalies) — **backlog placeholder**
  - Type: technical / observability (future)
  - Files: TBD (`tools/`, optional **Postgres** table, ship pipeline)
  - Spec: none (promote to `.cursor/projects/TECH-43.md` when scheduled)
  - Notes: Idea from **TECH-21** brainstorm **B2**; **schema_version** per line; same validator family as **TECH-40** (completed — **§ Completed**). **Schema** pipeline exists under `docs/schemas/` + **`npm run validate:fixtures`**.
  - Acceptance: issue refined with concrete consumer + storage choice; optional schema + sample sink
  - Depends on: none (soft: **TECH-40** completed — **§ Completed**)

- [ ] **TECH-18** — Migrate Information Architecture from Markdown to PostgreSQL (MCP evolution)
  - Type: infrastructure / tooling
  - Files: All `.cursor/specs/*.md`, `.cursor/rules/agent-router.mdc`, `.cursor/rules/invariants.mdc`, `ARCHITECTURE.md`; MCP server from **TECH-17** (initially **file-backed**); schema / migrations / seed from **TECH-44b**; `tools/mcp-ia-server/src/index.ts`, `docs/mcp-ia-server.md`
  - Spec: `.cursor/projects/TECH-18.md`
  - Notes: **Goal:** After **TECH-17** (MCP over **`.md` / `.mdc`**) and **TECH-44b** (Postgres + IA tables), **migrate authoritative IA content** into PostgreSQL and evolve the **same MCP** so **primary** retrieval is DB-backed. Markdown becomes **generated or secondary** for human reading. **Explicit dependency:** This work **extends the MCP built first on Markdown** in **TECH-17** — same tool contracts where possible, swapping implementation to query **TECH-44b**’s database. **Scope:** (1) Parse and ingest spec sections (`isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, etc.) into `spec_sections`. (2) Populate `relationships` (e.g. HeightMap↔Cell.height, PathTerraformPlan→Phase-1→Apply). (3) Populate `invariants` from `invariants.mdc`. (4) Extend tools: `what_do_i_need_to_know(task_description)`, `search_specs(query)`, `dependency_chain(term)`. (5) Script to regenerate `.md` from DB for review. (6) Update `agent-router.mdc` — MCP tools first, Markdown fallback second. **Acceptance:** Agent resolves a multi-spec task (e.g. “bridge over multi-level lake”) via MCP reading ≤ ~500 tokens of context instead of many full-file reads. **Phased MCP tools** (bundles, `backlog_search`, **`unity_context_section` after TECH-20** doc, etc.): see `.cursor/projects/TECH-18.md` and `docs/agent-tooling-verification-priority-tasks.md` (tasks 12–20, 28–32, 35). **Deferred unless reopened:** `findobjectoftype_scan`, `find_symbol` MCP tools (prefer **TECH-26** script).
  - Depends on: **TECH-44b** **§ Completed** (**TECH-17** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **TECH-15** — New Game / **geography initialization** performance
  - Type: performance / optimization
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, `InterstateManager.cs`, `ForestManager.cs`, `RegionalMapManager.cs`, `ProceduralRiverGenerator.cs` (as applicable)
  - Spec: `.cursor/projects/TECH-15.md`
  - Notes: Reduce wall-clock time and frame spikes when starting a **New Game** (**geography initialization**): **HeightMap**, lakes, procedural **rivers** (**FEAT-38** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)), **interstate**, **forests**, **map border** signs, **sorting order** passes, etc. **Priority:** Land the **Editor/batch JSON profiler** under `tools/reports/` (see spec) *before* or in parallel with deep optimization — agents need **measurable** phase breakdowns. **Related:** **FEAT-37c** (**Load Game** / **water map** persist — completed, archive) — this issue targets **geography initialization** cost only. **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 3, 22).

- [ ] **TECH-16** — **Simulation tick** performance v2 (per-tick **AUTO systems** pipeline)
  - Type: performance / optimization
  - Files: `SimulationManager.cs`, `TimeManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`, `DemandManager.cs`, `CityStats.cs` (as applicable)
  - Spec: `.cursor/projects/TECH-16.md`
  - Notes: Second-pass optimization of the **simulation tick** after early **Simulation optimization** work (completed). **Priority:** Ship **spec-labeled tick harness** JSON + **ProfilerMarker** names (see spec) so agents and CI can read **AUTO** pipeline cost *before* micro-optimizing allocations. **Related:** **BUG-14** (per-frame UI `FindObjectOfType`); **TECH-01** (manager decomposition may help profiling and hotspots). **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 4, 25); drift detection **TECH-29**.

- [ ] **TECH-33** — Asset introspection: **prefab** manifest + scene **MonoBehaviour** listing
  - Type: tooling
  - Files: `tools/` (Unity `-batchmode` or Editor script), `Assets/Prefabs/`, agreed scene path (e.g. `MainScene.unity`)
  - Spec: `.cursor/projects/TECH-33.md`
  - Notes: List prefabs with missing script references; list MonoBehaviour types/paths in scene for **BUG-19** / **TECH-07**. `docs/agent-tooling-verification-priority-tasks.md` tasks 26, 27.
  - Depends on: none

- [ ] **TECH-23** — Agent workflow: MCP **invariant preflight** for issue kickoff
  - Type: documentation / process
  - Files: `AGENTS.md`, optional `.cursor/templates/` or **How to Use This Backlog** section in this file, `docs/mcp-ia-server.md` (short pointer)
  - Notes: Document that implementation chats for **BUG-**/**FEAT-**/**TECH-** work should record **territory-ia** **`invariants_summary`**, **`router_for_task`**, and at least one **`spec_section`** (or equivalent slice) before substantive code edits—reduces **road preparation family**, **HeightMap**/**cell** sync, and per-frame **`FindObjectOfType`** mistakes. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-45** — **Cursor Skill:** **road** modification guardrails (**road stroke**, **road preparation family**, cache)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `.cursor/skills/` (TBD subfolder + `SKILL.md`); optional one-line pointer in `AGENTS.md`
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: **road placement** only through **road preparation family** ending in **`PathTerraformPlan`** + Phase-1 + **`Apply`** — never **`ComputePathPlan`** alone; call **`InvalidateRoadCache()`** after **road** changes; pull normative detail via **territory-ia** (`router_for_task` → **roads** / **geo**) — do not duplicate **`roads-system`** in the skill body. **Pattern:** [.cursor/skills/README.md](.cursor/skills/README.md) (thin skill + **Tool recipe** + MCP pointers).
  - Acceptance: **Skill** file committed; **`description`** names **road stroke**, **wet run**, **interstate**/**bridge** touchpoints where relevant
  - Depends on: none (soft: [.cursor/skills/README.md](.cursor/skills/README.md) conventions)

- [ ] **TECH-46** — **Cursor Skill:** **terrain** / **HeightMap** / **water** / **shore** edit guardrails
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `.cursor/skills/` (TBD subfolder + `SKILL.md`); optional pointer in `AGENTS.md`
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: keep **`HeightMap[x,y]`** and **`Cell.height`** in sync; **water** placement/removal → **`RefreshShoreTerrainAfterWaterUpdate`**; **shore band** and **river** monotonicity per **invariants**; use **`spec_section`** / **`router_for_task`** for **water-terrain** and **geo** slices — no spec paste. **Pattern:** [.cursor/skills/README.md](.cursor/skills/README.md).
  - Acceptance: **Skill** file committed; **`description`** triggers on **terraform**, **water map**, **cliff**, **shore** edits
  - Depends on: none (soft: [.cursor/skills/README.md](.cursor/skills/README.md))

- [ ] **TECH-47** — **Cursor Skill:** new **`MonoBehaviour`** **manager** wiring pattern
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `.cursor/skills/` (TBD subfolder + `SKILL.md`); optional pointer in `AGENTS.md` or `.cursor/specs/unity-development-context.md` **Decision Log**
  - Notes: **Deliverable type:** **Cursor Skill**. Checklist: scene **component**, never `new`; **`[SerializeField] private`** refs + **`FindObjectOfType`** fallback in **`Awake`**; **no new singletons**; do not add responsibilities to **`GridManager`** — extract helpers; align with **`.cursor/specs/unity-development-context.md`** via MCP slice when needed. **Pattern:** [.cursor/skills/README.md](.cursor/skills/README.md).
  - Acceptance: **Skill** file committed; **`description`** states “new manager / **MonoBehaviour** service” triggers
  - Depends on: none (soft: [.cursor/skills/README.md](.cursor/skills/README.md))

- [ ] **TECH-48** — **territory-ia** MCP: discovery from **project specs** (terms, domains, spec slices)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new or extended handlers, parsers); `tools/mcp-ia-server/README.md`; `docs/mcp-ia-server.md`; optional fixtures under `tools/mcp-ia-server/`; align notes with `.cursor/projects/TECH-18.md` when **search**/**bundle** tools land
  - Spec: none (promote to `.cursor/projects/TECH-48.md` when design stabilizes)
  - Notes: **Goal:** Make **project-spec-kickoff** and similar workflows cheaper and safer by improving how MCP turns **implementation**-oriented text (project **spec** body, backlog **Files**) into **glossary** matches and **`spec_section`** targets. **Candidate directions:** (1) Path-based tool: input `.cursor/projects/{ISSUE}.md` → ranked **glossary** candidates + suggested **`router_for_task`** **domain** strings + ordered **`spec_section`** queue with **max_chars** budget. (2) Improve **`glossary_discover`** ranking using tokens extracted from **`backlog_issue`** **Files**/**Notes** when `issue_id` is bundled in the same turn. (3) Optional composite read helper (defer if **TECH-18** `search_specs` / bundles subsume). **Does not** replace **`.cursor/skills/project-spec-kickoff/SKILL.md`** prose until tools are **shipped** and **`npm run verify`** green. **Related:** **TECH-58** **§ Completed** — [`BACKLOG.md`](BACKLOG.md); **`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:*`**; reuse **`project-spec-closeout-parse.ts`** for path-based discovery without duplicating MCP handlers.
  - Acceptance: ≥1 **measurable** improvement merged (new tool **or** clear ranking/UX win on existing tools) + docs updated; **`npm run verify`** green
  - Depends on: none (soft: dogfood with **project-spec-kickoff**; **TECH-18** for long-term search architecture)

- [ ] **TECH-24** — territory-ia MCP: parser regression policy (tests/fixtures when parsers change)
  - Type: tooling / code health
  - Files: `tools/mcp-ia-server/` (tests, fixtures, `scripts/verify-mcp.ts` or equivalent), `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Notes: When changing markdown parsers, fuzzy matching, or glossary ranking, extend **`node:test`** fixtures and keep **`npm run verify`** green (pattern from **FEAT-45**). No Unity. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-30** — Validate **BACKLOG** issue IDs referenced in `.cursor/projects/*.md`
  - Type: tooling / doc hygiene
  - Files: `tools/` (Node script), optional `package.json` `npm run` at repo root or under `tools/`
  - Spec: `.cursor/projects/TECH-30.md`
  - Notes: Every `[BUG-XX]` / `[TECH-XX]` / etc. front matter or link in active project specs must exist in `BACKLOG.md`. `docs/agent-tooling-verification-priority-tasks.md` task 9. **Related:** **TECH-50** completed — `npm run validate:dead-project-specs` (repo-wide missing `.cursor/projects/*.md` paths); coordinate shared **Node** helpers when implementing **TECH-30**.
  - Depends on: none

- [ ] **TECH-29** — CI / script: **simulation tick** call-order drift detector
  - Type: tooling / CI
  - Files: `tools/` (Node or shell), checked-in ordered manifest (derived from `.cursor/specs/simulation-system.md` **Tick execution order**), optional `.github/workflows/`; `SimulationManager.cs` as truth source to diff
  - Spec: `.cursor/projects/TECH-29.md`
  - Notes: Fail CI (or print advisory) when `ProcessSimulationTick` step order diverges from manifest without matching spec update. `docs/agent-tooling-verification-priority-tasks.md` task 5. Phase labels should stay aligned with **TECH-16** harness.
  - Depends on: **TECH-16** (stable spec-labeled phase names in harness — soft dependency for naming parity)

- [ ] **TECH-31** — **AUTO** / **simulation** scenario or fixture generator (regression capsules)
  - Type: tooling / test infrastructure
  - Files: `tools/`, Unity test assembly or Editor scripts, optional YAML/fixtures under `tools/fixtures/` or `Tests/`
  - Spec: `.cursor/projects/TECH-31.md`
  - Notes: Expand project templates or hand-authored constraints into Play Mode tests or serialized grid fixtures for **BUG-52**-class cases. `docs/agent-tooling-verification-priority-tasks.md` task 21.
  - Depends on: none

- [ ] **TECH-34** — Generate **`gridmanager-regions.json`** from `GridManager.cs` `#region` blocks
  - Type: tooling / IA
  - Files: `tools/` (Node or C# extractor), output e.g. `tools/mcp-ia-server/data/gridmanager-regions.json`; `GridManager.cs`
  - Spec: `.cursor/projects/TECH-34.md`
  - Notes: Supports **TECH-01** extraction planning and optional future MCP `gridmanager_region_map`. `docs/agent-tooling-verification-priority-tasks.md` task 28. Coordinate MCP registration with **TECH-18** when applicable.
  - Depends on: none (MCP wiring: **TECH-18**)

- [ ] **TECH-27** — **BACKLOG.md** glossary alignment pass (**Depends on** / **Spec** / **Files** / **Notes**)
  - Type: documentation / IA hygiene
  - Files: `BACKLOG.md`, `.cursor/specs/glossary.md`, optional `tools/` link-check script
  - Spec: `.cursor/projects/TECH-27.md`
  - Notes: Audit open issues so **Depends on**, **Spec**, **Files**, and **Notes** use vocabulary from **`.cursor/specs/glossary.md`** and linked **reference specs** where practical—improves **`backlog_issue`** usefulness and cross-agent consistency. **Optional automation:** script verifying glossary “Spec” column paths (and optional heading anchors) exist (`docs/agent-tooling-verification-priority-tasks.md` task 10). Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-26** — Repo scripts / CI: mechanical checks (**FindObjectOfType** in **Update**; optional **`gridArray`** gate)
  - Type: tooling / CI
  - Files: new script under `tools/` (Node or shell), optional CI workflow; align wording with `.cursor/rules/invariants.mdc`
  - Spec: `.cursor/projects/TECH-26.md`
  - Notes: Implement scanner for **`FindObjectOfType`** inside **`Update`/`LateUpdate`/`FixedUpdate`** (supports **BUG-14** prevention) and optional **`rg`** gate blocking new **`gridArray`/`cellArray`** use outside **`GridManager`** (**TECH-04**). **Phase 2:** hot-path static scan manifest from `ARCHITECTURE.md` / managers-reference to prioritize files in AUTO or per-frame paths (`docs/agent-tooling-verification-priority-tasks.md` tasks 1, 6). Priority order: `docs/agent-tooling-verification-priority-tasks.md`. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **BUG-53** — **Unity Editor:** **Territory Developer → Reports** menu missing, or **Export Sorting Debug** ineffective / not discoverable
  - Type: bug (tooling / agent workflow)
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` (`MenuItem` paths, **Play Mode** vs **Edit Mode** branches); Unity **Editor** script compilation / **asmdef** (if introduced later); `tools/reports/` path resolution (`Application.dataPath` parent)
  - Spec: `.cursor/specs/unity-development-context.md` §10 (**Editor agent diagnostics** — expected menus, outputs, prerequisites)
  - Notes: **Observed:** Developer does not see **Export Sorting Debug (Markdown)** or the whole **Reports** submenu, or expects full **Sorting order** data while still in **Edit Mode** / before **`GridManager`** **isInitialized**. **Expected (canonical):** Both **Export Agent Context** and **Export Sorting Debug (Markdown)** appear under **Territory Developer → Reports** whenever `AgentDiagnosticsReportsMenu.cs` compiles in an **Editor** folder assembly. **Sorting** markdown with per-**cell** **`TerrainManager`** breakdowns requires **Play Mode** after **geography initialization** (`GridManager.InitializeGrid`); **Edit Mode** only writes a stub explaining that. **Export Agent Context** (JSON) should still run in **Edit Mode** / **Play Mode** and write under `tools/reports/`. **Related:** completed **TECH-28** ([`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). Investigate compile errors, wrong scene/package, menu path mismatch, or UX gap (e.g. single combined command, **Console** log on success).
  - Acceptance: On a clean clone, after Unity imports scripts, both menu items are visible; **Sorting** export behavior matches §10; document any platform-specific caveat in the spec **Decision Log** or backlog **Notes**
  - Depends on: none

## High Priority
- [ ] **BUG-44** — **Cliff** prefabs: black gaps when a **water body** (**river** or **lake**) meets the **east** or **south** **map border**
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `PlaceCliffWallStack`, **map border** / max-X / max-Y edge cases vs **open water** cells), `WaterManager.cs` / `WaterMap.cs` if edge water placement interacts with **shore refresh**; **cliff** / **water-shore** prefabs under `Assets/Prefabs/` (per `.cursor/rules/coding-conventions.mdc` for new or adjusted assets)
  - Spec: `.cursor/specs/isometric-geography-system.md` (**map border**, water, **cliffs**, **sorting order** — sections covering shore/**cliff** stacks at boundaries)
  - Notes: **Observed:** Where a **river** channel or **lake** reaches the **east** or **south** **map border**, the **cliff** geometry that seals the edge is **missing or too short** under the water tiles, exposing **black void**; **grass cells** on the same **map border** still show correct **cliff** faces. Suggests **map border** **cliff** stacks or prefab variants do not account for **lower river bed** (`H_bed`) elevation at those edges. **Expected:** Continuous **cliff** wall to the same depth as neighboring land **cliffs**, or dedicated **map border** + water prefabs so no holes at east/south × water. **Related:** completed **BUG-42** (virtual foot / edge **cliffs** — may share root cause with **map border** × water placement).
  - Depends on: none

- [ ] **BUG-31** — Wrong prefabs at interstate entry/exit (border)
  - Type: fix
  - Files: `RoadPrefabResolver.cs`, `RoadManager.cs`
  - Notes: **Interstate** must be able to enter/exit at **map border** in any direction. Incorrect prefab selection at entry/exit cells. Isolated from BUG-30 for separate work.

- [ ] **BUG-28** — **Sorting order** between **slope** cell and **interstate** cell
  - Type: fix
  - Files: `GridManager.cs` (**Sorting order** region), `TerrainManager.cs`, `RoadManager.cs`
  - Notes: **Slope** cells and **interstate** cells render in wrong **sorting order**; one draws over the other incorrectly.

- [ ] **BUG-20** — **Utility buildings** (power plant, 3×3/2×2 multi-cell **buildings**) load incorrectly in LoadGame: end up under **grass cells** (**visual restore**)
  - Type: fix
  - Files: `GeographyManager.cs` (GetMultiCellBuildingMaxSortingOrder, ReCalculateSortingOrderBasedOnHeight), `BuildingPlacementService.cs` (LoadBuildingTile, RestoreBuildingTile), `GridManager.cs` (RestoreGridCellVisuals)
  - Notes: Overlaps **BUG-35** (completed 2026-03-22): flat **grass** removed with **buildings** on load. **BUG-34** addressed general load/**building** **sorting order**. Re-verify in Unity after **BUG-35** closure; close if power plants / multi-cell **utility buildings** sort correctly.

  - [ ] **TECH-01** — Extract responsibilities from large files (focus: **GridManager** decomposition next)
  - Type: refactor
  - Files: `GridManager.cs` (~2070 lines), `TerrainManager.cs` (~3500), `CityStats.cs` (~1200), `ZoneManager.cs` (~1360), `UIManager.cs` (~1240), `RoadManager.cs` (~1730)
  - Notes: Helpers already extracted (`GridPathfinder`, `GridSortingOrderService`, `ChunkCullingSystem`, `RoadCacheService`, `BuildingPlacementService`, etc.). **Next candidates from GridManager:** `BulldozeHandler` (~200 lines), `GridInputHandler` (~130 lines), `CoordinateConversionService` (~230 lines). Prioritize this workstream; see `ARCHITECTURE.md` (GridManager hub trade-off).

- [ ] **BUG-12** — Happiness UI always shows 50%
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness)
  - Spec: `.cursor/projects/BUG-12.md`
  - Notes: `GetHappiness()` returns hardcoded `50.0f` instead of reading `cityStats.happiness`. Blocks FEAT-23 (dynamic happiness).

- [ ] **BUG-14** — `FindObjectOfType` in Update/per-frame degrades performance
  - Type: fix (performance)
  - Files: `CursorManager.cs` (Update), `UIManager.cs` (UpdateUI)
  - Spec: `.cursor/projects/BUG-14.md`
  - Notes: `CursorManager` caches `UIManager` in `Start()`; **`UIManager.UpdateUI()`** still calls `FindObjectOfType` for **EmploymentManager**, **DemandManager**, and **StatisticsManager** each frame — cache in `Awake`/`Start`. **`UpdateGridCoordinatesDebugText`** may also call `FindObjectOfType` from `LateUpdate`; remove per-frame lookups per **invariants**. See project spec for current code pointers. **Prevention:** **TECH-26** CI/script scanner flags new per-frame **`FindObjectOfType`** use.

## Medium Priority
- [ ] **BUG-49** — Manual **street** drawing: preview builds the **road stroke** cell-by-cell (animated); should show full path at once
  - Type: bug (UX / preview)
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, preview placement / ghost or temp prefab updates per frame), `GridManager.cs` if road mode input drives incremental preview; any coroutine or per-tick preview extension of the **road stroke**
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (manual **streets** — preview behavior)
  - Notes: **Observed:** While drawing a **street**, **preview mode** visually **extends the road stroke one cell at a time**, like an animation, instead of updating the full proposed **road stroke** in one step. **Expected:** **No** step-by-step or staggered preview animation. The game should **compute the full valid road stroke** (same rules as commit / **road validation pipeline** / `TryPrepareRoadPlacementPlan` or equivalent) for the current **stroke**, **then** instantiate or refresh **preview** prefabs for that complete **road stroke** in a single update — or batch updates without visible per-cell delay. **Related:** completed **BUG-37** (2026-04-02); ensure preview vs commit paths stay consistent when fixing.
  - Acceptance: **Street** preview shows the full computed **road stroke** in one visual update; no visible cell-by-cell animation during drag
  - Depends on: none

- [ ] **BUG-19** — Mouse scroll wheel in Load Game scrollable menu also triggers camera zoom
  - Type: fix (UX)
  - Files: `CameraController.cs` (HandleScrollZoom), `UIManager.cs` (loadGameMenu, savedGamesListContainer), `MainScene.unity` (LoadGameMenuPanel / Scroll View hierarchy)
  - Spec: `.cursor/projects/BUG-19.md`
  - Notes: When scrolling over the Load Game save list, the mouse wheel scrolls the list AND zooms the camera. The scroll should only move the list up/down, not affect camera zoom or other game mechanisms that use the scroll wheel.
  - Proposed solution: In `CameraController.HandleScrollZoom()`, check `EventSystem.current.IsPointerOverGameObject()` before processing scroll. If the pointer is over UI (e.g. Load Game panel, Building Selector, any scrollable popup), skip the zoom logic and let the UI consume the scroll. This mirrors how `GridManager` already gates mouse clicks via `IsPointerOverGameObject()`. Requires `using UnityEngine.EventSystems`. Verify that the Load Game ScrollRect (Scroll View) has proper raycast target so `IsPointerOverGameObject()` returns true when hovering over it.

- [ ] **BUG-16** — Possible race condition in GeographyManager vs TimeManager initialization (**geography initialization**)
  - Type: fix
  - Files: `GeographyManager.cs`, `TimeManager.cs`, `GridManager.cs`
  - Notes: Unity does not guarantee Start() order. If TimeManager.Update() runs before GeographyManager completes **geography initialization**, it may access non-existent data. Use Script Execution Order or gate with `isInitialized`.

- [ ] **BUG-17** — `cachedCamera` is null when creating `ChunkCullingSystem`
  - Type: fix
  - Files: `GridManager.cs`
  - Spec: `.cursor/projects/BUG-17.md`
  - Notes: In InitializeGrid() ChunkCullingSystem is created with `cachedCamera`, but it is only assigned in Update(). May cause NullReferenceException.

- [ ] **BUG-48** — Minimap stays stale until toggling a layer (e.g. data-visualization / **desirability** / **urban centroid**)
  - Type: bug
  - Files: `MiniMapController.cs` (`RebuildTexture`, `Update`; layer toggles call `RebuildTexture` but nothing runs on **simulation tick**), `TimeManager.cs` / `SimulationManager.cs` if wiring refresh to the **simulation tick** or a shared event
  - Spec: `.cursor/projects/BUG-48.md`
  - Notes: **Observed:** The procedural minimap **does not refresh** as the city changes unless the player **toggles a minimap layer** (or other actions that call `RebuildTexture`, such as opening the panel). **Expected:** The minimap should track **zones**, **streets**, **open water**, **forests**, etc. **without** requiring layer toggles. **Implementation:** Rebuild at least **once per simulation tick** while the minimap is visible, **or** a **performance-balanced** approach (throttled full rebuild, dirty rect / incremental update, or event-driven refresh when grid/**zone**/**street**/**water body** data changes) — profile full `RebuildTexture` cost first (see project spec; measurement tooling **task 8** in `docs/agent-tooling-verification-priority-tasks.md`). Class summary in code states rebuilds on **geography initialization** completion, grid restore, panel open, and layer changes **not** on a fixed timer — that gap is this bug. **Related:** completed **BUG-32** (water on minimap); **FEAT-42** (optional **HeightMap** layer).
  - Depends on: none

- [ ] **BUG-52** — **AUTO** zoning: persistent **grass cells** between **undeveloped light zoning** and new **AUTO** **street** segments (gaps not filled on later **simulation ticks**)
  - Type: bug (behavior / regression suspicion)
  - Files: `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `SimulationManager.cs` / `TimeManager.cs` (**tick execution order**, **AUTO systems**), `GrowthBudgetManager.cs` (**growth budget** vs eligibility), `RoadCacheService.cs` (**road cache** / zoneability neighbors), `GridManager.cs` if placement queries change; `TerrainManager.cs` (`RestoreTerrainForCell`) only if investigation ties gap cells to post-**BUG-37** terrain state
  - Spec: `.cursor/specs/simulation-system.md` (**simulation tick**, **AUTO** pipeline), `.cursor/specs/managers-reference.md` (**Zones & Buildings**, **Demand**), `.cursor/specs/isometric-geography-system.md` §13.9 (**road reservation** / AUTO interaction) as needed
  - Notes: **Observed:** After **AUTO** places **streets** (path and visuals OK), **AUTO** zoning creates **RCI** **undeveloped light zoning** patches of varying sizes (acceptable), but strips of **grass cells** often remain **Moore**-adjacent to the **road stroke** — typically a **one-cell** buffer between **zoning** and **street**. Those gap **cells** appear to stay unzoned across many later **simulation ticks**, as if permanently ineligible, not merely deferred by **growth budget**. **Expected:** Variable patch sizes are fine; any **grass cell** that remains valid for **AUTO** zoning (per design) should eventually be a candidate on a future **simulation tick** unless explicitly ruled out by documented rules (e.g. corridor reservation). **Regression suspicion:** surfaced after **BUG-37** fix (`TerrainManager` — skip terrain rebuild on **building**-occupied **cells** during path terraform refresh); verify no accidental exclusion of road-adjacent **grass cells** in zone candidate sets, **road cache invalidation**, or neighbor queries. **Related:** **FEAT-36** (AUTO zoning candidate expansion); **FEAT-43** (**growth rings** / weights); completed **BUG-47** (**AUTO** roads + zoning coordination).
  - Acceptance: Repro in **AUTO** simulation: document coordinates of gap **grass cells**; confirm whether they are excluded from `AutoZoningManager` (or equivalent) forever or until manual action; fix or document intended rule so gaps either fill over time or are explained in spec/backlog.
  - Depends on: none (follow-up from completed **BUG-37**, 2026-04-02)

- [ ] **FEAT-21** — Expenses and maintenance system
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: No expenses: no **street** maintenance, no service costs, no salaries. Without expenses there is no economic tension. Add upkeep for **streets**, **utility buildings**, and services.

- [ ] **FEAT-22** — **Tax base** feedback on **demand (R / C / I)** and happiness
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: High taxes do not affect **demand (R / C / I)** or happiness. Loop: high taxes → less residential **demand** → less growth → less income.
  - Depends on: none (**BUG-02** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **FEAT-23** — Dynamic happiness based on city conditions
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`
  - Notes: Happiness only increases when placing **zones** (+100 per **building**). No effect from unemployment, **tax base**, services or pollution. Should be continuous multi-factor calculation with decay.
  - Depends on: BUG-12

- [ ] **FEAT-36** — Expand **AUTO** zoning and **AUTO** road candidates to include **forests** and cells meeting **land slope eligibility**
  - Type: feature
  - Files: `GridManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`
  - Notes: Treat **grass cells**, **forest (coverage)** cells, and cardinal-ramp **slopes** (per **land slope eligibility**) as valid candidates for **AUTO** zoning and **AUTO** road expansion. Capture any design notes in this issue or in `.cursor/specs/isometric-geography-system.md` if rules become stable.

- [ ] **FEAT-43** — **Urban growth rings**: tune **AUTO** road/zoning weights for a gradual center → edge gradient
  - Type: feature (simulation / balance)
  - Files: `UrbanCentroidService.cs` (**growth ring** boundaries, **urban centroid** distance), `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `GrowthBudgetManager.cs` if per-ring **growth budgets** apply; `GridManager.cs` / `DemandManager.cs` only if **desirability** or placement must align with **growth rings**
  - Notes: **Observed:** In **AUTO** simulation, cities tend toward a **dense core**, **under-developed middle growth rings**, and **outer rings that are more zoned than the middle** — not a smooth radial gradient. **Expected:** Development should fall off **gradually from the urban centroid**: **highest** **street** density and **AUTO** zoning pressure **near the centroid**, **moderate** in **mid growth rings**, and **lowest** in **outer growth rings**. Revisit **growth ring** radii/thresholds, per-ring weights for **AUTO** road growth vs zoning, and any caps or priorities that invert mid vs outer activity. **Related:** completed **FEAT-32** (**streets**/intersections by area), **FEAT-29** (**zone density** gradient around **urban centroids**), **FEAT-31** (roads toward **desirability**); completed **BUG-47** (2026-04-01, **AUTO** perpendicular stubs and junction refresh).
  - Depends on: none

- [ ] **FEAT-35** — Area demolition tool (bulldozer drag-to-select)
  - Type: feature
  - Files: `GridManager.cs`, `UIManager.cs`, `CursorManager.cs`
  - Notes: Manual tool to demolish all **buildings** and **zoning** in a rectangular area at once. Use the same area selection mechanism as **zoning**: hold mouse button, drag to define rectangle, release to demolish. Reuse **zoning**'s start/end position logic (zoningStartGridPosition, zoningEndGridPosition pattern). Demolish each **cell** in the selected area via DemolishCellAt. **Interstate** cells must remain non-demolishable. Consider preview overlay (e.g. red tint) during drag.

- [ ] **FEAT-03** — **Forest (coverage)** mode hold-to-place
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`
  - Spec: `.cursor/projects/FEAT-03.md`
  - Notes: Currently requires click per **cell**. Allow continuous drag.

- [ ] **FEAT-04** — Random **forest (coverage)** spray tool
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`, `CursorManager.cs`
  - Notes: Place **forest (coverage)** in area with random spray/brush distribution.

- [ ] **FEAT-06** — **Forest (coverage)** that grows over **simulation ticks**: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: **Forest (coverage)** maturation system over **simulation ticks**.

- [ ] **FEAT-08** — **Zone density** and **desirability** simulation: evolution to larger **buildings**
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing **buildings** evolve to larger versions based on **zone density** and **desirability**. (**TECH-15** / **TECH-16** — performance + harness work — live under **§ Agent ↔ Unity & MCP context lane**.)

## Code Health (technical debt)

- [ ] **TECH-13** — Remove obsolete **urbanization proposal** system (dead code, UI, models)
  - Type: refactor (cleanup)
  - Files: `UrbanizationProposalManager.cs`, `ProposalUIController.cs`, `UrbanizationProposal.cs` (and related), `SimulationManager.cs`, `UIManager.cs`, scene references, **save data** if any
  - Spec: `.cursor/projects/TECH-13.md`
  - Notes: The **urbanization proposal** feature is **obsolete** and intentionally **disabled**; the game is stable without it. **Keep** `UrbanizationProposalManager` disconnected from the simulation — do **not** re-enable proposals. **Keep** `UrbanCentroidService` / **urban growth rings** for **AUTO** roads and zoning (FEAT-32). This issue tracks **full removal** of proposal-specific code and UI after a safe audit (no **save data** breakage). Supersedes former **BUG-15** / **BUG-13**.

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access to the **cell** grid. Several classes violate this. Risk of subtle bugs when grid or **HeightMap** changes.

- [ ] **TECH-02** — Change public fields to `[SerializeField] private` in managers
  - Type: refactor
  - Files: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`
  - Spec: `.cursor/projects/TECH-02.md`
  - Notes: Dependencies and prefabs exposed as `public` allow accidental access from any class. Use `[SerializeField] private` to encapsulate.

- [ ] **TECH-03** — Extract magic numbers to constants or ScriptableObjects
  - Type: refactor
  - Files: multiple (GridManager, CityStats, RoadManager, UIManager, TimeManager, TerrainManager, WaterManager, EconomyManager, ForestManager, InterstateManager, etc.)
  - Spec: `.cursor/projects/TECH-03.md`
  - Notes: **Building** costs, economic balance, **height generation** parameters, **sorting order** offsets (**type offsets**, **DEPTH_MULTIPLIER**, **HEIGHT_MULTIPLIER**), **pathfinding cost model** weights, initial dates, probabilities — all hardcoded. Extract to named constants or configuration ScriptableObject for easier tuning.

- [ ] **TECH-05** — Extract duplicated dependency resolution pattern
  - Type: refactor
  - Files: ~25+ managers with `if (X == null) X = FindObjectOfType<X>()` block
  - Spec: `.cursor/projects/TECH-05.md`
  - Notes: Consider helper method, base class, or extension method to reduce duplication of Inspector + FindObjectOfType fallback pattern.

- [ ] **TECH-07** — ControlPanel: left vertical sidebar layout (category rows)
  - Type: refactor (UI/UX)
  - Files: `MainScene.unity` (`ControlPanel` hierarchy, RectTransform anchors, `LayoutGroup` / `ContentSizeFitter` as needed), `UIManager.cs` (only if toolbar/submenu positioning or references must follow the new dock), `UnitControllers/*SelectorButton.cs` (only if button wiring or parent references break after reparenting)
  - Spec sections: `.cursor/specs/ui-design-system.md` — **§3.3** (toolbar), **§1.3** (anchors/margins), **§4.3** (Canvas Scaler) as applicable.
  - Notes: Replace the bottom-centered horizontal **ribbon** with a **left-docked vertical** panel. Structure: **one row per category** (demolition, **RCI** **zoning**, **utility buildings**, **streets**, environment/**forests**, etc.), with **buttons laid out horizontally within each row** (e.g. `VerticalLayoutGroup` of rows, each row `HorizontalLayoutGroup`, or equivalent manual layout). Re-anchor dependent UI (e.g. **zone density** / tool option overlays) so they align to the new sidebar instead of the old bottom bar. Verify safe area and Canvas Scaler at reference resolutions; avoid overlapping the mini-map and debug readouts. Document final hierarchy in `docs/ui-design-system-context.md`. Link program charter: `docs/ui-design-system-project.md` (Backlog bridge). Spec/docs ticketed and cross-linked in **TECH-08** (completed).

*(**Spec pipeline & verification:** **TECH-60**–**TECH-63** **§ Completed** — **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; exploration [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md). **Agent–Unity / MCP tooling TECH-21 § Completed** — **JSON program (TECH-21)** (**TECH-40**–**TECH-41** **§ Completed**, **TECH-44a** **§ Completed**; **glossary**), **TECH-44** program (**umbrella § Completed** — [`BACKLOG.md`](BACKLOG.md); **TECH-44b**/**c** **§ Completed**; **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**; open follow-ups **TECH-53**, **TECH-54**). **Compute-lib:** **TECH-36** / **TECH-37**–**TECH-39** / **TECH-32** / **TECH-35** — **§ Compute-lib program** above. **Other open Agent-lane** rows (**TECH-15**/**TECH-16**, **TECH-18**, **TECH-23**–**TECH-31**, **TECH-33**–**TECH-34**, **TECH-43**, **TECH-45**–**TECH-48**, **TECH-59**, **BUG-53**) — **§ Agent ↔ Unity & MCP context lane**. **TECH-57** (kickoff), **TECH-49**–**TECH-52** (implement / close / **project-implementation-validation**), **TECH-56** (**project-new**), **TECH-50** (dead project-spec path scanner), **TECH-58** (**project-spec-close** helpers: **`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:*`**) — **§ Completed**. **Shipped skills:** **project-spec-kickoff**, **project-spec-implement**, **project-spec-close**, **project-implementation-validation**, **project-new**; `.cursor/skills/README.md` — see **§ Completed**.)*

## Low Priority

- [ ] **FEAT-09** — Trade / Production / Salaries
  - Type: feature (new system)
  - Files: `EconomyManager.cs`, `CityStats.cs` (+ new managers)
  - Notes: Economic system of production, trade between **RCI** **zones** and salaries.

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
  - Spec: `.cursor/projects/TECH-14.md`
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
  - Notes: Requires **water body** system with defined **sea** (**water body kind**). Depends on BUG-08.

- [ ] **FEAT-16** — Train system / train animations
  - Type: feature (new system)
  - Files: new manager + `GridManager.cs`
  - Notes: Railway network and animations.

- [ ] **FEAT-39** — Sea / **shore band**: **map border** region, infinite reservoir, tide direction (data)
  - Type: feature
  - Files: `WaterManager.cs`, `WaterMap.cs`, `TerrainManager.cs`, `GeographyManager.cs`
  - Notes: Define **sea** as a **water body kind** at the **map border** with **surface height (S)** and **shore band** rules. Coordinate with **FEAT-15** (ports). **FEAT-37c** (**water map** persist) completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-40** — Water sources & drainage (snowmelt, rain, overflow) — simulation
  - Type: feature
  - Files: new helpers + `WaterMap.cs`, `WaterManager.cs`, `SimulationManager.cs`
  - Notes: Not full fluid simulation; data-driven flow affecting **water bodies**, **surface height (S)**, and **depression-fill** dynamics. **FEAT-37c** / **FEAT-38** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-41** — **Water body** terrain tools (manual paint/modify, **AUTO** terraform) — extended
  - Type: feature
  - Files: `GridManager.cs`, `WaterManager.cs`, `UIManager.cs`, `TerraformingService.cs` (as needed)
  - Notes: Beyond legacy paint-at-**sea level**. Tools to create/modify **water bodies** with proper **surface height (S)**, **shore band**, and **water map** registration. **FEAT-37c** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

- [ ] **FEAT-42** — Minimap: optional **HeightMap** / relief shading layer
  - Type: feature (UI)
  - Files: `MiniMapController.cs`, `HeightMap` / `GridManager` read access as needed
  - Notes: Visualize terrain elevation (**HeightMap**) on the minimap (distinct from **zones**/**streets**/**open water** layers). Does not replace logical **water map** / **zone** data; base layer reliability stays in **FEAT-37a** / **FEAT-30** scope.
  - Depends on: none (can follow **FEAT-37a** polish)

- [ ] **FEAT-46** — **Geography** authoring: **territory** / **urban** area **map** editor + parameter dashboard
  - Type: feature (tools / **New Game** flow)
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `ForestManager.cs`, `UIManager.cs` (or dedicated **Editor** / in-game **wizard**); **JSON** / **ScriptableObject** templates (align **JSON program (TECH-21)** **TECH-41** **§ Completed**, **TECH-36** program)
  - Notes: In-game or **Editor** flow to author **city** / **territory** **maps** with **isometric** terrain controls: **map** size, **water** / **forest** / **height** mix, **sea** / **river** / **lake** proportions, etc. Reuse the same parameter pipeline for future **player** **terraform**, **basin** / **elevation** tools, **water body** placement in **depressions**, and **AUTO** **geography**-driven tools. **Spec:** canonical **geography initialization** + **water-terrain** + **geo** when implemented (no `.cursor/projects/` spec until scheduled).
  - Depends on: none (coordinates **FEAT-18**, **FEAT-41**, **TECH-36** program)

- [ ] **FEAT-47** — **Multipolar** **urban centroid** model, per-pole **urban growth rings**, **connurbation**
  - Type: feature (**simulation** / **AUTO** architecture)
  - Files: `UrbanCentroidService.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs`, `GrowthBudgetManager.cs` (as applicable)
  - Notes: Evolve **sim** §Rings from a single **urban centroid** to **multiple** **centroids** (**desirability** / employment **poles**), each with **ring** fields; preserve coherent **AUTO** **street** / **zoning** patterns across the **map**; long-term **connurbation** between distinct urban masses. **Desirability** **scoring** may use **grid** decay; **committed** **streets** remain **road preparation family** + **geo** §10. Coordinates **FEAT-43** (gradient tuning). **Spec:** **simulation-system** §Rings + **managers-reference** when implemented (no project spec until scheduled).
  - Depends on: none (soft: **FEAT-43**, **TECH-38** **UrbanGrowthRingMath**)

- [ ] **FEAT-48** — **Water body** volume budget: **basin** expand → **surface height (S)** adjusts; **Moore**-adjacent **dig** **fill**
  - Type: feature (**water** / **terraform**)
  - Files: `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `TerraformingService.cs`, water prefabs / **sorting order** (per **geo** §7, **water-terrain**)
  - Notes: **Not** full 3D **fluid** simulation. **Gameplay:** excavating a **cell** **Moore**-adjacent to **open water** fills the **depression**; **basin** volume conservation lowers or raises **surface height (S)**; **render** water prefabs at new **S** (may expose or cover **terrain** / **islands**). Optional **isometric** directional **fill** **animation**; **S** step changes not animated. Expands across **terraform** / **water** interactions per product plan. Coordinates **FEAT-40**, **FEAT-41**, **FEAT-39**. **Spec:** **isometric-geography-system** / **water-terrain** amendments when implemented (no project spec until scheduled).
  - Depends on: none (**FEAT-37c** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)); soft: **FEAT-41**, **TECH-36** program for **pure** **volume** helpers

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

*(**TECH-18**, **TECH-44** program (**umbrella § Completed**; **TECH-44a**/**b**/**c** **§ Completed**; **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**; open **TECH-53** / **TECH-54**), **TECH-21** **§ Completed** (**TECH-40**–**TECH-41**/**TECH-44a**; **glossary** **JSON program (TECH-21)**; open **TECH-43**), **TECH-45**–**TECH-48** — **§ Agent ↔ Unity & MCP context lane**; **TECH-36** program — **§ Compute-lib program**; **TECH-57** / **TECH-49** / **TECH-50** / **TECH-51** / **TECH-52** / **TECH-56** / **TECH-58** / **TECH-60** / **TECH-61** / **TECH-62** / **TECH-63** completed — **§ Completed**; **TECH-20** / **TECH-25** / **TECH-28** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**; other recent completions under **§ Completed**.)*

- [ ] **AUDIO-01** — Audio FX: demolition, placement, **zoning**, **forest (coverage)**, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and **height** (**HeightMap**) over the map.

---

## Completed (last 30 days)

- [x] **TECH-37** — **Computational** infra: **`tools/compute-lib/`** + pilot **MCP** tool (**World ↔ Grid**) (2026-04-04)
  - Type: tooling
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/`; `Assets/Scripts/Utilities/Compute/`; `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`; [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml)
  - Spec: (removed after closure — **glossary** **territory-compute-lib (TECH-37)**; geo §1.3 **Agent tooling** note; [`ARCHITECTURE.md`](ARCHITECTURE.md) **territory-ia** tools + **`tools/compute-lib/`**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`.cursor/projects/TECH-36.md`](.cursor/projects/TECH-36.md) umbrella; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`territory-compute-lib`**, **`isometric_world_to_grid`**, **`IsometricGridMath`**, golden **`world-to-grid.json`**, **IA tools** **CI** builds **compute-lib** before **mcp-ia-server**. **Authority:** **C#** / **Unity** remain **grid** truth; **Node** duplicates **verified** planar **World ↔ Grid** inverse only (**glossary** **World ↔ Grid conversion**).
  - Depends on: none (soft: **TECH-21** **§ Completed**)

- [x] **TECH-60** — **Spec pipeline & verification program** (umbrella): agent workflow, MCP, scripts, **test contracts** (2026-04-04)
  - Type: tooling / documentation / agent enablement
  - Files: [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`.cursor/skills/README.md`](.cursor/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml); **§ Completed** children **TECH-61**–**TECH-63** (this file)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-61**–**TECH-63**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`.cursor/skills/README.md`](.cursor/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); prerequisite rows **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30**, **TECH-37**, **TECH-38** — `.cursor/projects/*.md`; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Phased **TECH-61** (layer A), **TECH-62** (layer B — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**), **TECH-63** (layer C — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**). **Charter:** ids **TECH-60**–**TECH-63**; three layers vs monolithic umbrella. **Related:** **TECH-48** (MCP discovery — **TECH-62** overlap **§ Completed**); **TECH-23**; **TECH-45**–**TECH-47** (**Skills** README).
  - Depends on: none (prerequisites remain separate **BACKLOG** rows)

- [x] **TECH-63** — **Spec pipeline** layer **C**: Cursor **Skills** + **project spec** template (**test contracts**, workflow steps) (2026-04-04)
  - Type: documentation / agent enablement (**Cursor Skill** + template edits)
  - Files: `.cursor/skills/project-spec-kickoff/SKILL.md`, `.cursor/skills/project-spec-implement/SKILL.md`, `.cursor/skills/project-implementation-validation/SKILL.md`, `.cursor/skills/project-spec-close/SKILL.md`, `.cursor/skills/project-new/SKILL.md`; `.cursor/templates/project-spec-template.md`; `.cursor/projects/PROJECT-SPEC-STRUCTURE.md`; `.cursor/skills/README.md`; [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](.cursor/projects/PROJECT-SPEC-STRUCTURE.md) **§7b**; [`.cursor/skills/README.md`](.cursor/skills/README.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`## 7b. Test Contracts`** in template; **Skills** — **`depends_on_status`** preflight, **`router_for_task`** **`files`**, **Impact preflight**, **Phase exit** / **rollback**; **`AGENTS.md`** **§7b** pointer. **Does not** extend **`project_spec_closeout_digest`** for **§7b** — follow-up **BACKLOG** row if machine-read **test contracts** is required.
  - Depends on: **TECH-62** **§ Completed** (soft)

- [x] **TECH-62** — **Spec pipeline** layer **B**: **territory-ia** **`backlog_issue`** **`depends_on_status`** + **`router_for_task`** **`files`** / **`file_domain_hints`** (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (handlers, parsers); `tools/mcp-ia-server/tests/`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/package.json`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`backlog_issue`** returns **`depends_on_status`** per cited **Depends on** id; **`router_for_task`** accepts **`domain`** and/or **`files`**. **`@territory/mcp-ia-server`** **0.4.4**. **Deferred:** **`context_bundle`**, **`spec_section`** **`include_children`**, **`project_spec_status`** — **TECH-48** / follow-ups. **TECH-48** overlap and MVP split recorded in pre-closeout **Decision Log** (migrated to this row + **glossary**).
  - Depends on: **TECH-61** **§ Completed** (soft)

- [x] **TECH-61** — **Spec pipeline** layer **A**: repo **scripts** + validation **infrastructure** (`npm run`, optional `tools/invariant-checks/`) (2026-04-04)
  - Type: tooling / CI / agent enablement
  - Files: root [`package.json`](package.json) (`validate:all`, `description`); [`.cursor/skills/project-implementation-validation/SKILL.md`](.cursor/skills/project-implementation-validation/SKILL.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**; [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) — **project-implementation-validation**, **territory-ia spec-pipeline layer B (TECH-62)**, **territory-ia spec-pipeline program (TECH-60)**, **Documentation** row; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md) (reference)
  - Spec: (removed after closure — **glossary** **project-implementation-validation** / **`validate:all`**; **project-implementation-validation** **`SKILL.md`**; **`docs/mcp-ia-server.md`**; root **`package.json`**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; **TECH-62** **§ Completed**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`npm run validate:all`** chains **IA tools** steps 1–4 (**dead project spec**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**); triple-source rule with **project-implementation-validation** manifest and [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml). **Phase 2**/**3** optional scripts (**impact** / **diff** / **backlog-deps**, **`test:invariants`**) deferred per **Decision Log** — pick up under **TECH-30** / follow-up. **Does not** register MCP tools (**TECH-62** layer B **§ Completed** for **territory-ia** extensions — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**).
  - Depends on: none (soft: **TECH-50** **§ Completed**)

- [x] **TECH-21** — **JSON program** (umbrella; charter closed) (2026-04-03)
  - Type: technical / data interchange
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`projects/TECH-21-json-use-cases-brainstorm.md`](projects/TECH-21-json-use-cases-brainstorm.md); `.cursor/specs/glossary.md` — **JSON program (TECH-21)**, **Interchange JSON (artifact)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); `.cursor/specs/persistence-system.md`; `docs/planned-domain-ideas.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**, **TECH-41**, **TECH-44a**, **TECH-44**
  - Spec: (removed after closure — **glossary** **JSON program (TECH-21)**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/TECH-21-json-use-cases-brainstorm.md`](projects/TECH-21-json-use-cases-brainstorm.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**/**TECH-41**/**TECH-44a**/**TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella phases **TECH-40**/**TECH-41**/**TECH-44a** **§ Completed**; **Save data** format unchanged without a migration issue; charter **Decision Log** and **Open Questions** trace live in **glossary** + durable docs. **Ongoing process:** any **Save data** change needs a tracked migration issue; keep brainstorm FAQ aligned when editing interchange docs. **B2** append-only line log → **TECH-43** (open). **Postgres**/**IA** evolution: **TECH-44** **§ Completed**, **TECH-18**.
  - Depends on: none

- [x] **TECH-55b** — **Editor Reports: DB-first document storage + filesystem fallback** (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0005_editor_export_document.sql`; `.gitignore` (`tools/reports/.staging/`); `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; `.env.example`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`.cursor/specs/unity-development-context.md`](.cursor/specs/unity-development-context.md) §10; [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) — **Editor export registry**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup** **Editor export registry** + **Node**/**PATH** troubleshooting; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **DB-first** **`document jsonb`**; **`tools/reports/`** fallback; quiet success **`Debug.Log`** (optional verbose **EditorPrefs**); **`DATABASE_URL`** via **EditorPrefs** / **`.env.local`**; **`node`** resolution for GUI-launched **Unity** (**Volta**/Homebrew/**EditorPrefs**/**`NODE_BINARY`**); optional **`backlog_issue_id`** (**NULL** when unset); no backlog id as **Editor** product branding. **Operational:** run **`npm run db:migrate`** (**`0004`**/**`0005`**) before **`editor_export_*`** exist; **Postgres** user in **`DATABASE_URL`** must match local roles (e.g. Homebrew vs `postgres`).
  - Depends on: **TECH-55** **§ Completed**
  - Related: **TECH-44b**/**c** **§ Completed**, **TECH-59** (MCP staging)

- [x] **TECH-55** — **Automated Editor report registry** (Postgres, per **Reports** export type) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0004_editor_export_tables.sql`; `db/migrations/0005_editor_export_document.sql`; `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`.cursor/specs/unity-development-context.md`](.cursor/specs/unity-development-context.md) §10; [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) — **Editor export registry**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup**; **postgres-interchange-patterns** **Program extension mapping**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55b**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Per-export **`editor_export_*`** **B1** tables, **`register-editor-export.mjs`**, **`EditorPostgresExportRegistrar`**; **`normalizeIssueId`** parity with **`backlog-parser.ts`**. **TECH-55b** superseded persistence to **DB-first** full body + filesystem fallback (same closure batch). Does not replace **`dev_repro_bundle`** (**TECH-44c**).
  - Depends on: **TECH-44b** **§ Completed** (soft: **TECH-44c** **§ Completed**)
  - Related: **TECH-55b** **§ Completed**, **TECH-59**

- [x] **TECH-58** — **Agent closeout efficiency:** **project-spec-close** (**MCP** + **Node**) (2026-04-03)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`; `tools/mcp-ia-server/src/tools/project-spec-closeout-digest.ts`, `spec-sections.ts`; `tools/mcp-ia-server/src/tools/spec-section.ts` (shared extract); `tools/mcp-ia-server/scripts/project-spec-closeout-report.ts`, `project-spec-dependents.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/closeout-parse.test.ts`, `tests/tools/spec-section-batch.test.ts`; root `package.json` (`closeout:*`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ARCHITECTURE.md`](ARCHITECTURE.md); `AGENTS.md`; `.cursor/rules/agent-router.mdc`, `mcp-ia-default.mdc`; [`.cursor/skills/project-spec-close/SKILL.md`](.cursor/skills/project-spec-close/SKILL.md); [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](.cursor/projects/PROJECT-SPEC-STRUCTURE.md); [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) — **project-spec-close** / **IA index manifest** / **Reference spec** rows; `tools/mcp-ia-server/src/index.ts` (v0.4.3)
  - Spec: (removed after closure — [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows** + **Tools**; **glossary** **project-spec-close**; **project-spec-close** **`SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Lessons learned (TECH-58 closure)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + `project-implementation-validation`):** **`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:worksheet`** / **`closeout:dependents`** / **`closeout:verify`**; shared parser for future **TECH-48**. **TECH-51** closeout ordering unchanged. **`npm run verify`** / **`test:ia`** green.
  - Depends on: none (soft: **TECH-48**, **TECH-30**, **TECH-18**)

- [x] **TECH-56** — **Cursor Skill:** **`/project-new`** — new **BACKLOG** row + initial **project spec** + cross-links (**territory-ia** + optional web) (2026-04-06)
  - Type: documentation / agent enablement (**Cursor Skill** + **BACKLOG** / `.cursor/projects/` hygiene)
  - Files: `.cursor/skills/project-new/SKILL.md`; [`.cursor/skills/README.md`](.cursor/skills/README.md); `AGENTS.md` item 5; `.cursor/specs/glossary.md` — **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**
  - Spec: (removed after closure — [`.cursor/skills/project-new/SKILL.md`](.cursor/skills/project-new/SKILL.md); **glossary** **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **create-first** **Tool recipe (territory-ia)**; **`backlog_issue`** resolves **open** + **§ Completed (last 30 days)** in **`BACKLOG.md`** ([`docs/mcp-ia-server.md`](docs/mcp-ia-server.md)); [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) for **archive-only** ids; optional **`web_search`** external-only; **`npm run validate:dead-project-specs`** after new **`Spec:`** paths. **Decision Log:** skill folder **`project-new`**; revisit recipe when **TECH-48** ships. Complements **kickoff** / **implement** / **close** / **project-implementation-validation**.
  - Depends on: none (soft: [.cursor/skills/README.md](.cursor/skills/README.md); **TECH-49**–**TECH-52** **§ Completed** for sibling patterns)

- [x] **TECH-44** — **Postgres + interchange patterns** (merged program umbrella; charter closed) (2026-04-05)
  - Type: technical / infrastructure + architecture (program umbrella)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (**Program extension mapping (E1–E3)**); **TECH-44a**/**b**/**c** **§ Completed** rows (same section); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; `AGENTS.md` (umbrella programs); `.cursor/specs/glossary.md` — **Postgres interchange patterns**, **JSON program (TECH-21)**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping**; **glossary** **Postgres interchange patterns**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44a**/**b**/**c**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** Charter **§4** satisfied (**TECH-44a**/**b**/**c** **§ Completed**). **E2**/**E3** remain **TECH-53**/**TECH-54** (open); **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**. **Decision Log** entries migrated into [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) and **glossary**. **ID hygiene:** former erroneous **TECH-44** id on **project-spec-kickoff** completion → **TECH-57** (see below).
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-44c** — **Dev repro bundle registry** (**E1**) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `db/migrations/0003_dev_repro_bundle.sql`; `tools/postgres-ia/register-dev-repro.mjs`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Dev repro bundle registry**); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (Related pointer); repo root `package.json` (`db:register-repro`); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `.cursor/specs/unity-development-context.md` §10 (**Postgres registry** blurb); `.cursor/specs/glossary.md` — **Dev repro bundle**
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); glossary **Dev repro bundle**; **unity-development-context** §10; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **`dev_repro_bundle`** **B1** table + **`dev_repro_list_by_issue`**; **`register-dev-repro.mjs`** with **`normalizeIssueId`** parity to **`backlog-parser.ts`** (keep in sync — lesson in glossary). **Save data** / **Load pipeline** unchanged. Per-export **Unity** automation → **TECH-55** **§ Completed** (glossary **Editor export registry**).
  - Depends on: **TECH-44b** **§ Completed**

- [x] **TECH-44b** — Game **PostgreSQL** database; first milestone — **IA** schema + minimal read surface (2026-04-03)
  - Type: infrastructure / tooling
  - Files: `db/migrations/`; `tools/postgres-ia/`; `docs/postgres-ia-dev-setup.md`; `.env.example`; repo root `package.json` (`db:migrate`, `db:seed:glossary`, `db:glossary`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) (**PostgreSQL IA** subsection for **TECH-18**); `.cursor/specs/glossary.md` — **Postgres interchange patterns** row (**TECH-44b** milestone); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); `docs/agent-tooling-verification-priority-tasks.md` (row 11); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `.cursor/projects/TECH-18.md` (**Current State**); `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts` (open-issue fixture — e.g. **TECH-36**)
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) **Shipped decisions**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **glossary** **Postgres interchange patterns**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + local migrate/seed/smoke):** Versioned **IA** tables (`glossary`, `spec_sections`, `invariants`, `relationships`); **`ia_glossary_row_by_key`**; **`tools/postgres-ia/`** migrate/seed/read scripts; **`DATABASE_URL`** / **`.env.example`**; **MCP** remains **file-backed** until **TECH-18**. Does **not** replace Markdown authoring or **I1**/**I2** **CI** checks.
  - Depends on: **TECH-44a** **§ Completed**

- [x] **TECH-44a** — **Interchange + PostgreSQL patterns** (**B1**, **B3**, **P5**) (2026-04-03)
  - Type: technical / architecture (documentation)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); `.cursor/specs/persistence-system.md` (pointer); `.cursor/specs/glossary.md` — **Postgres interchange patterns (B1, B3, P5)**, **Interchange JSON** Spec column, **JSON program (TECH-21)**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md), [`projects/TECH-21-json-use-cases-brainstorm.md`](projects/TECH-21-json-use-cases-brainstorm.md), `docs/mcp-ia-server.md`, `docs/planned-domain-ideas.md`, `docs/cursor-agents-skills-mcp-study.md`, `docs/agent-tooling-verification-priority-tasks.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44** (umbrella — filed after **TECH-44a** closure), **TECH-21**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); **glossary** **Postgres interchange patterns**, **JSON program (TECH-21)**; **persistence-system** §Save; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**/**TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase C** of **TECH-21**. Normative **B1** row+**JSONB**, **B3** idempotent **patch** **envelope**, **P5** streaming, SQL vs **`artifact`** naming; explicit **Save data** / **Load pipeline** separation. **B2** → **TECH-43** only. Former **TECH-42** scope under **TECH-44** program.
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-41** — **JSON** payloads for **current** systems: **geography** params, **cell**/**chunk** interchange, snapshots, DTO layers (2026-04-11)
  - Type: technical / performance enablement
  - Files: `Assets/StreamingAssets/Config/geography-default.json`; `Assets/Scripts/Managers/GameManagers/GeographyInitParamsDto.cs`, `GeographyInitParamsLoader.cs`; `GeographyManager.cs`, `MapGenerationSeed.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `docs/schemas/cell-chunk-interchange.v1.schema.json`, `world-snapshot-dev.v1.schema.json`, `docs/schemas/README.md`; `tools/mcp-ia-server/src/schemas/geography-init-params-zod.ts`, `scripts/validate-fixtures.ts`, `tests/schemas/`; `.cursor/specs/glossary.md` — **Interchange JSON**, **geography_init_params**; **`ARCHITECTURE.md`** — **Interchange JSON**; **persistence-system** / **unity-development-context** cross-links
  - Spec: (removed after closure — **glossary** + **`ARCHITECTURE.md`** + [`docs/schemas/README.md`](docs/schemas/README.md) + **unity-development-context** §10 + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase B** of **JSON program (TECH-21)**. **G4** optional **`geography_init_params`** load from **StreamingAssets**; **G1**/**G2** Editor exports under **`tools/reports/`**; Zod parity + **`validate:fixtures`**; **E3** layering documented; **Save data** unchanged. **Deferred to FEAT-46:** apply **`water.seaBias`** / **`forest.coverageTarget`** to simulation. **`backlog_issue`** test target: open **JSON program**-related row (e.g. **TECH-59**).
  - Depends on: none (**TECH-40** completed — **§ Completed** **TECH-40**)

- [x] **TECH-40** — **JSON** infra: artifact identity, schemas, **CI** validation, **spec** + **glossary** indexes (2026-04-11)
  - Type: tooling / data interchange
  - Files: `docs/schemas/` (pilot schema + fixtures); repo root `package.json` (`validate:fixtures`, `generate:ia-indexes`, `validate:dead-project-specs`, `test:ia`); `tools/mcp-ia-server/scripts/validate-fixtures.ts`, `generate-ia-indexes.ts`, `src/ia-index/glossary-spec-ref.ts`, `data/spec-index.json`, `data/glossary-index.json`; `.github/workflows/ia-tools.yml`; `projects/TECH-21-json-use-cases-brainstorm.md` (policy §); `docs/mcp-ia-server.md`; `.cursor/specs/glossary.md` — **Documentation** (**IA index manifest**, **Interchange JSON**); [REFERENCE-SPEC-STRUCTURE.md](.cursor/specs/REFERENCE-SPEC-STRUCTURE.md) § Conventions item 7
  - Spec: (removed after closure — **glossary** + **REFERENCE-SPEC-STRUCTURE** + [`docs/schemas/README.md`](docs/schemas/README.md) + [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase A** of **JSON program (TECH-21)**. **`artifact`** / **`schema_version`** policy; JSON Schema Draft **2020-12** pilot **`geography_init_params`**; **`npm run validate:fixtures`**; committed **I1**/**I2** with **`generate:ia-indexes -- --check`** in **CI**. **`backlog_issue`** integration test uses an open issue in the **Agent** lane (e.g. **TECH-36**, **TECH-59**). **Related:** **TECH-24**, **TECH-30**, **TECH-34**; **TECH-43** **Depends on** updated.
  - Depends on: none (soft: align **TECH-37** **Zod** when touching **compute-lib**)

- [x] **TECH-57** — **Cursor Skills:** **infrastructure** + **kickoff** skill (project **spec** review / IA alignment) (2026-04-11)
  - Type: documentation / agent enablement (**Cursor Skill** + repo docs — no runtime game code)
  - Files: `.cursor/skills/README.md`; `.cursor/skills/project-spec-kickoff/SKILL.md`; `.cursor/templates/project-spec-review-prompt.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`
  - Spec: (removed after closure — conventions live under **`.cursor/skills/`** and **§4.4** of [`docs/cursor-agents-skills-mcp-study.md`](docs/cursor-agents-skills-mcp-study.md))
  - Notes: **Completed (verified per user):** Part 1 **README** + authoring rules; Part 2 **project-spec-kickoff** **`SKILL.md`** with **Tool recipe (territory-ia)** (`backlog_issue` → `invariants_summary` → `router_for_task` → …); paste template; **AGENTS.md** item 5 + doc hierarchy pointer; study doc **§4.4**. **Lesson (persisted in README):** **`router_for_task`** `domain` strings should match **`.cursor/rules/agent-router.mdc`** task-domain row labels (e.g. `Save / load`), not ad-hoc phrases. **Follow-up:** **TECH-48** (MCP discovery), **TECH-45**–**TECH-47** (domain skills). **Renumbered from erroneous id TECH-44** (collision with Postgres program **TECH-44** — corrected 2026-04-05).
  - Depends on: none

- [x] **TECH-49** — **Cursor Skill:** **implement** a **project spec** (execution workflow after kickoff) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `.cursor/skills/project-spec-implement/SKILL.md`; `.cursor/skills/README.md`; `.cursor/skills/project-spec-kickoff/SKILL.md` (cross-link); `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`; `docs/mcp-ia-server.md`; `.cursor/templates/project-spec-review-prompt.md`
  - Spec: (removed after closure — workflow in **`.cursor/skills/project-spec-implement/SKILL.md`**; closure record in this row)
  - Notes: **Completed (verified per user request to implement):** **project-spec-implement** **`SKILL.md`** with **Tool recipe (territory-ia)** (per-phase loop, **Branching**, **Seed prompt**, **unity-development-context** §10 pointer); README index row; **AGENTS.md** project-spec bullets + doc hierarchy; study doc **§4.4**; **`docs/mcp-ia-server.md`** “Project spec workflows”; paste template “After review: implement”. **Dry-run:** Meta — authoring followed the recipe while implementing this issue.
  - Depends on: none (soft: **TECH-57**)

- [x] **TECH-50** — **Doc hygiene:** **cascade** references when **project specs** close; **dead links**; **BACKLOG** as durable anchor (2026-04-03)
  - Type: tooling / doc hygiene / agent enablement
  - Files: `tools/validate-dead-project-spec-paths.mjs`; repo root `package.json` (`validate:dead-project-specs`); `.github/workflows/ia-tools.yml`; `.cursor/projects/PROJECT-SPEC-STRUCTURE.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/agent-tooling-verification-priority-tasks.md`; `tools/mcp-ia-server/README.md` (pointer only)
  - Spec: (removed after closure — **PROJECT-SPEC-STRUCTURE** closeout + **Lessons learned (TECH-50 closure)**; **`docs/mcp-ia-server.md`** **Project spec path hygiene**; this row)
  - Notes: **Completed (verified per user):** `npm run validate:dead-project-specs` + CI gate; **BACKLOG** checks strict **`Spec:`** lines on open rows only; **BACKLOG-ARCHIVE.md** excluded; advisory `--advisory` / `CI_DEAD_SPEC_ADVISORY=1`. **Lessons:** See **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-50 closure)**. **Deferred:** optional **territory-ia** MCP tool; shared **Node** module with **TECH-30**.
  - Depends on: none (soft: **TECH-30** — merge or share implementation)
  - Related: **TECH-51** completed — **`project-spec-close`** documents `npm run validate:dead-project-specs` in the closure workflow

- [x] **TECH-51** — **Cursor Skill:** **`project-spec-close`** — full **issue** / **project spec** closure workflow (IA, lessons, **BACKLOG**, cascade) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `.cursor/skills/project-spec-close/SKILL.md`; `.cursor/skills/README.md`; `.cursor/skills/project-spec-kickoff/SKILL.md`; `.cursor/skills/project-spec-implement/SKILL.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `docs/mcp-ia-server.md`; `.cursor/specs/glossary.md` — **Documentation**; `.cursor/projects/PROJECT-SPEC-STRUCTURE.md`
  - Spec: (removed after closure — **`.cursor/skills/project-spec-close/SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Closeout checklist** + **Lessons learned (TECH-51 closure)**; **glossary** **Project spec** / **project-spec-close**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** **IA persistence checklist** + ordered **Tool recipe (territory-ia)**; **persist IA → delete project spec → `validate:dead-project-specs` → BACKLOG Completed** (user-confirmed). **Decisions:** no duplicate **TECH-50** scanner in the skill; composite **closeout_preflight** MCP deferred (**TECH-48** / follow-up). **Related:** **TECH-52** completed — optional **`project-implementation-validation`** before closeout cascade when IA-heavy.
  - Depends on: none (soft: **TECH-50**, **TECH-57**, **TECH-49**)

- [x] **TECH-52** — **Cursor Skill:** **`project-implementation-validation`** — post-implementation tests + available code validations (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `.cursor/skills/project-implementation-validation/SKILL.md`; `.cursor/skills/README.md`; `.cursor/skills/project-spec-implement/SKILL.md`; `.cursor/skills/project-spec-kickoff/SKILL.md`; `.cursor/skills/project-spec-close/SKILL.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `tools/mcp-ia-server/README.md`
  - Spec: (removed after closure — **`.cursor/skills/project-implementation-validation/SKILL.md`**; **glossary** **Documentation** — **project-implementation-validation**; **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-52 closure)**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** ordered **validation manifest** (**IA tools** **CI** parity + advisory **`verify`**); **skip** matrix; **failure policy**; cross-links to **implement** / **close** / **kickoff**; **Phase 3** root aggregate **`npm run`** not shipped (optional **BACKLOG** follow-up). **Deferred:** **`run_validations`** MCP (**TECH-48** / follow-up); **Unity** one-liner → **TECH-15** / **TECH-16** / **UTF**.
  - Depends on: none (soft: **TECH-49**, **TECH-50**, **TECH-51**)
  - Related: **TECH-48** — MCP “validation bundle” tool out of scope unless new issue

*(Older batch moved to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) § **Recent archive** on 2026-04-10. Add new completions here for ~30 days, then archive.)*

> Full history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

---

## How to Use This Backlog

1. **Work on an issue**: Open chat in Cursor, reference `@BACKLOG.md` and request analysis or implementation of the issue by ID (e.g. "Analyze BUG-01 and propose a plan").
2. **Reprioritize**: Move the issue up or down within its section, or change section.
3. **Add new issue**: Assign the next available ID in the appropriate category and place in the correct priority section.
4. **Complete issue**: Move to "Completed" section with date, mark checkbox as `[x]`.
5. **In progress**: Move to "In progress" section when starting work.
6. **Dependencies**: Use `Depends on: ID` when an open issue must wait on another. **Convention:** every ID in `Depends on:` must appear **above** the dependent in this file (earlier in the same section or in a higher-priority section), **or** be **completed** in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) — then write `Depends on: none` and cite the archived id in **Notes**. Check dependencies before starting.

### ID Convention
| Prefix | Category |
|--------|----------|
| `BUG-XX` | Bugs and broken functionality |
| `FEAT-XX` | Features and enhancements |
| `TECH-XX` | Technical debt, refactors, code health |
| `ART-XX` | Art assets, prefabs, sprites |
| `AUDIO-XX` | Audio assets and audio system features |

### Issue Fields
- **Type**: fix, feature, refactor, art/assets, audio/feature, etc.
- **Files**: main files involved
- **Notes**: context, problem description or expected solution
- **Acceptance** (optional): concrete pass/fail criteria for verification
- **Depends on** (optional): IDs of issues that must be completed first

### Section Order
1. **Compute-lib program** (**TECH-36** umbrella; phased **TECH-37** → **TECH-38** → **TECH-39**; related **TECH-32**, **TECH-35**)
2. **Agent ↔ Unity & MCP context lane** (Unity exports, MCP, CI, performance harnesses, adjacent tooling)
3. In progress (actively being developed — insert above **High priority** when used)
4. High priority (critical bugs, core gameplay blockers)
5. Medium priority (important features, balance, improvements)
6. Code Health (technical debt, refactors, performance)
7. Low priority (new systems, polish, content)
8. Completed (last 30 days)
