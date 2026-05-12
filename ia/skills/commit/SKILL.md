---
name: commit
purpose: >-
  Topic-clustered commit pipeline. Sweeps full working tree (caller + orphan changes from
  closed parallel sessions), clusters dirty paths by topic (path-root + recent commit slug),
  validates each cluster via validate:fast --diff-paths, commits one per topic.
audience: agent
loaded_by: "skill:commit"
slices_via: none
description: >-
  Topic-clustered commit pipeline. Sweeps `git status --porcelain` + `git diff --stat HEAD`
  (caller session + orphan changes from closed parallel sessions), filters ephemeral noise
  (lockfiles, UserSettings/Layouts, cron-server.err), clusters dirty paths by topic
  (path-root buckets + slug attribution from recent commits), polls caller per cluster
  (include / skip / merge), stages cluster paths, runs `validate:fast --diff-paths` on the
  staged diff, commits with conventional message `{type}({scope}): {subject}` per cluster.
  Hard boundaries: never --no-verify, never --amend, never push, never mix clusters in one
  commit unless caller merges, never auto-include filtered noise. Includes orphan diffs in
  caller commit when orphan touches caller-edited files (no stash, no conflict polling).
  Triggers: "/commit", "commit topic clusters", "commit orphan changes", "sweep tree
  commit".
phases:
  - Scan working tree
  - Filter ephemeral noise
  - Cluster by topic
  - Poll caller per cluster
  - Stage + validate + commit (per cluster)
  - Summary
triggers:
  - /commit
  - commit topic clusters
  - commit orphan changes
  - sweep tree commit
argument_hint: "[--dry-run] [--include-noise] [--scope {slug}]"
model: inherit
reasoning_effort: medium
input_token_budget: 80000
pre_split_threshold: 70000
tools_role: planner
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - git diff/status output
  - validate:fast JSON output
hard_boundaries:
  - Never `git commit --no-verify` — hook failures must be diagnosed and fixed.
  - Never `git commit --amend` — always new commits per repo policy.
  - Never `git push` — caller pushes manually after review.
  - Never mix clusters in one commit unless caller explicitly merges via poll.
  - Never auto-include filtered noise (lockfiles, UserSettings/Layouts/*, cron-server.err, .claude/scheduled_tasks.lock) without `--include-noise`.
  - Never bypass `validate:fast --diff-paths` red verdict — STOP cluster on red, surface failure, let caller fix before re-running.
  - Never stage paths outside the cluster being committed — `git add {paths}` exact, never `git add -A` / `git add .`.
  - Never edit `id-counter.json` or `id:` yaml fields per invariant 13.
  - Untracked `??` paths included by default unless matched by noise filter; caller can `--dry-run` to preview.
  - Conflict policy = include orphan diff in caller commit (decision locked) — no stash, no per-file polling.
caller_agent: commit
---

# Commit — topic-clustered tree sweep

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: commit messages (normal English per conventional-commits), git diff/status verbatim, validate:fast JSON.

**Problem solved.** Agents close without commit → orphan diffs drift across parallel sessions. `/commit` sweeps full working tree, attributes dirty paths to topic clusters (path roots + recent slug), commits each cluster atomically with validated diff.

**Detection mode (locked):** Git working tree only. No `.claude/projects/**/*.jsonl` scan, no DB audit log query.

**Split policy (locked):** One commit per detected topic cluster. Caller approves each via poll.

**Conflict policy (locked):** Orphan diff included in caller commit when paths overlap. No stash.

**Pre-commit gate (locked):** `validate:fast --diff-paths {cluster_paths}` per cluster. Red → STOP cluster.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `--dry-run` | flag | Preview clusters + commit messages, no `git add` / `git commit` |
| `--include-noise` | flag | Include `.claude/scheduled_tasks.lock`, `UserSettings/Layouts/*`, `cron-server.err`, other ephemeral files |
| `--scope {slug}` | flag | Force-attribute all clusters to `{slug}` (override slug inference from `git log`) |

---

## Phase 0 — Scan working tree

```bash
git status --porcelain
git diff --stat HEAD
git diff --stat --cached
git log -20 --pretty='%h %s'
```

Parse:
- **Modified** (`M `, ` M`, `MM`) — staged + unstaged
- **Added** (`A `, `AM`) — staged-new
- **Untracked** (`??`) — new files not yet `git add`'d
- **Deleted** (`D `, ` D`) — file removed
- **Renamed** (`R `) — staged rename

Empty tree → exit `COMMIT: clean — nothing to commit`.

## Phase 1 — Filter ephemeral noise

Default-exclude (unless `--include-noise`):

```
.claude/scheduled_tasks.lock
.claude/projects/**
UserSettings/Layouts/**
tools/cron-server/launchd/*.err
tools/cron-server/launchd/*.out
**/.DS_Store
**/Temp/UnityLockfile
**/Library/**
**/obj/**
**/bin/**
```

Surfaced as `FILTERED: {N} ephemeral paths` line; caller can re-run with `--include-noise` if needed.

## Phase 2 — Cluster by topic

**Bucket heuristic — path roots** (matched in order, first wins):

| Root pattern | Bucket key |
|---|---|
| `Assets/Scripts/Domains/{X}/**` | `domains-{x}` |
| `Assets/Scripts/Managers/{X}.cs` | `domains-{x}` (same bucket as Domains/) |
| `Assets/Scripts/UI/{X}/**` | `ui-{x}` |
| `Assets/UI/**` (generated prefabs, snapshots) | `ui-bake` |
| `Assets/Scenes/**` | `scene-wire` |
| `Assets/Tests/EditMode/Atomization/Stage{N.N}/**` | `atomization-stage-{n.n}` |
| `Assets/Tests/**` | `tests-{first-subdir}` |
| `docs/explorations/{slug}.md` | `exploration-{slug}` |
| `docs/{topic}-*.md` (root docs) | `docs-{topic}` |
| `tools/mcp-ia-server/**` | `mcp-server` |
| `tools/scripts/{X}/**` or `tools/scripts/{X}.{ext}` | `tooling-{x}` |
| `tools/cron-server/**` | `cron-server` |
| `ia/skills/{slug}/**` | `skill-{slug}` |
| `ia/rules/**` | `ia-rules` |
| `ia/projects/{id}.md` | `project-{id}` (lowercased) |
| `ia/backlog/{id}.yaml` | `project-{id}` (merges with spec) |
| `ia/specs/**` | `ia-specs` |
| `db/migrations/**` | `db-migrations` |
| `.claude/**` (non-filtered) | `claude-config` |
| catch-all | `misc` |

**Slug attribution** — overlay recent commit slug onto bucket:
1. Parse `git log -10 --pretty='%s'` → extract `feat\|fix\|chore\|docs(({slug}(-stage-{N})?)):` captures.
2. Most-recent slug + stage = `active-slug-stage`.
3. If a bucket's paths fall under a domain known to be in that slug-stage's scope (heuristic: bucket key contains a slug-stage path root, OR caller passes `--scope {slug}`) → relabel bucket commit-scope as `{slug}-stage-{N}`.

**Cluster output shape:**

```
CLUSTER {key}: {N} paths, scope={commit-scope}, type={feat|fix|chore|docs}
  {path1}
  {path2}
  ...
  proposed commit: {type}({commit-scope}): {subject}
```

`type` inference:
- New files (`??` only) under `Assets/`, `tools/`, `web/` → `feat`
- Modified-only under `tools/`, `db/migrations/` → `fix` if recent commit log shows bugfix context, else `feat`
- Pure docs (`docs/**`, `ia/specs/**`, `ia/skills/**`, `ia/rules/**`) → `docs`
- Pure config (`.claude/**`, `UserSettings/**` with `--include-noise`) → `chore`
- Mixed → `feat` default

`subject` synthesis: scan diff content per cluster, pick 3–6 keywords describing the change, format `{verb} + {object}` (e.g., `WaterManager THIN + WaterService extraction`).

## Phase 3 — Poll caller per cluster

Per cluster, call `AskUserQuestion`:

```
Question: Commit cluster `{key}` ({N} paths, scope={commit-scope})?
  Proposed: {type}({commit-scope}): {subject}

Options:
  1. Commit as proposed
  2. Edit message (poll subject text)
  3. Merge with cluster {other-key}
  4. Skip — leave dirty
```

Merge action → re-runs Phase 3 on merged cluster (new combined commit message). Skip → cluster dropped, paths stay dirty.

**`--dry-run`:** print all clusters + proposed messages, exit. No poll, no commit.

## Phase 4 — Stage + validate + commit (per cluster, sequential)

Per approved cluster:

```bash
# stage exact paths (never -A / .)
git add {p1} {p2} ... {pN}

# scoped validation
npm run validate:fast -- --diff-paths "{p1},{p2},...,{pN}"
```

Red verdict → STOP cluster:
- Surface validate:fast JSON `failed_scripts`.
- Caller fixes → re-run `/commit` (cluster re-detected from remaining dirty tree).
- Other clusters NOT auto-committed (sequential — each gates next).

Green verdict → commit:

```bash
git commit -m "$(cat <<'EOF'
{type}({commit-scope}): {subject}

{optional body — only when diff > 200 lines or > 5 paths}

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

Idempotent on hook failure: `git status --porcelain` after each commit; staged paths cleared on success.

## Phase 5 — Summary

```
COMMIT: {N} clusters committed, {M} skipped, {K} filtered
  ✓ {sha1} {type}({scope}): {subject}
  ✓ {sha2} ...
  · skipped: {key} ({reason})
  · filtered: {N} ephemeral paths
  next: review with `git log -{N}` + push when ready
```

Stop conditions:
- `COMMIT: clean — nothing to commit` (Phase 0 empty tree)
- `COMMIT: STOPPED at cluster {key} — validate:fast red ({failed_scripts})` (Phase 4 gate)
- `COMMIT: STOPPED at cluster {key} — hook failure ({exit_code})` (Phase 4 pre-commit hook)
- `COMMIT: PASSED — {N} commits` (all approved clusters committed)

---

## Guardrails

- Never `git push` (caller pushes manually).
- Never `--amend` (new commits only).
- Never `--no-verify` (hooks are gospel).
- Never `git add -A` / `git add .` — exact paths per cluster.
- Sequential cluster processing — each Phase 4 gate must pass before next.
- Empty tree exit fast.
- Noise filter applied before clustering — cluster keys never include ephemeral paths.
- Conflict policy (orphan + caller touch same file) = include orphan diff verbatim, no stash, no per-file poll.
- Validate via `validate:fast --diff-paths {csv}` — scoped to cluster paths, not full tree.
