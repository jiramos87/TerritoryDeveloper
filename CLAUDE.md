# Claude Code — Territory Developer

This repository is configured for **Claude Code** with project-scoped MCP (`.mcp.json`) and workflow **skills** under `ia/skills/` (Markdown; same files Cursor uses via back-compat symlinks under `.cursor/skills/`—open them when the task matches). Native Claude Code surface (subagents, hooks, slash commands, output styles, project memory) is being added incrementally by [TECH-85](ia/projects/TECH-85-ia-migration.md).

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/mcp-ia-default.md

## MCP: territory-ia

1. **Approve** the project server when prompted (`claude mcp reset-project-choices` resets prior approvals).
2. The server **must** start with **working directory = repository root** so `tools/mcp-ia-server/src/index.ts` resolves.
3. Prefer **territory-ia** tools for IA lookups instead of reading whole `ia/specs/*.md` files when a slice is enough.

**Suggested tool order** (when tools are enabled): `backlog_issue` (if you have a `BUG-` / `FEAT-` / `TECH-` / … id) → `list_specs` if needed → `router_for_task` → `glossary_discover` / `glossary_lookup` → `spec_outline` / `spec_section` / `spec_sections` (or `invariants_summary`, `list_rules` / `rule_content`). For closing a project spec, use `project_spec_closeout_digest` after `backlog_issue`.

**Glossary tools:** Pass **English** in `glossary_discover` / `glossary_lookup` (translate from the conversation if the human writes in another language). For `glossary_discover`, use a **JSON array** for keywords.

**Postgres / Unity bridge:** `project_spec_journal_*`, `unity_bridge_command`, `unity_bridge_get`, and `unity_compile` need a resolvable DB URL (`DATABASE_URL` or `config/postgres-dev.json`) and, for bridge tools, Unity Editor on `REPO_ROOT` (run **`npm run unity:ensure-editor`** to auto-launch if not running; see **`docs/agent-led-verification-policy.md`** timeout escalation protocol). Root **`npm run verify:local`** (alias **`verify:post-implementation`**) chains **`validate:all`** (**IA** **Node** checks including **`territory-compute-lib`** build), **`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** **Editor** save/quit + relaunch, and **`db:bridge-playmode-smoke`** — see **`docs/mcp-ia-server.md`** and **`ARCHITECTURE.md`** (**Local verification**). **`docs/postgres-ia-dev-setup.md`** covers DB setup and the **`agent_bridge_job`** queue.

**Unity batch compile:** Run **`npm run unity:compile-check`** from the repo root when needed. **Do not** skip because **`$UNITY_EDITOR_PATH`** is empty in the agent shell — **`tools/scripts/unity-compile-check.sh`** loads **`.env`** / **`.env.local`** (see **`tools/scripts/load-repo-env.inc.sh`**) and can infer the **macOS** Hub path from **`ProjectSettings/ProjectVersion.txt`**.

If MCP is unavailable, use `ia/rules/agent-router.md` and targeted file reads. Canonical workflow and policies: **`AGENTS.md`**.

## Skills (read the matching `SKILL.md`)

| Skill folder | When to open |
|--------------|----------------|
| `ia/skills/project-new/SKILL.md` | New `BACKLOG.md` row + `ia/projects/{ISSUE_ID}.md` from a prompt |
| `ia/skills/project-spec-kickoff/SKILL.md` | Review or enrich a project spec before coding |
| `ia/skills/project-spec-implement/SKILL.md` | Execute a spec’s **Implementation Plan** |
| `ia/skills/project-implementation-validation/SKILL.md` | After MCP/schema/index work: Node checks aligned with CI |
| `ia/skills/project-spec-close/SKILL.md` | Close an issue: migrate IA, delete spec, archive backlog row |
| `ia/skills/ide-bridge-evidence/SKILL.md` | Unity Play Mode evidence via `unity_bridge_command` |
| `ia/skills/close-dev-loop/SKILL.md` | Fix → verify loop with `debug_context_bundle` and compile gate |
| `ia/skills/ui-hud-row-theme/SKILL.md` | HUD/menu rows with `UiTheme` and UI design spec |

Index and conventions: `ia/skills/README.md`.

## Project rules (summary)

- **Language:** All code, comments, XML docs, and `Debug.Log` messages must be **English**.
- **Invariants and guardrails:** `ia/rules/invariants.md`
- **Coding standards:** `ia/rules/coding-conventions.md`

## Claude Code native surface (TECH-85 Stage 1)

**Project memory:** [`MEMORY.md`](MEMORY.md) at repo root. Index of architectural decisions; one line per entry. Promote to `.claude/memory/{slug}.md` only when an entry exceeds ~10 lines (per Q12).

**Hooks** declared in `.claude/settings.json` (scripts under `tools/scripts/claude-hooks/`):

| Event | Script | Behavior |
|---|---|---|
| `SessionStart` | `session-start-prewarm.sh` | Prints branch, last `verify:local` exit, last bridge preflight exit, top in-progress issues |
| `PreToolUse(Bash)` | `bash-denylist.sh` | **Blocks** `git push --force*`, `git reset --hard*`, `rm -rf .cursor*/ia*/.claude*/MEMORY.md*/.git*`, `sudo *` (exit 2) |
| `PostToolUse(Edit\|Write\|MultiEdit)` | `cs-edit-reminder.sh` | Advisory: reminds to run `npm run unity:compile-check` after editing `Assets/**/*.cs` |
| `Stop` | `verification-reminder.sh` | Advisory: reminds to include a Verification block per `docs/agent-led-verification-policy.md` when `Assets/**/*.cs` or `tools/mcp-ia-server/**` was touched |

**Slash commands** (Stage 1 stubs under `.claude/commands/` — real wrappers land in **Stage 4**, see `ia/projects/TECH-85-ia-migration.md` § Stage 4 / Phase 4.3):

| Command | Stage 4 target |
|---|---|
| `/kickoff {ID}` | `spec-kickoff` subagent on `ia/projects/{ID}*.md` |
| `/implement {ID}` | `spec-implementer` subagent on `ia/projects/{ID}*.md` |
| `/verify` | `verifier` subagent + `verification-report` output style |
| `/testmode {ID}` | `test-mode-loop` subagent |
| `/closeout {ID}` | `closeout` subagent (umbrella close, with confirmation gate per Q6) |

**Permissions** in `.claude/settings.json` use **`defaultMode: "acceptEdits"`** (canonical project stance — empirical Stage 1 finding, see [TECH-85](ia/projects/TECH-85-ia-migration.md) §6 / §9 issue #4 / §10): file edits are auto-accepted so the human only interacts at phase / checkpoint boundaries instead of per file. Buckets: `allow` = `mcp__territory-ia__*` (single wildcard — covers every current and future MCP tool from `territory-ia`, including Stage 5's three new code-intelligence tools), safe Bash (read-only shell + structural-migration helpers `mkdir` / `ln` / `chmod` / `cp` / `mv` / `touch`), `Read` / `Glob` / `Grep` / `Edit` / `Write` / `MultiEdit` / `NotebookEdit`. `ask` = write-shaped Bash only (`rm`, `git add` / `commit` / `push` / `reset` / `rebase` / `checkout` / `merge` / `stash` / `clean`, `curl` / `wget`, `verify`, `unity:testmode-batch`) — **no MCP tools are gated in `ask`**, the wildcard covers them all. `deny` = `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {.cursor,ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *`. `enabledMcpjsonServers: ["territory-ia"]` is explicit — no `enableAllProjectMcpServers`. **Do not strip `defaultMode: "acceptEdits"`** and **do not split the `mcp__territory-ia__*` wildcard back into a per-tool list** in any future cleanup pass — both would re-introduce per-call approval friction for stage-executing agents.

**Skills surface:** `.claude/skills/*` are directory-level symlinks pointing at `ia/skills/*` (re-targeted to the neutral namespace in TECH-85 Stage 2 / Phase 2.5). Cursor reads the same recipes through `.cursor/skills/*` back-compat symlinks. Stage 1 introduced the `project-stage-close` skill, which any multi-stage spec invokes at the end of each non-final stage.
