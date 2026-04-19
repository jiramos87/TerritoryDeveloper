# Claude Code — Territory Developer

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/mcp-ia-default.md
@ia/rules/agent-output-caveman.md
@ia/rules/agent-lifecycle.md

## 1. What this repo is

Unity 2D isometric city builder with a Markdown-backed Information Architecture under `ia/{specs,rules,skills,projects,templates}` and a project-scoped MCP server (`territory-ia`, registered in `.mcp.json`). Workflow specifics: `AGENTS.md`. Runtime layers + dependency map: `ARCHITECTURE.md`. IA narrative: `docs/information-architecture-overview.md`.

## 2. MCP first

Prefer **`mcp__territory-ia__*`** tools over reading whole `ia/specs/*.md` files. Suggested order: `backlog_issue` (when you have a `BUG-/FEAT-/TECH-/ART-/AUDIO-` id) → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from the conversation) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. Issue-creation flow: `reserve_backlog_ids` (reserve id before writing yaml) → `backlog_record_validate` (validate yaml before materialize). Structured list queries: `backlog_list`. For closing a project spec: `project_spec_closeout_digest` after `backlog_issue`. The MCP server caches the schema in memory at session start; restart Claude Code (or use the matching CLI script via tsx) after editing tool descriptors. If MCP is unavailable, fall back to `ia/rules/agent-router.md` + targeted file reads.

## 3. Key files

| File | What it is |
|---|---|
| `MEMORY.md` (root) | Repo-scoped project memory. One-line entries; promote to `.claude/memory/{slug}.md` when an entry exceeds ~10 lines. Distinct from user auto-memory under `~/.claude-personal/projects/.../memory/` (cross-project, per-user). |
| `.claude/settings.json` | Hooks + permissions. **Do not strip `defaultMode: "acceptEdits"`** and **do not split the `mcp__territory-ia__*` wildcard** — both regress per-call approval friction. |
| `.claude/skills/{name}` | Directory-level symlinks → `ia/skills/{name}/`. |
| `.claude/agents/*.md` | Native subagents post-M6 — Opus pair-heads (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file-planner`, `project-new-planner`, `plan-author`, `plan-reviewer`, `opus-code-reviewer`, `opus-auditor`, `stage-closeout-planner`, `ship-stage`, `release-rollout`); Sonnet pair-tails + executors (`stage-file-applier`, `project-new-applier`, `plan-fix-applier`, `code-fix-applier`, `stage-closeout-applier`, `spec-implementer`, `verifier`, `verify-loop`, `test-mode-loop`). Retired under `.claude/agents/_retired/` — `spec-kickoff`, legacy Opus `closeout` (absorbed into `stage-closeout-planner/applier`). Each body carries `caveman:caveman` directive + `@`-loads `subagent-progress-emit` preamble. `ship-stage` = chain dispatcher (author → implement → verify-loop `--skip-path-b` → code-review → audit → closeout; batched Path B at stage end). `release-rollout` = umbrella driver (routes per-cell to `design-explore` / `master-plan-new` / `master-plan-extend` / `stage-decompose` / `stage-file` subagents; helpers `release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`). |
| `.claude/commands/*.md` | Slash command dispatchers → subagents under `.claude/agents/{name}.md` (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`, `/project-new`, `/author`, `/plan-review`, `/implement`, `/verify`, `/verify-loop`, `/testmode`, `/code-review`, `/audit`, `/ship-stage`, `/closeout`, `/release-rollout`). Retired under `.claude/commands/_retired/` — `/kickoff` (folded into `/author`). Each forwards a caveman-asserting prompt. `/closeout` confirmation prompts stay full English. |
| `.claude/output-styles/*.md` | 2 output styles — `verification-report` (JSON header + caveman summary, used by `/verify`) and `closeout-digest` (JSON header + caveman summary, used by Stage-scoped `/closeout`). |
| `ia/skills/*/SKILL.md` | Workflow recipes — open the matching `SKILL.md` when the task triggers. Index: `ia/skills/README.md`. Lifecycle recipes carry a caveman preamble + top-level `phases:` frontmatter (progress-emit contract). Pair skills: `stage-file-plan`/`stage-file-apply`, `project-new`/`project-new-apply`, `plan-review`/`plan-fix-apply`, `opus-code-review`/`code-fix-apply`, `stage-closeout-plan`/`stage-closeout-apply`. Bulk Stage 1×N non-pair: `plan-author`, `opus-audit`. Retired under `ia/skills/_retired/` — `project-spec-kickoff` (folded into `plan-author`), `project-stage-close` + `project-spec-close` (folded into Stage-scoped closeout pair). |
| `ia/rules/{invariants,terminology-consistency,mcp-ia-default,agent-output-caveman}.md` | Always-loaded guardrails (imported above). |
| `docs/agent-led-verification-policy.md` | Single canonical Verification policy. |
| `ia/backlog/{id}.yaml` | Per-issue **backlog record** (open issues). Source of truth for MCP + mutator skills. Written via `project-new` / `stage-file` / closeout; read by `backlog-parser.ts`. |
| `ia/backlog-archive/{id}.yaml` | Per-issue **backlog record** (closed issues). Moved from `ia/backlog/` on closeout. |
| `ia/state/id-counter.json` | Monotonic per-prefix id counter (TECH, FEAT, BUG, ART, AUDIO). Written exclusively via `tools/scripts/reserve-id.sh` under `flock`. Never hand-edit. |
| `BACKLOG.md`, `BACKLOG-ARCHIVE.md` | Generated **backlog view** — materialized by `bash tools/scripts/materialize-backlog.sh` from yaml records. Read-only for humans + dashboard; never edited directly by skills or agents. |
| `ia/skills/skill-train/SKILL.md` | On-demand skill retrospective. Reads target skill's Per-skill Changelog; aggregates recurring friction (≥2 occurrences); proposes unified-diff patch against Phase sequence / Guardrails / Seed prompt sections. User-gated; never auto-applies. Sibling producer: `release-rollout-skill-bug-log` (user-logged channel). |

## 4. Hooks

Hooks live in `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash denylist (PreToolUse) blocks: `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *` (exit 2). Verification policy: `docs/agent-led-verification-policy.md`. Hook scripts require `jq` on PATH; missing → sed fallback with conservative-deny (escaped quotes → empty string → hook allows).

## 5. Key commands

| Command | When |
|---|---|
| `npm run validate:all` | After IA / MCP / fixture / index work. Same chain CI runs. |
| `npm run unity:compile-check` | After C# edits. Loads `.env` / `.env.local`; **do not** skip because `$UNITY_EDITOR_PATH` is empty in the agent shell. |
| `npm run verify:local` (alias `verify:post-implementation`) | Full local chain on a configured dev machine: `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. See `ARCHITECTURE.md` (**Local verification**). |

Other commands (`validate:frontmatter`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` and the relevant skill bodies (`agent-test-mode-verify`, `bridge-environment-preflight`).

## 6. Web workspace (`web/`)

Next.js 14+ App Router workspace at `web/`. Full onboarding: `web/README.md`.

| Command | Purpose |
|---|---|
| `cd web && npm run dev` | Start dev server (http://localhost:4000) |
| `cd web && npm run build` | Production build |
| `npm run validate:web` | Lint + typecheck + build via root composition |
| `npm run deploy:web` | Deploy production to https://web-nine-wheat-35.vercel.app (auto-prunes newest 3). Manual only — closeout / stage-close no longer auto-deploy. |
| `npm run deploy:web:preview` | Deploy preview (non-prod) to a unique Vercel URL. |

| Route | Purpose | Auth | Render |
|-------|---------|------|--------|
| `/dashboard` | Master-plan progress dashboard | gated (bypass via `DASHBOARD_AUTH_SKIP=1`) | RSC |
| `/dashboard/releases` | Release picker | gated (TECH-358 matcher) | RSC |
| `/dashboard/releases/:releaseId/progress` | Release progress tree | gated (TECH-358 matcher) | RSC + `PlanTree` Client island |

Auth gate for `/dashboard*` inherits from `web/proxy.ts` matcher (TECH-358).

**Live dashboard freshness:** `/dashboard` fetches `ia/projects/*master-plan*.md` from GitHub raw via Next.js ISR (5-min revalidate) on Vercel. Push to deployed branch → visible within ~5 min without redeploy. Run `npm run deploy:web` only when instant refresh or code change required.

**Caveman-exception boundary:** full English applies only to user-facing rendered text under `web/content/**` and page-body JSX strings in `web/app/**/page.tsx`. App shell code, identifiers, comments, commits, IA prose stay caveman. Authority: `ia/rules/agent-output-caveman.md` §exceptions.

Orchestrator: `ia/projects/web-platform-master-plan.md` (permanent — never closeable via `/closeout`).

## 7. Where to find more

- Workflow + lifecycle: `AGENTS.md`
- IA stack overview: `docs/information-architecture-overview.md`
- MCP tool catalog: `docs/mcp-ia-server.md`
