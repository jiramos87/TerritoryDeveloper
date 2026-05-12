---
description: Topic-clustered commit pipeline. Sweeps `git status --porcelain` + `git diff --stat HEAD` (caller session + orphan changes from closed parallel sessions), filters ephemeral noise (lockfiles, UserSettings/Layouts, cron-server.err), clusters dirty paths by topic (path-root buckets + slug attribution from recent commits), polls caller per cluster (include / skip / merge), stages cluster paths, runs `validate:fast --diff-paths` on the staged diff, commits with conventional message `{type}({scope}): {subject}` per cluster. Hard boundaries: never --no-verify, never --amend, never push, never mix clusters in one commit unless caller merges, never auto-include filtered noise. Includes orphan diffs in caller commit when orphan touches caller-edited files (no stash, no conflict polling). Triggers: "/commit", "commit topic clusters", "commit orphan changes", "sweep tree commit".
argument-hint: "[--dry-run] [--include-noise] [--scope {slug}]"
---

# /commit — Topic-clustered commit pipeline. Sweeps full working tree (caller + orphan changes from closed parallel sessions), clusters dirty paths by topic (path-root + recent commit slug), validates each cluster via validate:fast --diff-paths, commits one per topic.

Drive `$ARGUMENTS` via the [`commit`](../agents/commit.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, git diff/status output, validate:fast JSON output. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /commit
- commit topic clusters
- commit orphan changes
- sweep tree commit
<!-- skill-tools:body-override -->

<!-- skill-tools:body-override -->

**Scope:** Working-tree sweep. Caller-session changes + orphan diffs from closed parallel sessions. One commit per topic cluster.

**Locked decisions:**
- Detection = git working tree only (no `.claude/*.jsonl`, no DB audit log)
- Split = one commit per topic cluster
- Conflict = include orphan diff in caller commit (no stash)
- Pre-commit gate = `validate:fast --diff-paths` per cluster

**Related:**
- [`caveman:caveman-commit`](../caveman-commit.md) — caveman-style commit message helper. `/commit` invokes per-cluster after subject synthesis.
- [`/verify-loop`](verify-loop.md) — full closed-loop verification (use BEFORE `/commit` when bridge/compile evidence needed).

## Dispatch

Single Agent invocation with `subagent_type: "commit"` carrying `$ARGUMENTS` verbatim. Subagent runs `ia/skills/commit/SKILL.md` end-to-end inline (no nested dispatch).

## Argument forms

- `/commit` — full sweep + poll per cluster
- `/commit --dry-run` — preview clusters + proposed messages, no `git add` / `git commit`
- `/commit --include-noise` — include `.claude/scheduled_tasks.lock`, `UserSettings/Layouts/*`, `cron-server.err`
- `/commit --scope {slug}` — force-attribute all clusters to `{slug}` (override `git log` slug inference)

## Pipeline summary output

After all clusters processed:

```
COMMIT: {N} clusters committed, {M} skipped, {K} filtered
  ✓ {sha1} {type}({scope}): {subject}
  ✓ {sha2} ...
  · skipped: {key} ({reason})
  · filtered: {N} ephemeral paths
  next: review `git log -{N}` + push when ready
```

## Hard boundaries

- Never `git push` — caller pushes manually after review.
- Never `git commit --amend` — new commits only.
- Never `git commit --no-verify` — hooks are gospel.
- Never `git add -A` / `git add .` — exact paths per cluster.
- Sequential cluster gate — Phase 4 validates before next cluster runs.
- Noise filter applied before clustering — ephemeral paths never reach commit.
- Orphan diff included verbatim when path overlaps caller's edits — no stash, no per-file poll.
