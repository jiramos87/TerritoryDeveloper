# Territory IA MCP server

Local [Model Context Protocol](https://modelcontextprotocol.io/) server for **Territory Developer** information architecture. It reads the **same** on-disk sources agents already use: `ia/specs/*.md`, `ia/rules/*.md`, `glossary.md`, and root docs registered in `buildRegistry()` (e.g. `AGENTS.md`, `ARCHITECTURE.md`).

Canonical integration notes: [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) and [`ia/rules/agent-router.md`](../../ia/rules/agent-router.md) (subsection **MCP — territory-ia**).

Abstract pattern (reusable outside this game): [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md).

## Prerequisites

- **Node.js 18+**
- Repository root as the process working directory (Cursor’s default when the workspace is this repo)

## Commands

| Command | Purpose |
|--------|---------|
| `npm install` | Install dependencies (run once under `tools/mcp-ia-server/`). |
| `npm run dev` | Run the server via `tsx` (stdio MCP). |
| `npm run build` | Emit JavaScript to `dist/` with `tsc`. |
| `npm start` | Run compiled `dist/index.js` (stdio MCP). |
| `npm test` | Unit tests (`node:test` + `tsx`) for parser and tool helpers. |
| `npm run test:watch` | Tests in watch mode. |
| `npm run test:coverage` | Parser + **ia-index** line coverage with **c8** (gate ≥90%). |
| `npm run verify` | From this directory: spawns the server the same way as Cursor (via repo root + `npx -y tsx …`) and exercises all **29** tools through the MCP SDK client. |
| `scripts/run-unity-bridge-once.ts` | From repo root: `npm run db:bridge-agent-context` — one **`unity_bridge_command`**-equivalent call (needs Postgres + Unity). Optional **`BRIDGE_TIMEOUT_MS`**. |
| `scripts/bridge-playmode-smoke.ts` | From repo root: `npm run db:bridge-playmode-smoke -- [seed_cell]` — same as MCP **`unity_bridge_command`** (`runUnityBridgeCommand`): **`get_play_mode_status` → `enter_play_mode` → `debug_context_bundle` → `exit_play_mode`**. Needs Postgres (**`DATABASE_URL`** or **`config/postgres-dev.json`**) and Unity Editor open on **`REPO_ROOT`** (run **`npm run unity:ensure-editor`** to auto-launch if not running). |
| Root **`npm run verify:local`** | **`validate:all`** (includes **`compute-lib:build`**) then **`post-implementation-verify.sh --skip-node-checks`**: **`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** **Unity** save/quit + relaunch, **`db:bridge-playmode-smoke`**. **`npm run verify:post-implementation`** is an alias. [`tools/scripts/verify-local.sh`](../../tools/scripts/verify-local.sh). See [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) and [`ARCHITECTURE.md`](../../ARCHITECTURE.md). |
| `npm run validate:fixtures` | **AJV** (JSON Schema Draft 2020-12): valid fixtures under `docs/schemas/fixtures/` must pass; invalid fixtures must fail. |
| `npm run generate:ia-indexes` | Writes `data/spec-index.json` and `data/glossary-index.json`. Pass `--check` to assert they match the generator (used in **CI**). |

From the **repository root**, `package.json` exposes `npm run validate:fixtures` and `npm run generate:ia-indexes` via `npm --prefix tools/mcp-ia-server`, and `npm run validate:dead-project-specs` (**TECH-50** completed — [`tools/validate-dead-project-spec-paths.mjs`](../validate-dead-project-spec-paths.mjs); see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md)). For an **ordered** post-change checklist (**CI** parity), see [`ia/skills/project-implementation-validation/SKILL.md`](../../ia/skills/project-implementation-validation/SKILL.md) (**TECH-52** completed).

## Cursor integration

The repo includes `.mcp.json`, which starts the server with:

- `npx -y tsx tools/mcp-ia-server/src/index.ts`
- `REPO_ROOT=.` so paths resolve against the workspace root

If your MCP host uses a different working directory, set `REPO_ROOT` to the **absolute** repository path, or adjust the command in `mcp.json`.

## Environment

| Variable | Meaning |
|----------|---------|
| `REPO_ROOT` | Root used to resolve `ia/specs`, `ia/rules`, and root markdown. Defaults to `process.cwd()`. |
| `DATABASE_URL` | Optional **PostgreSQL** URI; overrides committed **`config/postgres-dev.json`** when set. When no URL resolves (and not **CI**), **`project_spec_journal_*`** return **`db_unconfigured`**. |

## Tools (41)

| Tool | Description |
|------|-------------|
| **`backlog_issue`** | One matching issue by `issue_id` (e.g. `BUG-37`): searches `BACKLOG.md` (**open** rows) then `BACKLOG-ARCHIVE.md` (**`[x]`** completions). Returns `status`, `backlog_section`, `Files` / `Spec` / `Notes` / `Acceptance` / `depends_on`, `depends_on_status` (cited ids: `open` / `completed` / `not_in_backlog`, `soft_only`, `satisfied`), `raw_markdown`. **`soft_only`** includes `**ID** (soft: …)` when the parenthetical does not cite another issue id — see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) (**Issue kickoff workflow**). Not in `list_specs`. |
| **`list_specs`** | Registry entries: `key`, `relativePath`, `description`, `category`, `lineCount`. Optional filter `category` (e.g. `rule`). |
| **`spec_outline`** | Nested heading outline with line ranges. `spec` accepts key, filename, or alias (`geo` → `isometric-geography-system`, `roads` → `roads-system`, `unity` / `unityctx` → `unity-development-context`, `refspec` / `specstructure` → `reference-spec-structure`, …). |
| **`spec_section`** | Body for one section: canonical `spec` + `section` (id `13.4`, slug, title substring, or fuzzy typo). Aliases: `key` / `doc` → spec; `section_heading` / `heading` → section; numeric `section` coerced to string. `max_chars` or `maxChars` (default 3000) with `truncated` / `totalChars`. |
| **`spec_sections`** | Batch: `sections` array; each element uses the same shape as **`spec_section`**. Response `results` map keyed by `spec::section`. Optional `max_requests` (default 20, max 50). |
| **`stage_closeout_digest`** | Exactly one of `issue_id` or `spec_path` (`ia/projects/{ISSUE_ID}.md`). Returns structured closeout prep JSON (`schema_version` 1, section bodies, `cited_issue_ids`, keywords, heuristic `checklist_hints`). Read-only. Called N times per closing Stage by `stage-closeout-apply` (Sonnet pair-tail, seam #4). Renamed from `project_spec_closeout_digest` in lifecycle-refactor T7.14. |
| **`project_spec_journal_persist`** | Append **Decision Log** + **Lessons learned** from the project spec into **`ia_project_spec_journal`** (`DATABASE_URL` required). Optional `git_sha`. |
| **`project_spec_journal_search`** | Full-text / keyword overlap search over the journal; optional `raw_text_for_tokens`. |
| **`project_spec_journal_get`** | Full row by numeric `id`. |
| **`project_spec_journal_update`** | Patch `body_markdown` / `keywords` for a row. |
| **`glossary_discover`** | Keyword discovery over glossary rows (**English** `query` / `keywords` only — translate from the user’s language before calling). Scores **Term**, **Definition**, **Spec**, and category; returns ranked `term`, `specReference`, optional `spec` alias + `registryKey`, `matchReasons`, `score`. Params: `query` and/or `keywords` (alias `terms`); `q` / `search` for query; `max_results` / `maxResults` (default 10, cap 25). |
| **`glossary_lookup`** | Glossary row: exact (case-insensitive) then fuzzy; **`term` must be English** (glossary language). Bracket text like `[x,y]` normalized for matching. |
| **`router_for_task`** | Match task hints to specs using `agent-router.md` tables. Provide **`domain`** and/or **`files`** (max 40 paths); at least one required. Merges optional **`file_domain_hints`** from path heuristics with table rows. |
| **`invariants_summary`** | Invariants + guardrails from `invariants.md`. |
| **`list_rules`** | All `.md` rules with frontmatter (`alwaysApply`, `globs`, description). |
| **`rule_content`** | Rule markdown body without frontmatter. `rule: "roads"` resolves **`roads.md`** (use `spec_section` / `spec_outline` with alias `roads` for the **roads-system** spec). |
| **`isometric_world_to_grid`** | **Computational** ( **`tools/compute-lib`** ): planar `world_x` / `world_y` + `tile_width` / `tile_height` → `cell_x` / `cell_y` (**isometric-geography-system** §1.3; glossary **World ↔ Grid conversion**). Optional `origin_x` / `origin_y`. Returns `{ ok, cell_x, cell_y }` or `{ ok: false, error }` (`VALIDATION_ERROR` for bad input). |
| **`growth_ring_classify`** | **Computational:** urban **growth ring** from cell + centroids + `urban_cell_count` or `urban_radius` (simulation-system §Rings; parity **UrbanGrowthRingMath**). Returns `{ ok, data: { ring, urban_radius, distance_to_pole } }`. |
| **`grid_distance`** | **Computational:** **Chebyshev** or **Manhattan** distance between integer cells (not geo §10 pathfinding costs). Optional `map_width` / `map_height` (≤ 256). |
| **`pathfinding_cost_preview`** | **Computational v1:** Manhattan steps × `unit_cost_per_step` — labeled **approximation** only; not committed **A\*** / geo §10 costs. |
| **`geography_init_params_validate`** | **Computational:** Zod validation for **Geography initialization** interchange v1 (`artifact` + `schema_version` 1). Pass document fields as the tool argument object. |
| **`desirability_top_cells`** | **Stub:** returns `NOT_AVAILABLE` until **TECH-66** (`BACKLOG.md`) Unity **`batchmode`** export exists. |
| **`unity_bridge_command`** | **IDE agent bridge** (glossary): **`kind`** **`export_agent_context`** \| **`get_console_logs`** \| **`capture_screenshot`** \| **`enter_play_mode`** \| **`exit_play_mode`** \| **`get_play_mode_status`** \| **`get_compilation_status`** \| **`debug_context_bundle`** + **`timeout_ms`** (default **30000**, max **120000**). Inserts **`agent_bridge_job`** (**`request` jsonb** includes **`params`** per kind). **`export_agent_context`:** optional **`seed_cell`** (`"x,y"` Moore center, e.g. **`"3,0"`**; else selection or `(0,0)`). **`get_console_logs`:** optional **`since_utc`**, **`severity_filter`**, **`tag_filter`**, **`max_lines`** (1–2000). **`capture_screenshot`:** optional **`camera`** (GameObject name), **`filename_stem`**, **`include_ui`** (boolean, default false — **Game view** + Overlay UI via **`ScreenCapture`**; ignores **`camera`** when true). **`get_compilation_status`:** synchronous **`response.compilation_status`**. **`debug_context_bundle`:** required **`seed_cell`**; optional **`include_screenshot`**, **`include_console`**, **`include_anomaly_scan`** (default true); reuses console filters / **`max_lines`**; **`response.bundle`** combines export path, screenshot, lines, anomalies. Requires **`DATABASE_URL`**, migration **0008**, Unity on **REPO_ROOT** (run **`npm run unity:ensure-editor`** to auto-launch), **`AgentBridgeCommandRunner`**. On timeout, follow **timeout escalation protocol** in [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). |
| **`unity_bridge_get`** | **IDE agent bridge** (glossary): read **`agent_bridge_job`** by **`command_id`**; optional **`wait_ms`** for short blocking poll. |
| **`unity_compile`** | **IDE agent bridge** shortcut: enqueues **`get_compilation_status`** (same **`runUnityBridgeCommand`** path as **`unity_bridge_command`**). Argument: **`timeout_ms`**. |
| **`backlog_search`** | Keyword search across open / archived backlog issues. `query` (required), `scope` (`open` \| `archive` \| `all`, default `open`), `max_results` (1–50, default 10). Returns ranked results with `issue_id`, `title`, `type`, `status`, `section`, `score`, truncated `notes`. |
| **`invariant_preflight`** | Composite context tool: given `issue_id`, bundles invariants + router matches + relevant spec sections in one call. Infers domains from issue **Files**; fetches up to 6 spec sections (800 chars each). |
| **`findobjectoftype_scan`** | Static regex scan of C# files for `FindObjectOfType` / `FindObjectsOfType` in `Update` / `LateUpdate` / `FixedUpdate` methods. Optional `path` (default `Assets/Scripts/`). Returns `violation_count` + `violations[]` (`file`, `line`, `method`, `snippet`). |
| **`city_metrics_query`** | Read recent rows from **`city_metrics_history`** (Unity **`MetricsRecorder`** per-tick snapshots). Optional **`scenario_id`**, **`last_n_rows`** (1–500). Returns **`db_unconfigured`**, **`table_missing`**, or **`rows`**. |
| **`backlog_list`** | Structured list of backlog yaml records. Optional filters (`status`, `type`, `priority`, `section`). Read-only. |
| **`backlog_record_validate`** | Validate a backlog yaml record (in-memory) against the canonical schema before materialization. Returns `{ ok, errors }`. |
| **`reserve_backlog_ids`** | Reserve monotonic ids for one or more prefixes via the on-disk counter (`ia/state/id-counter.json`) under `flock`. Used before writing new yaml. |
| **`rule_section`** | Return a single Cursor rule (`.mdc`) heading section. Companion to **`rule_content`** for progressive disclosure. |
| **`parent_plan_validate`** | Validate `parent_plan` locator fields on one or more backlog yaml records (schema v2). Used by `validate:parent-plan-locator`. |
| **`plan_apply_validate`** | Validate a Plan-Apply tuple payload against the pair contract before the Sonnet pair-tail applies it. |
| **`master_plan_locate`** | Reverse-lookup: reads yaml `parent_plan` + `task_key` → returns `{ plan, step, stage, phase, task_key, row_line, row_raw }` for the matching master-plan row. |
| **`master_plan_next_pending`** | Given a master plan path, return the next non-Done task row (honors Done / archived / pending states). |
| **`csharp_class_summary`** | Summarize a C# class file: public surface, `[SerializeField]` refs, manager references, method signatures. Static regex + shape heuristics. |
| **`unity_callers_of`** | Static callers scan for a given C# method name across `Assets/Scripts/`. |
| **`unity_subscribers_of`** | Static subscribers scan for a given C# event name across `Assets/Scripts/`. |
| **`unity_bridge_lease`** | Acquire / release the Unity bridge single-agent lease (companion to `unity_bridge_command`). Coordinates concurrent agents on one Unity instance. |

**Examples (conceptual):**

- `backlog_issue` → `{ "issue_id": "BUG-37" }`
- `list_specs` → `{}`
- `spec_outline` → `{ "spec": "geo" }`
- `spec_section` → `{ "spec": "geo", "section": "13.4", "max_chars": 8000 }` (or `{ "key": "geo", "section_heading": 14 }`)
- `spec_sections` → `{ "sections": [ { "spec": "geo", "section": "1" }, { "spec": "roads", "section": "validation" } ] }`
- `stage_closeout_digest` → `{ "issue_id": "FEAT-49" }` or `{ "spec_path": "ia/projects/FEAT-49.md" }`
- `project_spec_journal_persist` → `{ "issue_id": "FEAT-49", "git_sha": "abc123…" }`
- `project_spec_journal_search` → `{ "query": "road stroke decision", "max_results": 8 }`
- `glossary_discover` → `{ "query": "manual street trace neighbors", "max_results": 8 }`
- `glossary_lookup` → `{ "term": "wet run" }`
- `router_for_task` → `{ "domain": "roads" }` or `{ "files": ["Assets/Scripts/Managers/GameManagers/GridManager.cs"] }`
- `rule_content` → `{ "rule": "roads", "max_chars": 50000 }`
- `isometric_world_to_grid` → `{ "world_x": 0, "world_y": 0, "tile_width": 1, "tile_height": 0.5 }`
- `growth_ring_classify` → `{ "cell": { "x": 0, "y": 0 }, "centroids": [{ "x": 0, "y": 0 }], "urban_cell_count": 500 }`
- `grid_distance` → `{ "ax": 0, "ay": 0, "bx": 1, "by": 1, "mode": "manhattan" }`
- `pathfinding_cost_preview` → `{ "from_cell": { "x": 0, "y": 0 }, "to_cell": { "x": 2, "y": 3 } }`
- `geography_init_params_validate` → body of `docs/schemas/fixtures/geography-init-params.good.json`
- `desirability_top_cells` → `{}`
- `unity_bridge_command` → `{ "timeout_ms": 40000 }` or `{ "kind": "enter_play_mode", "timeout_ms": 40000 }` (requires **Postgres** + **Unity** on **REPO_ROOT**; agent-led verification uses **40 s** initial + **escalation protocol**; max **120000**)
- `unity_bridge_get` → `{ "command_id": "<uuid from bridge command>", "wait_ms": 0 }`
- `unity_compile` → `{ "timeout_ms": 40000 }` (alias **`get_compilation_status`**; max **120000**)

## Closeout CLI (from repository root)

Shipped with **TECH-58**; scripts live under `scripts/` (they default `REPO_ROOT` to the repository root).

| Root `npm run` | Purpose |
|----------------|---------|
| `closeout:worksheet -- --issue FEAT-49` | Print Markdown worksheet; add `--json` for digest JSON only. |
| `closeout:dependents -- --issue FEAT-49` | List `file:line` hits for the id or `ia/projects/FEAT-49.md` (see script header for scan roots / limitations). |
| `closeout:verify` | Runs `validate:dead-project-specs` then `generate:ia-indexes --check`. Local convenience; **CI** **ia-tools** remains the gate for merges. |

## Architecture

**IDE agent bridge** tools use **full Zod object schemas** in `registerTool` so `@modelcontextprotocol/sdk` emits JSON Schema for **`tools/list`** with **`timeout_ms.maximum`** = **`UNITY_BRIDGE_TIMEOUT_MS_MAX`** (120000). Unit tests in `tests/tools/unity-bridge-command.test.ts` assert that contract (hosts such as **Cursor** validate arguments against the listed schema).

```mermaid
flowchart LR
  subgraph entry [Entry]
    IDX[index.ts]
  end
  subgraph core [Core]
    CFG[config.ts]
    PAR[parser/markdown-parser.ts]
    FZ[parser/fuzzy.ts]
    TP[parser/table-parser.ts]
    GP[parser/glossary-parser.ts]
    BKP[parser/backlog-parser.ts]
  end
  subgraph mcp [MCP tools]
    BI[backlog-issue.ts]
    LS[list-specs.ts]
    SO[spec-outline.ts]
    SS[spec-section.ts]
    SSB[spec-sections.ts]
    PSD[stage-closeout-digest.ts]
    GL[glossary-lookup.ts]
    GD[glossary-discover.ts]
    RF[router-for-task.ts]
    IS[invariants-summary.ts]
    LR[list-rules.ts]
    RC[rule-content.ts]
    CMP[compute/*.ts]
  end
  IDX --> CFG
  IDX --> BI
  IDX --> LS
  IDX --> SO
  IDX --> SS
  IDX --> SSB
  IDX --> PSD
  IDX --> GL
  IDX --> GD
  IDX --> RF
  IDX --> IS
  IDX --> LR
  IDX --> RC
  IDX --> CMP
  LS --> CFG
  LS --> PAR
  SO --> CFG
  SO --> PAR
  SS --> CFG
  SS --> PAR
  SS --> FZ
  SSB --> CFG
  SSB --> PAR
  SSB --> FZ
  PSD --> CFG
  GL --> CFG
  GL --> GP
  GL --> FZ
  GD --> CFG
  GD --> GP
  GD --> FZ
  RF --> CFG
  RF --> TP
  IS --> CFG
  LR --> CFG
  RC --> CFG
  BI --> CFG
  BI --> BKP
  GP --> PAR
  GP --> TP
```

- **`config.ts`** — Resolves repo root, scans specs, rules, and root docs; builds registry; spec key aliases; separate rule key resolution for `rule_content`.
- **`markdown-parser.ts`** — Frontmatter (`gray-matter`), heading tree, section extraction, optional **parse cache** keyed by absolute path.
- **`instrumentation.ts`** — Optional per-tool timing on **stderr** when **`DEBUG_MCP_COMPUTE=1`** (safe for stdio MCP).
- **Tools** — Handlers return JSON in MCP **text** content blocks.

## Adding a tool

1. Add `src/tools/<name>.ts` or `src/tools/compute/<name>.ts` exporting `registerYourTool(server, registry)` (computational tools live under **`tools/compute/`**).
2. Use `registerTool` on the `McpServer` instance with a Zod `inputSchema` shape (see existing tools).
3. Import and call the register function from `src/index.ts` (re-export from `tools/compute/index.ts` when adding to the computational family).
4. Document the tool here and in `docs/mcp-ia-server.md`; extend `scripts/verify-mcp.ts` if needed.

## Troubleshooting

| Symptom | Check |
|--------|--------|
| Tools return empty or wrong paths | `REPO_ROOT` must point at the **repository root** (folder containing `ia/specs`). |
| Cursor does not list the server | `.mcp.json` and Node/npm available; restart Cursor after config changes. |
| `verify` fails | Run from `tools/mcp-ia-server/` with repo dependencies installed (`npm install`); ensure working copy includes expected spec/rule files. |
| Slow repeated calls | Parsed documents are cached in memory until process exit; restart server after large doc edits. |

## Debugging

- Run `npm run dev` from `tools/mcp-ia-server/` with `REPO_ROOT` pointing at the repo; the process speaks MCP over stdio, so attach a client or use Cursor’s MCP log output.
- Tool timing lines appear on **stderr** when **`DEBUG_MCP_COMPUTE=1`** (e.g. `[territory-ia] spec_section 0.4ms`).

## Dependency note

The implementation uses **`@modelcontextprotocol/sdk`** (stable 1.x). The split package `@modelcontextprotocol/server` is a separate distribution; pin and imports should follow the SDK you install.

**`territory-compute-lib`** (`file:../compute-lib`) supplies **`isometric_world_to_grid`** and other **TECH-37**+ **pure** math. **`npm run verify`** runs **`npm run build`** in **`../compute-lib`** first so **`dist/`** exists (that folder is gitignored; **CI** builds it in **`ia-tools`** before **`mcp-ia-server`** **`npm ci`**).
