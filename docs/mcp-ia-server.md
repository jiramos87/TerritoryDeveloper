# Territory IA MCP server (territory-ia)

Recommended [Model Context Protocol](https://modelcontextprotocol.io/) server that exposes the **same on-disk information architecture** the repo already uses for agents: `.cursor/specs/*.md`, `.cursor/rules/*.mdc`, `glossary.md`, root docs such as `AGENTS.md` and `ARCHITECTURE.md` (via `buildRegistry()`), and **`BACKLOG.md`** (via the `backlog_issue` tool only—not listed in `list_specs`).

## Relationship to agent routing

Task-to-spec priorities match **`.cursor/rules/agent-router.mdc`**. That rule file also contains **“MCP — territory-ia”** with the default tool order when the server is enabled. The MCP does not replace rules or specs; it returns **slices** (sections, glossary rows, router table matches) so agents avoid loading multi-hundred-line files whole.

## Policy for Cursor agents

- **Terminology:** Tool names (`snake_case`) and descriptions should align with [`AGENTS.md`](../AGENTS.md) — glossary-backed domain terms, same vocabulary as specs and backlog. When adding or renaming tools, update this file and [`tools/mcp-ia-server/README.md`](../tools/mcp-ia-server/README.md) together with `registerTool` in code.
- **Glossary tools (`glossary_discover`, `glossary_lookup`):** The on-disk glossary is **English**. Agents must pass **English** in `query` / `keywords` / `term`. If the human conversation is in another language, **translate** the concepts into English (canonical domain words such as **street**, **road stroke**, **wet run**) before calling these tools. The server does not provide multilingual matching.
- **Workspace expectation:** This repo is set up so **Cursor** can run **territory-ia** from `.cursor/mcp.json`. Agents with tool access should **prefer MCP** for IA lookups in **Agent** chats.
- **Human / IDE settings:** Whether tool runs require a click to approve is controlled by **Cursor** (e.g. auto-run or approval settings for MCP/tools)—not by this repo. Adjust in Cursor **Settings** if you want fewer prompts.
- **Not guaranteed every turn:** The model still chooses whether to call a tool; repo rules and `AGENTS.md` exist to **bias** behavior toward MCP first.
- **Cursor User Rules (optional):** In **Cursor Settings → Rules for AI** (or your global user rules), add a one-liner such as: *In the territory-developer workspace, prefer territory-ia MCP tools (`backlog_issue`, then spec/glossary/router tools) before reading whole spec files.* The repo cannot enforce IDE settings; this duplicates the intent of `AGENTS.md` for every chat.

## Issue kickoff workflow

When starting work on **`BUG-XX` / `FEAT-XX` / `TECH-XX`** (etc.), call **`backlog_issue`** with `issue_id` first to get `Files`, `Spec`, `Notes`, `Acceptance`, `status`, and `raw_markdown` without loading all of `BACKLOG.md`. Then use `router_for_task` / `glossary_discover` / `glossary_lookup` / `spec_section` (or **`spec_sections`** when several slices are needed in one turn) as needed. Older issues may live only in `BACKLOG-ARCHIVE.md` (not covered by v1 `backlog_issue`).

## Project spec workflows (Cursor Skills)

Repo **Cursor Skills** define **ordered** MCP usage for `.cursor/projects/{ISSUE_ID}.md`:

- **Create new issue + project spec stub from a prompt:** [`.cursor/skills/project-new/SKILL.md`](../.cursor/skills/project-new/SKILL.md) (**TECH-56**, completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed**).
- **Review / enrich before code:** [`.cursor/skills/project-spec-kickoff/SKILL.md`](../.cursor/skills/project-spec-kickoff/SKILL.md).
- **Execute Implementation Plan:** [`.cursor/skills/project-spec-implement/SKILL.md`](../.cursor/skills/project-spec-implement/SKILL.md).
- **Close after verified work:** [`.cursor/skills/project-spec-close/SKILL.md`](../.cursor/skills/project-spec-close/SKILL.md) — persist IA (**glossary**, **reference specs**, **`ARCHITECTURE.md`**, rules, **`docs/`**), delete `.cursor/projects/{ISSUE_ID}.md`, `npm run validate:dead-project-specs`, **BACKLOG** **Completed** (user-confirmed). **TECH-58 helpers:** MCP **`project_spec_closeout_digest`** (structured extract from the project spec after **`backlog_issue`**), **`spec_sections`** (batch **`spec_section`** slices), and root **`npm run closeout:worksheet`**, **`closeout:dependents`**, **`closeout:verify`** (dead-spec validation + **`generate:ia-indexes --check`** — local convenience, **CI** remains authoritative via **IA tools** workflow).
- **Post-implementation validation (TECH-52, completed 2026-04-03):** [`.cursor/skills/project-implementation-validation/SKILL.md`](../.cursor/skills/project-implementation-validation/SKILL.md) — optional ordered **`npm`** checks (**dead project spec** paths, **`tools/mcp-ia-server`** tests, **`validate:fixtures`**, **`generate:ia-indexes --check`**, advisory **`verify`**) aligned with [`.github/workflows/ia-tools.yml`](../.github/workflows/ia-tools.yml). Use after **MCP** / **schema** / index-source edits; **project-spec-close** may reference this before the mandatory **`validate:dead-project-specs`** cascade step.

See also [`AGENTS.md`](../AGENTS.md) (Before You Start) and [`.cursor/skills/README.md`](../.cursor/skills/README.md).

## Tools (12)

| Tool | Role |
|------|------|
| `backlog_issue` | One **open** issue from `BACKLOG.md` by id (`issue_id`); structured fields + `raw_markdown`. Nested sub-items (e.g. TECH-01 under BUG-20) supported. Completed-only rows: `BACKLOG-ARCHIVE.md` (**Recent archive** / older sections). |
| `list_specs` | Discover registered documents (`key`, path, category, description). |
| `spec_outline` | Heading tree for a spec/rule/doc; supports aliases (`geo`, `roads`, `unity` / `unityctx` → `unity-development-context`, `refspec` / `specstructure` → `reference-spec-structure`, …). |
| `spec_section` | Body under one heading (id, slug, substring, or fuzzy heading match); `max_chars` truncation. Parameters `spec` + `section` are canonical; aliases `key`/`doc` for spec and `section_heading`/`heading` for section are accepted (numeric section coerced to string) so mis-keyed tool calls still succeed. |
| `spec_sections` | Batch variant: `sections` array of objects, each with the same fields as `spec_section` per slice. Returns `results` keyed by `spec::section` (duplicate keys get a numeric suffix). Optional `max_requests` (default 20, cap 50). |
| `project_spec_closeout_digest` | Parse `.cursor/projects/{ISSUE_ID}.md`: `issue_id` **xor** `spec_path`; returns `schema_version`, sections map, `cited_issue_ids`, `suggested_english_keywords`, heuristic `checklist_hints` for **project-spec-close** G1–I1 prep. Does not write files or author normative prose. |
| `glossary_discover` | Rough **English** keywords → ranked glossary terms (term, definition, Spec column); use before `glossary_lookup` when the exact term is unknown. Translate from the conversation if needed. |
| `glossary_lookup` | Glossary **English** term; exact then fuzzy (typos). Use the exact **Term** string from the table when possible. |
| `router_for_task` | Match a task domain to specs using `agent-router.mdc` tables. |
| `invariants_summary` | Numbered invariants and guardrails from `invariants.mdc`. |
| `list_rules` | All `.mdc` rules with frontmatter metadata. |
| `rule_content` | Rule body without YAML frontmatter; `rule` key resolves `roads` → `roads.mdc` (not the `roads-system` spec alias). |

## Implementation and operations

- **Code:** `tools/mcp-ia-server/` (TypeScript, `@modelcontextprotocol/sdk`).
- **Cursor:** `.cursor/mcp.json` launches `npx -y tsx tools/mcp-ia-server/src/index.ts` from the repo root; set `REPO_ROOT` if the host cwd is not the repository root.
- **Verify:** From `tools/mcp-ia-server/`, run `npm run verify` (spawns server like Cursor and calls tools via the SDK).
- **Full developer README:** `tools/mcp-ia-server/README.md`.

## PostgreSQL IA (TECH-44b) integration point for TECH-18

**TECH-18** will move **territory-ia** retrieval toward a **DB-backed** path using the **TECH-44b** schema. Until that ships, MCP tools remain **file-backed** (Markdown / generated JSON indexes).

| Item | Location |
|------|----------|
| **DDL** (ordered) | [`db/migrations/`](../db/migrations/) — `glossary`, `spec_sections`, `invariants`, `relationships`, `schema_migrations` |
| **Apply + seed + smoke read** | [`tools/postgres-ia/`](../tools/postgres-ia/) (`apply-migrations.mjs`, `seed-glossary-sample.mjs`, `glossary-by-key.mjs`) |
| **Dev setup** | [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) |
| **Connection** | **`DATABASE_URL`** only (see repository **`.env.example`**; never commit secrets) |
| **Example read** | SQL function **`ia_glossary_row_by_key(text)`** — `SELECT * FROM ia_glossary_row_by_key('heightmap');` after optional seed |

**Suggested MCP module (future):** a small `pg` client in `tools/mcp-ia-server/` (or shared package) that reads **`DATABASE_URL`**, runs parameterized queries / calls the functions above, and maps rows into existing tool response shapes — **TECH-18** scope; **TECH-44b** does not register new MCP tools.

## Future work (out of scope for TECH-17)

Full-text search across all IA documents is tracked as **TECH-18**; database-backed IA and evolved tools are **TECH-44b** / **TECH-18** in [`BACKLOG.md`](../BACKLOG.md).

**TECH-21** program (**TECH-40**–**TECH-41** **§ Completed**, **TECH-44a** **§ Completed** — [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md)): JSON Schema, **CI** fixture validation, and **generated** **spec**/**glossary** index JSON (machine manifests only — **not** a second copy of spec bodies; see **TECH-18**). **Postgres** program: **TECH-44** (**umbrella § Completed** — [`BACKLOG.md`](../BACKLOG.md); **TECH-44b**, **TECH-44c**; **Program extension mapping** in [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md)).

- **Schemas + fixtures:** [`docs/schemas/README.md`](../docs/schemas/README.md). Validate from repo root: `npm run validate:fixtures` (delegates to `tools/mcp-ia-server`).
- **Project spec path hygiene (TECH-50, completed 2026-04-03):** From repo root, `npm run validate:dead-project-specs` runs [`tools/validate-dead-project-spec-paths.mjs`](../tools/validate-dead-project-spec-paths.mjs) (durable docs + open **BACKLOG** **`Spec:`** lines). The **IA tools** workflow runs it when `.cursor/**`, `docs/**`, `projects/**`, or related paths change. **Lessons / edge cases:** see **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-50 closure)**. Optional MCP tool + **TECH-30** shared **Node** module: future backlog work, not part of **TECH-50** delivery.
- **I1 / I2 indexes (committed):** `tools/mcp-ia-server/data/spec-index.json` (**spec** keys, paths, heading `section_id`s) and `glossary-index.json` (**glossary** term → `spec_key` + `anchor`). Regenerate after editing `.cursor/specs/*.md` or `glossary.md`: `npm run generate:ia-indexes` under `tools/mcp-ia-server/` (or `npm run generate:ia-indexes` from the repo root — forwards extra args such as `--check`). **CI** runs `generate:ia-indexes -- --check` in the **IA tools** workflow so committed JSON stays in sync. MCP **may** later read these files; **`list_specs`**, **`spec_outline`**, **`spec_section`**, and **`spec_sections`** remain authoritative for slice retrieval today.

Program charter: [`.cursor/projects/TECH-21.md`](../.cursor/projects/TECH-21.md).

For a **domain-neutral** description of this architecture (Markdown corpus, registry, parser spine, tool families, verification) — useful when starting a similar MCP in another repo — see [`mcp-markdown-ia-pattern.md`](mcp-markdown-ia-pattern.md).
