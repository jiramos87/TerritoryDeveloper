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

When starting work on **`BUG-XX` / `FEAT-XX` / `TECH-XX`** (etc.), call **`backlog_issue`** with `issue_id` first to get `Files`, `Spec`, `Notes`, `Acceptance`, `status`, and `raw_markdown` without loading all of `BACKLOG.md`. Then use `router_for_task` / `glossary_discover` / `glossary_lookup` / `spec_section` as needed. Older issues may live only in `BACKLOG-ARCHIVE.md` (not covered by v1 `backlog_issue`).

## Tools (10)

| Tool | Role |
|------|------|
| `backlog_issue` | One issue from `BACKLOG.md` by id (`issue_id`); structured fields + `raw_markdown`. Nested sub-items (e.g. TECH-01 under BUG-20) supported. |
| `list_specs` | Discover registered documents (`key`, path, category, description). |
| `spec_outline` | Heading tree for a spec/rule/doc; supports aliases (`geo`, `roads`, `unity` / `unityctx` → `unity-development-context`, `refspec` / `specstructure` → `reference-spec-structure`, …). |
| `spec_section` | Body under one heading (id, slug, substring, or fuzzy heading match); `max_chars` truncation. Parameters `spec` + `section` are canonical; aliases `key`/`doc` for spec and `section_heading`/`heading` for section are accepted (numeric section coerced to string) so mis-keyed tool calls still succeed. |
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

## Future work (out of scope for TECH-17)

Full-text search across all IA documents is tracked as **TECH-18**; database-backed IA and evolved tools are **TECH-19** / **TECH-18** in `BACKLOG.md`.

For a **domain-neutral** description of this architecture (Markdown corpus, registry, parser spine, tool families, verification) — useful when starting a similar MCP in another repo — see [`mcp-markdown-ia-pattern.md`](mcp-markdown-ia-pattern.md).
