# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.
>
> **Agent ↔ Unity context (2026-04-02):** Work that improves **territory-ia** / MCP understanding of **Unity**, **Unity → workspace JSON** (and other machine-readable exports), **structured logs / harness output**, and **scene–prefab** introspection for IDE agents is **prioritized ahead** of unrelated refactors. The ordered lane is **§ Agent ↔ Unity & MCP context lane** below. Gameplay blockers in **§ High Priority** still take precedence when they stop play or corrupt saves.

---

## Agent ↔ Unity & MCP context lane (highest priority)

Ordered for **MCP Unity context** → **JSON / reports from Unity** → **MCP platform** → **agent workflow & CI helpers** → **research tooling**.

- [ ] **TECH-21** — **JSON program** (umbrella): interchange, schemas, runtime payloads, future backend shapes
  - Type: technical / data interchange
  - Files: umbrella only — see **TECH-40**, **TECH-41**, **TECH-42**; charter `.cursor/projects/TECH-21.md`; [`projects/TECH-21-json-use-cases-brainstorm.md`](projects/TECH-21-json-use-cases-brainstorm.md); reference `.cursor/specs/persistence-system.md`, `docs/planned-domain-ideas.md`
  - Spec: `.cursor/projects/TECH-21.md`
  - Notes: **Program charter** for **JSON** DTOs, **CI** validation, **spec/glossary** machine indexes (no full-spec duplication — **TECH-18**), **Geography initialization** / harness payloads (**TECH-15**, **TECH-38** Wave D, **TECH-39** `geography_init_params_validate`), and **TECH-19**-ready row+blob patterns. **Phased delivery:** **TECH-40** (infra + indexes + schema policy), **TECH-41** (current game/tooling payloads + **parse-once** / catalogs), **TECH-42** (future domains + DB/API envelopes + streaming guidance). **Related:** **TECH-36** program (**compute-lib** shares Zod/schemas with MCP), **FEAT-46**–**FEAT-48** (planned parameters in [`docs/planned-domain-ideas.md`](docs/planned-domain-ideas.md)), **TECH-16** tick harness JSON, **FEAT-37c** (**Load pipeline** — completed, [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)).
  - Acceptance: **TECH-40**, **TECH-41**, and **TECH-42** each satisfy their own **Acceptance** lines in this file (program **complete** when all three **complete**); player **Save data** never regresses without an explicit migration issue
  - Depends on: none (child order **TECH-40** → **TECH-41** → **TECH-42**)

- [ ] **TECH-36** — **Computational program** (umbrella): **geometry**, **stochastics**, **algorithms** + **territory-ia** tools
  - Type: tooling / code health / agent enablement
  - Files: umbrella only — see **TECH-37**, **TECH-38**, **TECH-39**; charter `.cursor/projects/TECH-36.md`; reference specs: `.cursor/specs/isometric-geography-system.md`, `.cursor/specs/simulation-system.md`, `.cursor/specs/managers-reference.md`
  - Spec: `.cursor/projects/TECH-36.md`
  - Notes: **Program charter** with resolved product/tooling decisions. **Phased delivery:** **TECH-37** (**`tools/compute-lib/`** + pilot **`registerTool`**), **TECH-38** (Unity **pure** **compute** + **`tools/`** harnesses), **TECH-39** (computational **MCP** suite). **Related:** **TECH-21** program (**TECH-40**–**TECH-42** — JSON DTOs/schemas), **TECH-28** (completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)), **TECH-32**, **TECH-35**; product follow-ups **FEAT-46** (geography authoring UI), **FEAT-47** (**multipolar** **urban growth rings**), **FEAT-48** (**water body** volume / **surface height (S)**).
  - Acceptance: **TECH-37**, **TECH-38**, and **TECH-39** each satisfy their own **Acceptance** lines in this file (program **complete** when all three **complete**)
  - Depends on: none (child issues **TECH-37** → **TECH-38** → **TECH-39** track implementation order)

- [ ] **TECH-37** — **Computational** infra: **`tools/compute-lib/`** + pilot **MCP** tool (**World ↔ Grid**)
  - Type: tooling
  - Files: `tools/compute-lib/` (new package); `tools/mcp-ia-server/`; `Assets/Scripts/Utilities/Compute/` (convention **README** / optional stub); `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Spec: `.cursor/projects/TECH-37.md`
  - Notes: **Phase A** of **TECH-36**. Shared **Zod**/**TS** **schemas**; **`isometric_world_to_grid`** (or equivalent) **`registerTool`**; **golden** tests **Node** ↔ Unity export. **Architectural rule:** **C#** authoritative for **grid** truth; **compute-lib** only for **verified** **pure** math.
  - Acceptance: **`tools/compute-lib`** tests pass; **MCP** **`npm run verify`** green with pilot tool; docs updated per `.cursor/projects/TECH-37.md` §8
  - Depends on: none (soft: **TECH-21**)

- [ ] **TECH-38** — **Core** **computational** modules (Unity **utilities** + **`tools/`** harnesses)
  - Type: code health / performance enablement
  - Files: `Assets/Scripts/Utilities/Compute/`; `GridManager.cs` (**CoordinateConversionService**), `GridPathfinder.cs`, `UrbanCentroidService.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterManager.cs`, `DemandManager.cs` / `CityStats.cs` (as extractions land); `tools/reports/`; **UTF** tests
  - Spec: `.cursor/projects/TECH-38.md`
  - Notes: **Phase B** of **TECH-36**. **Behavior-preserving** extractions; **UrbanGrowthRingMath** **multipolar**-ready for **FEAT-47**; **stochastic** **geography initialization** documentation; **no** second **pathfinding** authority. Prepare **batchmode** hooks for **TECH-39**.
  - Acceptance: inventory doc + **≥ 3** **pure** modules with tests or **golden** **JSON**; **RNG** derivation doc; **invariants** respected — see `.cursor/projects/TECH-38.md` §8
  - Depends on: **TECH-37**

- [ ] **TECH-39** — **territory-ia** **computational** **MCP** tool suite
  - Type: tooling / agent enablement
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/src/` (compute handlers); `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Spec: `.cursor/projects/TECH-39.md`
  - Notes: **Phase C** of **TECH-36**. **`growth_ring_classify`**, **`grid_distance`**, **`pathfinding_cost_preview`**, **`geography_init_params_validate`**, **`desirability_top_cells`** (honest **NOT_AVAILABLE** until **TECH-38** **batchmode**); **many** **`snake_case`** tools, shared **compute-lib** core.
  - Acceptance: **≥ 4** new tools beyond **TECH-37** pilot (or **Decision Log** consolidation); **`npm run verify`** green; docs updated — see `.cursor/projects/TECH-39.md` §8
  - Depends on: **TECH-37** (soft: **TECH-38** for **heavy** tools)


- [ ] **TECH-40** — **JSON** infra: artifact identity, schemas, **CI** validation, **spec** + **glossary** indexes
  - Type: tooling / data interchange
  - Files: `docs/schemas/` (pilot schema + fixtures); repo root `package.json` (delegates `validate:fixtures` / `generate:ia-indexes`); `tools/mcp-ia-server/scripts/validate-fixtures.ts`, `generate-ia-indexes.ts`, `data/spec-index.json`, `data/glossary-index.json`; `.github/workflows/ia-tools.yml`; `projects/TECH-21-json-use-cases-brainstorm.md` (policy §); `docs/mcp-ia-server.md`
  - Spec: `.cursor/projects/TECH-40.md`
  - Notes: **Phase A** of **TECH-21**. Defines **`artifact` / `kind`** naming + when to use in-file **`schema_version`** vs schema **`$id` / filename** only; JSON Schema + **`npm run validate:fixtures`** (or equivalent); **I1** spec index manifest; **I2** glossary→anchor index; **P3** = validate in **CI**, not in player hot paths. **Related:** **TECH-24** (parser regression), **TECH-30** (issue id validation in specs), **TECH-34** (generated JSON pattern).
  - Acceptance: documented versioning policy + ≥1 checked-in schema + CI validation green; **I1** + **I2** generated artifacts or scripts merged per `.cursor/projects/TECH-40.md` §8
  - Depends on: none (soft: align with **TECH-37** Zod naming when touching **compute-lib**)

- [ ] **TECH-41** — **JSON** payloads for **current** systems: **geography** params, **cell**/**chunk** interchange, snapshots, DTO layers
  - Type: technical / performance enablement
  - Files: `StreamingAssets/` or `Assets/.../Resources/` (if adopted), `Assets/Scripts/` (DTOs, one-shot load), `tools/reports/`, **TECH-38** harness outputs; **TECH-16** profiler JSON shapes (if schema-owned here); `.cursor/specs/persistence-system.md` when **CellData**-shaped tooling touches **Save data** semantics
  - Spec: `.cursor/projects/TECH-41.md`
  - Notes: **Phase B** of **TECH-21**. **G4** **Geography initialization** parameter files (**FEAT-46**-aligned knobs as they exist today); **G2** single-**cell** / **chunk** JSON for tools (**HeightMap** / **Cell** fields consistent with glossary); **G1** read-only world snapshots (Editor/dev); **E3** documented layering (**MonoBehaviour** ↔ interchange DTO ↔ **CellData**); **P1** parse-once at boot / **Load pipeline** / init boundaries; **P2** static **catalog** arrays (not per **simulation tick**); **P4** optional `by_id` for hot static catalogs. Coordinate **TECH-39** validation tool inputs.
  - Acceptance: at least one **runtime or Editor** JSON load path + one **export** path documented with glossary-aligned fields; **no** unintentional **Save data** format change — see `.cursor/projects/TECH-41.md` §8
  - Depends on: **TECH-40**

- [ ] **TECH-42** — **JSON** for **future** domains: DB shapes, API envelopes, large-document **streaming**
  - Type: technical / architecture (future-facing)
  - Files: `.cursor/projects/TECH-19.md` (cross-links only until DB lands); `docs/planned-domain-ideas.md` (parameter evolution); optional design appendix under `docs/`
  - Spec: `.cursor/projects/TECH-42.md`
  - Notes: **Phase C** of **TECH-21**. **B1** row + JSONB column pattern; **B3** idempotent **patch** envelope as **API contract** (not one fixed table); **P5** when **Load pipeline** or exports need incremental readers; links **FEAT-47**/**FEAT-48** data needs without implementing them. **B2** tracked separately (**TECH-43** — backlog only, no project spec).
  - Acceptance: written patterns merged per `.cursor/projects/TECH-42.md` §8; **TECH-19** field naming compatible where applicable
  - Depends on: **TECH-41** (soft: **TECH-40** for shared schema policy)

- [ ] **TECH-43** — Append-only **JSON** line **event log** (telemetry / sim anomalies) — **backlog placeholder**
  - Type: technical / observability (future)
  - Files: TBD (`tools/`, optional **Postgres** table, ship pipeline)
  - Spec: none (promote to `.cursor/projects/TECH-43.md` when scheduled)
  - Notes: Idea from **TECH-21** brainstorm **B2**; **schema_version** per line; same validator family as **TECH-40** when implemented. **Does not** start until **TECH-40** schema pipeline exists.
  - Acceptance: issue refined with concrete consumer + storage choice; optional schema + sample sink
  - Depends on: **TECH-40** (soft)

- [ ] **TECH-19** — Game PostgreSQL database; first milestone — IA schema for MCP + basic tools
  - Type: infrastructure / tooling
  - Files: new project outside `Assets/Scripts/` (PostgreSQL schema, migrations, optional small service or MCP-adjacent module); seed scripts as needed
  - Spec: `.cursor/projects/TECH-19.md`
  - Notes: **Goal:** Introduce a **game-owned** PostgreSQL database (long-term: not only AI — analytics, metagame, ops, etc.; document intended product uses as they land). **First concrete milestone:** tables and migrations for **Information Architecture** data that MCP will eventually query: e.g. `glossary` (term, conceptual_def, technical_def, spec_address, section, category), `spec_sections` (spec_abbrev, section_id, title, content, parent_section), `invariants`, `relationships` (term_a, relation, term_b) — adjust names/types to match implementation. Ship a **minimal** programmatic surface (SQL views, repo functions, or thin API) plus a **basic** tool set (same *families* as **TECH-17**, but **wired to Postgres** where applicable) to prove read paths. **Optional:** seed a small subset from `.cursor/specs/glossary.md` to validate the pipeline. **Does not** ingest full specs or replace Markdown as source of truth — that is **TECH-18**. **Stack:** PostgreSQL (psql / DBeaver compatible), migrations (tool of choice).
  - Depends on: none

- [ ] **TECH-18** — Migrate Information Architecture from Markdown to PostgreSQL (MCP evolution)
  - Type: infrastructure / tooling
  - Files: All `.cursor/specs/*.md`, `.cursor/rules/agent-router.mdc`, `.cursor/rules/invariants.mdc`, `ARCHITECTURE.md`; MCP server from **TECH-17** (initially **file-backed**); schema / migrations / seed from **TECH-19**; `tools/mcp-ia-server/src/index.ts`, `docs/mcp-ia-server.md`
  - Spec: `.cursor/projects/TECH-18.md`
  - Notes: **Goal:** After **TECH-17** (MCP over **`.md` / `.mdc`**) and **TECH-19** (Postgres + IA tables), **migrate authoritative IA content** into PostgreSQL and evolve the **same MCP** so **primary** retrieval is DB-backed. Markdown becomes **generated or secondary** for human reading. **Explicit dependency:** This work **extends the MCP built first on Markdown** in **TECH-17** — same tool contracts where possible, swapping implementation to query **TECH-19**’s database. **Scope:** (1) Parse and ingest spec sections (`isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, etc.) into `spec_sections`. (2) Populate `relationships` (e.g. HeightMap↔Cell.height, PathTerraformPlan→Phase-1→Apply). (3) Populate `invariants` from `invariants.mdc`. (4) Extend tools: `what_do_i_need_to_know(task_description)`, `search_specs(query)`, `dependency_chain(term)`. (5) Script to regenerate `.md` from DB for review. (6) Update `agent-router.mdc` — MCP tools first, Markdown fallback second. **Acceptance:** Agent resolves a multi-spec task (e.g. “bridge over multi-level lake”) via MCP reading ≤ ~500 tokens of context instead of many full-file reads. **Phased MCP tools** (bundles, `backlog_search`, **`unity_context_section` after TECH-20** doc, etc.): see `.cursor/projects/TECH-18.md` and `docs/agent-tooling-verification-priority-tasks.md` (tasks 12–20, 28–32, 35). **Deferred unless reopened:** `findobjectoftype_scan`, `find_symbol` MCP tools (prefer **TECH-26** script).
  - Depends on: **TECH-19** (**TECH-17** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))

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
  - Notes: **Goal:** Make **project-spec-kickoff** and similar workflows cheaper and safer by improving how MCP turns **implementation**-oriented text (project **spec** body, backlog **Files**) into **glossary** matches and **`spec_section`** targets. **Candidate directions:** (1) Path-based tool: input `.cursor/projects/{ISSUE}.md` → ranked **glossary** candidates + suggested **`router_for_task`** **domain** strings + ordered **`spec_section`** queue with **max_chars** budget. (2) Improve **`glossary_discover`** ranking using tokens extracted from **`backlog_issue`** **Files**/**Notes** when `issue_id` is bundled in the same turn. (3) Optional composite read helper (defer if **TECH-18** `search_specs` / bundles subsume). **Does not** replace **`.cursor/skills/project-spec-kickoff/SKILL.md`** prose until tools are **shipped** and **`npm run verify`** green.
  - Acceptance: ≥1 **measurable** improvement merged (new tool **or** clear ranking/UX win on existing tools) + docs updated; **`npm run verify`** green
  - Depends on: none (soft: dogfood with **project-spec-kickoff**; **TECH-18** for long-term search architecture)

- [ ] **TECH-50** — **Doc hygiene:** **cascade** references when **project specs** close; **dead links**; **BACKLOG** as durable anchor
  - Type: tooling / doc hygiene / agent enablement
  - Files: `tools/` (Node or shell script); optional root or `tools/` `package.json` `npm run`; `.cursor/projects/PROJECT-SPEC-STRUCTURE.md`; `AGENTS.md`; optional `tools/mcp-ia-server/` + `docs/mcp-ia-server.md`; `docs/agent-tooling-verification-priority-tasks.md` (task index if applicable)
  - Spec: `.cursor/projects/TECH-50.md`
  - Notes: **Policy:** When a **project spec** (`.cursor/projects/{ISSUE_ID}.md`) is **deleted** after verified completion, **durable** docs (skills, `docs/`, rules, `AGENTS.md`, etc.) must **not** keep **markdown links** or “see spec at …” pointers to that path. The **durable** trace is the **issue** row in **`BACKLOG.md`** (or **`BACKLOG-ARCHIVE.md`**). **Deliverables:** (1) **Script** — scan repo for links/paths to missing `.cursor/projects/*.md` (and optionally stale `Spec:` lines in **BACKLOG**); exit non-zero for **CI** or documented advisory mode. (2) **Optional** **territory-ia** MCP tool — thin wrapper or shared core with the script so agents discover broken references without ad-hoc `rg`. (3) **Docs** — closeout checklist in **PROJECT-SPEC-STRUCTURE** + pointer in **AGENTS.md** (project spec lifecycle). **Coordinate** with **TECH-30** (issue id validation **inside** active `.cursor/projects/*.md`) to avoid duplicate **Node** utilities—prefer one package or shared module if both land.
  - Acceptance: merged script (or integrated into **TECH-30** deliverable with **TECH-50** scope covered) + documented `npm run` (or equivalent); **PROJECT-SPEC-STRUCTURE** / **AGENTS** updated; optional MCP tool documented in **`docs/mcp-ia-server.md`** if shipped; **`npm run verify`** green if MCP code changes
  - Depends on: none (soft: **TECH-30** — merge or share implementation)

- [ ] **TECH-24** — territory-ia MCP: parser regression policy (tests/fixtures when parsers change)
  - Type: tooling / code health
  - Files: `tools/mcp-ia-server/` (tests, fixtures, `scripts/verify-mcp.ts` or equivalent), `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`
  - Notes: When changing markdown parsers, fuzzy matching, or glossary ranking, extend **`node:test`** fixtures and keep **`npm run verify`** green (pattern from **FEAT-45**). No Unity. Source: `projects/agent-friendly-tasks-with-territory-ia-context.md` §4.
  - Depends on: none

- [ ] **TECH-30** — Validate **BACKLOG** issue IDs referenced in `.cursor/projects/*.md`
  - Type: tooling / doc hygiene
  - Files: `tools/` (Node script), optional `package.json` `npm run` at repo root or under `tools/`
  - Spec: `.cursor/projects/TECH-30.md`
  - Notes: Every `[BUG-XX]` / `[TECH-XX]` / etc. front matter or link in active project specs must exist in `BACKLOG.md`. `docs/agent-tooling-verification-priority-tasks.md` task 9. **Related:** **TECH-50** (dead links to **deleted** project specs repo-wide — coordinate implementation).
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

- [ ] **TECH-32** — **Urban growth rings** / centroid recompute what-if (research tooling)
  - Type: tooling / research
  - Files: `tools/` or Unity Editor batch; parameters from **FEAT-43** / **FEAT-36** notes as inputs
  - Spec: `.cursor/projects/TECH-32.md`
  - Notes: Compare full **UrbanCentroidService** recompute every tick vs throttled/approximate strategies; report desync or behavior risk vs glossary **sim §Rings**. Non-player-facing evidence for tuning. `docs/agent-tooling-verification-priority-tasks.md` task 24.
  - Depends on: none (coordinates with **FEAT-43**)

- [ ] **TECH-35** — Research spike: property-based / random mutation **invariant** fuzzing (optional)
  - Type: research / test harness
  - Files: TBD test assembly or `tools/` prototype
  - Spec: `.cursor/projects/TECH-35.md`
  - Notes: High setup cost; only if geometric / ordering bugs justify. Predicates from **invariants** (HeightMap/**cell** sync, **road cache**, **shore band**, etc.). `docs/agent-tooling-verification-priority-tasks.md` task 38. **Non-goals:** production fuzz in player builds.
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

*(Agent–Unity / MCP tooling **TECH-21** program **TECH-40**–**TECH-42**, **TECH-23**–**TECH-39** (including **TECH-36** program **TECH-37**–**TECH-39**), **TECH-43** (placeholder), **TECH-45**–**TECH-47** (**Cursor Skill** pack), **TECH-48** (MCP discovery from project specs), **TECH-50** (project spec closeout / dead link hygiene), **TECH-15**/**TECH-16** performance+harness — listed in **§ Agent ↔ Unity & MCP context lane** above. **Shipped skills:** **project-spec-kickoff**, **project-spec-implement**, `.cursor/skills/README.md` — see **§ Completed**.)*

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
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `ForestManager.cs`, `UIManager.cs` (or dedicated **Editor** / in-game **wizard**); **JSON** / **ScriptableObject** templates (align **TECH-21** program **TECH-41**, **TECH-36** program)
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

*(**TECH-18**, **TECH-19**, **TECH-21** program (**TECH-40**–**TECH-42**, **TECH-43**), **TECH-45**–**TECH-48**, **TECH-50** — listed in **§ Agent ↔ Unity & MCP context lane** above; **TECH-49** completed — **§ Completed**; **TECH-20** / **TECH-25** / **TECH-28** completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**; other recent completions under **§ Completed**.)*

- [ ] **AUDIO-01** — Audio FX: demolition, placement, **zoning**, **forest (coverage)**, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and **height** (**HeightMap**) over the map.

---

## Completed (last 30 days)

- [x] **TECH-44** — **Cursor Skills:** **infrastructure** + **kickoff** skill (project **spec** review / IA alignment) (2026-04-11)
  - Type: documentation / agent enablement (**Cursor Skill** + repo docs — no runtime game code)
  - Files: `.cursor/skills/README.md`; `.cursor/skills/project-spec-kickoff/SKILL.md`; `.cursor/templates/project-spec-review-prompt.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`
  - Spec: (removed after closure — conventions live under **`.cursor/skills/`** and **§4.4** of [`docs/cursor-agents-skills-mcp-study.md`](docs/cursor-agents-skills-mcp-study.md))
  - Notes: **Completed (verified per user):** Part 1 **README** + authoring rules; Part 2 **project-spec-kickoff** **`SKILL.md`** with **Tool recipe (territory-ia)** (`backlog_issue` → `invariants_summary` → `router_for_task` → …); paste template; **AGENTS.md** item 5 + doc hierarchy pointer; study doc **§4.4**. **Lesson (persisted in README):** **`router_for_task`** `domain` strings should match **`.cursor/rules/agent-router.mdc`** task-domain row labels (e.g. `Save / load`), not ad-hoc phrases. **Follow-up:** **TECH-48** (MCP discovery), **TECH-45**–**TECH-47** (domain skills).
  - Depends on: none

- [x] **TECH-49** — **Cursor Skill:** **implement** a **project spec** (execution workflow after kickoff) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `.cursor/skills/project-spec-implement/SKILL.md`; `.cursor/skills/README.md`; `.cursor/skills/project-spec-kickoff/SKILL.md` (cross-link); `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`; `docs/mcp-ia-server.md`; `.cursor/templates/project-spec-review-prompt.md`
  - Spec: (removed after closure — workflow in **`.cursor/skills/project-spec-implement/SKILL.md`**; closure record in this row)
  - Notes: **Completed (verified per user request to implement):** **project-spec-implement** **`SKILL.md`** with **Tool recipe (territory-ia)** (per-phase loop, **Branching**, **Seed prompt**, **unity-development-context** §10 pointer); README index row; **AGENTS.md** project-spec bullets + doc hierarchy; study doc **§4.4**; **`docs/mcp-ia-server.md`** “Project spec workflows”; paste template “After review: implement”. **Dry-run:** Meta — authoring followed the recipe while implementing this issue.
  - Depends on: none (soft: **TECH-44**)

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
1. In progress (actively being developed)
2. High priority (critical bugs, core gameplay blockers)
3. Medium priority (important features, balance, improvements)
4. Code Health (technical debt, refactors, performance)
5. Low priority (new systems, polish, content)
6. Completed (last 30 days)
