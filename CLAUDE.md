# Claude Code — Territory Developer

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/agent-output-caveman.md
@ia/rules/agent-principles.md

## 1. What this repo is

Unity 2D isometric city builder + Markdown IA (`ia/{specs,rules,skills,projects,templates}`) + project-scoped MCP server (`territory-ia`, `.mcp.json`). Cross-harness workflow: **`AGENTS.md`**. Runtime layers + dep map: `ia/specs/architecture/layers.md`. Decisions: `ia/specs/architecture/decisions.md`. Root `ARCHITECTURE.md` = index stub. This file = Claude Code deltas only; everything not Claude-specific lives in `AGENTS.md`.

## 2. MCP first

Force-loaded `ia/rules/invariants.md` carries the MCP-first directive + universal safety. Tool order, fallback, schema-cache caveat: see that file.

## 3. Task routing (trigger → read)

| Trigger | Read |
|---|---|
| Architecture — sub-spec roles + lifecycle index | `ia/specs/architecture/README.md` |
| Architecture — system layers / dependency map | `ia/specs/architecture/layers.md` (MCP `spec_section architecture/layers`) |
| Architecture — data flows / init order / persistence | `ia/specs/architecture/data-flows.md` |
| Architecture — agent IA / MCP / JSON interchange / bridge | `ia/specs/architecture/interchange.md` |
| Architecture — decisions + trade-offs (DEC-A1..N) | `ia/specs/architecture/decisions.md` (DB: `arch_decisions` table) |
| Unity C# / `GridManager` / `HeightMap` / roads / water / cliffs | `ia/rules/unity-invariants.md` (MCP `rule_content unity-invariants`; `invariants_summary` auto-merges with universal) |
| Lifecycle commands — `/stage-file`, `/ship-stage`, `/stage-authoring`, `/plan-review`, `/audit`, `/implement`, `/verify-loop` | `docs/agent-lifecycle.md` §1 (flow) + §2 (seam → surface matrix) |
| Meta / preview composite-skill behavior — `/unfold {TARGET_COMMAND} {ARGS...}` | `ia/skills/unfold/SKILL.md` (+ `docs/agent-lifecycle.md` §2 Row M) — emits decision-tree plan under `ia/plans/`; read-only, NO execution |
| Web workspace (`web/`) | `web/README.md` — dev commands, routes, dashboard diagnostic recipe, caveman-exception boundary |
| Web backend logic / Next.js App Router | `ia/rules/web-backend-logic.md` |
| Verification block format | `docs/agent-led-verification-policy.md` |
| Branch author self-review (review prose shape) | `ia/rules/agent-code-review-self.md` |
| Claude Code Task tool + paste-ready handoffs | `docs/agent-lifecycle.md` §10 |
| MCP server code / tool registration | `tools/mcp-ia-server/src/index.ts` + MCP `list_*` schemas. The `.md` catalog (`docs/mcp-ia-server.md`) can lag — treat as human overview only. |
| Ephemeral project state (active blockers, sprint decisions) | `MEMORY.md` (root — on-demand only) |
| Backlog / issues | `mcp__territory-ia__backlog_issue` (by id) |
| Last verify:local, bridge-preflight, queued scenario | `mcp__territory-ia__runtime_state` (fallback: read `ia/state/runtime-state.json` if present). Active task / stage (when written): per-harness active-session JSON in `.claude/` / `.cursor/`. |

## 4. Claude-native surface

- **Hooks.** `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash PreToolUse denylist — see force-loaded `invariants.md`. Do NOT strip `defaultMode: "acceptEdits"` or split the `mcp__territory-ia__*` wildcard.
- **Subagents.** `.claude/agents/*.md` — **GENERATED** from `ia/skills/{slug}/SKILL.md` frontmatter via `tools/scripts/skill-tools/`. Edit SKILL.md (frontmatter + optional `agent-body.md`), then `npm run skill:sync:all`. Direct edits caught by `npm run validate:skill-drift` (in `validate:all`). Seam → subagent map: `docs/agent-lifecycle.md §2`. Retired: `.claude/agents/_retired/`.
- **Slash commands.** `.claude/commands/*.md` — **GENERATED** from same SKILL.md pipeline. Edit SKILL.md (frontmatter + optional `command-body.md`), then `npm run skill:sync:all`. Retired: `.claude/commands/_retired/`.
- **Output styles.** `.claude/output-styles/*.md` — `verification-report`, `closeout-digest`.
- **Skill preamble.** Shared Tier 1 cache block: `ia/skills/_preamble/stable-block.md`. Order fixed (F5 invalidation cascade). Subagent cache floors validated by `npm run validate:cache-block-sizing`.
- **Project MEMORY.** `MEMORY.md` at repo root; on-demand only (not force-loaded). Promote entries to `.claude/memory/{slug}.md` once past ~10 lines.

## 5. Key commands

| Command | When |
|---|---|
| `npm run validate:all` | After IA / MCP / fixture / index / rules edits (same chain CI runs) |
| `npm run unity:compile-check` | After C# edits. `$UNITY_EDITOR_PATH` loaded by the script itself — do NOT skip. |
| `npm run verify:local` (alias `verify:post-implementation`) | Full local chain: `validate:all` + compile-check + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. See `ia/specs/architecture/interchange.md` (**Local verification**). |
| `npm run validate:claude-imports` | Assert every `@`-import in this file exists + stays within line budget. Drift gate. |

Further commands (`validate:frontmatter`, `validate:cache-block-sizing`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` + relevant skill bodies.

## 6. Web design spec (authoritative)

| File | Role |
| --- | --- |
| `web/lib/design-system.md` | Type, spacing, motion, alias tables — canonical spec for the web design layer |
| `web/lib/design-tokens.ts` + `web/app/globals.css` (`@theme`, `ds-*`) | Derived token surfaces; keep new `ds-*` in CSS, not a legacy `tailwind.config.ts` |

**Page copy:** user-facing body strings in `web/app/**/page.tsx` stay full English; app shell, identifiers, and IA prose stay caveman — `ia/rules/agent-output-caveman.md` exceptions.
