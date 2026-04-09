# Claude Code — Territory Developer

This repository is configured for **Claude Code** with project-scoped MCP (`.mcp.json`) and workflow **skills** under `.cursor/skills/` (Markdown; same files Cursor uses—open them when the task matches).

## MCP: territory-ia

1. **Approve** the project server when prompted (`claude mcp reset-project-choices` resets prior approvals).
2. The server **must** start with **working directory = repository root** so `tools/mcp-ia-server/src/index.ts` resolves.
3. Prefer **territory-ia** tools for IA lookups instead of reading whole `.cursor/specs/*.md` files when a slice is enough.

**Suggested tool order** (when tools are enabled): `backlog_issue` (if you have a `BUG-` / `FEAT-` / `TECH-` / … id) → `list_specs` if needed → `router_for_task` → `glossary_discover` / `glossary_lookup` → `spec_outline` / `spec_section` / `spec_sections` (or `invariants_summary`, `list_rules` / `rule_content`). For closing a project spec, use `project_spec_closeout_digest` after `backlog_issue`.

**Glossary tools:** Pass **English** in `glossary_discover` / `glossary_lookup` (translate from the conversation if the human writes in another language). For `glossary_discover`, use a **JSON array** for keywords.

**Postgres / Unity bridge:** `project_spec_journal_*`, `unity_bridge_command`, `unity_bridge_get`, and `unity_compile` need a resolvable DB URL (`DATABASE_URL` or `config/postgres-dev.json`) and, for bridge tools, Unity Editor on `REPO_ROOT`. Root **`npm run verify:local`** (alias **`verify:post-implementation`**) chains **`validate:all`** (**IA** **Node** checks including **`territory-compute-lib`** build), **`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** **Editor** save/quit + relaunch, and **`db:bridge-playmode-smoke`** — see **`docs/mcp-ia-server.md`** and **`ARCHITECTURE.md`** (**Local verification**). **`docs/postgres-ia-dev-setup.md`** covers DB setup and the **`agent_bridge_job`** queue.

**Unity batch compile:** Run **`npm run unity:compile-check`** from the repo root when needed. **Do not** skip because **`$UNITY_EDITOR_PATH`** is empty in the agent shell — **`tools/scripts/unity-compile-check.sh`** loads **`.env`** / **`.env.local`** (see **`tools/scripts/load-repo-env.inc.sh`**) and can infer the **macOS** Hub path from **`ProjectSettings/ProjectVersion.txt`**.

If MCP is unavailable, use `.cursor/rules/agent-router.mdc` and targeted file reads. Canonical workflow and policies: **`AGENTS.md`**.

## Skills (read the matching `SKILL.md`)

| Skill folder | When to open |
|--------------|----------------|
| `.cursor/skills/project-new/SKILL.md` | New `BACKLOG.md` row + `.cursor/projects/{ISSUE_ID}.md` from a prompt |
| `.cursor/skills/project-spec-kickoff/SKILL.md` | Review or enrich a project spec before coding |
| `.cursor/skills/project-spec-implement/SKILL.md` | Execute a spec’s **Implementation Plan** |
| `.cursor/skills/project-implementation-validation/SKILL.md` | After MCP/schema/index work: Node checks aligned with CI |
| `.cursor/skills/project-spec-close/SKILL.md` | Close an issue: migrate IA, delete spec, archive backlog row |
| `.cursor/skills/ide-bridge-evidence/SKILL.md` | Unity Play Mode evidence via `unity_bridge_command` |
| `.cursor/skills/close-dev-loop/SKILL.md` | Fix → verify loop with `debug_context_bundle` and compile gate |
| `.cursor/skills/ui-hud-row-theme/SKILL.md` | HUD/menu rows with `UiTheme` and UI design spec |

Index and conventions: `.cursor/skills/README.md`.

## Project rules (summary)

- **Language:** All code, comments, XML docs, and `Debug.Log` messages must be **English**.
- **Invariants and guardrails:** `.cursor/rules/invariants.mdc`
- **Coding standards:** `.cursor/rules/coding-conventions.mdc`
