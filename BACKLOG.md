# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first): **§ Compute-lib program**, then **§ Agent ↔ Unity & MCP context lane**, then **§ UI-as-code program** (umbrella **§ Completed** — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**), then **§ Gameplay & simulation lane**, then **High** / **Medium** / **Code Health** / **Low**.
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.
>
> **Priority:** **Spec pipeline** and **compute-lib** program charter are closed — trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) and **glossary** (**territory-ia spec-pipeline program**, **Compute-lib program**). Exploration: [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md). **§ UI-as-code program** umbrella **§ Completed** — **glossary** **UI-as-code program**; **`ui-design-system.md`** (**Codebase inventory (uGUI)**); [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**. **§ Compute-lib program** below (**TECH-38** + **TECH-32**/**TECH-35** research; **TECH-39** computational **MCP** suite — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). **§ Agent ↔ Unity & MCP context lane** follows, then **§ UI-as-code program** (open **FEAT-50** polish only), then **§ Gameplay & simulation lane** (player-facing **simulation** / **AUTO** / density). **Gameplay** blockers in **§ High Priority** stay **interrupt** work when they **stop play** or **corrupt saves**.

---

## Compute-lib program

**Dependency order:** Pilot **compute-lib** + **World ↔ Grid** MCP shipped — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md), **glossary** **territory-compute-lib**. **TECH-39** (computational **MCP** suite) **§ Completed** — same archive + **glossary** **Computational MCP tools (TECH-39)**. **TECH-38** (**C#** **pure** modules + harnesses) extends **`Utilities/Compute/`** and **`tools/reports/`** against **glossary** **Compute-lib program** + reference specs (no umbrella **Depends on**). **Related research** (**TECH-32**, **TECH-35**): `Depends on: none`, but run after **TECH-38** surfaces exist when comparing **UrbanGrowthRingMath** / **RNG** notes.

- [ ] **TECH-38** — **Core** **computational** modules (Unity **utilities** + **`tools/`** harnesses)
  - Type: code health / performance enablement
  - Files: `Assets/Scripts/Utilities/Compute/`; `GridManager.cs` (**CoordinateConversionService**), `GridPathfinder.cs`, `UrbanCentroidService.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterManager.cs`, `DemandManager.cs` / `CityStats.cs` (as extractions land); `tools/reports/`; **UTF** tests
  - Spec: none — **glossary** **C# compute utilities**; `tools/reports/compute-utilities-inventory.md`, `tools/reports/compute-utilities-rng-derivation.md`
  - Notes: **Behavior-preserving** extractions; **UrbanGrowthRingMath** **multipolar**-ready for **FEAT-47**; **stochastic** **geography initialization** documentation; **no** second **pathfinding** authority. Prepare **batchmode** hooks for **TECH-66** / **glossary** **Computational MCP tools (TECH-39)** follow-ups. **Context:** **glossary** **Compute-lib program**, **territory-compute-lib (TECH-37)**, **Computational MCP tools (TECH-39)** ([`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)).
  - Acceptance: inventory doc + **≥ 3** **pure** modules with tests or **golden** **JSON**; **RNG** derivation doc; **invariants** respected — see `tools/reports/compute-utilities-inventory.md` and bullets above
  - Depends on: none (pilot milestone in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

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

Ordered for **MCP Unity context** → **JSON / reports from Unity** → **MCP platform** → **agent workflow & CI helpers** → **research tooling**. (**§ Compute-lib program** above: **TECH-38** + **TECH-32**/**TECH-35**.) **Prerequisites:** **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30** (this lane — existing `.cursor/projects/*.md`); **TECH-38** + archived **TECH-39** (**§ Compute-lib program** / [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). **Spec-pipeline** charter: **glossary** **territory-ia spec-pipeline program** + archive.

- [ ] **TECH-59** — **territory-ia** MCP: stage **Editor** export registry payload (**BACKLOG** issue id + JSON documents)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (new **`registerTool`** handler + validation); `tools/mcp-ia-server/tests/`; `tools/mcp-ia-server/README.md`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `Assets/Scripts/Editor/` (menu: read staged file → apply **EditorPrefs** / optional one-shot register); **`.gitignore`** (staging path); optional JSON schema under `docs/schemas/` or fixture under `tools/mcp-ia-server/`; [`.cursor/skills/README.md`](.cursor/skills/README.md) pointer if agent recipe updates
  - Spec: `.cursor/projects/TECH-59.md`
  - Notes: **Problem:** Agents cannot set **Unity** **EditorPrefs**; developers should not re-type **`backlog_issue_id`** and JSON shapes by hand every time. **Direction:** MCP tool accepts **`issue_id`** + one or more **JSON** objects (typed by **export kind** or free-form envelope per **Decision Log**), writes a **gitignored** **staging file** under the repo; Unity menu **Apply MCP-staged registry…** loads file, validates, sets **EditorPrefs** (**`TerritoryDeveloper.EditorExportRegistry.BacklogIssueId`**), and optionally triggers the existing **Node** / **Postgres** path so the dev **clicks once**. **Non-goals:** MCP opens **TCP** to **Postgres**; no secrets in MCP arguments (connection string stays **EditorPrefs** / env). **Overlap:** **TECH-48** (MCP discovery — different scope); **Editor export registry** (glossary) shipped — staging may align when implemented.
  - Acceptance: per `.cursor/projects/TECH-59.md` §8; **`npm run verify`** / **`npm run test:ia`** green when MCP code ships
  - Depends on: none (soft: **TECH-24** for parser/test policy)
  - Related: **TECH-48**, **TECH-18** (long-term DB-backed IA); glossary **Editor export registry**

- [ ] **TECH-53** — **Schema validation history** (Postgres extension **E2** track)
  - Type: technical / CI / data
  - Files: `.github/workflows/` (e.g. extend **ia-tools**), `docs/schemas/`, `docs/schemas/fixtures/`; optional **Postgres** table (IA schema milestone in archive)
  - Spec: none (backlog-only — no `.cursor/projects/` spec)
  - Notes: Persist per-CI-run outcomes of **`npm run validate:fixtures`** / **JSON Schema** checks so regressions on **Interchange JSON** and fixtures are visible over time. Align row shape with [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **B1** if stored in **Postgres**. Program pointer: same doc **Program extension mapping (E1–E3)**; Postgres umbrella trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: agreed storage (artifact file, DB rows, or workflow summary) + documented query or review path; English **Notes** updated when implementation choice is fixed
  - Depends on: none (soft: IA **Postgres** milestone + JSON infra in archive)

- [ ] **TECH-54** — **Agent patch proposal staging** (Postgres extension **E3** track)
  - Type: tooling / agent workflow
  - Files: optional **Postgres** migrations; `tools/` or thin HTTP handler; `docs/`
  - Spec: none (backlog-only — no `.cursor/projects/` spec)
  - Notes: Queue **B3**-style idempotent patch envelopes ([`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md)) with explicit lifecycle (**pending** / **approved** / **rejected**) before humans merge changes to git; **`natural_key`** for deduplication. **Not** player **Save data**. Program pointer: same doc **Program extension mapping (E1–E3)**; Postgres umbrella trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: documented state machine + at least one insert/list path (script, SQL, or API); conflict policy recorded in issue **Notes** or [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) / [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) when implementing
  - Depends on: none (soft: IA **Postgres** milestone + interchange patterns doc in archive)

- [ ] **TECH-43** — Append-only **JSON** line **event log** (telemetry / sim anomalies) — **backlog placeholder**
  - Type: technical / observability (future)
  - Files: TBD (`tools/`, optional **Postgres** table, ship pipeline)
  - Spec: none (promote to `.cursor/projects/TECH-43.md` when scheduled)
  - Notes: Idea from **JSON interchange program** brainstorm **B2** (`projects/json-use-cases-brainstorm.md`); **schema_version** per line; same validator family as shipped JSON infra (archive). **Schema** pipeline exists under `docs/schemas/` + **`npm run validate:fixtures`**.
  - Acceptance: issue refined with concrete consumer + storage choice; optional schema + sample sink
  - Depends on: none (soft: JSON infra milestone in archive)

- [ ] **TECH-18** — Migrate Information Architecture from Markdown to PostgreSQL (MCP evolution)
  - Type: infrastructure / tooling
  - Files: All `.cursor/specs/*.md`, `.cursor/rules/agent-router.mdc`, `.cursor/rules/invariants.mdc`, `ARCHITECTURE.md`; MCP server (file-backed **territory-ia** — shipped, see archive); schema / migrations / seed from IA **Postgres** milestone (archive); `tools/mcp-ia-server/src/index.ts`, `docs/mcp-ia-server.md`
  - Spec: `.cursor/projects/TECH-18.md`
  - Notes: **Goal:** After file-backed MCP and IA **Postgres** tables, **migrate authoritative IA content** into PostgreSQL and evolve the **same MCP** so **primary** retrieval is DB-backed. Markdown becomes **generated or secondary** for human reading. **Explicit dependency:** This work **extends the MCP built first on Markdown** — same tool contracts where possible, swapping implementation to query the IA database. **Scope:** (1) Parse and ingest spec sections (`isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, etc.) into `spec_sections`. (2) Populate `relationships` (e.g. HeightMap↔Cell.height, PathTerraformPlan→Phase-1→Apply). (3) Populate `invariants` from `invariants.mdc`. (4) Extend tools: `what_do_i_need_to_know(task_description)`, `search_specs(query)`, `dependency_chain(term)`. (5) Script to regenerate `.md` from DB for review. (6) Update `agent-router.mdc` — MCP tools first, Markdown fallback second. **Acceptance:** Agent resolves a multi-spec task (e.g. “bridge over multi-level lake”) via MCP reading ≤ ~500 tokens of context instead of many full-file reads. **Phased MCP tools** (bundles, `backlog_search`, **`unity_context_section`** after **unity-development-context** spec, etc.): see `.cursor/projects/TECH-18.md` and `docs/agent-tooling-verification-priority-tasks.md` (tasks 12–20, 28–32, 35). **Deferred unless reopened:** `findobjectoftype_scan`, `find_symbol` MCP tools (prefer **TECH-26** script).
  - Depends on: none (soft: MCP baseline + IA **Postgres** milestone — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **TECH-15** — New Game / **geography initialization** performance
  - Type: performance / optimization
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, `InterstateManager.cs`, `ForestManager.cs`, `RegionalMapManager.cs`, `ProceduralRiverGenerator.cs` (as applicable)
  - Spec: `.cursor/projects/TECH-15.md`
  - Notes: Reduce wall-clock time and frame spikes when starting a **New Game** (**geography initialization**): **HeightMap**, lakes, procedural **rivers** (shipped — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)), **interstate**, **forests**, **map border** signs, **sorting order** passes, etc. **Priority:** Land the **Editor/batch JSON profiler** under `tools/reports/` (see spec) *before* or in parallel with deep optimization — agents need **measurable** phase breakdowns. **Related:** **Load Game** / **water map** persist work is archived — this issue targets **geography initialization** cost only. **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 3, 22).

- [ ] **TECH-16** — **Simulation tick** performance v2 (per-tick **AUTO systems** pipeline)
  - Type: performance / optimization
  - Files: `SimulationManager.cs`, `TimeManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`, `DemandManager.cs`, `CityStats.cs` (as applicable)
  - Spec: `.cursor/projects/TECH-16.md`
  - Notes: Second-pass optimization of the **simulation tick** after early **Simulation optimization** work (completed). **Priority:** Ship **spec-labeled tick harness** JSON + **ProfilerMarker** names (see spec) so agents and CI can read **AUTO** pipeline cost *before* micro-optimizing allocations. **Related:** **BUG-14** (per-frame UI `FindObjectOfType`); **TECH-01** (manager decomposition may help profiling and hotspots). **Tooling:** `docs/agent-tooling-verification-priority-tasks.md` (tasks 4, 25); drift detection **TECH-29**.

- [ ] **TECH-33** — Asset introspection: **prefab** manifest + scene **MonoBehaviour** listing
  - Type: tooling
  - Files: `tools/` (Unity `-batchmode` or Editor script), `Assets/Prefabs/`, agreed scene path (e.g. `MainScene.unity`)
  - Spec: `.cursor/projects/TECH-33.md`
  - Notes: List prefabs with missing script references; list MonoBehaviour types/paths in scene for **BUG-19** / **toolbar** layout work. `docs/agent-tooling-verification-priority-tasks.md` tasks 26, 27.
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
  - Notes: **Goal:** Make **project-spec-kickoff** and similar workflows cheaper and safer by improving how MCP turns **implementation**-oriented text (project **spec** body, backlog **Files**) into **glossary** matches and **`spec_section`** targets. **Candidate directions:** (1) Path-based tool: input `.cursor/projects/{ISSUE}.md` → ranked **glossary** candidates + suggested **`router_for_task`** **domain** strings + ordered **`spec_section`** queue with **max_chars** budget. (2) Improve **`glossary_discover`** ranking using tokens extracted from **`backlog_issue`** **Files**/**Notes** when `issue_id` is bundled in the same turn. (3) Optional composite read helper (defer if **TECH-18** `search_specs` / bundles subsume). **Does not** replace **`.cursor/skills/project-spec-kickoff/SKILL.md`** prose until tools are **shipped** and **`npm run verify`** green. **Related:** closeout helpers shipped (**`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:*`**, **`project-spec-closeout-parse.ts`**) — trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Acceptance: ≥1 **measurable** improvement merged (new tool **or** clear ranking/UX win on existing tools) + docs updated; **`npm run verify`** green
  - Depends on: none (soft: dogfood with **project-spec-kickoff**; **TECH-18** for long-term search architecture)

- [ ] **TECH-24** — territory-ia MCP: parser regression policy (tests/fixtures when parsers change)
  - Type: tooling / code health
  - Files: `tools/mcp-ia-server/` (tests, fixtures, `scripts/verify-mcp.ts` or equivalent), `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Notes: When changing markdown parsers, fuzzy matching, or glossary ranking, extend **`node:test`** fixtures and keep **`npm run verify`** green (same pattern as **`glossary_discover`** / parser fixtures — see archive). No Unity. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-30** — Validate **BACKLOG** issue IDs referenced in `.cursor/projects/*.md`
  - Type: tooling / doc hygiene
  - Files: `tools/` (Node script), optional `package.json` `npm run` at repo root or under `tools/`
  - Spec: `.cursor/projects/TECH-30.md`
  - Notes: Every `[BUG-XX]` / `[TECH-XX]` / etc. front matter or link in active project specs must exist in `BACKLOG.md` (open rows) or `BACKLOG-ARCHIVE.md` when cited as historical. `docs/agent-tooling-verification-priority-tasks.md` task 9. **Related:** `npm run validate:dead-project-specs` (repo-wide missing `.cursor/projects/*.md` paths) — shipped; coordinate shared **Node** helpers when implementing **TECH-30**.
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
  - Notes: **Observed:** Developer does not see **Export Sorting Debug (Markdown)** or the whole **Reports** submenu, or expects full **Sorting order** data while still in **Edit Mode** / before **`GridManager`** **isInitialized**. **Expected (canonical):** Both **Export Agent Context** and **Export Sorting Debug (Markdown)** appear under **Territory Developer → Reports** whenever `AgentDiagnosticsReportsMenu.cs` compiles in an **Editor** folder assembly. **Sorting** markdown with per-**cell** **`TerrainManager`** breakdowns requires **Play Mode** after **geography initialization** (`GridManager.InitializeGrid`); **Edit Mode** only writes a stub explaining that. **Export Agent Context** (JSON) should still run in **Edit Mode** / **Play Mode** and write under `tools/reports/`. **Related:** agent diagnostics menu shipped — trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Investigate compile errors, wrong scene/package, menu path mismatch, or UX gap (e.g. single combined command, **Console** log on success).
  - Acceptance: On a clean clone, after Unity imports scripts, both menu items are visible; **Sorting** export behavior matches §10; document any platform-specific caveat in the spec **Decision Log** or backlog **Notes**
  - Depends on: none

## UI-as-code program (exploration)

**Charter (§ Completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**):** Reduce **manual Unity Editor** work for **HUD**, **menus**, **panels**, and **toolbars** by making **UI** composable from the **IDE** (Cursor) and **AI agents** — via **reference spec** clarity (**`ui-design-system.md`**), shipped **runtime** **`UiTheme`** + **`UIManager` partials** + prefab **v0**, **Editor** menus (**`unity-development-context.md`** **§10**), **Cursor Skills**, and optional **territory-ia** affordances. **UI** spans **multiple scenes**; **UI** inventory export and spec prose are **per scene**. **As-built baseline:** **`ui-design-system.md`** + committed [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json) — **glossary** **UI design system (reference spec)**. **Codebase inventory (uGUI):** **`ui-design-system.md`** **Related files**. **Ongoing:** refresh **inventory** + baseline JSON when hierarchies shift; optional **`ui_theme_tokens` MCP** — file a new **BACKLOG** row if product wants it.

- [ ] **FEAT-50** — **UI** visual polish: aesthetic refinement (**HUD**, panels, **toolbar**, **MainMenu**)
  - Type: feature / UX polish
  - Files: `Assets/` **UI** prefabs and scenes (`MainMenu.unity`, `MainScene.unity`); `.cursor/specs/ui-design-system.md` (**as-built** vs **Target** alignment); [`docs/ui-visual-polish-exploration-FEAT-50.md`](docs/ui-visual-polish-exploration-FEAT-50.md); `docs/reports/ui-inventory-as-built-baseline.json` (refresh when **color** / **typography** tokens change)
  - Spec: `.cursor/projects/FEAT-50.md`
  - Spec sections: `.cursor/specs/ui-design-system.md` — **§1** **Foundations**, **§2** components, **§3** patterns; `.cursor/specs/unity-development-context.md` **§10** if **Editor** export workflows touch **UI** inventory; `docs/ui-as-built-ui-critique.md` (optional cross-check)
  - Notes: Look-and-feel pass—palette, **typography** rhythm, spacing, icon treatment, contrast, optional motion—**without** changing **simulation** or **grid** rules. Scoped separately from shipped **UI-as-code** infrastructure (**UiTheme**, **`UIManager` partials**, **modal** wiring — **`ui-design-system.md`** **§5.2**); may refine tokens already in **`DefaultUiTheme.asset`** / scenes. Exploration doc captures product/design options before wide **Prefab** edits.
  - Acceptance: per `.cursor/projects/FEAT-50.md` **§8**; adopted **token** changes reflected in **ui-design-system** **as-built** tables and baseline JSON where applicable
  - Depends on: none (soft: **BUG-19** if restyled scrollable **panels** overlap **camera** input)
  - Related: **BUG-19**

## Gameplay & simulation lane

Player-facing **simulation**, **AUTO** growth, **happiness** feedback, and **urban growth rings** / **zone density** depth. Order: **BUG-12** before **FEAT-23** (dependency). **§ High Priority** still holds map/render/save **interrupt** bugs.

- [ ] **FEAT-21** — Expenses and maintenance system
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: No expenses: no **street** maintenance, no service costs, no salaries. Without expenses there is no economic tension. Add upkeep for **streets**, **utility buildings**, and services.

- [ ] **FEAT-22** — **Tax base** feedback on **demand (R / C / I)** and happiness
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: High taxes do not affect **demand (R / C / I)** or happiness. Loop: high taxes → less residential **demand** → less growth → less income.
  - Depends on: none (legacy tax feedback fix — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

- [ ] **BUG-12** — Happiness UI always shows 50%
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness)
  - Spec: `.cursor/projects/BUG-12.md`
  - Notes: `GetHappiness()` returns hardcoded `50.0f` instead of reading `cityStats.happiness`. Blocks FEAT-23 (dynamic happiness).

- [ ] **FEAT-23** — Dynamic happiness based on city conditions
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`
  - Notes: Happiness only increases when placing **zones** (+100 per **building**). No effect from unemployment, **tax base**, services or pollution. Should be continuous multi-factor calculation with decay.
  - Depends on: BUG-12

- [ ] **BUG-52** — **AUTO** zoning: persistent **grass cells** between **undeveloped light zoning** and new **AUTO** **street** segments (gaps not filled on later **simulation ticks**)
  - Type: bug (behavior / regression suspicion)
  - Files: `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `SimulationManager.cs` / `TimeManager.cs` (**tick execution order**, **AUTO systems**), `GrowthBudgetManager.cs` (**growth budget** vs eligibility), `RoadCacheService.cs` (**road cache** / zoneability neighbors), `GridManager.cs` if placement queries change; `TerrainManager.cs` (`RestoreTerrainForCell`) only if investigation ties gap cells to post–street-commit terrain refresh behavior
  - Spec: `.cursor/specs/simulation-system.md` (**simulation tick**, **AUTO** pipeline), `.cursor/specs/managers-reference.md` (**Zones & Buildings**, **Demand**), `.cursor/specs/isometric-geography-system.md` §13.9 (**road reservation** / AUTO interaction) as needed
  - Notes: **Observed:** After **AUTO** places **streets** (path and visuals OK), **AUTO** zoning creates **RCI** **undeveloped light zoning** patches of varying sizes (acceptable), but strips of **grass cells** often remain **Moore**-adjacent to the **road stroke** — typically a **one-cell** buffer between **zoning** and **street**. Those gap **cells** appear to stay unzoned across many later **simulation ticks**, as if permanently ineligible, not merely deferred by **growth budget**. **Expected:** Variable patch sizes are fine; any **grass cell** that remains valid for **AUTO** zoning (per design) should eventually be a candidate on a future **simulation tick** unless explicitly ruled out by documented rules (e.g. corridor reservation). **Regression suspicion:** surfaced after **TerrainManager** path-terraform refresh skipped **building**-occupied **cells**; verify no accidental exclusion of road-adjacent **grass cells** in zone candidate sets, **road cache invalidation**, or neighbor queries. **Related:** **FEAT-36** (AUTO zoning candidate expansion); **FEAT-43** (**growth rings** / weights); **AUTO** road/zoning coordination fixes in archive.
  - Acceptance: Repro in **AUTO** simulation: document coordinates of gap **grass cells**; confirm whether they are excluded from `AutoZoningManager` (or equivalent) forever or until manual action; fix or document intended rule so gaps either fill over time or are explained in spec/backlog.
  - Depends on: none

- [ ] **FEAT-43** — **Urban growth rings**: tune **AUTO** road/zoning weights for a gradual center → edge gradient
  - Type: feature (simulation / balance)
  - Files: `UrbanCentroidService.cs` (**growth ring** boundaries, **urban centroid** distance), `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `GrowthBudgetManager.cs` if per-ring **growth budgets** apply; `GridManager.cs` / `DemandManager.cs` only if **desirability** or placement must align with **growth rings**
  - Notes: **Observed:** In **AUTO** simulation, cities tend toward a **dense core**, **under-developed middle growth rings**, and **outer rings that are more zoned than the middle** — not a smooth radial gradient. **Expected:** Development should fall off **gradually from the urban centroid**: **highest** **street** density and **AUTO** zoning pressure **near the centroid**, **moderate** in **mid growth rings**, and **lowest** in **outer growth rings**. Revisit **growth ring** radii/thresholds, per-ring weights for **AUTO** road growth vs zoning, and any caps or priorities that invert mid vs outer activity. **Related:** earlier **AUTO** road/**desirability**/**zone density** features and perpendicular-stub fixes — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Depends on: none

- [ ] **FEAT-08** — **Zone density** and **desirability** simulation: evolution to larger **buildings**
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing **buildings** evolve to larger versions based on **zone density** and **desirability**. (**TECH-15** / **TECH-16** — performance + harness work — live under **§ Agent ↔ Unity & MCP context lane**.)

## High Priority
- [ ] **BUG-44** — **Cliff** prefabs: black gaps when a **water body** (**river** or **lake**) meets the **east** or **south** **map border**
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `PlaceCliffWallStack`, **map border** / max-X / max-Y edge cases vs **open water** cells), `WaterManager.cs` / `WaterMap.cs` if edge water placement interacts with **shore refresh**; **cliff** / **water-shore** prefabs under `Assets/Prefabs/` (per `.cursor/rules/coding-conventions.mdc` for new or adjusted assets)
  - Spec: `.cursor/specs/isometric-geography-system.md` (**map border**, water, **cliffs**, **sorting order** — sections covering shore/**cliff** stacks at boundaries)
  - Notes: **Observed:** Where a **river** channel or **lake** reaches the **east** or **south** **map border**, the **cliff** geometry that seals the edge is **missing or too short** under the water tiles, exposing **black void**; **grass cells** on the same **map border** still show correct **cliff** faces. Suggests **map border** **cliff** stacks or prefab variants do not account for **lower river bed** (`H_bed`) elevation at those edges. **Expected:** Continuous **cliff** wall to the same depth as neighboring land **cliffs**, or dedicated **map border** + water prefabs so no holes at east/south × water. **Related:** prior shore/**cliff** merge work may share root cause — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
  - Depends on: none

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
  - Notes: **Observed:** While drawing a **street**, **preview mode** visually **extends the road stroke one cell at a time**, like an animation, instead of updating the full proposed **road stroke** in one step. **Expected:** **No** step-by-step or staggered preview animation. The game should **compute the full valid road stroke** (same rules as commit / **road validation pipeline** / `TryPrepareRoadPlacementPlan` or equivalent) for the current **stroke**, **then** instantiate or refresh **preview** prefabs for that complete **road stroke** in a single update — or batch updates without visible per-cell delay. **Related:** street commit vs terrain refresh fixes in archive — keep preview/commit paths consistent.
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
  - Notes: **Observed:** The procedural minimap **does not refresh** as the city changes unless the player **toggles a minimap layer** (or other actions that call `RebuildTexture`, such as opening the panel). **Expected:** The minimap should track **zones**, **streets**, **open water**, **forests**, etc. **without** requiring layer toggles. **Implementation:** Rebuild at least **once per simulation tick** while the minimap is visible, **or** a **performance-balanced** approach (throttled full rebuild, dirty rect / incremental update, or event-driven refresh when grid/**zone**/**street**/**water body** data changes) — profile full `RebuildTexture` cost first (see project spec; measurement tooling **task 8** in `docs/agent-tooling-verification-priority-tasks.md`). Class summary in code states rebuilds on **geography initialization** completion, grid restore, panel open, and layer changes **not** on a fixed timer — that gap is this bug. **Related:** water layer alignment shipped in archive; **FEAT-42** (optional **HeightMap** layer).
  - Depends on: none

- [ ] **FEAT-36** — Expand **AUTO** zoning and **AUTO** road candidates to include **forests** and cells meeting **land slope eligibility**
  - Type: feature
  - Files: `GridManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`
  - Notes: Treat **grass cells**, **forest (coverage)** cells, and cardinal-ramp **slopes** (per **land slope eligibility**) as valid candidates for **AUTO** zoning and **AUTO** road expansion. Capture any design notes in this issue or in `.cursor/specs/isometric-geography-system.md` if rules become stable.

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

## Code Health (technical debt)

- [ ] **TECH-13** — Remove obsolete **urbanization proposal** system (dead code, UI, models)
  - Type: refactor (cleanup)
  - Files: `UrbanizationProposalManager.cs`, `ProposalUIController.cs`, `UrbanizationProposal.cs` (and related), `SimulationManager.cs`, `UIManager.cs`, scene references, **save data** if any
  - Spec: `.cursor/projects/TECH-13.md`
  - Notes: The **urbanization proposal** feature is **obsolete** and intentionally **disabled**; the game is stable without it. **Keep** `UrbanizationProposalManager` disconnected from the simulation — do **not** re-enable proposals. **Keep** `UrbanCentroidService` / **urban growth rings** for **AUTO** roads and zoning. This issue tracks **full removal** of proposal-specific code and UI after a safe audit (no **save data** breakage). Supersedes older proposal bugs — see archive.

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


*(Umbrella programs (**spec-pipeline**, **JSON**/**Postgres** interchange, **compute-lib**, **Cursor Skills**) and **Editor export registry** are archived under [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) with **glossary** pointers. Open **Agent** lane rows are listed in **§ Agent ↔ Unity & MCP context lane**; **§ Gameplay & simulation lane** lists **BUG-12**, **FEAT-23**, **BUG-52**, **FEAT-43**, **FEAT-08**; **§ Compute-lib program** above holds **TECH-38** + **TECH-32**/**TECH-35**; **TECH-39** **MCP** suite is archived.)*

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
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `ForestManager.cs`, `UIManager.cs` (or dedicated **Editor** / in-game **wizard**); **JSON** / **ScriptableObject** templates (align **glossary** **Interchange JSON** + **Compute-lib program** / **territory-compute-lib (TECH-37)**)
  - Notes: In-game or **Editor** flow to author **city** / **territory** **maps** with **isometric** terrain controls: **map** size, **water** / **forest** / **height** mix, **sea** / **river** / **lake** proportions, etc. Reuse the same parameter pipeline for future **player** **terraform**, **basin** / **elevation** tools, **water body** placement in **depressions**, and **AUTO** **geography**-driven tools. **Spec:** canonical **geography initialization** + **water-terrain** + **geo** when implemented (no `.cursor/projects/` spec until scheduled).
  - Depends on: none (coordinates **FEAT-18**, **FEAT-41**; soft: **glossary** **Compute-lib program** / **JSON program (TECH-21)** docs)

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

*(Program history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Open lanes: **§ Compute-lib program**, **§ Agent ↔ Unity & MCP context lane**, then standard priority sections.)*

- [ ] **AUDIO-01** — Audio FX: demolition, placement, **zoning**, **forest (coverage)**, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and **height** (**HeightMap**) over the map.

---

## How to Use This Backlog

1. **Work on an issue**: Open chat in Cursor, reference `@BACKLOG.md` and request analysis or implementation of the issue by ID (e.g. "Analyze BUG-01 and propose a plan").
2. **Reprioritize**: Move the issue up or down within its section, or change section.
3. **Add new issue**: Assign the next available ID in the appropriate category and place in the correct priority section.
4. **Complete issue**: Remove the row from **BACKLOG.md** and append a **`[x]`** row with date to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) only (there is **no** “Completed” section in **BACKLOG.md**). After closure, **strip citations to that issue id** from durable docs (glossary, reference specs, rules, skills, `docs/`, code comments) per **project-spec-close** — **BACKLOG.md** (open rows), **BACKLOG-ARCHIVE.md**, and the new archived row may still name the id.
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
1. **Compute-lib program** (**TECH-38** open; **TECH-39** archived; pilot **compute-lib** in archive; related **TECH-32**, **TECH-35**; charter — **glossary** **Compute-lib program** / [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))
2. **Agent ↔ Unity & MCP context lane** (Unity exports, MCP, CI, performance harnesses, adjacent tooling)
3. In progress (actively being developed — insert above **High priority** when used)
4. High priority (critical bugs, core gameplay blockers)
5. Medium priority (important features, balance, improvements)
6. Code Health (technical debt, refactors, performance)
7. Low priority (new systems, polish, content)
8. **Archive** — completed work lives only in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)
