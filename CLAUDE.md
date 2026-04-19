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
| `.claude/agents/*.md` | 13 native subagents — `design-explore`, `master-plan-new`, `master-plan-extend`, `stage-file`, `project-new`, `spec-kickoff`, `spec-implementer`, `verifier`, `verify-loop`, `test-mode-loop`, `ship-stage`, `closeout`, `release-rollout`. Opus orchestrators (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-file`, `project-new`, `spec-kickoff`, `ship-stage`, `closeout`, `release-rollout`); Sonnet executors (`spec-implementer`, `verifier`, `verify-loop`, `test-mode-loop`). Each body carries a `caveman:caveman` directive (subagents run in fresh context and do not inherit the parent SessionStart hook). `release-rollout` dispatches ABOVE the single-issue flow — routes per-cell to `design-explore` / `master-plan-new` / `master-plan-extend` / `stage-decompose` / `stage-file` subagents, backed by 3 Sonnet / Opus helper skills (`release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`). `ship-stage` dispatches BETWEEN single-issue `/ship` and `/release-rollout` — drives all non-Done tasks of one Stage X.Y through kickoff → implement → verify-loop → closeout with cached MCP context + batched Path B at stage end. |
| `.claude/commands/*.md` | Slash command dispatchers → subagents under `.claude/agents/{name}.md` (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-file`, `/project-new`, `/kickoff`, `/implement`, `/verify`, `/verify-loop`, `/testmode`, `/ship-stage`, `/closeout`, `/release-rollout`). Each forwards a caveman-asserting prompt. `/closeout` confirmation prompts stay full English. |
| `.claude/output-styles/*.md` | 2 output styles — `verification-report` (JSON header + caveman summary, used by `/verify`) and `closeout-digest` (JSON header + caveman summary, used by `/closeout`). |
| `ia/skills/*/SKILL.md` | Workflow recipes — open the matching `SKILL.md` when the task triggers. Index: `ia/skills/README.md`. The 6 lifecycle recipes (`project-spec-kickoff`, `project-spec-implement`, `project-implementation-validation`, `agent-test-mode-verify`, `project-spec-close`, `project-stage-close`) carry a caveman preamble so direct (non-subagent) invocations inherit the same default. |
| `ia/rules/{invariants,terminology-consistency,mcp-ia-default,agent-output-caveman}.md` | Always-loaded guardrails (imported above). |
| `docs/agent-led-verification-policy.md` | Single canonical Verification policy. |
| `ia/backlog/{id}.yaml` | Per-issue **backlog record** (open issues). Source of truth for MCP + mutator skills. Written via `project-new` / `stage-file` / closeout; read by `backlog-parser.ts`. |
| `ia/backlog-archive/{id}.yaml` | Per-issue **backlog record** (closed issues). Moved from `ia/backlog/` on closeout. |
| `ia/state/id-counter.json` | Monotonic per-prefix id counter (TECH, FEAT, BUG, ART, AUDIO). Written exclusively via `tools/scripts/reserve-id.sh` under `flock`. Never hand-edit. |
| `BACKLOG.md`, `BACKLOG-ARCHIVE.md` | Generated **backlog view** — materialized by `bash tools/scripts/materialize-backlog.sh` from yaml records. Read-only for humans + dashboard; never edited directly by skills or agents. |
| `ia/skills/skill-train/SKILL.md` | On-demand skill retrospective. Reads target skill's Per-skill Changelog; aggregates recurring friction (≥2 occurrences); proposes unified-diff patch against Phase sequence / Guardrails / Seed prompt sections. User-gated; never auto-applies. Sibling producer: `release-rollout-skill-bug-log` (user-logged channel). |

## 4. Hooks

Hooks live in `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash denylist (PreToolUse) blocks: `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *` (exit 2). Verification policy: `docs/agent-led-verification-policy.md`.

## 5. Key commands

> **FREEZE — Lifecycle Refactor (M0–M8) active.** Branch `feature/lifecycle-collapse-cognitive-split`. Do NOT run `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` outside `ia/projects/lifecycle-refactor-master-plan.md` orchestration until M8 sign-off (T4.2.1). State: `ia/state/lifecycle-refactor-migration.json`. Snapshot: `ia/state/pre-refactor-snapshot/`. Authoritative orchestrator: `ia/projects/lifecycle-refactor-master-plan.md`.

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
