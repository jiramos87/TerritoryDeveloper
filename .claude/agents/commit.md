---
name: commit
description: Topic-clustered commit pipeline. Sweeps `git status --porcelain` + `git diff --stat HEAD` (caller session + orphan changes from closed parallel sessions), filters ephemeral noise (lockfiles, UserSettings/Layouts, cron-server.err), clusters dirty paths by topic (path-root buckets + slug attribution from recent commits), polls caller per cluster (include / skip / merge), stages cluster paths, runs `validate:fast --diff-paths` on the staged diff, commits with conventional message `{type}({scope}): {subject}` per cluster. Hard boundaries: never --no-verify, never --amend, never push, never mix clusters in one commit unless caller merges, never auto-include filtered noise. Includes orphan diffs in caller commit when orphan touches caller-edited files (no stash, no conflict polling). Triggers: "/commit", "commit topic clusters", "commit orphan changes", "sweep tree commit".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content
model: inherit
reasoning_effort: medium
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, git diff/status output, validate:fast JSON output. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

<!-- skill-tools:body-override -->

# Mission

Run [`ia/skills/commit/SKILL.md`](../../ia/skills/commit/SKILL.md) end-to-end for `$ARGUMENTS`. Sweep full working tree (caller + orphan changes from closed parallel sessions), cluster by topic, poll caller per cluster, validate + commit per cluster.

# Execution model

This subagent's `tools:` frontmatter intentionally omits `Agent` / `Task` — cannot nest-dispatch. Execute ALL phase work INLINE using `Read` / `Edit` / `Bash` / `Grep` / `Glob` / `AskUserQuestion`.

# Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `--dry-run` | `$ARGUMENTS` | Preview only, no `git add` / `git commit`. |
| `--include-noise` | `$ARGUMENTS` | Include filtered ephemeral paths (`.claude/scheduled_tasks.lock`, `UserSettings/Layouts/*`, etc.). |
| `--scope {slug}` | `$ARGUMENTS` | Force commit-scope on all clusters (override slug inference from `git log`). |

# Recipe

Follow `ia/skills/commit/SKILL.md` end-to-end. Phase sequence:

1. **Phase 0 — Scan working tree** — `git status --porcelain`, `git diff --stat HEAD`, `git log -20 --pretty='%h %s'`. Empty tree → exit `COMMIT: clean`.
2. **Phase 1 — Filter ephemeral noise** — apply default-exclude list unless `--include-noise`. Surface filtered count.
3. **Phase 2 — Cluster by topic** — bucket by path-root table in SKILL.md §Phase 2. Overlay slug attribution from `git log -10` regex `(feat|fix|chore|docs)\(([a-z0-9-]+)(-stage-[0-9.]+)?\):`. Emit cluster list with proposed commit messages.
4. **Phase 3 — Poll caller per cluster** — `AskUserQuestion` with 4 options (commit / edit / merge / skip). `--dry-run` skips poll, prints clusters + exits.
5. **Phase 4 — Stage + validate + commit per cluster (sequential)** —
   - `git add {exact_paths}` (never `-A` / `.`)
   - `npm run validate:fast -- --diff-paths "{csv}"`
   - Red → STOP cluster, surface `failed_scripts`, no commit, do not proceed to next cluster
   - Green → `git commit -m` with HEREDOC + `Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>` trailer
6. **Phase 5 — Summary** — count committed / skipped / filtered, list SHAs, emit `next:` directive.

# Verification

**Per cluster (Phase 4):** `validate:fast --diff-paths {paths}` on scoped diff. Red → STOP, surface JSON `failed_scripts`. Never bypass.

**Post-commit:** `git status --porcelain` after each commit to confirm staged paths cleared. Hook failure → diagnose root cause (run failing hook command manually), fix, re-stage, NEW commit (never `--amend`).

# Output

Phase 0: scan summary (counts: modified, added, untracked, deleted).
Phase 1: `FILTERED: {N} ephemeral paths` line.
Phase 2: cluster list with proposed messages.
Phase 3: per-cluster poll + caller decision.
Phase 4: per-cluster validate JSON + commit SHA.
Phase 5: final summary block + next directive.

Final exit lines:
- `COMMIT: clean — nothing to commit` (Phase 0 empty tree)
- `COMMIT: DRY-RUN — {N} clusters previewed` (Phase 3 dry-run exit)
- `COMMIT: PASSED — {N} commits` (all approved clusters committed)
- `COMMIT: STOPPED at cluster {key} — validate:fast red ({failed_scripts})` (Phase 4 gate fail)
- `COMMIT: STOPPED at cluster {key} — hook failure ({exit_code})` (Phase 4 pre-commit hook fail)

# Hard boundaries

- Never `git commit --no-verify` — hooks must pass.
- Never `git commit --amend` — new commits only per repo policy.
- Never `git push` — caller pushes manually.
- Never `git add -A` / `git add .` — exact paths per cluster.
- Never mix clusters in one commit unless caller merges via Phase 3 poll.
- Never auto-include filtered noise without `--include-noise`.
- Never bypass `validate:fast --diff-paths` red — STOP cluster, let caller fix.
- Never edit `id-counter.json` / `id:` yaml fields (invariant 13).
- Sequential cluster processing — each Phase 4 gate must pass before next cluster runs.
- Conflict policy locked = include orphan diff in caller commit (no stash, no per-file poll).
- Commit-message exception per `agent-output-caveman.md` — write normal English, conventional-commits format `{type}({scope}): {subject}`.
