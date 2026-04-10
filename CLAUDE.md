# Claude Code — Territory Developer

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/mcp-ia-default.md

## 1. What this repo is

Unity 2D isometric city builder with a Markdown-backed Information Architecture under `ia/{specs,rules,skills,projects,templates}` and a project-scoped MCP server (`territory-ia`, registered in `.mcp.json`). Cursor reads the same content through `.cursor/...` back-compat symlinks. Workflow specifics: `AGENTS.md`. Runtime layers + dependency map: `ARCHITECTURE.md`. IA narrative: `docs/information-architecture-overview.md`.

## 2. MCP first

Prefer **`mcp__territory-ia__*`** tools over reading whole `ia/specs/*.md` files. Suggested order: `backlog_issue` (when you have a `BUG-/FEAT-/TECH-/ART-/AUDIO-` id) → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from the conversation) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. For closing a project spec: `project_spec_closeout_digest` after `backlog_issue`. The MCP server caches the schema in memory at session start; restart Claude Code (or use the matching CLI script via tsx) after editing tool descriptors. If MCP is unavailable, fall back to `ia/rules/agent-router.md` + targeted file reads.

## 3. Key files

| File | What it is |
|---|---|
| `MEMORY.md` (root) | Project memory. One-line entries; promote to `.claude/memory/{slug}.md` only when an entry exceeds ~10 lines (per Q12). |
| `.claude/settings.json` | Hooks + permissions. **Do not strip `defaultMode: "acceptEdits"`** and **do not split the `mcp__territory-ia__*` wildcard** — both regress per-call approval friction (TECH-85 §6 / §9 issue #4 / §10). |
| `.claude/skills/{name}` | Directory-level symlinks → `ia/skills/{name}/`. Same recipes Cursor reads. |
| `.claude/commands/*.md` | Slash command stubs (`/kickoff /implement /verify /testmode /closeout`); real wrappers land in **TECH-85 Stage 4** (subagent dispatch). |
| `ia/skills/*/SKILL.md` | Workflow recipes — open the matching `SKILL.md` when the task triggers. Index: `ia/skills/README.md`. |
| `ia/rules/{invariants,terminology-consistency,mcp-ia-default}.md` | Always-loaded guardrails (imported above). |
| `docs/agent-led-verification-policy.md` | Single canonical Verification policy. |

## 4. Hooks (declared in `.claude/settings.json`, scripts under `tools/scripts/claude-hooks/`)

| Event | Script | Behavior |
|---|---|---|
| `SessionStart` | `session-start-prewarm.sh` | Branch + last `verify:local` exit + last bridge preflight exit + top in-progress issues. Reads marker files; does not run preflights. |
| `PreToolUse(Bash)` | `bash-denylist.sh` | **Blocks** `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {.cursor,ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *` (exit 2). |
| `PostToolUse(Edit\|Write\|MultiEdit)` | `cs-edit-reminder.sh` | Advisory after editing `Assets/**/*.cs` — run `npm run unity:compile-check`. |
| `Stop` | `verification-reminder.sh` | Advisory at session stop when `Assets/**/*.cs` or `tools/mcp-ia-server/**` was touched — emit a Verification block per the policy doc. |

## 5. Key commands

| Command | When |
|---|---|
| `npm run validate:all` | After IA / MCP / fixture / index work. Same chain CI runs. |
| `npm run validate:frontmatter` | After editing files under `ia/` — confirms the four-field IA frontmatter. Advisory. |
| `npm run unity:compile-check` | After C# edits. Loads `.env` / `.env.local`; **do not** skip because `$UNITY_EDITOR_PATH` is empty in the agent shell. |
| `npm run verify:local` (alias `verify:post-implementation`) | Full local chain on a configured dev machine: `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. See `ARCHITECTURE.md` (**Local verification**). |
| `npm run unity:testmode-batch -- --quit-editor-first --scenario-id …` | Path A test mode batch. Always release the project lock first. |
| `npm run db:bridge-preflight` | Before any `unity_bridge_command` call in a session. |

## 6. Where to find more

- Workflow + lifecycle: `AGENTS.md`
- IA stack overview: `docs/information-architecture-overview.md`
- MCP tool catalog: `docs/mcp-ia-server.md`
- TECH-85 native Claude Code migration plan: `ia/projects/TECH-85-ia-migration.md`
