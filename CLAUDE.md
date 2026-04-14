# Claude Code — Territory Developer

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/mcp-ia-default.md
@ia/rules/agent-output-caveman.md
@ia/rules/agent-lifecycle.md

## 1. What this repo is

Unity 2D isometric city builder with a Markdown-backed Information Architecture under `ia/{specs,rules,skills,projects,templates}` and a project-scoped MCP server (`territory-ia`, registered in `.mcp.json`). Workflow specifics: `AGENTS.md`. Runtime layers + dependency map: `ARCHITECTURE.md`. IA narrative: `docs/information-architecture-overview.md`.

## 2. MCP first

Prefer **`mcp__territory-ia__*`** tools over reading whole `ia/specs/*.md` files. Suggested order: `backlog_issue` (when you have a `BUG-/FEAT-/TECH-/ART-/AUDIO-` id) → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from the conversation) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. For closing a project spec: `project_spec_closeout_digest` after `backlog_issue`. The MCP server caches the schema in memory at session start; restart Claude Code (or use the matching CLI script via tsx) after editing tool descriptors. If MCP is unavailable, fall back to `ia/rules/agent-router.md` + targeted file reads.

## 3. Key files

| File | What it is |
|---|---|
| `MEMORY.md` (root) | Repo-scoped project memory. One-line entries; promote to `.claude/memory/{slug}.md` when an entry exceeds ~10 lines. Distinct from user auto-memory under `~/.claude-personal/projects/.../memory/` (cross-project, per-user). |
| `.claude/settings.json` | Hooks + permissions. **Do not strip `defaultMode: "acceptEdits"`** and **do not split the `mcp__territory-ia__*` wildcard** — both regress per-call approval friction. |
| `.claude/skills/{name}` | Directory-level symlinks → `ia/skills/{name}/`. |
| `.claude/agents/*.md` | 10 native subagents — `design-explore`, `master-plan-new`, `stage-file`, `project-new`, `spec-kickoff`, `spec-implementer`, `verifier`, `verify-loop`, `test-mode-loop`, `closeout`. Opus orchestrators (`design-explore`, `master-plan-new`, `stage-file`, `project-new`, `spec-kickoff`, `closeout`); Sonnet executors (`spec-implementer`, `verifier`, `verify-loop`, `test-mode-loop`). Each body carries a `caveman:caveman` directive (subagents run in fresh context and do not inherit the parent SessionStart hook). |
| `.claude/commands/*.md` | Slash command dispatchers → subagents under `.claude/agents/{name}.md` (`/design-explore`, `/master-plan-new`, `/stage-file`, `/project-new`, `/kickoff`, `/implement`, `/verify`, `/verify-loop`, `/testmode`, `/closeout`). Each forwards a caveman-asserting prompt. `/closeout` confirmation prompts stay full English. |
| `.claude/output-styles/*.md` | 2 output styles — `verification-report` (JSON header + caveman summary, used by `/verify`) and `closeout-digest` (JSON header + caveman summary, used by `/closeout`). |
| `ia/skills/*/SKILL.md` | Workflow recipes — open the matching `SKILL.md` when the task triggers. Index: `ia/skills/README.md`. The 6 lifecycle recipes (`project-spec-kickoff`, `project-spec-implement`, `project-implementation-validation`, `agent-test-mode-verify`, `project-spec-close`, `project-stage-close`) carry a caveman preamble so direct (non-subagent) invocations inherit the same default. |
| `ia/rules/{invariants,terminology-consistency,mcp-ia-default,agent-output-caveman}.md` | Always-loaded guardrails (imported above). |
| `docs/agent-led-verification-policy.md` | Single canonical Verification policy. |

## 4. Hooks

Hooks live in `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash denylist (PreToolUse) blocks: `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *` (exit 2). Verification policy: `docs/agent-led-verification-policy.md`.

## 5. Key commands

| Command | When |
|---|---|
| `npm run validate:all` | After IA / MCP / fixture / index work. Same chain CI runs. |
| `npm run unity:compile-check` | After C# edits. Loads `.env` / `.env.local`; **do not** skip because `$UNITY_EDITOR_PATH` is empty in the agent shell. |
| `npm run verify:local` (alias `verify:post-implementation`) | Full local chain on a configured dev machine: `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. See `ARCHITECTURE.md` (**Local verification**). |

Other commands (`validate:frontmatter`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` and the relevant skill bodies (`agent-test-mode-verify`, `bridge-environment-preflight`).

## 6. Where to find more

- Workflow + lifecycle: `AGENTS.md`
- IA stack overview: `docs/information-architecture-overview.md`
- MCP tool catalog: `docs/mcp-ia-server.md`
