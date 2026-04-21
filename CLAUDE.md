# Claude Code — Territory Developer

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/agent-output-caveman.md
@ia/rules/agent-principles.md

## 1. What this repo is

Unity 2D isometric city builder + Markdown IA (`ia/{specs,rules,skills,projects,templates}`) + project-scoped MCP server (`territory-ia`, `.mcp.json`). Cross-harness workflow: **`AGENTS.md`**. Runtime layers + dep map: `ARCHITECTURE.md`. This file = Claude Code deltas only; everything not Claude-specific lives in `AGENTS.md`.

## 2. MCP first

Force-loaded `ia/rules/invariants.md` carries the MCP-first directive + universal safety. Tool order, fallback, schema-cache caveat: see that file.

## 3. Task routing (trigger → read)

| Trigger | Read |
|---|---|
| Unity C# / `GridManager` / `HeightMap` / roads / water / cliffs | `ia/rules/unity-invariants.md` (MCP `rule_content unity-invariants`; `invariants_summary` auto-merges with universal) |
| Lifecycle commands — `/stage-file`, `/ship-stage`, `/closeout`, `/plan-review`, `/author`, `/audit`, `/implement`, `/verify-loop` | `docs/agent-lifecycle.md` §1 (flow) + §2 (seam → surface matrix) |
| Web workspace (`web/`) | `web/README.md` — dev commands, routes, dashboard diagnostic recipe, caveman-exception boundary |
| Web backend logic / Next.js App Router | `ia/rules/web-backend-logic.md` |
| Verification block format | `docs/agent-led-verification-policy.md` |
| MCP server code / tool registration | `tools/mcp-ia-server/src/index.ts` + MCP `list_*` schemas. The `.md` catalog (`docs/mcp-ia-server.md`) can lag — treat as human overview only. |
| Ephemeral project state (active blockers, sprint decisions) | `MEMORY.md` (root — on-demand only) |
| Backlog / issues | `mcp__territory-ia__backlog_issue` (by id) |

## 4. Claude-native surface

- **Hooks.** `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash PreToolUse denylist — see force-loaded `invariants.md`. Do NOT strip `defaultMode: "acceptEdits"` or split the `mcp__territory-ia__*` wildcard.
- **Subagents.** `.claude/agents/*.md`. Seam → subagent map: `docs/agent-lifecycle.md §2`. Retired: `.claude/agents/_retired/`.
- **Slash commands.** `.claude/commands/*.md` dispatch to `.claude/agents/{name}.md`. Retired: `.claude/commands/_retired/`.
- **Output styles.** `.claude/output-styles/*.md` — `verification-report`, `closeout-digest`.
- **Skill preamble.** Shared Tier 1 cache block: `ia/skills/_preamble/stable-block.md`. Order fixed (F5 invalidation cascade). Subagent cache floors validated by `npm run validate:cache-block-sizing`.
- **Project MEMORY.** `MEMORY.md` at repo root; on-demand only (not force-loaded). Promote entries to `.claude/memory/{slug}.md` once past ~10 lines.

## 5. Key commands

| Command | When |
|---|---|
| `npm run validate:all` | After IA / MCP / fixture / index / rules edits (same chain CI runs) |
| `npm run unity:compile-check` | After C# edits. `$UNITY_EDITOR_PATH` loaded by the script itself — do NOT skip. |
| `npm run verify:local` (alias `verify:post-implementation`) | Full local chain: `validate:all` + compile-check + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. See `ARCHITECTURE.md` (**Local verification**). |
| `npm run validate:claude-imports` | Assert every `@`-import in this file exists + stays within line budget. Drift gate. |

Further commands (`validate:frontmatter`, `validate:cache-block-sizing`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` + relevant skill bodies.
