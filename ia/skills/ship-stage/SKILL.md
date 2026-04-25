---
name: ship-stage
purpose: >-
  Two-pass DB-backed Stage chain. Pass A = per-task implement + unity:compile-check fast-fail gate +
  task_status_flip(implemented). NO per-task commits. Pass B = per-stage verify-loop + code-review
  (inline fix cap=1) + per-task task_status_flip(verified→done) + stage_closeout_apply + single
  stage-end commit + per-task task_commit_record + stage_verification_flip. Resume gate via task_state
  status query (no git scan).
audience: agent
loaded_by: "skill:ship-stage"
slices_via: stage_bundle, task_state, task_spec_section, glossary_lookup, invariants_summary
description: >-
  Opus orchestrator. Drives every non-terminal task of one Stage X.Y through a two-pass DB-backed
  chain. Pass A (per-task): implement + unity:compile-check fast-fail gate +
  task_status_flip(implemented). NO per-task commits — Pass A leaves a dirty worktree. Pass B
  (per-stage): verify-loop on cumulative HEAD diff + code-review on Stage diff (inline fix cap=1) +
  per-task task_status_flip(verified→done) + stage_closeout_apply + master_plan_change_log_append
  (audit row) + single stage commit feat({slug}-stage-X.Y) + per-task task_commit_record +
  stage_verification_flip(pass, commit_sha). Resume gate queries task_state per pending task;
  status='implemented' skips Pass A. PASS_B_ONLY when all tasks implemented but stage not done. Idle
  exit when all tasks done/archived AND ia_stages.status=done. Triggers: "/ship-stage", "ship stage",
  "chain stage tasks".
phases:
  - Parse stage
  - Stage state load
  - Context load
  - Plan Digest readiness gate
  - Resume gate
  - Pass A per-task (implement + compile + status flip)
  - Pass B per-stage (verify + code-review)
  - Inline closeout (DB-only)
  - Stage commit + verification record
  - Chain digest
  - Next-stage resolver
triggers:
  - /ship-stage
  - ship stage
  - chain stage tasks
argument_hint: {MASTER_PLAN_PATH} {STAGE_ID} [--no-resume] [--force-model {model}]
model: inherit
reasoning_effort: high
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__spec_outline
  - mcp__territory-ia__list_specs
  - mcp__territory-ia__invariant_preflight
  - mcp__territory-ia__stage_bundle
  - mcp__territory-ia__stage_state
  - mcp__territory-ia__task_state
  - mcp__territory-ia__task_bundle
  - mcp__territory-ia__task_spec_section
  - mcp__territory-ia__task_spec_body
  - mcp__territory-ia__master_plan_state
  - mcp__territory-ia__master_plan_render
  - mcp__territory-ia__stage_render
  - mcp__territory-ia__master_plan_preamble_write
  - mcp__territory-ia__master_plan_change_log_append
  - mcp__territory-ia__task_status_flip
  - mcp__territory-ia__stage_closeout_apply
  - mcp__territory-ia__task_commit_record
  - mcp__territory-ia__stage_verification_flip
  - mcp__territory-ia__journal_append
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - chain-level digest JSON
  - destructive-op confirmations
hard_boundaries:
  - Sequential per-task dispatch only — no parallel.
  - "**Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers all Pass A diffs + code-review fixes + closeout mv. Do NOT emit `feat({ISSUE_ID}):` per task."
  - Resume gate (Phase 4) queries DB via `task_state` / `stage_bundle` only. Do NOT git-scan.
  - Stop on first Pass A gate failure (implement, compile, scene-wiring); do NOT continue to next task.
  - Do NOT rollback Pass A status flips on `STAGE_VERIFY_FAIL` or `STAGE_CODE_REVIEW_CRITICAL_TWICE` — DB stays `implemented`; worktree stays dirty; human repairs via re-run.
  - Code-review critical re-entry cap=1; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
  - "**Code-reviewer applies fixes inline via direct Edit/Write.** Do NOT write `§Code Fix Plan` tuples."
  - "**Pass B (verify → code-review → closeout → commit → verification record) is MANDATORY. `PASSED` is forbidden until Step 8 commit + verification flip succeed.** Applies on resume path too (PASS_B_ONLY)."
caller_agent: ship-stage
---

# Ship-stage — DB-backed two-pass chain dispatcher

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md). H3 `### Stage X.Y` heading; 5-col task table `Task | Name | Issue | Status | Intent`; 2-level Stage > Task hierarchy.

**Related:**
- [`verify-loop`](../verify-loop/SKILL.md) — internal `MAX_ITERATIONS=2` fix loop (no outer retry here).
- [`stage-authoring`](../stage-authoring/SKILL.md) — populates §Plan Digest in DB before this skill runs.
- [`spec-implementer`](../spec-implementer/SKILL.md) — Pass A implement subagent.
- [`opus-code-reviewer`](../opus-code-reviewer/SKILL.md) — Pass B code-review (inline fix).
- Scene wiring contract: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md).
- Verification policy: [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

## Normative — closeout is part of `PASSED`

When Pass B upstream gates succeed (verify-loop `verdict: pass`; code-review not critical second time), the chain **must** run **Step 4 inline closeout** in the **same** invocation. Do **not**:

- Emit `SHIP_STAGE {STAGE_ID}: PASSED` after verify or code-review alone.
- Tell the operator to run a separate `/closeout` later.

`SHIP_STAGE {STAGE_ID}: PASSED` is valid **only** after `stage_closeout_apply` succeeded + stage commit landed + `stage_verification_flip(pass, commit_sha)` recorded.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `MASTER_PLAN_PATH` | User prompt | Repo-relative `ia/projects/{slug}-master-plan.md`. Slug = basename minus `-master-plan.md`. |
| `STAGE_ID` | User prompt | Stage identifier as in master plan header. Accept `Stage 1.1`, `1.1`, `Stage 1`, `1`. Strip `Stage ` prefix when calling DB MCP. |
| `--no-resume` | Optional flag | Disables Step 4 resume gate; every task with status≠done gets fresh Pass A. Use for forensic replay or worktree reset recovery. |

**Dispatch-shape agnostic:** identical behavior whether invoked as Task-dispatched subagent or inline by orchestrator.

---

## Step 0 — Parse stage

1. Parse `MASTER_PLAN_PATH` + `STAGE_ID` from `$ARGUMENTS`. Missing either → STOPPED + usage.
2. Derive `SLUG` = basename of `MASTER_PLAN_PATH` minus `-master-plan.md` (e.g. `ia/projects/citystats-overhaul-master-plan.md` → `citystats-overhaul`).
3. Normalize `STAGE_ID_DB` = `STAGE_ID` minus optional `Stage ` prefix (e.g. `Stage 1.1` → `1.1`).
4. Store `SESSION_ID` = `ship-stage-{SLUG}-{STAGE_ID_DB}-{ISO8601_compact}` for `journal_append` calls.

---

## Step 1 — Stage state load

Call `stage_bundle(slug=SLUG, stage_id=STAGE_ID_DB)`. Returns:
- `master_plan_title`
- `stage`: `{stage_id, title, status, ...}`
- `tasks`: array of `{task_id, title, status, ...}` in stage order
- `status_counts`
- `next_pending`
- `latest_verification`

**Stale-DB guard:** if call returns `not_found` for the stage → STOPPED + `Next: claude-personal "/stage-file {MASTER_PLAN_PATH} {STAGE_ID}"` (stage not yet filed in DB).

**Branches:**
- `stage.status == "done"` AND all `tasks.status ∈ {done, archived}` → idle exit `SHIP_STAGE {STAGE_ID}: all tasks already done. No work needed.` + Step 7 next-stage resolver.
- Else → continue.

Define:
- `PENDING_TASKS` = tasks with `status ∈ {pending, implemented}` (drives Pass A + Pass B).
- `STAGE_TASK_IDS` = all task ids in `tasks` (full Stage scope for code-review + closeout).

---

## Step 2 — Context load

Run [`domain-context-load`](../domain-context-load/SKILL.md) once for the stage domain:

```
keywords: derive from master_plan_title + stage.title (English)
tooling_only_flag: <auto-detect per heuristic below; default false>
context_label: "{SLUG} {STAGE_ID_DB}"
```

**`tooling_only_flag` heuristic:** flip to `true` when `MASTER_PLAN_PATH` matches `/mcp-lifecycle-tools|ia-infrastructure|tooling|bridge-environment|backlog-yaml-mcp|ia-dev-db/` OR stage touches only `tools/**`, `ia/**`, `.claude/**`, `docs/**`, `web/**` (no `Assets/**/*.cs`).

Store payload `{glossary_anchors, router_domains, spec_sections, invariants}` as `CHAIN_CONTEXT`. Pass to per-task `spec-implementer` + Stage-scoped `opus-code-reviewer`.

---

## Step 3 — §Plan Digest readiness gate

For each task in `PENDING_TASKS`:
- Call `task_spec_section(task_id, "Plan Digest")`.
- Treat as **digested** when section exists AND content is non-empty AND no line matches `_pending` case-insensitively AND sub-headings `§Goal`, `§Acceptance`, `§Mechanical Steps` present.

**If ALL digested:** continue to Step 4.

**If ANY missing:** STOPPED. Emit:
```
SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}
Next: claude-personal "/stage-authoring {MASTER_PLAN_PATH} Stage {STAGE_ID_DB}"
```

**Note:** `/stage-authoring` is the DB-backed single-skill that populates `§Plan Digest`. Pre-DB legacy specs already upgraded.

---

## Step 4 — Resume gate

**`--no-resume`:** treat every task in `PENDING_TASKS` as Pass A required → jump to Step 5.

**Default (resume on):**

For each task in `PENDING_TASKS`, classify:
- `task.status == "pending"` → `pass_a_required = true`
- `task.status == "implemented"` → `pass_a_required = false` (Pass A already landed; worktree carries dirty changes OR was reset since)

Emit:
```
SHIP_STAGE resume: Pass A status scan — pending: [{ids}] ; implemented: [{ids}]
```

**Branches:**
- All `pass_a_required = true` → **Full Pass A**. Continue Step 5.
- Some true, some false → **Partial Pass A**. Step 5 loop skips already-implemented tasks.
- All false (`PENDING_TASKS` all `implemented`) → **PASS_B_ONLY**. Skip Step 5. Jump to Step 6. Emit `SHIP_STAGE resume: all pending tasks status=implemented — entering Pass B`.

**Worktree state guard:** when entering PASS_B_ONLY, run `git status --porcelain`. If empty → STOPPED `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. DB says implemented but disk has nothing. Manual repair: re-run Pass A or task_status_flip back to pending.` Otherwise continue.

---

## Step 5 — Pass A: per-task loop (sequential, fail-fast, NO COMMITS)

**Entry:** if Step 4 jumped to PASS_B_ONLY → do not enter Step 5.

For each task in `PENDING_TASKS` in stage order:

```
CURRENT_TASK_ID = task.task_id
```

**Resume skip:** if `pass_a_required == false` for this task → skip Step 5.1 + 5.2 + 5.3. Append journal entry with `pass_a_resume_skipped: true`. Continue to next task.

### Step 5.1 — Implement

Dispatch `spec-implementer` subagent (Sonnet):

> Mission: Execute §Plan Digest §Mechanical Steps for `{CURRENT_TASK_ID}` end-to-end. Read §Plan Digest via `task_spec_section(task_id, "Plan Digest")`. Pre-loaded context: {CHAIN_CONTEXT}. **Do NOT commit.** End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → STOPPED + partial chain digest + `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID_DB}"` (re-enter after fix; resume gate picks up where loop stopped).

### Step 5.2 — Compile gate + scene-wiring preflight

Run `npm run unity:compile-check` (~15 s).

**Scene-wiring preflight:** if §Plan Digest carries a Scene Wiring step, confirm worktree diff includes an edit to `Assets/Scenes/*.unity` OR adds a prefab under `Assets/Prefabs/**`. Use `git diff --name-only` (no commit yet — diff is unstaged worktree changes). Missing wiring under fired trigger → STOPPED:

```
STOPPED at {CURRENT_TASK_ID} — scene_wiring: §Plan Digest Scene Wiring step fired but no Assets/Scenes/*.unity edit in worktree
```

**Compile failure:** STOPPED:

```
STOPPED at {CURRENT_TASK_ID} — compile_gate: {reason}
```

Partial chain digest. `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID_DB}"` after fix.

### Step 5.3 — Status flip (NO commit)

Call `task_status_flip(task_id=CURRENT_TASK_ID, new_status="implemented")`.

Append journal entry:
```
journal_append({
  session_id: SESSION_ID,
  phase: "pass_a.implemented",
  payload_kind: "task_status_flip",
  payload: { task_id: CURRENT_TASK_ID, new_status: "implemented", compile_gate: "passed" },
  task_id: CURRENT_TASK_ID,
  slug: SLUG,
  stage_id: STAGE_ID_DB
})
```

Continue to next task.

**After Step 5 loop:** all `PENDING_TASKS` are `implemented` in DB. Worktree is dirty with cumulative Pass A changes. No commits yet.

---

## Step 6 — Pass B: per-stage bulk (runs ONCE)

**Order is fixed:** 6.1 verify → 6.2 code-review (inline fix cap=1) → 6.3 status flip done.

### Step 6.1 — Verify-loop on cumulative HEAD diff

Cumulative delta anchor = `HEAD` (no per-task commits — worktree carries all Pass A changes). Equivalent: `git diff HEAD`.

Dispatch `verify-loop` subagent (Sonnet) with full Path A + Path B (no `--skip-path-b`):

> Mission: Run full verify-loop (Path A + Path B) on cumulative stage delta = `git diff HEAD`. Stage: {SLUG} {STAGE_ID_DB}. Tasks: {STAGE_TASK_IDS}. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict == "pass"`.

**Verify failure:** STOPPED:

```
SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL
```

Chain digest with `stage_verify: failed` + escalation object mirroring inner verify-loop `gap_reason` taxonomy. Pick `bridge_kind_missing` over `human_judgment_required` whenever a missing `unity_bridge_command` kind could close the loop. No automatic retry. Worktree stays dirty (no rollback). Human owns repair.

Journal:
```
journal_append({ phase: "pass_b.verify", payload_kind: "verify_result", payload: { verdict: "pass" } })
```

### Step 6.2 — Code-review on Stage-level diff (inline fix cap=1)

Dispatch `opus-code-reviewer` subagent (Opus) with Stage diff + shared context:

> Mission: Run code-review on Stage diff = `git diff HEAD`. STAGE_MCP_BUNDLE: {CHAIN_CONTEXT}. All N §Plan Digest sections from `task_spec_section(task_id, "Plan Digest")` are the acceptance reference. Scene-wiring check per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md). Emit verdict (PASS / minor / critical). **On critical: apply fixes inline via direct Edit/Write tools — do NOT write `§Code Fix Plan` tuples + do NOT dispatch plan-applier (E14 retired the code-fix mode).**

**Verdict PASS / minor:** continue to Step 6.3.

**Verdict critical (first time):**
1. Reviewer applied inline fixes (per mission). Worktree carries new diff.
2. Re-enter Step 6.1 verify-loop (cap = 1).
3. Re-run Step 6.2 code-review.
4. Second critical → STOPPED `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest. Human review required. Worktree stays dirty.

Journal each iteration:
```
journal_append({ phase: "pass_b.code_review", payload_kind: "review_verdict", payload: { verdict, iteration } })
```

### Step 6.3 — Per-task status flip (verified → done)

For each task in `STAGE_TASK_IDS` (not just `PENDING_TASKS` — full Stage scope):
- Skip if `task.status ∈ {done, archived}` (defensive guard for re-entry).
- Else call `task_status_flip(task_id, "verified")` then `task_status_flip(task_id, "done")`.
  - Two flips required by enum order (`pending → implemented → verified → done → archived`); `task_status_flip` does not allow skipping intermediate states. (Cross-check: if a task is already `verified`, only the second flip runs.)

Journal: per task `phase: "pass_b.task_done", payload_kind: "task_status_flip", payload: { task_id, new_status: "done" }`.

---

## Step 7 — Inline closeout (DB-only)

**Mandatory** when Step 6 upstream gates succeeded.

### Step 7.1 — DB closeout

Call `stage_closeout_apply(slug=SLUG, stage_id=STAGE_ID_DB)`.

Returns `{slug, stage_id, archived_task_count, stage_status: "done"}`.

**Failure** (any non-terminal task remains): STOPPED `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}`. Should not happen on green path (Step 6.3 flipped all to done) — implies DB drift. Human repair.

### Step 7.2 — Change-log audit row

Append audit row to `ia_master_plan_change_log` via MCP:

```
master_plan_change_log_append({
  slug: SLUG,
  kind: "stage_closed",
  body: "Stage {STAGE_ID_DB} closed — {archived_task_count} tasks archived ({STAGE_TASK_IDS})"
})
```

**No filesystem mv** — DB is sole source of truth for task spec bodies.

Journal: `phase: "closeout.apply", payload_kind: "closeout_result", payload: { archived_task_count }`.

---

## Step 8 — Stage commit + verification record

### Step 8.1 — Stage commit (single, covers all Pass A + closeout changes)

Stage worktree state at this point:
- All Pass A implementation changes (uncommitted — never committed in Pass A).
- Code-review inline fixes (if any iteration ran).
- DB-only closeout (Step 7) leaves no filesystem diff — only `ia_*` tables mutated.

Single commit covers everything. Format:

```
feat({SLUG}-stage-{STAGE_ID_DB}): {short summary from master_plan_title or Stage title}

Stage {STAGE_ID_DB} — {N} tasks: {comma-separated STAGE_TASK_IDS}

Pass A: implement + compile (all tasks)
Pass B: verify-loop pass + code-review {PASS|minor}
Closeout: {archived_task_count} tasks archived; ia_stages.status=done
```

Stage worktree:
1. `git add -A` (stages all Pass A diffs + any code-review fixes).
2. `git commit -m "$(cat <<'EOF' ... EOF)"` (HEREDOC to preserve formatting).
3. Capture commit sha: `STAGE_COMMIT_SHA=$(git rev-parse HEAD)`.

**Hook failure:** if commit fails (pre-commit hook red), do NOT amend or retry blindly. Investigate, fix, re-stage, create NEW commit. Capture new sha.

### Step 8.2 — Per-task commit record

For each task in `STAGE_TASK_IDS`:

```
task_commit_record({
  task_id,
  commit_sha: STAGE_COMMIT_SHA,
  commit_kind: "feat",
  message: "Stage {STAGE_ID_DB} — {SLUG}"
})
```

UNIQUE(task_id, commit_sha) → idempotent on re-run.

### Step 8.3 — Stage verification flip

Call:

```
stage_verification_flip({
  slug: SLUG,
  stage_id: STAGE_ID_DB,
  verdict: "pass",
  commit_sha: STAGE_COMMIT_SHA,
  notes: "ship-stage Pass B green",
  actor: "ship-stage"
})
```

History-preserving INSERT — latest row reflects this run.

Journal: `phase: "stage.commit", payload_kind: "stage_commit", payload: { commit_sha: STAGE_COMMIT_SHA, archived_task_count }`.

---

## Step 9 — Chain digest

Emit one chain-level stage digest at chain end (success or STOPPED).

**Format:** mirrors `.claude/output-styles/closeout-digest.md` (JSON header + caveman summary).

```json
{
  "chain_stage_digest": true,
  "master_plan": "{MASTER_PLAN_PATH}",
  "slug": "{SLUG}",
  "stage_id": "{STAGE_ID_DB}",
  "session_id": "{SESSION_ID}",
  "tasks_shipped": ["TECH-xxx", "TECH-yyy"],
  "tasks_stopped_at": null,
  "stage_verify": "passed|failed|skipped",
  "stage_commit_sha": "{STAGE_COMMIT_SHA}",
  "archived_task_count": N,
  "next_handoff": {
    "case": "filed|pending|skeleton|umbrella-done|stopped|stage_verify_fail",
    "command": "/ship-stage|/stage-file|/closeout",
    "args": "ia/projects/{slug}-master-plan.md Stage X.Y",
    "shell": "claude-personal \"...\""
  }
}
```

Caveman summary follows JSON: tasks shipped, stage commit sha (short), verify outcome, next step.

---

## Step 10 — Next-stage resolver

Re-call `master_plan_state(slug=SLUG)`. Scan stages after `STAGE_ID_DB`:

**3 cases (priority order):**

1. **Next filed stage** — next stage with ≥1 task `status ∈ {pending, implemented}` (real ids, not `_pending_`):
   → `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage X.Y"`

2. **Next pending stage** — next stage where tasks are `_pending_` (not yet filed in DB):
   → `Next: claude-personal "/stage-file {MASTER_PLAN_PATH} Stage X.Y"`

3. **Umbrella done** — no more stages:
   → `Next: claude-personal "/closeout {UMBRELLA_ISSUE_ID}"` if identifiable from master plan header. Else `All stages done — umbrella close pending.`

**Skeleton stages** (no tasks at all) are author-time gaps; surface as `STOPPED — skeleton stage encountered: Stage X.Y. Author tasks via stage-authoring or extend master plan.`

---

## Exit lines

- **Success:** `SHIP_STAGE {STAGE_ID}: PASSED` + chain digest + `Next:` handoff. Only after Step 7 closeout + Step 8 stage commit + `stage_verification_flip` succeeded.
- **Readiness gate fail:** `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff.
- **Stale-DB stage not found:** `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` + `/stage-file` handoff.
- **PASS_B_ONLY worktree-clean inconsistency:** `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. DB says implemented but disk has nothing.` + manual repair directive.
- **Pass A implement failure:** `STOPPED at {ISSUE_ID} — implement: {reason}` + partial chain digest + `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID_DB}"` after fix.
- **Pass A compile failure:** `STOPPED at {ISSUE_ID} — compile_gate: {reason}` + partial chain digest + same `/ship-stage` re-entry.
- **Pass A scene-wiring failure:** `STOPPED at {ISSUE_ID} — scene_wiring: ...` + same `/ship-stage` re-entry after wiring.
- **Pass B verify failure:** `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` + human review directive. Worktree stays dirty.
- **Pass B code-review critical twice:** `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest + human review required.
- **Closeout failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` + chain digest + DB-drift repair directive.
- **Stage commit failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` + chain digest + repair directive (do NOT amend; investigate hook).

---

## Hard boundaries

- Sequential per-task dispatch only — tasks share files + invariants; no parallel.
- **Pass A NEVER commits.** No `git commit feat({ISSUE_ID})` per task. Single stage commit at Step 8.1.
- Resume gate (Step 4) queries `task_state` / `stage_bundle` — does NOT git-scan for commit subjects.
- Stop on first Pass A gate failure (compile, scene-wiring, implement); do NOT continue to next task.
- Do NOT roll back Pass A status flips on STAGE_VERIFY_FAIL or STAGE_CODE_REVIEW_CRITICAL_TWICE — DB stays at `implemented`; worktree stays dirty; human repairs via re-run after fix.
- Code-review critical re-entry cap = 1; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
- Code-reviewer applies fixes **inline via direct Edit/Write** — do NOT write `§Code Fix Plan` tuples.
- Inline closeout (Step 7) is mandatory on green Pass B — Stage closeout always runs inline.
- Stage commit at Step 8.1 covers ALL changes (Pass A + code-review fixes) in ONE commit. Closeout (Step 7) is DB-only — no filesystem diff.
- `domain-context-load` fires ONCE at chain start (Step 2); do NOT re-call per task.
- Do NOT auto-invoke `/stage-authoring` from inside `/ship-stage` — Step 3 is a readiness gate only, hands off if missing.
- Do NOT read or edit `ia/projects/{slug}-master-plan.md` OR `ia/projects/{slug}/index.md` OR `ia/projects/{slug}/stage-*.md` OR `ia/projects/{ISSUE_ID}.md` — DB is source of truth. Closeout writes audit rows via `master_plan_change_log_append`, never `git mv` to `_closed/`.
- DB is source of truth. Do NOT fall back to filesystem-only operations on `db_unavailable` — escalate to human; halt chain.

---

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{MASTER_PLAN_PATH}` | Repo-relative path to master plan (e.g. `ia/projects/citystats-overhaul-master-plan.md`) |
| `{SLUG}` | basename of `MASTER_PLAN_PATH` minus `-master-plan.md` |
| `{STAGE_ID}` | Stage identifier as user typed (e.g. `Stage 1.1`) |
| `{STAGE_ID_DB}` | `STAGE_ID` minus `Stage ` prefix (DB-canonical, e.g. `1.1`) |
| `{SESSION_ID}` | `ship-stage-{SLUG}-{STAGE_ID_DB}-{ISO8601_compact}` |
| `{CURRENT_TASK_ID}` | Active task id in Step 5 loop |
| `{STAGE_TASK_IDS}` | All task ids in stage (Step 1 result) |
| `{PENDING_TASKS}` | Tasks with `status ∈ {pending, implemented}` (drives Pass A + Pass B scope) |
| `{CHAIN_CONTEXT}` | `domain-context-load` payload `{glossary_anchors, router_domains, spec_sections, invariants}` |
| `{STAGE_COMMIT_SHA}` | Captured `git rev-parse HEAD` after Step 8.1 commit |

---

## Open Questions

- Crash-survivable session journal: `journal_append` writes to `ia_ship_stage_journal` table — survives process crash. Resume on re-invocation reads journal by `session_id` to detect mid-Pass-B state (e.g. verify done but status-flip not). Currently Step 6 re-runs as a unit; finer sub-step resume deferred.

---

## Changelog

### 2026-04-24 — Step 8 of `ia-dev-db-refactor`: Pass A no-commit + Pass B inline closeout (DB-backed)

**Status:** applied (smoke pending)

**Symptom:**
Pre-Step-8 ship-stage made per-task commits in Pass A, then ran Stage closeout via `stage-closeout-planner` → `plan-applier` Mode stage-closeout pair. N+1 commits per stage (N task commits + 1 closeout commit). Resume gate scanned `git log --first-parent` for `feat({ISSUE_ID}):` / `fix({ISSUE_ID}):` subjects. Closeout pair wrote `§Stage Closeout Plan` tuples + applied them via plan-applier.

**Fix per design C9 / C10 / E11 / E13 / E14:**
- **Pass A (per-task):** implement + `unity:compile-check` + `task_status_flip(implemented)`. NO commit.
- **Pass B (per-stage):** `verify-loop` on cumulative HEAD diff + `opus-code-reviewer` (inline Edit/Write fix cap=1, no `§Code Fix Plan` tuples) + per-task `task_status_flip(verified→done)`.
- **Inline closeout:** `stage_closeout_apply` (DB) + `master_plan_change_log_append` audit row. Drops `stage-closeout-plan` + `stage-closeout-apply` skills + `plan-applier` code-fix mode + `stage-closeout-planner` agent. No filesystem mv post Step 9.6 (folder shape retired; flat task specs deleted).
- **Stage commit:** single `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covering all Pass A diffs + code-review fixes (E13). Closeout is DB-only.
- **Per-task commit record:** `task_commit_record(task_id, STAGE_COMMIT_SHA, "feat")` per task (shared sha; idempotent per UNIQUE).
- **Stage verification:** `stage_verification_flip(slug, stage_id, "pass", STAGE_COMMIT_SHA)` (E11 history-preserving).
- **Resume gate:** `task_state.status == "implemented"` query replaces git scan. PASS_B_ONLY when all `PENDING_TASKS` are `implemented`. Worktree-clean inconsistency check at PASS_B_ONLY entry.
- **Drop:** `--per-task-verify` flag (legacy rollback for pre-DB), Step 1.6 git scan, lazy-migration `§Plan Author → §Plan Digest` branch, `Step 1.5` reference to `plan-author` / `plan-review`.

**Acceptance gate:** smoke run on filed+authored throwaway stage on `feature/ia-dev-db-refactor` branch. All tasks `implemented → verified → done`; `ia_master_plan_change_log` carries `stage_closed` audit row; single `feat({slug}-stage-X.Y):` commit on branch; no per-task commits during Pass A; `ia_stage_verifications` row populated.

**Rollout row:** ia-dev-db-refactor-step-8

---
