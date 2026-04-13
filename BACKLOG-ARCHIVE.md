# Backlog Archive — Territory Developer

> Completed issues archived from `BACKLOG.md`. A **2026-04-04** batch holds the former **Completed** slice from `BACKLOG.md`; the **Recent archive** block holds items moved on **2026-04-10**. Older completions follow under **Pre-2026-03-22 archive**.

---

## Completed (moved from BACKLOG.md, 2026-04-13)

- [x] **TECH-90** — Extract `Cell` abstract base type (coord, height, shared primitives) (2026-04-13)
  - Type: refactor / infrastructure
  - Files: `Assets/Scripts/Managers/UnitManagers/CellBase.cs` (new), `Assets/Scripts/Managers/UnitManagers/Cell.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 1 of cell-type split. Abstract `CellBase : MonoBehaviour` extracted under `Territory.Core` carrying scale-universal primitives only (`x`, `y`, `height`, `sortingOrder`, `transformPosition`). `Cell : CellBase`; all city-specific fields (roads, buildings, zones, forests, water, cliffs, interstate, desirability) stay on `Cell`. Compile-only; zero caller edits; rename `Cell` → `CityCell` deferred to TECH-91. Invariant #1 (`HeightMap` ↔ `Cell.height`) unaffected — field inheritance preserves dual-write syntax. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: `CellBase.cs` exists w/ 5 fields only; `Cell : CellBase`; 5 fields removed from `Cell.cs`; `npm run unity:compile-check` green; `npm run validate:all` green; no caller edits outside the two files
  - Depends on: **TECH-89**

- [x] **TECH-89** — Parent-id round-trip + legacy-migration tests (testmode) (2026-04-13)
  - Type: test / verification
  - Files: `Assets/Scripts/Editor/AgentTestModeBatchRunner.cs` (DTO `schema_version` 1 → 2 + `regionId` / `countryId` fields + `IdMatches` sentinel helper), `tools/fixtures/scenarios/parent-id-seeded-32x32/` (save + golden), `tools/fixtures/scenarios/parent-id-legacy-32x32/` (save + golden), `tools/fixtures/scenarios/reference-flat-32x32/agent-testmode-golden-ticks{0,3}.json` (regen)
  - Spec: (removed after closure)
  - Notes: Two testmode scenarios + golden-snapshot extension assert parent region id / parent country id persist through Load pipeline. Seeded modern fixture (schema v1 + committed GUIDs) → load → golden asserts `GridManager.ParentRegionId` / `.ParentCountryId` equal seeded values. Legacy fixture (schema 0, ids absent) → load → `MigrateLoadedSaveData` allocates placeholder GUIDs → `IdMatches(goldenValue, runtimeValue)` accepts `"<guid>"` sentinel iff `Guid.TryParseExact` succeeds. Existing reference-flat-32x32 goldens regenerated for DTO bump. Closes Stage 1.1 verification. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Step 1 / Stage 1.1.
  - Acceptance: testmode scenarios green (seeded + legacy + regenerated reference); fixtures committed; `npm run validate:all` + `unity:compile-check` green
  - Depends on: **TECH-87**

## Completed (moved from BACKLOG.md, 2026-04-12)

- [x] **TECH-87** — Parent-scale identity fields on `GameSaveData` + save migration (2026-04-12)
  - Type: infrastructure / save
  - Files: `Assets/Scripts/SaveSystem/GameSaveData.cs`, `Assets/Scripts/SaveSystem/SaveManager.cs` (version bump + migration path), `ia/specs/save-system.md` (§schema), `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Added non-null `region_id` + `country_id` (GUID) to `GameSaveData`. Bumped save version. Legacy saves load w/ placeholder GUIDs. Glossary rows landed for **parent region id** + **parent country id**. No runtime behavior change beyond ids being present. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Step 1 / Stage 1.1.
  - Acceptance: fields serialize + deserialize round-trip; legacy save loads w/ placeholder ids; save version bumped; glossary rows land; `npm run validate:all` green
  - Depends on: none

## Completed (moved from BACKLOG.md, 2026-04-11)

- [x] **TECH-85** — IA migration to neutral `ia/` namespace + native Claude Code layer (2026-04-11)
  - Type: tooling / IA infrastructure / agent enablement
  - Files: `ia/{specs,rules,skills,projects,templates}`; `tools/mcp-ia-server/src/config.ts`; `tools/mcp-ia-server/src/tools/{router-for-task,project-spec-journal,project-spec-closeout-digest,glossary-lookup,unity-callers-of,unity-subscribers-of,csharp-class-summary}.ts`; `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`; `tools/mcp-ia-server/scripts/generate-ia-indexes.ts`; `tools/validate-dead-project-spec-paths.mjs`; `.claude/{settings.json,skills/,agents/,output-styles/,commands/,memory/}`; `tools/scripts/claude-hooks/`; `MEMORY.md`; densification pass over `docs/`, `AGENTS.md`, `CLAUDE.md`, `ARCHITECTURE.md`
  - Spec: (removed after closure — glossary rows **Code intelligence MCP tools**, **Glossary graph**, extended **IA index manifest** to I3)
  - Notes: Five stages shipped by fresh agents against a stage/phase execution model. Stage 1 — bootstrap Claude Code layer (`.claude/settings.json` with `acceptEdits` + `mcp__territory-ia__*` wildcard, 4 hooks, 5 slash command stubs, `MEMORY.md` seed, `project-stage-close` skill). Stage 2 — structural move `.cursor/{specs,rules,skills,projects,templates}` → `ia/...`, cross-extension `.md → .md` symlinks for back-compat, MCP server path constants, validator symlink-awareness. Stage 3 — four-field IA frontmatter on 74 files, `validate:frontmatter` validator, verification policy consolidated to `docs/agent-led-verification-policy.md` (single canonical source), `AGENTS.md` / `BACKLOG.md` / `CLAUDE.md` / `docs/information-architecture-overview.md` densified. Stage 4 — 5 native subagents (`spec-kickoff`, `spec-implementer`, `verifier`, `test-mode-loop`, `closeout`; Opus orchestrators + Sonnet executors), 5 real slash commands, 2 output styles (`verification-report`, `closeout-digest` — JSON header + caveman summary), caveman directive enforced at 4 layers (16 path grep gate). Stage 5 — 3 new code-intelligence MCP tools (`unity_callers_of`, `unity_subscribers_of`, `csharp_class_summary`) + `glossary_lookup` extended to graph shape (`related`, `cited_in`, `appears_in_code`), precomputed `glossary-graph-index.json` (I3 companion to I1 / I2). Cursor remains a first-class consumer throughout via back-compat symlinks. Canonical stances locked: `permissions.defaultMode: "acceptEdits"` (discovered after in-vivo chicken-and-egg friction with default mode), `mcp__territory-ia__*` wildcard (vs per-tool list), 4-layer caveman directive (subagent body + skill preamble + slash command body + stage-close handoff template), subagent `tools` field as explicit per-subagent allow-list (not wildcard).
  - Acceptance: `ia/` populated with frontmatter (76 files); back-compat symlinks resolve via cross-extension `.md → .md`; MCP server reads from `ia/`; `npm run validate:all` + `npm run verify:local` green end-to-end; 5 subagents + 5 slash commands + 4 hooks + 2 output styles operative under `.claude/`; 3 new MCP tools (`unity_callers_of`, `unity_subscribers_of`, `csharp_class_summary`) registered; `glossary_lookup` returns `{term, definition, related, cited_in, appears_in_code}`; verification policy consolidated; caveman directive present on 16 paths
  - Depends on: none

## Completed (moved from BACKLOG.md, 2026-04-09)

- [x] **FEAT-22** — **Tax base** feedback on **demand (R / C / I)** and happiness (2026-04-09)
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`, `EmploymentManager.cs`, `UIManager.Theme.cs`, `UIManager.Hud.cs`, `UIManager.Toolbar.cs`
  - Spec: (removed after closure — **glossary** **Tax base**, **Demand (R / C / I)**, **Happiness**; **managers-reference** **Demand (R / C / I)**; **simulation-system** daily pass note; this row)
  - Notes: **Hybrid model:** **per-sector** tax scaling on R/C/I **demand** plus **happiness**-**target** multiplier; **highest** (not average) **tax** rate vs comfort band for **happiness**; same-day **demand** refresh after **happiness** in `PerformDailyUpdates`; **tax** UI calls `RefreshHappinessAfterPolicyChange()`. Tunable weights on **`CityStats`** / **`DemandManager`**. Grid debug **HUD** chrome: **ScrollRect** for long copy; square panel aligned between **DataPanelButtons** and **ControlPanel**.
  - Depends on: none (happiness + **monthly maintenance** shipped — **glossary** / archive)

## Completed (moved from BACKLOG.md, 2026-04-08)

- [x] **FEAT-21** — Expenses and maintenance system (2026-04-08)
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `GrowthBudgetManager.cs`
  - Spec: (removed after closure — **glossary** **Monthly maintenance**; **managers-reference** §Demand; **simulation-system** **Calendar and monthly economy**; this row)
  - Notes: **Monthly maintenance** after **tax base** on calendar day 1; **street** cost from `roadCount`, **power plant** cost from `GetRegisteredPowerPlantCount()`; `SpendMoney` uses `RemoveMoney`; HUD / growth budget use net projected cash flow. Optional **TECH-82** **city events** audit trail still open.
  - Depends on: none (happiness system shipped — see **FEAT-23** below)

## Completed (moved from BACKLOG.md, 2026-04-07)

- [x] **FEAT-23** — Dynamic happiness based on city conditions (2026-04-07)
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`, `CityStatsUIController.cs`, `UIManager.Hud.cs`, `AgentBridgeCommandRunner.cs`
  - Notes: Replaced unbounded `int` happiness accumulator with normalized 0–100 float score recalculated each simulation tick from 6 weighted factors (employment, tax burden, service coverage stub, forest bonus, development base, pollution penalty). Convergence rate scales with population. Introduced foundational city-wide **pollution** model (industrial buildings + power plants − forest absorption). Happiness feeds back into **demand (R / C / I)** via multiplier in `DemandManager`. Old saves clamp happiness to 0–100 on load. Migrated: **glossary** (Happiness, Pollution), **mgrs** §Demand + §World, **ARCHITECTURE.md** dependency table.

---

## Completed (moved from BACKLOG.md, 2026-04-04)

- [x] **TECH-36** — **Computational program** (umbrella; charter closed) (2026-04-04)
  - Type: tooling / code health / agent enablement
  - Files: umbrella only — **glossary** **Compute-lib program**; pilot **`tools/compute-lib/`** + **TECH-37**; **TECH-39** **MCP** suite; [`ARCHITECTURE.md`](ARCHITECTURE.md) **Compute** row; `ia/specs/isometric-geography-system.md`, `ia/specs/simulation-system.md`, `ia/specs/managers-reference.md`
  - Spec: (removed after closure — **glossary** **Compute-lib program**; **TECH-37**/**TECH-39** rows below; open **C#** / **research** follow-ups remain on [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program** — **TECH-38**, **TECH-32**, **TECH-35**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella retired from open **BACKLOG**; **TECH-38** no longer gates closure. **Authority** and **tooling** trace: **glossary** **Compute-lib program**, **territory-compute-lib (TECH-37)**, **C# compute utilities (TECH-38)**, **Computational MCP tools (TECH-39)**.
  - Depends on: none

- [x] **TECH-37** — **Computational** infra: **`tools/compute-lib/`** + pilot **MCP** tool (**World ↔ Grid**) (2026-04-04)
  - Type: tooling
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/`; `Assets/Scripts/Utilities/Compute/`; `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`; [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml)
  - Spec: (removed after closure — **glossary** **territory-compute-lib (TECH-37)**; geo §1.3 **Agent tooling** note; [`ARCHITECTURE.md`](ARCHITECTURE.md) **territory-ia** tools + **`tools/compute-lib/`**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **glossary** **Compute-lib program**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-36**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`territory-compute-lib`**, **`isometric_world_to_grid`**, **`IsometricGridMath`**, golden **`world-to-grid.json`**, **IA tools** **CI** builds **compute-lib** before **mcp-ia-server**. **Authority:** **C#** / **Unity** remain **grid** truth; **Node** duplicates **verified** planar **World ↔ Grid** inverse only (**glossary** **World ↔ Grid conversion**).
  - Depends on: none (soft: **TECH-21** **§ Completed**)

- [x] **TECH-39** — **territory-ia** **computational** **MCP** tool suite (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/src/tools/compute/`; `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`; `Assets/Scripts/Utilities/Compute/` (parity surfaces)
  - Spec: (removed after closure — no project spec; **glossary** **Computational MCP tools (TECH-39)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program** follow-ups; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`growth_ring_classify`**, **`grid_distance`**, **`pathfinding_cost_preview`** v1, **`geography_init_params_validate`**, **`desirability_top_cells`** (**`NOT_AVAILABLE`** stub until **TECH-66**); shared **`territory-compute-lib`**. **Deferred** work: **TECH-65**, **TECH-66**, **TECH-64**, **TECH-32**, **TECH-15**/**TECH-16** (see open **BACKLOG**).
  - Depends on: none (soft: **TECH-38** for **heavy** tools; pilot milestone in archive)

- [x] **TECH-60** — **Spec pipeline & verification program** (umbrella): agent workflow, MCP, scripts, **test contracts** (2026-04-04)
  - Type: tooling / documentation / agent enablement
  - Files: [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`ia/skills/README.md`](ia/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml); **§ Completed** children **TECH-61**–**TECH-63** (this file)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-61**–**TECH-63**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`ia/skills/README.md`](ia/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); prerequisite rows **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30**, **TECH-37**, **TECH-38** — `ia/projects/*.md`; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Phased **TECH-61** (layer A), **TECH-62** (layer B — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**), **TECH-63** (layer C — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**). **Charter:** ids **TECH-60**–**TECH-63**; three layers vs monolithic umbrella. **Related:** **TECH-48** (MCP discovery — **TECH-62** overlap **§ Completed**); **TECH-23**; **TECH-45**–**TECH-47** (**Skills** README).
  - Depends on: none (prerequisites remain separate **BACKLOG** rows)

- [x] **TECH-63** — **Spec pipeline** layer **C**: Cursor **Skills** + **project spec** template (**test contracts**, workflow steps) (2026-04-04)
  - Type: documentation / agent enablement (**Cursor Skill** + template edits)
  - Files: `ia/skills/project-spec-kickoff/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/project-implementation-validation/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/project-new/SKILL.md`; `ia/templates/project-spec-template.md`; `ia/projects/PROJECT-SPEC-STRUCTURE.md`; `ia/skills/README.md`; [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md) **§7b**; [`ia/skills/README.md`](ia/skills/README.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; this row)
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
  - Files: root [`package.json`](package.json) (`validate:all`, `description`); [`ia/skills/project-implementation-validation/SKILL.md`](ia/skills/project-implementation-validation/SKILL.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **project-implementation-validation**, **territory-ia spec-pipeline layer B (TECH-62)**, **territory-ia spec-pipeline program (TECH-60)**, **Documentation** row; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md) (reference)
  - Spec: (removed after closure — **glossary** **project-implementation-validation** / **`validate:all`**; **project-implementation-validation** **`SKILL.md`**; **`docs/mcp-ia-server.md`**; root **`package.json`**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; **TECH-62** **§ Completed**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`npm run validate:all`** chains **IA tools** steps 1–4 (**dead project spec**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**); triple-source rule with **project-implementation-validation** manifest and [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml). **Phase 2**/**3** optional scripts (**impact** / **diff** / **backlog-deps**, **`test:invariants`**) deferred per **Decision Log** — pick up under **TECH-30** / follow-up. **Does not** register MCP tools (**TECH-62** layer B **§ Completed** for **territory-ia** extensions — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**).
  - Depends on: none (soft: **TECH-50** **§ Completed**)

- [x] **TECH-21** — **JSON program** (umbrella; charter closed) (2026-04-03)
  - Type: technical / data interchange
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); `ia/specs/glossary.md` — **JSON program (TECH-21)**, **Interchange JSON (artifact)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); `ia/specs/persistence-system.md`; `docs/planned-domain-ideas.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**, **TECH-41**, **TECH-44a**, **TECH-44**
  - Spec: (removed after closure — **glossary** **JSON program (TECH-21)**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**/**TECH-41**/**TECH-44a**/**TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella phases **TECH-40**/**TECH-41**/**TECH-44a** **§ Completed**; **Save data** format unchanged without a migration issue; charter **Decision Log** and **Open Questions** trace live in **glossary** + durable docs. **Ongoing process:** any **Save data** change needs a tracked migration issue; keep brainstorm FAQ aligned when editing interchange docs. **B2** append-only line log → **TECH-43** (open). **Postgres**/**IA** evolution: **TECH-44** **§ Completed**, **TECH-18**.
  - Depends on: none

- [x] **TECH-55b** — **Editor Reports: DB-first document storage + filesystem fallback** (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0005_editor_export_document.sql`; `.gitignore` (`tools/reports/.staging/`); `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; `.env.example`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **Editor export registry**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup** **Editor export registry** + **Node**/**PATH** troubleshooting; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **DB-first** **`document jsonb`**; **`tools/reports/`** fallback; quiet success **`Debug.Log`** (optional verbose **EditorPrefs**); **`DATABASE_URL`** via **EditorPrefs** / **`.env.local`**; **`node`** resolution for GUI-launched **Unity** (**Volta**/Homebrew/**EditorPrefs**/**`NODE_BINARY`**); optional **`backlog_issue_id`** (**NULL** when unset); no backlog id as **Editor** product branding. **Operational:** run **`npm run db:migrate`** (**`0004`**/**`0005`**) before **`editor_export_*`** exist; **Postgres** user in **`DATABASE_URL`** must match local roles (e.g. Homebrew vs `postgres`).
  - Depends on: **TECH-55** **§ Completed**
  - Related: **TECH-44b**/**c** **§ Completed**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived; absorbed former **TECH-59**)

- [x] **TECH-55** — **Automated Editor report registry** (Postgres, per **Reports** export type) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0004_editor_export_tables.sql`; `db/migrations/0005_editor_export_document.sql`; `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **Editor export registry**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup**; **postgres-interchange-patterns** **Program extension mapping**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55b**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Per-export **`editor_export_*`** **B1** tables, **`register-editor-export.mjs`**, **`EditorPostgresExportRegistrar`**; **`normalizeIssueId`** parity with **`backlog-parser.ts`**. **TECH-55b** superseded persistence to **DB-first** full body + filesystem fallback (same closure batch). Does not replace **`dev_repro_bundle`** (**TECH-44c**).
  - Depends on: **TECH-44b** **§ Completed** (soft: **TECH-44c** **§ Completed**)
  - Related: **TECH-55b** **§ Completed**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived)

- [x] **TECH-58** — **Agent closeout efficiency:** **project-spec-close** (**MCP** + **Node**) (2026-04-03)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`; `tools/mcp-ia-server/src/tools/project-spec-closeout-digest.ts`, `spec-sections.ts`; `tools/mcp-ia-server/src/tools/spec-section.ts` (shared extract); `tools/mcp-ia-server/scripts/project-spec-closeout-report.ts`, `project-spec-dependents.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/closeout-parse.test.ts`, `tests/tools/spec-section-batch.test.ts`; root `package.json` (`closeout:*`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ARCHITECTURE.md`](ARCHITECTURE.md); `AGENTS.md`; `ia/rules/agent-router.md`, `mcp-ia-default.md`; [`ia/skills/project-spec-close/SKILL.md`](ia/skills/project-spec-close/SKILL.md); [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md); [`ia/specs/glossary.md`](ia/specs/glossary.md) — **project-spec-close** / **IA index manifest** / **Reference spec** rows; `tools/mcp-ia-server/src/index.ts` (v0.4.3)
  - Spec: (removed after closure — [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows** + **Tools**; **glossary** **project-spec-close**; **project-spec-close** **`SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Lessons learned (TECH-58 closure)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + `project-implementation-validation`):** **`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:worksheet`** / **`closeout:dependents`** / **`closeout:verify`**; shared parser for future **TECH-48**. **TECH-51** closeout ordering unchanged. **`npm run verify`** / **`test:ia`** green.
  - Depends on: none (soft: **TECH-48**, **TECH-30**, **TECH-18**)

- [x] **TECH-56** — **Cursor Skill:** **`/project-new`** — new **BACKLOG** row + initial **project spec** + cross-links (**territory-ia** + optional web) (2026-04-06)
  - Type: documentation / agent enablement (**Cursor Skill** + **BACKLOG** / `ia/projects/` hygiene)
  - Files: `ia/skills/project-new/SKILL.md`; [`ia/skills/README.md`](ia/skills/README.md); `AGENTS.md` item 5; `ia/specs/glossary.md` — **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**
  - Spec: (removed after closure — [`ia/skills/project-new/SKILL.md`](ia/skills/project-new/SKILL.md); **glossary** **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **create-first** **Tool recipe (territory-ia)**; **`backlog_issue`** resolves **`BACKLOG.md`** then [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) ([`docs/mcp-ia-server.md`](docs/mcp-ia-server.md)); optional **`web_search`** external-only; **`npm run validate:dead-project-specs`** after new **`Spec:`** paths. **Decision Log:** skill folder **`project-new`**; revisit recipe when **TECH-48** ships. Complements **kickoff** / **implement** / **close** / **project-implementation-validation**.
  - Depends on: none (soft: [ia/skills/README.md](ia/skills/README.md); **TECH-49**–**TECH-52** **§ Completed** for sibling patterns)

- [x] **TECH-44** — **Postgres + interchange patterns** (merged program umbrella; charter closed) (2026-04-05)
  - Type: technical / infrastructure + architecture (program umbrella)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (**Program extension mapping (E1–E3)**); **TECH-44a**/**b**/**c** **§ Completed** rows (same section); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; `AGENTS.md` (umbrella programs); `ia/specs/glossary.md` — **Postgres interchange patterns**, **JSON program (TECH-21)**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping**; **glossary** **Postgres interchange patterns**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44a**/**b**/**c**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** Charter **§4** satisfied (**TECH-44a**/**b**/**c** **§ Completed**). **E2**/**E3** remain **TECH-53**/**TECH-54** (open); **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**. **Decision Log** entries migrated into [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) and **glossary**. **ID hygiene:** former erroneous **TECH-44** id on **project-spec-kickoff** completion → **TECH-57** (see below).
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-44c** — **Dev repro bundle registry** (**E1**) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `db/migrations/0003_dev_repro_bundle.sql`; `tools/postgres-ia/register-dev-repro.mjs`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Dev repro bundle registry**); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (Related pointer); repo root `package.json` (`db:register-repro`); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `ia/specs/unity-development-context.md` §10 (**Postgres registry** blurb); `ia/specs/glossary.md` — **Dev repro bundle**
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); glossary **Dev repro bundle**; **unity-development-context** §10; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **`dev_repro_bundle`** **B1** table + **`dev_repro_list_by_issue`**; **`register-dev-repro.mjs`** with **`normalizeIssueId`** parity to **`backlog-parser.ts`** (keep in sync — lesson in glossary). **Save data** / **Load pipeline** unchanged. Per-export **Unity** automation → **TECH-55** **§ Completed** (glossary **Editor export registry**).
  - Depends on: **TECH-44b** **§ Completed**

- [x] **TECH-44b** — Game **PostgreSQL** database; first milestone — **IA** schema + minimal read surface (2026-04-03)
  - Type: infrastructure / tooling
  - Files: `db/migrations/`; `tools/postgres-ia/`; `docs/postgres-ia-dev-setup.md`; `.env.example`; repo root `package.json` (`db:migrate`, `db:seed:glossary`, `db:glossary`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) (**PostgreSQL IA** subsection for **TECH-18**); `ia/specs/glossary.md` — **Postgres interchange patterns** row (**TECH-44b** milestone); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); `docs/agent-tooling-verification-priority-tasks.md` (row 11); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `ia/projects/TECH-18.md` (**Current State**); `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts` (open-issue fixture — e.g. **TECH-75d**)
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) **Shipped decisions**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **glossary** **Postgres interchange patterns**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + local migrate/seed/smoke):** Versioned **IA** tables (`glossary`, `spec_sections`, `invariants`, `relationships`); **`ia_glossary_row_by_key`**; **`tools/postgres-ia/`** migrate/seed/read scripts; **`DATABASE_URL`** / **`.env.example`**; **MCP** remains **file-backed** until **TECH-18**. Does **not** replace Markdown authoring or **I1**/**I2** **CI** checks.
  - Depends on: **TECH-44a** **§ Completed**

- [x] **TECH-44a** — **Interchange + PostgreSQL patterns** (**B1**, **B3**, **P5**) (2026-04-03)
  - Type: technical / architecture (documentation)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); `ia/specs/persistence-system.md` (pointer); `ia/specs/glossary.md` — **Postgres interchange patterns (B1, B3, P5)**, **Interchange JSON** Spec column, **JSON program (TECH-21)**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md), `docs/mcp-ia-server.md`, `docs/planned-domain-ideas.md`, `docs/cursor-agents-skills-mcp-study.md`, `docs/agent-tooling-verification-priority-tasks.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44** (umbrella — filed after **TECH-44a** closure), **TECH-21**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); **glossary** **Postgres interchange patterns**, **JSON program (TECH-21)**; **persistence-system** §Save; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**/**TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase C** of **TECH-21**. Normative **B1** row+**JSONB**, **B3** idempotent **patch** **envelope**, **P5** streaming, SQL vs **`artifact`** naming; explicit **Save data** / **Load pipeline** separation. **B2** → **TECH-43** only. Former **TECH-42** scope under **TECH-44** program.
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-41** — **JSON** payloads for **current** systems: **geography** params, **cell**/**chunk** interchange, snapshots, DTO layers (2026-04-11)
  - Type: technical / performance enablement
  - Files: `Assets/StreamingAssets/Config/geography-default.json`; `Assets/Scripts/Managers/GameManagers/GeographyInitParamsDto.cs`, `GeographyInitParamsLoader.cs`; `GeographyManager.cs`, `MapGenerationSeed.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `docs/schemas/cell-chunk-interchange.v1.schema.json`, `world-snapshot-dev.v1.schema.json`, `docs/schemas/README.md`; `tools/mcp-ia-server/src/schemas/geography-init-params-zod.ts`, `scripts/validate-fixtures.ts`, `tests/schemas/`; `ia/specs/glossary.md` — **Interchange JSON**, **geography_init_params**; **`ARCHITECTURE.md`** — **Interchange JSON**; **persistence-system** / **unity-development-context** cross-links
  - Spec: (removed after closure — **glossary** + **`ARCHITECTURE.md`** + [`docs/schemas/README.md`](docs/schemas/README.md) + **unity-development-context** §10 + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase B** of **JSON program (TECH-21)**. **G4** optional **`geography_init_params`** load from **StreamingAssets**; **G1**/**G2** Editor exports under **`tools/reports/`**; Zod parity + **`validate:fixtures`**; **E3** layering documented; **Save data** unchanged. **Deferred to FEAT-46:** apply **`water.seaBias`** / **`forest.coverageTarget`** to simulation. **`backlog_issue`** test target: open **Agent** lane row (e.g. **TECH-75d**).
  - Depends on: none (**TECH-40** completed — **§ Completed** **TECH-40**)

- [x] **TECH-40** — **JSON** infra: artifact identity, schemas, **CI** validation, **spec** + **glossary** indexes (2026-04-11)
  - Type: tooling / data interchange
  - Files: `docs/schemas/` (pilot schema + fixtures); repo root `package.json` (`validate:fixtures`, `generate:ia-indexes`, `validate:dead-project-specs`, `test:ia`); `tools/mcp-ia-server/scripts/validate-fixtures.ts`, `generate-ia-indexes.ts`, `src/ia-index/glossary-spec-ref.ts`, `data/spec-index.json`, `data/glossary-index.json`; `.github/workflows/ia-tools.yml`; `projects/json-use-cases-brainstorm.md` (policy §); `docs/mcp-ia-server.md`; `ia/specs/glossary.md` — **Documentation** (**IA index manifest**, **Interchange JSON**); [REFERENCE-SPEC-STRUCTURE.md](ia/specs/REFERENCE-SPEC-STRUCTURE.md) § Conventions item 7
  - Spec: (removed after closure — **glossary** + **REFERENCE-SPEC-STRUCTURE** + [`docs/schemas/README.md`](docs/schemas/README.md) + [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase A** of **JSON program (TECH-21)**. **`artifact`** / **`schema_version`** policy; JSON Schema Draft **2020-12** pilot **`geography_init_params`**; **`npm run validate:fixtures`**; committed **I1**/**I2** with **`generate:ia-indexes -- --check`** in **CI**. **`backlog_issue`** integration test uses an open issue in the **Agent** lane (e.g. **TECH-75d**). **Related:** **TECH-24**, **TECH-30**, **TECH-34**; **TECH-43** **Depends on** updated.
  - Depends on: none (soft: align **TECH-37** **Zod** when touching **compute-lib**)

- [x] **TECH-57** — **Cursor Skills:** **infrastructure** + **kickoff** skill (project **spec** review / IA alignment) (2026-04-11)
  - Type: documentation / agent enablement (**Cursor Skill** + repo docs — no runtime game code)
  - Files: `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/templates/project-spec-review-prompt.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`
  - Spec: (removed after closure — conventions live under **`ia/skills/`** and **§4.4** of [`docs/cursor-agents-skills-mcp-study.md`](docs/cursor-agents-skills-mcp-study.md))
  - Notes: **Completed (verified per user):** Part 1 **README** + authoring rules; Part 2 **project-spec-kickoff** **`SKILL.md`** with **Tool recipe (territory-ia)** (`backlog_issue` → `invariants_summary` → `router_for_task` → …); paste template; **AGENTS.md** item 5 + doc hierarchy pointer; study doc **§4.4**. **Lesson (persisted in README):** **`router_for_task`** `domain` strings should match **`ia/rules/agent-router.md`** task-domain row labels (e.g. `Save / load`), not ad-hoc phrases. **Follow-up:** **TECH-48** (MCP discovery), **TECH-45**–**TECH-47** (domain skills). **Renumbered from erroneous id TECH-44** (collision with Postgres program **TECH-44** — corrected 2026-04-05).
  - Depends on: none

- [x] **TECH-49** — **Cursor Skill:** **implement** a **project spec** (execution workflow after kickoff) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `ia/skills/project-spec-implement/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md` (cross-link); `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`; `docs/mcp-ia-server.md`; `ia/templates/project-spec-review-prompt.md`
  - Spec: (removed after closure — workflow in **`ia/skills/project-spec-implement/SKILL.md`**; closure record in this row)
  - Notes: **Completed (verified per user request to implement):** **project-spec-implement** **`SKILL.md`** with **Tool recipe (territory-ia)** (per-phase loop, **Branching**, **Seed prompt**, **unity-development-context** §10 pointer); README index row; **AGENTS.md** project-spec bullets + doc hierarchy; study doc **§4.4**; **`docs/mcp-ia-server.md`** “Project spec workflows”; paste template “After review: implement”. **Dry-run:** Meta — authoring followed the recipe while implementing this issue.
  - Depends on: none (soft: **TECH-57**)

- [x] **TECH-50** — **Doc hygiene:** **cascade** references when **project specs** close; **dead links**; **BACKLOG** as durable anchor (2026-04-03)
  - Type: tooling / doc hygiene / agent enablement
  - Files: `tools/validate-dead-project-spec-paths.mjs`; repo root `package.json` (`validate:dead-project-specs`); `.github/workflows/ia-tools.yml`; `ia/projects/PROJECT-SPEC-STRUCTURE.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/agent-tooling-verification-priority-tasks.md`; `tools/mcp-ia-server/README.md` (pointer only)
  - Spec: (removed after closure — **PROJECT-SPEC-STRUCTURE** closeout + **Lessons learned (TECH-50 closure)**; **`docs/mcp-ia-server.md`** **Project spec path hygiene**; this row)
  - Notes: **Completed (verified per user):** `npm run validate:dead-project-specs` + CI gate; **BACKLOG** checks strict **`Spec:`** lines on open rows only; **BACKLOG-ARCHIVE.md** excluded; advisory `--advisory` / `CI_DEAD_SPEC_ADVISORY=1`. **Lessons:** See **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-50 closure)**. **Deferred:** optional **territory-ia** MCP tool; shared **Node** module with **TECH-30**.
  - Depends on: none (soft: **TECH-30** — merge or share implementation)
  - Related: **TECH-51** completed — **`project-spec-close`** documents `npm run validate:dead-project-specs` in the closure workflow

- [x] **TECH-51** — **Cursor Skill:** **`project-spec-close`** — full **issue** / **project spec** closure workflow (IA, lessons, **BACKLOG**, cascade) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `ia/skills/project-spec-close/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/skills/project-spec-implement/SKILL.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `docs/mcp-ia-server.md`; `ia/specs/glossary.md` — **Documentation**; `ia/projects/PROJECT-SPEC-STRUCTURE.md`
  - Spec: (removed after closure — **`ia/skills/project-spec-close/SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Closeout checklist** + **Lessons learned (TECH-51 closure)**; **glossary** **Project spec** / **project-spec-close**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** **IA persistence checklist** + ordered **Tool recipe (territory-ia)**; **persist IA → delete project spec → `validate:dead-project-specs` → BACKLOG Completed** (user-confirmed). **Decisions:** no duplicate **TECH-50** scanner in the skill; composite **closeout_preflight** MCP deferred (**TECH-48** / follow-up). **Related:** **TECH-52** completed — optional **`project-implementation-validation`** before closeout cascade when IA-heavy.
  - Depends on: none (soft: **TECH-50**, **TECH-57**, **TECH-49**)

- [x] **TECH-52** — **Cursor Skill:** **`project-implementation-validation`** — post-implementation tests + available code validations (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `ia/skills/project-implementation-validation/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-implement/SKILL.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/skills/project-spec-close/SKILL.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `tools/mcp-ia-server/README.md`
  - Spec: (removed after closure — **`ia/skills/project-implementation-validation/SKILL.md`**; **glossary** **Documentation** — **project-implementation-validation**; **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-52 closure)**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** ordered **validation manifest** (**IA tools** **CI** parity + advisory **`verify`**); **skip** matrix; **failure policy**; cross-links to **implement** / **close** / **kickoff**; **Phase 3** root aggregate **`npm run`** not shipped (optional **BACKLOG** follow-up). **Deferred:** **`run_validations`** MCP (**TECH-48** / follow-up); **Unity** one-liner → **TECH-15** / **TECH-16** / **UTF**.
  - Depends on: none (soft: **TECH-49**, **TECH-50**, **TECH-51**)
  - Related: **TECH-48** — MCP “validation bundle” tool out of scope unless new issue

*(Older batch moved to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) § **Recent archive** on 2026-04-10. Add new completions here for ~30 days, then archive.)*

> Full history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

---

---

## Recent archive (moved from BACKLOG.md, 2026-04-10)

- [x] **TECH-88** — `GridManager` parent-id surface + new-game placeholder allocation (2026-04-13)
  - Type: infrastructure / runtime
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` (PascalCase properties; save fields stay lowercase `regionId` / `countryId` per TECH-87). One-shot `HydrateParentIds(regionId, countryId)` with null/empty guard + `_parentIdsHydrated` duplicate guard (`Debug.LogError` + return, no throw). `GameSaveManager.NewGame()` allocates `Guid.NewGuid()` pair post-`ResetGrid()` + hydrates eagerly (shifts allocation earlier than previous lazy-on-first-save). `LoadGame` hydrates after `MigrateLoadedSaveData` + local id cache, before `RestoreGrid`. `BuildCurrentGameSaveData` keeps fallback as defense-in-depth for scenario-builder paths. No consumers yet — surface only; consumed by ≥1 city system in Step 2. Orchestrator: `multi-scale-master-plan.md` Step 1 / Stage 1.1.
  - Depends on: **TECH-87**

- [x] **BUG-12** — Happiness UI always shows 50% (2026-04-07)
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness), `GridManager.cs` (HandleBuildingStatsReset), `CityStats.cs` (RemoveMoney Debug.Log)
  - Spec: (removed after closure — no glossary/reference spec changes; Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close` + user):** `GetHappiness()` now reads `cityStats.happiness` instead of returning hardcoded `50.0f`. Format changed from `{F1}%` to `{N0}` (raw integer) for consistency with legacy HUD. Also fixed: bulldoze not reversing stats for developed buildings (`HandleBuildingStatsReset` skipped `HandleBuildingDemolition` when `buildingType != null`); removed noisy `Debug.Log` in `RemoveMoney`. `GetHappinessColor` thresholds kept as-is — revisit in **FEAT-23**.

- [x] **TECH-76** — **Information Architecture** system overview document (2026-04-07)
  - Type: documentation
  - Files: `docs/information-architecture-overview.md` (new); `AGENTS.md` (cross-link); `ARCHITECTURE.md` (cross-link)
  - Spec: (removed after closure — this row)
  - Notes: **Completed (verified).** Single ~220-line document at [`docs/information-architecture-overview.md`](docs/information-architecture-overview.md) describing the IA system as a coherent design: philosophy (slice don't load, one vocabulary, knowledge flows back), layer diagram (ASCII), 6-stage knowledge lifecycle, semantic model axes (vocabulary/routing/invariants), consistency mechanisms table, MCP tool ecosystem, skill system lifecycle table, optional Postgres layer, and 6 extension checklists (reference spec, MCP tool, skill, glossary term, rule, Postgres table). Cross-linked from `AGENTS.md` documentation hierarchy and `ARCHITECTURE.md` § Agent IA. **IA evolution lane** context: [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md).
  - Depends on: none

- [x] **TECH-84** — **High-priority MCP diagnostic & discovery tools** (six-tool suite) (2026-04-07)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/tools/backlog-search.ts`, `tools/mcp-ia-server/src/tools/invariant-preflight.ts`, `tools/mcp-ia-server/src/tools/findobjectoftype-scan.ts`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` (extended `kind` enum); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (three new bridge cases + `CreateOk` factory); `tools/mcp-ia-server/src/index.ts`; `docs/mcp-ia-server.md` (28 tools); `tools/mcp-ia-server/README.md` (27 tools)
  - Spec: (removed after closure — **IA project spec journal**; this row)
  - Notes: **Completed (verified).** Six MCP tools shipped in **territory-ia** v0.5.0: **(1) `backlog_search`** — keyword search across backlog issues. **(2) `invariant_preflight`** — composite context bundle (invariants + router + spec sections) for an issue. **(3) `findobjectoftype_scan`** — static C# scan for per-frame `FindObjectOfType` violations. **(4) `economy_balance_snapshot`** — bridge: economy/happiness/demand from Play Mode. **(5) `prefab_manifest`** — bridge: scene MonoBehaviours + missing scripts. **(6) `sorting_order_debug`** — bridge: renderers + sorting order at a cell. 115 tests pass; `npm run verify` green. Also added `parseAllBacklogIssues` to `backlog-parser.ts`, exported `parseInvariantsBody` and `collectRouterData` for internal reuse, C# `AgentBridgeResponseFileDto.CreateOk` factory method. **Migrated content:** [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md).
  - Depends on: none

- [x] **TECH-75** — **Close Dev Loop** orchestration: agent-driven Play Mode verification (2026-04-07)
  - Type: orchestration spec (no umbrella BACKLOG row)
  - Files: (removed after closure — **glossary** **IDE agent bridge**; **`close-dev-loop`** Skill; **`bridge-environment-preflight`** Skill; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); **IA project spec journal**; this row)
  - Spec: (removed after closure)
  - Notes: **Completed (verified):** All sub-issues shipped: **TECH-75a** (Play Mode bridge `kind` values), **TECH-75b** (`debug_context_bundle` + anomaly scanner), **TECH-75c** (`close-dev-loop` Skill + compile gate), **TECH-75d** (dev environment preflight). Agent can enter Play Mode, collect evidence, detect anomalies, verify fixes, and exit — zero human Unity interaction. MVP exit criteria met. Absorbed **TECH-59** (MCP staging superseded by direct Play Mode control). Open follow-ups: `unity_debug_bundle` sugar tool (deferred); Game view auto-focus; multi-seed-cell bundle.
  - Depends on: none
  - Related: **TECH-75a**, **TECH-75b**, **TECH-75c**, **TECH-75d** (all **§ Recent archive**)

- [x] **TECH-75d** — **Close Dev Loop**: dev environment **preflight** (Postgres + **IDE agent bridge** readiness) (2026-04-07)
  - Type: tooling / agent enablement (**scripts** + **Cursor Skill** + docs)
  - Files: `tools/mcp-ia-server/scripts/bridge-preflight.ts`; root `package.json` (`db:bridge-preflight`); `ia/skills/bridge-environment-preflight/SKILL.md`; `ia/skills/README.md`; `ia/skills/close-dev-loop/SKILL.md` (Step 0); `ia/skills/ide-bridge-evidence/SKILL.md`; `AGENTS.md`; `docs/postgres-ia-dev-setup.md`; `docs/mcp-ia-server.md`; `config/README.md`; orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **bridge-environment-preflight** Skill; **close-dev-loop** Step 0; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) **Bridge environment preflight**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; this row)
  - Notes: **Completed (verified):** Node preflight script (`bridge-preflight.ts`) with stable exit codes 0–4; imports `resolveIaDatabaseUrl`; checks Postgres connectivity and `agent_bridge_job` table presence. `npm run db:bridge-preflight` at repo root. **bridge-environment-preflight** Cursor Skill with bounded repair policy (one attempt per failure class). **close-dev-loop** Step 0 upgraded from optional to concrete. All four exit codes verified on dev machine (0/1/2/3 + post-migrate restore).
  - Depends on: none (soft: **`close-dev-loop`** shipped)
  - Related: **TECH-75** orchestration, **TECH-75b** (**§ Recent archive**), **TECH-75c** (**§ Recent archive**), **TECH-75a**

- [x] **TECH-75b** — **Close Dev Loop**: context bundle + anomaly detection (2026-04-09)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/AgentBridgeAnomalyScanner.cs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/scripts/bridge-playmode-smoke.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; **ide-bridge-evidence**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Bridge **`kind`** **`debug_context_bundle`** — Moore **Agent context** export + deferred **Game view** screenshot + console snapshot + **`AgentBridgeAnomalyScanner`** rules (`missing_border_cliff`, `heightmap_cell_desync`, `redundant_shore_cliff`). CLI **`npm run db:bridge-playmode-smoke`** uses **`runUnityBridgeCommand`** (same path as MCP **`unity_bridge_command`**). Optional **`unity_debug_bundle`** MCP sugar still deferred (open **BACKLOG** follow-up if scoped).
  - Depends on: none (Play Mode bridge **`kind`** values — this file **TECH-75a**)
  - Related: **TECH-75** orchestration, **TECH-75c** (**§ Completed** — this file **Recent archive**), **TECH-75a**

- [x] **TECH-75c** — **Close Dev Loop**: Cursor Skill orchestrating fix → verify → report (2026-04-09)
  - Type: documentation / agent enablement (**Cursor Skill**) + bridge **`kind`**
  - Files: `ia/skills/close-dev-loop/SKILL.md`; `ia/skills/README.md`; [`AGENTS.md`](AGENTS.md); root [`package.json`](package.json) **`unity:compile-check`**; `tools/scripts/unity-compile-check.sh`; `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (**`get_compilation_status`**); `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` (**`unity_compile`**); `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Agent bridge job queue** troubleshooting); [`ARCHITECTURE.md`](ARCHITECTURE.md); orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **`ia/skills/close-dev-loop/SKILL.md`**; **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`close-dev-loop`** Skill (before/after **`debug_context_bundle`**, **compile gate**: **`get_compilation_status`** / **`unity_compile`**, **`npm run unity:compile-check`**, **`get_console_logs`**); **`JsonUtility`** response shape note in **unity-development-context** §10. Optional **`unity_debug_bundle`** MCP sugar still deferred.
  - Depends on: none (soft: **`debug_context_bundle`** — **this file** **TECH-75b**)
  - Related: **TECH-75** orchestration, **TECH-75b**, **TECH-75d** (archived), **TECH-75a**

- [x] **BUG-54** — **Utility building** / **zoning** overlay stripped **brown cliff** stacks on **map border** **cells** (void toward **off-grid** exterior) (2026-04-10)
  - Type: bug (rendering / terrain layering)
  - Files: `GridManager.cs` (`DestroyCellChildren`, `DestroyCellChildrenExceptForest`), `TerrainManager.cs` (`IsCliffStackTerrainObject`), `BuildingPlacementService.cs`, `ZoneManager.cs` (`PlaceZone`, `PlaceZoneAt`, `RestoreZoneTile`); [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §5.7 **Cell child cleanup (overlays)**
  - Spec: (removed after closure — normative **geo** §5.7 bullet **Cell child cleanup (overlays)**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`TerrainManager.IsCliffStackTerrainObject`**; **`GridManager.DestroyCellChildren`** skips **cliff** (and existing **slope**) instances during **`destroyFlatGrass`** **building** cleanup; **`DestroyCellChildrenExceptForest`** applies the same skips so **undeveloped light zoning** brush and restore do not wipe **map border** stacks. **`RestoreTerrainForCell`** early exit on **building**-occupied **cells** prevented relying on post-place **cliff** rebuild alone.
  - Depends on: none
  - Related: **BUG-20**, **BUG-31**; archived **BUG-44** (water × **map border** — different cause)

- [x] **TECH-75a** — **Close Dev Loop**: Play Mode bridge commands + readiness signal (2026-04-08)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); [`AGENTS.md`](AGENTS.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) (**IDE agent bridge** bullet)
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Play Mode bridge smoke (MCP, agent-led)**; orchestration archived (this file **Recent archive**) §7 phase 1; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Bridge **`kind`** **`enter_play_mode`**, **`exit_play_mode`**, **`get_play_mode_status`**; readiness via **`GridManager.isInitialized`**; **`UnityEditor.SessionState`** for enter/exit wait across domain reload; **`GameView`** focus via reflection before **`EnterPlaymode`**; concurrent same-type jobs rejected; deferred screenshot pump unified in **`OnEditorUpdate`**. **MCP** smoke + optional **Play Mode** sequence documented in **`AGENTS.md`** / **`docs/mcp-ia-server.md`**. **Subsequent Close Dev Loop:** context bundle **TECH-75b** (this file); **`close-dev-loop`** Skill **TECH-75c** (this file **Recent archive**); **TECH-75d** dev preflight on [`BACKLOG.md`](BACKLOG.md).
  - Depends on: none (extends **TECH-73**/**TECH-74** Phase 1 bridge)
  - Related: **TECH-75** orchestration, **TECH-75b** (archived this file), **TECH-75c**, **TECH-73**, **TECH-74**, **TECH-59** (absorbed)

- [x] **BUG-44** — **Cliff** prefabs: black gaps when a **water body** meets the **east** or **south** **map border** (2026-04-07)
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `GetCliffWallDropSouth`, `GetCliffWallDropEast`, `ResolveCliffWallDropAfterSuppression`, `PlaceCliffWallStackCore`, `ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary`); [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §5.6.1, §5.7; [`ia/specs/glossary.md`](ia/specs/glossary.md) **Map border**, **Cliff suppression**; [`ARCHITECTURE.md`](ARCHITECTURE.md) **Water** subsection
  - Spec: (removed after closure — normative **geo** §5.7 **Map border (exterior void)** / **Map border × water-shore**; **glossary** **Map border** / **Cliff suppression**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **South**/**east** faces toward **off-grid** void stack brown **cliff** segments to **`MIN_HEIGHT`** (full height drop); **`PlaceCliffWalls`** passes **`MIN_HEIGHT`** as low foot for stack depth. **Water-shore** primary cells suppress duplicate brown **cliff** toward that void. **Water-shore** world-**Y** nudge applies only when the lower neighbor is on-grid. No **water–water cascade** on outermost **map border** cells. **Prior** virtual-foot-from-cardinals approach dropped — see **Decision Log** in **IA project spec journal** if persisted.
  - Depends on: none
  - Related: **BUG-42**, **BUG-45**, **BUG-43**

- [x] **TECH-59** — **territory-ia** MCP: stage **Editor** export registry payload — **absorbed into Close Dev Loop** (2026-04-07)
  - Type: tooling / agent enablement
  - Files: (no implementation shipped — scope absorbed into **Close Dev Loop** (**TECH-75**))
  - Spec: (deleted — `ia/projects/TECH-59.md` removed; concept superseded by **Close Dev Loop** orchestration archived (this file **Recent archive**))
  - Notes: **Absorbed (not implemented):** Original goal was MCP staging for **Editor export registry** payload (**`backlog_issue_id`** + JSON documents) with a Unity menu to apply. Superseded because the **Close Dev Loop** program (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) lets the agent enter Play Mode and collect evidence directly, eliminating the need to pre-stage registry parameters. Registry staging may reappear as a sub-task if needed, but is no longer a standalone issue.
  - Depends on: none
  - Related: **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-55b** **§ Completed**, **TECH-48**

- [x] **TECH-73** — **Unity** ↔ **IDE** **agent bridge** program (**Phase 1** — **Postgres** **`agent_bridge_job`**) (2026-04-06)
  - Type: tooling / agent enablement (program umbrella — Phase 1 shipped)
  - Files: [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) (charter / optional later phases); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); `db/migrations/0008_agent_bridge_job.sql`; `tools/postgres-ia/agent-bridge-dequeue.mjs`; `tools/postgres-ia/agent-bridge-complete.mjs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/scripts/run-unity-bridge-once.ts`; root **`npm run db:bridge-agent-context`**; `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/EditorPostgresBridgeJobs.cs`; `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` (**ExportAgentContextForAgentBridge**); `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**, **Editor export registry**
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **territory-ia** **`unity_bridge_command`** / **`unity_bridge_get`** + **Unity** **Node** dequeue/complete; **`TryPersistReport`** **Postgres-only** (no **`tools/reports/`** fallback for registry exports). **Optional later phases** (HTTP): charter doc + open **BACKLOG** when scoped. **Console** / **screenshot** bridge kinds shipped — **TECH-74** **§ Completed** (this file). **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) supersedes **TECH-59** staging concept — agent drives Play Mode directly.
  - Depends on: none (soft: glossary **Editor export registry** — **TECH-55**/**TECH-55b** archived; **unity-development-context** §10 **Reports** menus)
  - Related: **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-48**, **TECH-33**, **TECH-38**, **TECH-18**, **BUG-53**, **TECH-74**

- [x] **TECH-74** — **territory-ia** MCP + **IDE agent bridge**: **`get_console_logs`** and **`capture_screenshot`** (2026-04-07)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/src/index.ts`; `tools/mcp-ia-server/package.json`; `tools/mcp-ia-server/README.md`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs`; `Assets/Scripts/Editor/AgentBridgeScreenshotCapture.cs`; [`.gitignore`](.gitignore) **`tools/reports/bridge-screenshots/`**; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §4.3; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); [`AGENTS.md`](AGENTS.md); [`ia/templates/project-spec-template.md`](ia/templates/project-spec-template.md) §7b example
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §4.3 **Shipped**; **TECH-73** **§ Completed** **Phase 1** sibling; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`unity_bridge_command`** **`kind`** **`get_console_logs`** / **`capture_screenshot`**; **`response.log_lines`**; **Play Mode** PNG under **`tools/reports/bridge-screenshots/`**; **`params.include_ui`** uses **Game view** **`ScreenCapture`** (**Overlay** UI); **`runUnityBridgeCommand`** **`timeout_ms`** default/clamp; **`@territory/mcp-ia-server`** **0.4.13**. **Node:** **`npm run verify`** / **`npm run test:ia`** green. **Skills:** optional **Play** evidence workflow **`ide-bridge-evidence`**. Charter §5.1 sugar tool names remain aliases only.
  - Depends on: none (soft: **TECH-24** when parser / **Zod** shapes for bridge tools change)
  - Related: **TECH-73**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-48**, **TECH-24**

- [x] **BUG-19** — Mouse scroll wheel in Load Game scrollable menu also triggers camera zoom (2026-04-07)
  - Type: fix (UX)
  - Files: `CameraController.cs` (HandleScrollZoom — `IsPointerOverBlockingUi` guard)
  - Spec: (removed — fix shipped as part of **TECH-69** UI-as-code capstone; normative **`ui-design-system.md`** **§3.5** scroll-zoom checklist)
  - Notes: **Closed (resolved by other issue):** The `IsPointerOverGameObject` guard in `CameraController.HandleScrollZoom` was implemented during **TECH-69**. Scroll over UI panels (Load Game, Building Selector) no longer triggers camera zoom.
  - Depends on: none
  - Related: **TECH-69**, **TECH-67**

- [x] **BUG-53** — **Unity Editor:** **Territory Developer → Reports** menu / **Export Sorting Debug** tooling gap (2026-04-06)
  - Type: bug (tooling / agent workflow)
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `tools/reports/` path resolution (`Application.dataPath` parent); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10 (**Editor agent diagnostics**); [`ARCHITECTURE.md`](ARCHITECTURE.md) **Editor agent diagnostics** bullet; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §2.4 / §7 / §10 (**Agent** bridge next steps)
  - Spec: [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10 (authoritative — no project spec)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **Territory Developer → Reports** shows **Export Agent Context** and **Export Sorting Debug (Markdown)** after compile; **Sorting** full breakdown in **Play Mode** with initialized **grid** matches §10; **Edit Mode** stub behavior unchanged. **Original ship:** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-28**. **Bridge** doc updated so **Reports** is no longer listed as an open prerequisite; **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) supersedes the staging concept.
  - Depends on: none
  - Related: **TECH-28**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-64**

- [x] **FEAT-50** — **UI** visual polish: aesthetic refinement (**HUD**, panels, **toolbar**, **MainMenu**) (2026-04-11)
  - Type: feature / UX polish
  - Files: `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/MainScene.unity`; `Assets/UI/Theme/DefaultUiTheme.asset`; `Assets/Scripts/Managers/GameManagers/UiTheme.cs`, `UIManager.cs` + **`UIManager.*.cs`** partials; `CameraController.cs`; `MainMenuController.cs`; **Controllers** under `Assets/Scripts/Controllers/UnitControllers/` as wired; `ia/specs/ui-design-system.md` (**§1**, **§3.5**, **§5.2**, **§5.3**); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/ui-data-dashboard-exploration.md`](docs/ui-data-dashboard-exploration.md) (dashboard charter — renamed from legacy filename)
  - Spec: (removed after closure — normative **`ui-design-system.md`** **as-built** / **Target** + **§5.3** polish patterns; **§3.5** **BUG-19** touch / **WASD** note; exploration doc **`docs/ui-data-dashboard-exploration.md`**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`UiTheme`**-first **HUD** / **MainMenu** pass; **CanvasGroup** popup fades; **RCI** demand gauge bars; **welcome** briefing (**PlayerPrefs**); **CameraController** **UI** blocking (touch **fingerId** + **WASD**); construction cost / grid debug chrome; **`UiCanvasGroupUtility`**. **Deferred:** optional **`ui_theme_tokens` MCP** — open **BACKLOG** if product wants it. **Dashboard** mechanics: **FEAT-51** + **`docs/ui-data-dashboard-exploration.md`**.
  - Depends on: none (soft: **BUG-19**)
  - Related: **FEAT-51**, **BUG-19**, **BUG-14**, **TECH-67**, **TECH-69**

- [x] **TECH-71** — **IA project spec journal**: Postgres **Decision Log** / **Lessons learned** + MCP tools + **Skills** hooks (2026-04-11)
  - Type: tooling / agent workflow / Postgres dev surface
  - Files: `db/migrations/0007_ia_project_spec_journal.sql`; [`config/postgres-dev.json`](config/postgres-dev.json); [`config/README.md`](config/README.md); `tools/postgres-ia/resolve-database-url.mjs`; `tools/mcp-ia-server/src/ia-db/` (incl. `journal-repo.ts`, `pool.ts`, `resolve-database-url.ts`); `tools/mcp-ia-server/src/tools/project-spec-journal.ts`; `tools/mcp-ia-server/scripts/persist-project-spec-journal.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/ia-db/`; `tools/mcp-ia-server/package.json`; [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md); [`.env.example`](.env.example); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`tools/postgres-ia/README.md`](tools/postgres-ia/README.md); [`ia/specs/glossary.md`](ia/specs/glossary.md); [`ia/skills/project-spec-close/SKILL.md`](ia/skills/project-spec-close/SKILL.md); [`ia/skills/project-new/SKILL.md`](ia/skills/project-new/SKILL.md); [`ia/skills/project-spec-kickoff/SKILL.md`](ia/skills/project-spec-kickoff/SKILL.md); [`ia/rules/agent-router.md`](ia/rules/agent-router.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); root [`package.json`](package.json)
  - Spec: (removed after closure — **glossary** **IA project spec journal**; [`config/README.md`](config/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) **territory-ia** tool list + **Postgres** dev surfaces; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`ia_project_spec_journal`** + MCP **`project_spec_journal_*`**; **Skills** **J1** + optional **project-new** / **project-spec-kickoff** journal search; committed dev URI **`config/postgres-dev.json`** + **`resolve-database-url`** (**postgres-ia** + **mcp-ia-server**); **`npm run db:persist-project-journal`** at closeout.
  - Depends on: none (soft: **TECH-24** for parser policy when extending closeout parser)
  - Related: **TECH-48**, **TECH-18**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived)

- [x] **TECH-67** — **UI-as-code program** (umbrella) (2026-04-10)
  - Type: tooling / documentation / agent enablement (program closeout)
  - Files: `ia/specs/ui-design-system.md` (**Overview**, **Codebase inventory (uGUI)**, **§5.2**, **§3**); `ia/specs/glossary.md` (**UI-as-code program**, **UI design system (reference spec)**); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`docs/ui-as-built-ui-critique.md`](docs/ui-as-built-ui-critique.md); `docs/reports/ui-inventory-as-built-baseline.json`; `Assets/Scripts/Editor/UiInventoryReportsMenu.cs`; `ia/skills/ui-hud-row-theme/`; **BACKLOG.md** (**§ UI-as-code program** header)
  - Spec: (removed after closure — **`ui-design-system.md`** **Codebase inventory (uGUI)** + **§6** revision history; **glossary** rows above; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-69** capstone row; this row)
  - Notes: **Completed (`/project-spec-close`):** Umbrella charter, **§4.4** inventory, backlog bridge, phased plan, and **§8** acceptance migrated off `ia/projects/TECH-67.md`; **FEAT-50** visual polish completed **2026-04-11** (this file **Recent archive**). Optional **`ui_theme_tokens` MCP** still unscoped.
  - Depends on: none
  - Related: **TECH-69**, **TECH-68**, **TECH-70**, **TECH-07**, **FEAT-50**, **TECH-33**, **BUG-53**, **BUG-19**

- [x] **TECH-69** — **UI improvements using UI-as-code** (**TECH-67** program capstone) (2026-04-04)
  - Type: refactor / tooling / UX (umbrella closeout)
  - Files: `Assets/Scenes/MainMenu.unity`; `MainScene.unity`; `MainMenuController.cs`; `UIManager.cs` + **`UIManager.*.cs` partials**; `CameraController.cs` (**scroll** over **UI** zoom gate); `UiTheme.cs`; `Assets/UI/Theme/`; `Assets/UI/Prefabs/`; `UiThemeValidationMenu.cs`; `UiPrefabLibraryScaffoldMenu.cs`; `ia/specs/ui-design-system.md`; `ia/specs/unity-development-context.md` **§10**; `ia/specs/managers-reference.md`; `ia/skills/ui-hud-row-theme/`; `docs/ui-as-built-ui-critique.md` (planning trace)
  - Spec: (removed after closure — normative **`ui-design-system.md`** **§5.2**, **§3.2**, **§3.5**; **`unity-development-context.md`** **§10**; **`managers-reference`** **UIManager**; **glossary** **UI-as-code program**; **TECH-67** umbrella row (archived same batch); this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`UiTheme`** + **MainMenu** serialization; **`partial` `UIManager`**; **Editor** **Validate UI Theme** + **Scaffold UI Prefab Library v0**; **`ui-hud-row-theme`** **Skill**; **typography** policy and **Canvas Scaler** matrix in **`ui-design-system.md`**; **modal** **Esc** contract + **§3.5** scroll vs zoom (**BUG-19** code path). **Deferred:** optional **territory-ia** **`ui_theme_tokens`** — file under open **BACKLOG** if product wants it.
  - Depends on: **TECH-67** (umbrella)
  - Related: **TECH-67**, **TECH-33**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **BUG-19**, **BUG-14**, **BUG-53**, **FEAT-50**

- [x] **TECH-07** — **ControlPanel**: left vertical sidebar layout (category rows) (2026-04-04)
  - Type: refactor (UI/UX)
  - Files: `Assets/Scenes/MainScene.unity` (**`UI/City/Canvas`**, **`ControlPanel`** hierarchy); `UIManager.cs`; `Assets/Scripts/Controllers/UnitControllers/*SelectorButton.cs` (as wired); `ia/specs/ui-design-system.md` **§3.3**, **§1.3**, **§4.3**, **Codebase inventory (uGUI)**
  - Spec: (removed after closure — **`ui-design-system.md`** **§3.3** **toolbar**; **glossary** **UI design system (reference spec)**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-08** historical doc bridge; this row)
  - Notes: **Completed (manual scene work + backlog purge):** **Left**-docked **vertical** **toolbar** implemented directly in **`MainScene.unity`**; open **BACKLOG** row retired. **Trace:** prior doc ticket **TECH-08** (archived) linked **§3.3** target copy to this work.
  - Depends on: none (soft: **TECH-67** program context)
  - Related: **TECH-67**

- [x] **TECH-68** — **As-built** **UI** documentation: align **`ui-design-system.md`** with **shipped** **Canvas** / **HUD** / **popups** (2026-04-04)
  - Type: documentation / agent enablement
  - Files: `ia/specs/ui-design-system.md`; `ia/specs/glossary.md` (**UI design system (reference spec)**, **UI-as-code program**); `ia/specs/unity-development-context.md` **§10** (UI inventory baseline row); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/reports/README.md`](docs/reports/README.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) (**UI-as-code** trace); `Assets/Scripts/Editor/UiInventoryReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); **TECH-67** umbrella project spec (**Phase 1** — removed after **TECH-67** closure)
  - Spec: (removed after closure — **glossary** **UI design system (reference spec)**; **`ui-design-system.md`** **Machine-readable traceability**; **`unity-development-context.md`** **§10**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **As-built** reference spec + committed **UI** inventory baseline; **Editor** export + **Postgres** **`ui_inventory`** kind documented without backlog id branding. **Umbrella:** **TECH-67** **§8** first bullet checked; **TECH-69** **Depends on** no longer cites this row.
  - Depends on: none (soft: **TECH-67** program context)

- [x] **TECH-70** — **UI-as-code** umbrella maintenance & multi-scene **UI** traceability (2026-04-04)
  - Type: documentation / tooling / agent enablement
  - Files: **TECH-67** umbrella project spec (**§4.4**, **§4.6**, **§4.9**, **§7** Phase **0** — removed after **TECH-67** closure); [`ia/specs/ui-design-system.md`](ia/specs/ui-design-system.md); [`Assets/Scripts/Editor/UiInventoryReportsMenu.cs`](Assets/Scripts/Editor/UiInventoryReportsMenu.cs); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/reports/README.md`](docs/reports/README.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) **§10**; [`db/migrations/0006_editor_export_ui_inventory.sql`](db/migrations/0006_editor_export_ui_inventory.sql) (**Postgres** **`editor_export_ui_inventory`**)
  - Spec: (removed after closure — **`ui-design-system.md`** **Codebase inventory (uGUI)** ongoing hygiene + **Machine-readable traceability**; [`docs/reports/README.md`](docs/reports/README.md) **Postgres vs baseline** note; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella **§4.9** resolutions + **Decision Log**; **baseline JSON** aligned to **Postgres** **`document`** (export timestamp); **`RegionScene`** / **`CityScene`** rename deferred (**BACKLOG** / **`ui-design-system.md`** hygiene when scenes land); **`validate:all`** green on implementation pass. Ongoing hygiene: **`ui-design-system.md`** + baseline JSON (**no** separate open umbrella row after **TECH-67** closure).
  - Depends on: none (soft: **TECH-67** program context)
  - Related: **TECH-67**, **TECH-33**, **BUG-53**

- [x] **TECH-28** — Unity Editor: **agent diagnostics** (context JSON + sorting debug export) (2026-04-02)
  - Type: tooling / agent workflow
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`, `tools/reports/` (generated output; see `.gitignore`), `.gitignore`
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** **Territory Developer → Reports → Export Agent Context** writes `tools/reports/agent-context-{timestamp}.json` (`schema_version`, `exported_at_utc`, scene, selection, bounded **Cell** / **HeightMap** / **WaterMap** sample via **`GridManager.GetCell`** only). **Export Sorting Debug (Markdown)** writes `sorting-debug-{timestamp}.md` in **Play Mode** using **`TerrainManager`** sorting APIs and capped **`SpriteRenderer`** `sortingOrder` listing. **Agents:** reference `@tools/reports/agent-context-….json` or `@tools/reports/sorting-debug-….md` in Cursor prompts (paths under repo root). `docs/agent-tooling-verification-priority-tasks.md` tasks 2, 23. **Canonical expected behavior** and troubleshooting: `ia/specs/unity-development-context.md` §10; if menus or **Sorting** export regress, file a new **open** row on [`BACKLOG.md`](BACKLOG.md) (attach **Console** output and sample exports per §10 **Verification**).
  - Depends on: none

- [x] **TECH-25** — Incremental authoring milestones for `unity-development-context.md` (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `ia/specs/unity-development-context.md`; `projects/agent-friendly-tasks-with-territory-ia-context.md` (pointer wording); `docs/agent-tooling-verification-priority-tasks.md`; `BACKLOG.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts` (backlog smoke test → **TECH-28**)
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** Merged milestone slices **M1**–**M7** into **`unity-development-context.md`** — lifecycle (**`ZoneManager`**, **`WaterManager`**, coroutine/`Invoke` examples), Inspector / **Addressables** guard, **`SerializeField`** scan note + **`DemandManager`**, prefab/**YAML**/**meta** cautions, **`GridManager`** + **`GridSortingOrderService`** sorting entry points (formula still geo §7), **`GeographyManager`** init + **BUG-16** pointer, **`GetComponent`** per-frame row, glossary (**Geography initialization**), §1 roadmap (**TECH-18**, **TECH-26**, **TECH-28**). **`npm run verify`** under **`tools/mcp-ia-server/`**.
  - Depends on: **TECH-20** (umbrella spec)

- [x] **TECH-20** — In-repo Unity development context for agents (spec + concept index) (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `ia/specs/unity-development-context.md`; `AGENTS.md`; `ia/rules/agent-router.md`; `tools/mcp-ia-server/src/config.ts` (`unity` / `unityctx` → `unity-development-context`); `docs/mcp-ia-server.md`; `tools/mcp-ia-server/README.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts`; `tools/mcp-ia-server/tests/tools/build-registry.test.ts`; `tools/mcp-ia-server/tests/tools/config-aliases.test.ts`; [`ia/specs/REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) (router authoring note)
  - Spec: [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) (authoritative); project spec removed after closure
  - Notes: **Completed (verified per user):** First-party **Unity** reference for **MonoBehaviour** / **Inspector** / **`FindObjectOfType`** / execution order; **territory-ia** `list_specs` key `unity-development-context`; **agent-router** row avoids **`router_for_task`** token collisions with geography queries (see **REFERENCE-SPEC-STRUCTURE**). Unblocks **TECH-18** `unity_context_section`; follow-up polish shipped in **TECH-25** (completed).
  - Depends on: none

- [x] **BUG-37** — Manual **street** drawing clears **buildings** and **zones** on cells adjacent to the **road stroke** (2026-04-02)
  - Type: bug
  - Files: `TerrainManager.cs` (`RestoreTerrainForCell` — **BUG-37**: skip `PlaceFlatTerrain` / slope rebuild when `GridManager.IsCellOccupiedByBuilding`; sync **HeightMap** / **cell** height + transform first); `RoadManager.cs`, `PathTerraformPlan.cs` (call path unchanged)
  - Spec: `ia/projects/BUG-37.md`; `ia/specs/isometric-geography-system.md` §14 (manual **streets**)
  - Notes: **Completed (verified per user):** Commit/AUTO `PathTerraformPlan.Apply` Phase 2/3 was refreshing **Moore** neighbors and stacking **grass** under **RCI** **buildings** / footprint **cells** (preview skipped **Apply**, so only commit showed the bug). **Fix:** preserve development by returning after height/sync when the **cell** is **building**-occupied. **Follow-up:** **BUG-52** if **AUTO** zoning shows persistent **grass** buffers beside new **streets** (investigate correlation).
  - Depends on: none

- [x] **TECH-22** — Canonical terminology pass on **reference specs** (`ia/specs`) (2026-04-02)
  - Type: documentation / refactor (IA)
  - Files: `ia/specs/glossary.md`, `isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, `REFERENCE-SPEC-STRUCTURE.md`; `BACKLOG.md` (one **map border** wording fix); `tools/mcp-ia-server/tests/parser/fuzzy.test.ts` (§13 heading fixture); [`ia/projects/TECH-22.md`](ia/projects/TECH-22.md)
  - Spec: [`ia/specs/glossary.md`](ia/specs/glossary.md); [`ia/specs/REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) (deprecated → canonical table + MCP **`glossary_discover`** hint)
  - Notes: **Completed (verified per user):** Glossary/spec alignment — **map border** vs local **cell** edges; umbrella **street or interstate**; **road validation pipeline** wording; §13 retitled in geo; authoring table in `REFERENCE-SPEC-STRUCTURE.md`. `AGENTS.md` / MCP `config.ts` unchanged (no spec key changes).
  - Depends on: none

- [x] **FEAT-45** — MCP **`glossary_discover`**: keyword-style discovery over **glossary** rows (2026-04-02)
  - Type: feature (IA / tooling)
  - Files: `tools/mcp-ia-server/src/tools/glossary-discover.ts`, `tools/mcp-ia-server/src/tools/glossary-lookup.ts`, `tools/mcp-ia-server/src/parser/glossary-discover-rank.ts`, `tools/mcp-ia-server/src/index.ts`, `tools/mcp-ia-server/package.json`, `tools/mcp-ia-server/tests/parser/glossary-discover-rank.test.ts`, `tools/mcp-ia-server/tests/tools/glossary-discover.test.ts`, `tools/mcp-ia-server/scripts/verify-mcp.ts`, [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`docs/mcp-markdown-ia-pattern.md`](docs/mcp-markdown-ia-pattern.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md), [`AGENTS.md`](AGENTS.md), [`ia/rules/agent-router.md`](ia/rules/agent-router.md), [`ia/rules/mcp-ia-default.md`](ia/rules/mcp-ia-default.md)
  - Spec: [`ia/projects/FEAT-45.md`](ia/projects/FEAT-45.md)
  - Notes: **Completed (verified per user):** **`glossary_discover`** tool (territory-ia **v0.4.2**): Phase A deterministic ranking over **Term** / **Definition** / **Spec** / category; optional **`spec`** alias + **`registryKey`** from Spec cell; `hint_next_tools`; empty-query branch with fuzzy **term** suggestions. Agents must pass **English** in glossary tools; documented in MCP README, `docs/mcp-ia-server.md`, `AGENTS.md`, and Cursor rules. **`npm test`** / **`npm run verify`** under `tools/mcp-ia-server/`. **Phase B** (scoring linked spec body) deferred.
  - Depends on: **TECH-17** (MCP IA server — baseline)

- [x] **TECH-17** — MCP server for agentic Information Architecture (Markdown sources) (2026-04-02)
  - Type: infrastructure / tooling
  - Files: `tools/mcp-ia-server/`; `.mcp.json`; `ia/specs/*.md`, `ia/rules/*.md`, `AGENTS.md`, `ARCHITECTURE.md` as sources; `docs/mcp-ia-server.md`; docs updates in `AGENTS.md`, `ARCHITECTURE.md`, `ia/rules/project-overview.md`, `agent-router.md` (MCP subsection)
  - Notes: **Shipped:** Node + `@modelcontextprotocol/sdk` stdio server with tools including `list_specs`, `spec_outline`, `spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `backlog_issue` (BACKLOG.md by id); spec aliases; fuzzy glossary/section fallbacks; `spec_section` input aliases for LLM mis-keys; parse cache; stderr timing; `node:test` + c8 coverage on `src/parser/**`; `npm run verify`. **Reference:** `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md` (generic pattern), `tools/mcp-ia-server/README.md`. **Retrospective / design history:** `ia/projects/TECH-17a.md`, `TECH-17b.md`, `TECH-17c.md` (§9–11 post-ship; delete when no longer needed).
  - Depends on: none

- [x] **BUG-51** — Diagonal / corner-up land slopes vs roads: design closure (2026-04-01)
  - Type: bug (closed by policy + implementation, not by fixing prefab-on-diagonal art)
  - Files: `RoadStrokeTerrainRules.cs`, `RoadManager.cs` (`TryBuildFilteredPathForRoadPlan`, `TryPrepareRoadPlacementPlanLongestValidPrefix`, `TryPrepareDeckSpanPlanFromAdjacentStroke`), `GridPathfinder.cs`, `InterstateManager.cs` (`IsCellAllowedForInterstate`), `RoadPrefabResolver.cs`, `TerraformingService.cs`, `Cell.cs` (route-first / BUG-51 technical work — see spec)
  - Spec: `ia/specs/roads-system.md` (land slope stroke policy, route-first paragraph), `ia/specs/isometric-geography-system.md` §3.3.3–§3.3.4, §13.10
  - Notes: **Closed (verified):** The original report asked for **correct road prefabs on diagonal and corner-up terrain**. The chosen resolution was **not** to fully support roads on those land slope types. Instead, **road strokes are invalid on land that is not flat and not a cardinal ramp** (`TerrainSlopeType`: `Flat`, `North`, `South`, `East`, `West` only). Pure diagonals (`NorthEast`, …) and corner-up types (`*Up`) are excluded. **Behavior:** silent **prefix truncation** — preview and commit only include cells up to the last allowed cell; cursor may keep moving diagonally without extending preview. **Scope:** manual, AUTO, and interstate. **First cell blocked:** no placement, no notification. **`Road cannot extend further…`** is **not** posted when the only issue is no slope-valid prefix (e.g. stroke starts on diagonal). **Exceptions in stroke truncation / walkability:** path cells at `HeightMap` height ≤ 0 (wet span) and `IsWaterSlopeCell` shore tiles still pass the truncator so FEAT-44 bridges are not cut. **Still in codebase:** BUG-51 **route-first** resolver topology (`pathOnlyNeighbors`), `Cell` path hints, terraform preservation on diagonal wedge when `preferSlopeClimb && dSeg == 0`, `GetWorldPositionForPrefab` anchoring — documented under roads spec **BUG-51 (route-first)**.
  - Depends on: none

- [x] **BUG-47** — AUTO simulation: perpendicular street stubs, reservations, junction prefab refresh (2026-04-01)
  - Type: bug / feature
  - Files: `AutoRoadBuilder.cs` (`FindPath*ForAutoSimulation`, `HasParallelRoadTooClose` + `excludeAlongDir`, batch prefab refresh), `AutoSimulationRoadRules.cs`, `AutoZoningManager.cs`, `RoadCacheService.cs`, `GridPathfinder.cs`, `GridManager.cs`, `IGridManager.cs`, `RoadManager.cs` (`RefreshRoadPrefabsAfterBatchPlacement`, bridge-deck skip); `ia/specs/isometric-geography-system.md` §13.9, `ia/rules/roads.md`, `ia/rules/simulation.md`
  - Spec: `ia/specs/isometric-geography-system.md` §13.9
  - Notes: **Completed (verified in-game):** AUTO can trace perpendicular stubs/connectors and crossings: land = grass/forest/undeveloped light zoning; dedicated AUTO pathfinder; road frontier and extension cells include that class; perpendicular branches pass parent-axis `excludeAlongDir` in `HasParallelRoadTooClose`; auto-zoning skips axial corridor and extension cells. **Visual:** `PlaceRoadTileFromResolved` did not refresh neighbors; added deduplicated per-tick refresh (`RefreshRoadPrefabsAfterBatchPlacement`), skipping bridge deck re-resolve. **Lessons:** any batch `FromResolved` flow must document explicit junction refresh; keep generic `FindPath` separate from AUTO pathfinding.
  - Depends on: none

- [x] **FEAT-44** — High-deck water bridges: cliff banks, uniform deck height, manual + AUTO placement (2026-03-30)
  - Type: feature
  - Files: `RoadManager.cs` (`TryPrepareDeckSpanPlanFromAdjacentStroke`, `TryPrepareLockedDeckSpanBridgePlacement`, `TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord`, `TryExtendCardinalStreetPathWithBridgeChord`, `StrokeHasWaterOrWaterSlopeCells`, `StrokeLastCellIsFirmDryLand`, FEAT-44 validation / chord walk), `TerraformingService.cs` (`TryBuildDeckSpanOnlyWaterBridgePlan`, `TryAssignWaterBridgeDeckDisplayHeight`), `AutoRoadBuilder.cs` (`TryGetStreetPlacementPlan`, `BuildFullSegmentInOneTick` — atomic water-bridge completion), `PathTerraformPlan.cs` (`HasTerraformHeightMutation`, deck display height docs), `RoadPrefabResolver.cs` (bridge deck resolution); rules/spec: `ia/rules/roads.md`, `ia/specs/isometric-geography-system.md` §13
  - Spec: `ia/specs/isometric-geography-system.md` §13 (bridges, shared validation, AUTO behavior)
  - Notes: **Completed (verified per user):** **Manual:** locked lip→chord preview uses a **deck-span-only** plan (`TerraformAction.None`, `TryBuildDeckSpanOnlyWaterBridgePlan`) so valid crossings are not blocked by cut-through / Phase-1 on complex tails; commit matches preview via shared `TryPrepareDeckSpanPlanFromAdjacentStroke`. **AUTO:** extends cardinal strokes with the same `WalkStraightChordFromLipThroughWetToFarDry` when the next step is wet/shore; runs longest-prefix plus programmatic deck-span and **prefers** deck-span when the stroke is wet or yields a longer expanded path. **AUTO water crossings** are **all-or-nothing in one tick**: require a **firm dry exit**, enough remaining tile budget for every new tile, a **single lump** `TrySpend` for the bridge, otherwise **`Revert`** — no half bridges. **Uniform deck:** one `waterBridgeDeckDisplayHeight` for all bridge deck prefabs on the span; assignment **prefers the exit (mesa) dry cell** after the wet run, then entry, then legacy lip fallback. **Description (issue):** Elevated road / bridge crossings across cliff-separated banks and variable terrain with correct clearance, FEAT-44 path rules, and consistent sorting/pathfinding per geography spec.

- [x] **BUG-50** — River–river junction: shore Moore topology, junction post-pass diagonal SlopeWater, upper-brink cliff water stacks + isometric anchor at shore grid (2026-03-28)
  - Type: bug / polish
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `IsOpenWaterForShoreTopology`, `NeighborMatchesShoreOwnerForJunctionTopology`, `ApplyJunctionCascadeShorePostPass`, `ApplyUpperBrinkShoreWaterCascadeCliffStacks`, `TryPlaceWaterCascadeCliffStack` / `waterSurfaceAnchorGrid`, `PlaceCliffWallStackCore` sorting reference), `WaterManager.Membership.cs`, `WaterMap.cs` (`TryFindRiverRiverSurfaceStepBetweenBodiesNear`)
  - Spec: `ia/specs/isometric-geography-system.md` **§12.8.1**
  - Notes: **Completed (verified):** Default shore masks use **`IsOpenWaterForShoreTopology`** (junction-brink dry land not counted). **`RefreshShoreTerrainAfterWaterUpdate`** runs **`ApplyJunctionCascadeShorePostPass`** (extended topology + **`forceJunctionDiagonalSlopeForCascade`**) then **`ApplyUpperBrinkShoreWaterCascadeCliffStacks`** ( **`CliffSouthWater`** / **`CliffEastWater`** on **`UpperBrink`** only). Cascade **Y** anchor and sorting use **`waterSurfaceAnchorGrid`** at the **shore** cell so wide-river banks align with the isometric water plane. **`ARCHITECTURE.md`** Water bullet and **§12.8.1** document pipeline and authority.

- [x] **BUG-45** — Adjacent water bodies at different surface heights: merge, prefab refresh at intersections, straight slope/cliff transitions (2026-03-27)
  - Type: bug / polish
  - Files: `WaterManager.cs` (`UpdateWaterVisuals` — Pass A/B, `ApplyLakeHighToRiverLowContactFallback`), `WaterMap.cs` (`ApplyMultiBodySurfaceBoundaryNormalization`, `ApplyWaterSurfaceJunctionMerge`, `IsLakeSurfaceStepContactForbidden`, lake–river fallback), `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `SelectPerpendicularWaterCornerPrefabs`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`), `ProceduralRiverGenerator.cs` / `TestRiverGenerator.cs` as applicable; `docs/water-junction-merge-implementation-plan.md`
  - Spec: `ia/specs/isometric-geography-system.md` — **§5.6.2**, **§12.7**
  - Notes: **Completed (verified):** Pass A/B multi-body surface handling; lake-at-step exclusions; full-cardinal **`RefreshWaterCascadeCliffs`** (incl. mirror N/W lower pool); perpendicular multi-surface shore corner preference; lake-high vs river-low rim fallback. **Assign** `cliffWaterSouthPrefab` / **`cliffWaterEastPrefab`** on `TerrainManager` for visible cascades (west→east steps use **East**). **Map border** water × brown **cliff** seal: **geo** §5.7 / **Recent archive** **BUG-44**; bridges × cliff-water **BUG-43**; optional N/W cascade art (camera).

- [x] **BUG-42** — Water shores & cliffs: terrain + water (lakes + rivers); water–water cascades; shore coherence — merged **BUG-33** + **BUG-41** (2026-03-26)
  - Type: bug / feature
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `PlaceCliffWalls`, `PlaceCliffWallStackCore`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`, `ClampShoreLandHeightsToAdjacentWaterSurface`, `IsLandEligibleForWaterShorePrefabs`), `WaterManager.cs` (`PlaceWater`, `UpdateWaterVisuals`), `ProceduralRiverGenerator.cs` (inner-corner shore continuity §13.5), `ProceduralRiverGenerator` / `WaterMap` as applicable; `cliffWaterSouthPrefab` & `cliffWaterEastPrefab` under `Assets/Prefabs/`
  - Spec: `ia/specs/isometric-geography-system.md` (§2.4.1 shore band height coherence, §4.2 gate, §5.6–§5.7, §5.6.2 water–water cascades, §12–§13, §15)
  - Notes: **Completed (verified):** **Shore band height coherence** — `HeightMap` clamp on Moore shore ring vs adjacent logical surface; water-shore prefab gate uses **`V = max(MIN_HEIGHT, S−1)`** vs **land height**. **River** inner-corner promotion + bed assignment guard. **Water–water cascades** — `RefreshWaterCascadeCliffs` after full `UpdateWaterVisuals`; **`PlaceCliffWallStackCore`** shared with brown cliffs; cascade Y anchor matches **water tile** (`GetWorldPositionVector` at `visualSurfaceHeight` + `tileHeight×0.25`). **Out of scope / follow-up:** visible **north/west** cliff meshes (camera); **map border** brown **cliff** seal vs water — **geo** §5.7 / **Recent archive** **BUG-44**; bridges × cliff-water (**BUG-43**); optional **N/S/E/W** “waterfall” art beyond **S/E** stacks — track separately if needed. **Multi-body junctions:** completed **[BUG-45](#bug-45)** (2026-03-27).

- [x] **BUG-33** — Lake shore / edge prefab bugs — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **BUG-41** — River corridors: shore prefabs + cliff stacks — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **FEAT-38** — Procedural rivers during geography / terrain generation (2026-03-24)
  - Type: feature
  - Files: `GeographyManager.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterMap.cs`, `WaterManager.cs`, `WaterBody.cs`, `Cell.cs` / `CellData.cs` (as needed)
  - Spec: `ia/specs/isometric-geography-system.md` §12–§13
  - Notes: **Completed:** `WaterBody` classification + merge (river vs lake/sea); `GenerateProceduralRiversForNewGame()` after `InitializeWaterMap`, before interstate; `ProceduralRiverGenerator` (BFS / forced centerline, border margin, transverse + longitudinal monotonicity, `WaterMap` river bodies). **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (merged **BUG-33** + **BUG-41**, 2026-03-26).

- [x] **BUG-39** — Bay / inner-corner shore prefabs: cliff art alignment vs stacked cliffs (2026-03-24)
  - Type: fix (art vs code)
  - Files: `TerrainManager.cs` (`GetCliffWallSegmentWorldPositionOnSharedEdge`, `PlaceCliffWallStack`), `Assets/Sprites/Cliff/CliffEast.png`, `Assets/Sprites/Cliff/CliffSouth.png`, cliff prefabs under `Assets/Prefabs/Cliff/`
  - Notes: **Resolved:** Inspector-tunable per-face placement (`cliffWallSouthFaceNudgeTileWidthFraction` / `HeightFraction`, `cliffWallEastFaceNudgeTileWidthFraction` / `HeightFraction`) and water-shore Y offset (`cliffWallWaterShoreYOffsetTileHeightFraction`) so cliff sprites align with the south/east diamond faces and water-shore cells after art was moved inside the textures. Further shore/gap / cascade work → completed **[BUG-42](#bug-42)** (2026-03-26) where applicable.

- [x] **BUG-40** — Shore cliff walls draw in front of nearer (foreground) water tiles (2026-03-24)
  - Type: fix (sorting / layers)
  - Files: `TerrainManager.cs` (`PlaceCliffWallStack`, `GetMaxCliffSortingOrderFromForegroundWaterNeighbors`)
  - Notes: **Resolved:** Cliff `sortingOrder` is capped against registered **foreground** water neighbors (`nx+ny < highX+highY`) using their `Cell.sortingOrder`, so brown cliff segments do not draw above nearer water tiles. See `ia/specs/isometric-geography-system.md` §15.2.

- [x] **BUG-36** — Lake generation: seeded RNG (reproducible + varied per New Game) (2026-03-24)
  - Type: fix
  - Files: `WaterMap.cs` (`InitializeLakesFromDepressionFill`, `LakeFillSettings`), `WaterManager.cs`, `MapGenerationSeed.cs` (`GetLakeFillRandomSeed`), `TerrainManager.cs` (`EnsureGuaranteedLakeDepressions` shuffle)
  - Notes: `LakeFillSettings.RandomSeed` comes from map generation seed; depression-fill uses a seeded `System.Random`; bowl shuffle uses a derived seed. Same template no longer forces identical lake bodies across unrelated runs; fixed seed still reproduces. Spec: `ia/specs/isometric-geography-system.md` §12.3. **Related:** **BUG-08**, **FEAT-38**.

- [x] **BUG-35** — Load Game: multi-cell buildings — grass on footprint (non-pivot) could draw above building; 1×1 grass + building under one cell (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs` (`DestroyCellChildren`), `ZoneManager.cs` (`PlaceZoneBuilding`, `PlaceZoneBuildingTile`), `BuildingPlacementService.cs` (`UpdateBuildingTilesAttributes`), `GridSortingOrderService.cs` (`SetZoneBuildingSortingOrder`, `SyncCellTerrainLayersBelowBuilding`)
  - Notes: `DestroyCellChildren(..., destroyFlatGrass: true)` when placing/restoring **RCI and utility** buildings so flat grass prefabs are not kept alongside the building (runtime + load). Multi-cell `SetZoneBuildingSortingOrder` still calls **grass-only** `SyncCellTerrainLayersBelowBuilding` for each footprint cell. **BUG-20** may be re-verified against this. Spec: [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §7.4.

- [x] **BUG-34** — Load Game: zone buildings / utilities render under terrain or water edges (`sortingOrder` snapshot vs building layer) (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs`, `ZoneManager.cs`, `TerrainManager.cs`, `BuildingPlacementService.cs`, `GridSortingOrderService.cs`, `Cell.cs`, `CellData.cs`, `GameSaveManager.cs`
  - Notes: Deterministic restore order; open water and shores aligned with runtime sorting; multi-cell RCI passes `buildingSize`; post-load building sort pass; optional grass sync via `SyncCellTerrainLayersBelowBuilding`. **BUG-35** (completed 2026-03-22) adds `destroyFlatGrass` on building placement/restore. Spec summary: `ia/specs/isometric-geography-system.md` §7.4.

- [x] **FEAT-37c** — Persist `WaterMapData` in saves + snapshot load (no terrain/water regen on load) (2026-03-22)
  - Type: feature
  - Files: `GameSaveManager.cs`, `WaterManager.cs`, `TerrainManager.cs`, `GridManager.cs`, `Cell.cs`, `CellData.cs`, `WaterBodyType.cs`
  - Notes: `GameSaveData.waterMapData`; `WaterManager.RestoreWaterMapFromSaveData`; `RestoreGridCellVisuals` applies saved `sortingOrder` and prefabs; legacy saves without `waterMapData` supported. **Follow-up:** building vs terrain sorting on load — **BUG-34** (completed); multi-cell footprint / grass under building — **BUG-35** (completed 2026-03-22).

- [x] **FEAT-37b** — Variable-height water: sorting, roads/bridges, `SEA_LEVEL` removal (no lake shore prefab scope) (2026-03-24)
  - Type: feature + refactor
  - Files: `GridSortingOrderService.cs`, `RoadPrefabResolver.cs`, `RoadManager.cs`, `AutoRoadBuilder.cs`, `ForestManager.cs`, `TerrainManager.cs` (water height queries, bridge/adjacency paths — **exclude** shore placement methods)
  - Notes: Legacy `SEA_LEVEL` / `cell.height == 0` assumptions removed or generalized for sorting, roads, bridges, non-shore water adjacency. Shore tiles **not** in scope (37a + completed **[BUG-42](#bug-42)**). Verified in Unity.

- [x] **BUG-32** — Lakes / `WaterMap` water not shown on minimap (desync with main map) (2026-03-23)
  - Type: fix (UX / consistency)
  - Files: `MiniMapController.cs`, `GeographyManager.cs`, `WaterManager.cs`, `WaterMap.cs`
  - Notes: Minimap water layer aligned with `WaterManager` / `WaterMap` (rebuild timing, `GetCellColor`, layer toggles). Verified in Unity.

- [x] **FEAT-37a** — WaterBody + WaterMap depression-fill (lake data & procedural placement) (2026-03-22)
  - Type: feature + refactor
  - Files: `WaterBody.cs`, `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `LakeFeasibility.cs`
  - Notes: `WaterBody` + per-cell body ids; `WaterMap.InitializeLakesFromDepressionFill` + `LakeFillSettings` (depression-fill, bounded pass, artificial fallback, merge); `LakeFeasibility` / `EnsureGuaranteedLakeDepressions` terrain bowls; `WaterMapData` v2 + legacy load; centered 40×40 template + extended terrain. **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (2026-03-26); **FEAT-37b** / **FEAT-37c** completed; building sort on load **BUG-34** (completed); multi-cell footprint / grass under building **BUG-35** (completed 2026-03-22).

---

## Pre-2026-03-22 archive

- [x] **TECH-12** — Water system refactor: planning pass (objectives, rules, scope, child issues) (2026-03-21)
  - Type: planning / documentation
  - Files: `ia/specs/isometric-geography-system.md` (§12), `BACKLOG.md` (FEAT-37, BUG-08 splits), `ARCHITECTURE.md` (Terrain / Water as needed)
  - Notes: **Goal:** Before implementation of **FEAT-37**, produce a single agreed definition of **objectives**, **rules** (data + gameplay + rendering), **known bugs** to fold in, **non-goals / phases**, and **concrete child issues** (IDs) ordered for development. Link outcomes in this spec and in `FEAT-37`. Overlaps **BUG-08** (generation), **FEAT-15** (ports/sea). **Does not** implement code — only backlog + spec updates and issue breakdown.
  - Depends on: nothing (blocks structured FEAT-37 execution)

- [x] **BUG-30** — Incorrect road prefabs when interstate climbs slopes (2026-03-20)
  - Type: fix
  - Files: `TerraformingService.cs`, `RoadPrefabResolver.cs`, `PathTerraformPlan.cs`, `RoadManager.cs` (shared pipeline)
  - Notes: Segment-based Δh for scale-with-slopes; corner/upslope cells use `GetPostTerraformSlopeTypeAlongExit` (aligned with travel); live-terrain fallback + `RestoreTerrainForCell` force orthogonal ramp when `action == None` and cardinal `postTerraformSlopeType`. Spec: `ia/specs/isometric-geography-system.md` §14.7. Verified in Unity.

- [x] **TECH-09** — Remove obsolete `TerraformNeeded` from TerraformingService (2026-03-20)
  - Type: refactor (dead code removal)
  - Files: `TerraformingService.cs`
  - Notes: Removed `[Obsolete]` `TerraformNeeded` and `GetOrthogonalFromRoadDirection` (only used by it). Path-based terraforming uses `ComputePathPlan` only.

- [x] **TECH-10** — Fix `TerrainManager.DetermineWaterSlopePrefab` north/south sea logic (2026-03-20)
  - Type: fix (code health)
  - Files: `TerrainManager.cs`
  - Notes: Replaced impossible `if (!hasSeaLevelAtNorth)` under `hasSeaLevelAtNorth` with NE/NW corner handling and East-style branch for sea north+south strips (`southEast` / `southEastUpslope`). South-only coast mirrors East; removed unreachable `hasSeaLevelAtSouth` else (handled by North block first).

- [x] **TECH-11** — Namespace `Territory.Terrain` for TerraformingService and PathTerraformPlan (2026-03-20)
  - Type: refactor
  - Files: `TerraformingService.cs`, `PathTerraformPlan.cs`, `ARCHITECTURE.md`, `ia/rules/project-overview.md`
  - Notes: Wrapped both types in `namespace Territory.Terrain`. Dependents already had `using Territory.Terrain`. Docs updated to drop "global namespace" examples for these files.

- [x] **TECH-08** — UI design system docs: TECH-07 (ControlPanel sidebar) ticketed and wired (2026-03-20)
  - Type: documentation
  - Files: `BACKLOG.md` (TECH-07), `docs/ui-design-system-project.md` (Backlog bridge), `docs/ui-design-system-context.md` (Toolbar — ControlPanel), `ia/specs/ui-design-system.md` (§3.3 layout variants), `ARCHITECTURE.md`, `AGENTS.md`, `ia/rules/managers-guide.md`
  - Notes: This issue records the documentation and cross-links only. **TECH-07** (executable **ControlPanel** layout) was later completed manually in **`MainScene.unity`** and archived (**Recent archive**, **2026-04-04**).

- [x] **BUG-25** — Fix bugs in manual street segment drawing (2026-03-19)
  - Type: fix
  - Files: `RoadManager.cs`, `RoadPrefabResolver.cs` (also: `GridManager.cs`, `TerraformingService.cs`, `PathTerraformPlan.cs`, `GridPathfinder.cs` for prior spec work)
  - Notes: Junction/T/cross prefabs: `HashSet` path membership + `SelectFromConnectivity` for 3+ cardinal neighbors in `RoadPrefabResolver`; post-placement `RefreshRoadPrefabAt` pass on placed cells in `TryFinalizeManualRoadPlacement`. Spec: `ia/specs/isometric-geography-system.md` §14. Optional follow-up: `postTerraformSlopeType` on refresh, crossroads prefab audit.
- [x] **BUG-27** — Interstate pathfinding bugs (2026-03-19)
  - Border endpoint scoring (`ComputeInterstateBorderEndpointScore`), sorted candidates, `PickLowerCostInterstateAStarPath` (avoid-high vs not, pick cheaper), `InterstateAwayFromGoalPenalty` and cost tuning in `RoadPathCostConstants`. Spec: `ia/specs/isometric-geography-system.md` §14.5.
- [x] **BUG-29** — Cut-through: high hills cut through disappear leaving crater (2026-03-19)
  - Reject cut-through when `maxHeight - baseHeight > 1`; cliff/corridor context in `TerrainManager` / `PathTerraformPlan`; map-edge margin `cutThroughMinCellsFromMapEdge`; Phase 1 validation ring in `PathTerraformPlan`; interstate uses `forbidCutThrough`. Spec: `ia/specs/isometric-geography-system.md` §14.6.

- [x] **FEAT-24** — Auto-zoning for Medium and Heavy density (2026-03-19)
- [x] **BUG-23** — Interstate route generation is flaky; never created in New Game flow (2026-03-19)
- [x] **BUG-26** — Interstate prefab selection and pathfinding improvements (2026-03-19)
  - Elbow audit, validation, straightness bonus, slope cost, parallel sampling, bridge approach (Rule F), cut-through expansion. Follow-up: BUG-27 / BUG-29 / **BUG-30** completed 2026-03-19–2026-03-20; remaining: BUG-28 (sorting), BUG-31 (prefabs at entry/exit).
- [x] **TECH-06** — Documentation sync: specs aligned with backlog and rules; BUG-26, FEAT-36 added; ARCHITECTURE, file counts, helper services updated; zoning plan translated to English (2026-03-19)
- [x] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs (2026-03-18)
- [x] **FEAT-34** — Zoning and building on slopes (2026-03-16)
- [x] **FEAT-33** — Urban remodeling: expropriations and redevelopment (2026-03-12)
- [x] **FEAT-31** — Auto roads grow toward high desirability areas (2026-03-12)
- [x] **FEAT-30** — Mini map layer toggles + desirability visualization (2026-03-12)
- [x] **BUG-24** — Growth budget not recalculated when income changes (2026-03-12)
- [x] **BUG-06** — Streets should not cost so much energy (2026-03-12)
- [x] **FEAT-32** — More streets and intersections in central and mid-urban areas (AUTO mode) (2026-03-12)
- [x] **BUG-22** — Auto zoning must not block street segment ends (AUTO mode) (2026-03-11)
- [x] **FEAT-25** — Growth budget tied to real income (2026-03-11)
- [x] **BUG-10** — `IndustrialHeavyZoning` never generates buildings (2026-03-11)
- [x] **FEAT-26** — Use desirability for building spawn selection (2026-03-10)
- [x] **BUG-07** — Better zone distribution: less random, more homogeneous by neighbourhoods/sectors (2026-03-10)
- [x] **FEAT-29** — Density gradient around urban centroids (AUTO mode) (2026-03-10)
- [x] **FEAT-17** — Mini-map (2026-03-09)
- [x] **FEAT-01** — Add delta change to total budget (e.g. $25,000 (+$1,200)) (2026-03-09)
- [x] **BUG-03** — Growth % sets amount instead of percentage of total budget (2026-03-09)
- [x] **BUG-02** — Taxes do not work (2026-03-09)
- [x] **BUG-05** — Do not remove cursor preview from buildings when constructing (2026-03-09)
- [x] **BUG-21** — Zoning cost is not charged when placing zones (2026-03-09)
- [x] **FEAT-02** — Add construction cost counter to mouse cursor (2026-03-09)
- [x] **FEAT-28** — Right-click drag-to-pan (grab and drag map) with inertia/fling (2026-03-09)
- [x] **BUG-04** — Pause mode stops camera movement; camera speed tied to simulation speed (2026-03-09)
- [x] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths (2026-03-09)
- [x] **FEAT-27** — Main menu with Continue, New Game, Load City, Options (2026-03-08)
- [x] **BUG-11** — Demand uses `Time.deltaTime` causing framerate dependency (2026-03-11)
- [x] **BUG-21** — Demand fix: unemployment-based RCI, remove environmental from demand, desirability for density (2026-03-11)
- [x] **BUG-01** — Save game, Load game and New game were broken (2026-03-07)
- [x] **BUG-09** — `Cell.GetCellData()` does not serialize cell state (2026-03-07)
- [x] **DONE** — Forest cannot be placed adjacent to water (2026-03)
- [x] **DONE** — Demolish forests at all heights + all building types (2026-03)
- [x] **DONE** — When demolishing forest on slope, correct terrain prefab restored via heightMap read (2026-03)
- [x] **DONE** — Interstate Road (2026-03)
- [x] **DONE** — CityNetwork sim (2026-03)
- [x] **DONE** — Forests on slopes (2026-03)
- [x] **DONE** — Growth simulation — AUTO mode (2026-03)
- [x] **DONE** — Simulation optimization (2026-03)
- [x] **DONE** — Codebase improvement for efficient AI agent contextualization (2026-03)