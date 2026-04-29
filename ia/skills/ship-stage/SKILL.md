---
name: ship-stage
purpose: >-
  Two-pass DB-backed Stage chain. Pass A = per-task implement + unity:compile-check fast-fail gate +
  task_status_flip(implemented). NO per-task commits. Pass B = per-stage verify-loop + per-task
  task_status_flip(verified→done) + stage_closeout_apply + single stage-end commit + per-task
  task_commit_record + stage_verification_flip. Resume gate via task_state status query (no git
  scan).
audience: agent
loaded_by: "skill:ship-stage"
slices_via: stage_bundle, task_state, task_spec_section, glossary_lookup, invariants_summary
description: >-
  Opus orchestrator. Drives every non-terminal task of one Stage X.Y through a two-pass DB-backed
  chain. Pass A (per-task): implement + unity:compile-check fast-fail gate +
  task_status_flip(implemented). NO per-task commits — Pass A leaves a dirty worktree. Pass B
  (per-stage): verify-loop on cumulative HEAD diff + per-task task_status_flip(verified→done) +
  stage_closeout_apply + master_plan_change_log_append (audit row) + single stage commit
  feat({slug}-stage-X.Y) + per-task task_commit_record + stage_verification_flip(pass, commit_sha).
  Code-review intentionally NOT part of this chain — verify-loop + validation are the gate;
  standalone /code-review remains available out-of-band. Resume gate queries task_state per pending
  task; status='implemented' skips Pass A. PASS_B_ONLY when all tasks implemented but stage not
  done. On resume with new diff a fresh stage commit is created. Idle exit when all tasks
  done/archived AND ia_stages.status=done. Triggers: "/ship-stage", "ship stage", "chain stage
  tasks".
phases:
  - Parse stage
  - Stage state load
  - Baseline worktree snapshot (chain-scope guard)
  - Context load
  - Plan Digest readiness gate
  - Resume gate
  - Pass A per-task (implement + compile + status flip)
  - Pass B per-stage (verify + verified→done flips)
  - Inline closeout (DB-only)
  - Stage commit (chain-scope delta only) + verification record
  - Chain digest
  - Next-stage resolver
triggers:
  - /ship-stage
  - ship stage
  - chain stage tasks
argument_hint: "{SLUG} {STAGE_ID} [--no-resume] [--force-model {model}]"
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
  - mcp__territory-ia__stage_claim
  - mcp__territory-ia__stage_claim_release
  - mcp__territory-ia__claim_heartbeat
  - mcp__territory-ia__arch_drift_scan
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
  - "**Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers all Pass A diffs (chain-scope only). Do NOT emit `feat({ISSUE_ID}):` per task."
  - Resume gate (Phase 4) queries DB via `task_state` / `stage_bundle` only. Do NOT git-scan.
  - Stop on first Pass A gate failure (implement, compile, scene-wiring); do NOT continue to next task.
  - Do NOT rollback Pass A status flips on `STAGE_VERIFY_FAIL` — DB stays `implemented`; worktree stays dirty; human repairs via re-run.
  - "**No code-review in this chain.** Verify-loop + validation are the gate; standalone `/code-review` is a separate out-of-band seam (lifecycle row 9)."
  - "**Pass B (verify → verified→done flips → closeout → commit → verification record) is MANDATORY. `PASSED` is forbidden until Step 8 commit + verification flip succeed.** Applies on resume path too (PASS_B_ONLY)."
  - "**Stage commit at Step 8.1 stages ONLY chain-scope paths (delta vs `BASELINE_DIRTY` from Step 1.5).** NEVER `git add -A` / `git add .` / blanket-stage. Pre-existing dirty files stay in worktree, untouched."
caller_agent: ship-stage
---

# Ship-stage — DB-backed two-pass chain dispatcher

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md). DB-backed (`ia_master_plans` / `ia_stages` / `ia_tasks`); 2-level Stage > Task hierarchy.

**Related:**
- [`verify-loop`](../verify-loop/SKILL.md) — internal `MAX_ITERATIONS=2` fix loop (no outer retry here).
- [`stage-authoring`](../stage-authoring/SKILL.md) — populates §Plan Digest in DB before this skill runs.
- [`project-spec-implement`](../project-spec-implement/SKILL.md) — Pass A implement subagent.
- [`opus-code-review`](../opus-code-review/SKILL.md) — **NOT chained here**; standalone seam invoked out-of-band via `/code-review {ISSUE_ID}` per Task when the operator wants it.
- Scene wiring contract: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md).
- Verification policy: [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

## Normative — closeout is part of `PASSED`

When Pass B verify-loop succeeds (`verdict: pass`), the chain **must** run **Step 7 inline closeout** in the **same** invocation. Do **not** emit `SHIP_STAGE {STAGE_ID}: PASSED` after verify alone — Step 7 closeout + Step 8 commit + verification flip must succeed first.

`SHIP_STAGE {STAGE_ID}: PASSED` is valid **only** after `stage_closeout_apply` succeeded + stage commit landed + `stage_verification_flip(pass, commit_sha)` recorded.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | User prompt | Master-plan slug (e.g. `blip`, `citystats-overhaul`). Validated via `master_plan_state(slug)`. |
| `STAGE_ID` | User prompt | Stage identifier as in master plan. Accept `Stage 1.1`, `1.1`, `Stage 1`, `1`. Strip `Stage ` prefix when calling DB MCP. |
| `--no-resume` | Optional flag | Disables Step 4 resume gate; every task with status≠done gets fresh Pass A. Use for forensic replay or worktree reset recovery. |

**Dispatch-shape agnostic:** identical behavior whether invoked as Task-dispatched subagent or inline by orchestrator.

---

## Step 0 — Parse stage

1. Parse `SLUG` + `STAGE_ID` from `$ARGUMENTS`. Missing either → STOPPED + usage.
2. Normalize `STAGE_ID_DB` = `STAGE_ID` minus optional `Stage ` prefix (e.g. `Stage 1.1` → `1.1`).
3. Store `SESSION_ID` = `ship-stage-{SLUG}-{STAGE_ID_DB}-{ISO8601_compact}` for `journal_append` calls.

---

## Step 1 — Stage state load

Call `stage_bundle(slug=SLUG, stage_id=STAGE_ID_DB)`. Returns:
- `master_plan_title`
- `stage`: `{stage_id, title, status, ...}`
- `tasks`: array of `{task_id, title, status, ...}` in stage order
- `status_counts`
- `next_pending`
- `latest_verification`

**Stale-DB guard:** if call returns `not_found` for the slug → STOPPED + `Next: claude-personal "/master-plan-new ..."` (slug not in DB; author master plan first). If slug exists but stage missing → STOPPED + `Next: claude-personal "/stage-file {SLUG} {STAGE_ID}"` (stage not yet filed in DB).

**Branches:**
- `stage.status == "done"` AND all `tasks.status ∈ {done, archived}` → idle exit `SHIP_STAGE {STAGE_ID}: all tasks already done. No work needed.` + Step 10 next-stage resolver.
- Else → continue.

Define:
- `PENDING_TASKS` = tasks with `status ∈ {pending, implemented}` (drives Pass A + Pass B).
- `STAGE_TASK_IDS` = all task ids in `tasks` (full Stage scope for closeout).

---

## Step 1.5 — Baseline worktree snapshot (chain-scope guard)

**Purpose:** capture pre-existing dirty paths so Step 8.1 commits ONLY files mutated during this chain. Prevents sweeping unrelated work streams (sibling master plans, in-flight refactors, untracked artifacts) into the stage commit.

Run `git status --porcelain` once at chain entry. Parse each line as `{XY} {path}` where `XY` = 2-char status flags (e.g. ` M`, `??`, `MM`, `A `). Build set:

```
BASELINE_DIRTY = { "{XY}{NUL}{path}" } for every line in `git status --porcelain` output
```

Store as chain-scope variable. Read-only — never mutated after Step 1.5.

**Special handling for `??` (untracked):** include in BASELINE_DIRTY verbatim. If the chain later modifies an untracked file (e.g. adds it via implement), the path will still match `BASELINE_DIRTY` → excluded from stage commit. This is correct — untracked files predating the chain are NOT chain-owned.

Journal:
```
journal_append({ phase: "chain.baseline", payload_kind: "baseline_snapshot", payload: { entries: BASELINE_DIRTY.size } })
```

---

## Step 2 — Context load

Run [`domain-context-load`](../domain-context-load/SKILL.md) once for the stage domain:

```
keywords: derive from master_plan_title + stage.title (English)
tooling_only_flag: <auto-detect per heuristic below; default false>
context_label: "{SLUG} {STAGE_ID_DB}"
```

**`tooling_only_flag` heuristic:** flip to `true` when `SLUG` matches `/mcp-lifecycle-tools|ia-infrastructure|tooling|bridge-environment|backlog-yaml-mcp|ia-dev-db/` OR stage touches only `tools/**`, `ia/**`, `.claude/**`, `docs/**`, `web/**` (no `Assets/**/*.cs`).

Store payload `{glossary_anchors, router_domains, spec_sections, invariants}` as `CHAIN_CONTEXT`. Pass to per-task `spec-implementer` work.

---

## Step 3 — §Plan Digest readiness gate

For each task in `PENDING_TASKS`:
- Call `task_spec_section(task_id, "§Plan Digest")` (literal `§` prefix — see [`plan-digest-contract.md` §Section heading literal](../../rules/plan-digest-contract.md)).
- Treat as **digested** when section exists AND content is non-empty AND no line matches `_pending` case-insensitively AND sub-headings `§Goal` + `§Acceptance` + (`§Work Items` OR `§Mechanical Steps`) present. Both digest shapes (relaxed §Work Items / legacy §Mechanical Steps) are valid; implementer detects shape per `ia/skills/project-spec-implement/SKILL.md` step 3.

**If ALL digested:** continue to Step 4.

**If ANY missing:** STOPPED. Emit:
```
SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}
Next: claude-personal "/stage-authoring {SLUG} Stage {STAGE_ID_DB}"
```

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

**Worktree state guard (baseline-delta):** when entering PASS_B_ONLY, compute current dirty set:

```
CURRENT_DIRTY = { "{XY}{NUL}{path}" } for every line in `git status --porcelain` output
STAGE_TOUCHED = paths where (XY+path tuple) ∈ CURRENT_DIRTY AND (XY+path tuple) ∉ BASELINE_DIRTY
```

Also fold in **state changes** to baseline paths: if a path appeared in BASELINE_DIRTY with flags `XY₀` and now appears with flags `XY₁ ≠ XY₀` (e.g. baseline ` M`, current `MM` — staged additional changes), include in `STAGE_TOUCHED` (the chain mutated it further).

If `STAGE_TOUCHED` is empty → STOPPED `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean (no chain-scope changes vs baseline). DB says implemented but disk has nothing for this chain. Manual repair: re-run Pass A or task_status_flip back to pending.` Otherwise continue.

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

> Mission: Execute §Plan Digest for `{CURRENT_TASK_ID}` end-to-end. Read §Plan Digest via `task_spec_section(task_id, "§Plan Digest")`. Detect shape per `ia/skills/project-spec-implement/SKILL.md` step 3 — relaxed (`§Work Items` present) → locate anchors against HEAD + apply minimal diffs + run single `validator_gate` from `§Invariants & Gate`. Legacy (`§Mechanical Steps` present) → apply each Edit tuple verbatim with per-step gates. Pre-loaded context: {CHAIN_CONTEXT}. **Do NOT commit.** End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → STOPPED + partial chain digest + `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID_DB}"` (re-enter after fix; resume gate picks up where loop stopped).

### Step 5.2 — Compile gate + scene-wiring preflight

Run `npm run unity:compile-check` (~15 s).

**Scene-wiring preflight:** if §Plan Digest carries a Scene Wiring entry (legacy: dedicated mechanical step; relaxed: §Work Items row prefixed `(Scene Wiring)`), confirm worktree diff includes an edit to `Assets/Scenes/*.unity` OR adds a prefab under `Assets/Prefabs/**`. Use `git diff --name-only` (no commit yet — diff is unstaged worktree changes). Missing wiring under fired trigger → STOPPED:

```
STOPPED at {CURRENT_TASK_ID} — scene_wiring: §Plan Digest Scene Wiring step fired but no Assets/Scenes/*.unity edit in worktree
```

**Compile failure:** STOPPED:

```
STOPPED at {CURRENT_TASK_ID} — compile_gate: {reason}
```

Partial chain digest. `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID_DB}"` after fix.

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

**Order is fixed:** 6.1 verify → 6.3 status flip done. **Code-review is NOT part of this chain** — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band.

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

DB is sole source of truth for task spec bodies — no filesystem mv.

Journal: `phase: "closeout.apply", payload_kind: "closeout_result", payload: { archived_task_count }`.

---

## Step 8 — Stage commit + verification record

### Step 8.1 — Stage commit (single, covers chain-scope diff only)

Stage worktree state at this point:
- All Pass A implementation changes (uncommitted — never committed in Pass A).
- DB-only closeout (Step 7) leaves no filesystem diff — only `ia_*` tables mutated.
- May ALSO carry pre-existing dirty paths from sibling work streams (captured in `BASELINE_DIRTY` at Step 1.5).

**Chain-scope delta computation (mandatory):**

```
CURRENT_DIRTY = { "{XY}{NUL}{path}" } for every line in `git status --porcelain` output
STAGE_TOUCHED_PATHS = sorted unique set of paths where:
  (a) path appears in CURRENT_DIRTY with flags XY₁ AND
      (path was absent from BASELINE_DIRTY OR appeared with different flags XY₀ ≠ XY₁)
```

`STAGE_TOUCHED_PATHS` is the ONLY set staged for this commit. Paths in BASELINE_DIRTY whose flags are unchanged stay in the worktree, untouched by this commit.

**Resume note:** if `STAGE_TOUCHED_PATHS` is empty (PASS_B_ONLY where all Pass A diffs already committed in a prior run AND no new edits this run), skip the commit + reuse the latest existing stage commit sha (`git rev-parse HEAD`). Otherwise create a fresh commit per below.

Single commit covers everything chain-scoped. Format:

```
feat({SLUG}-stage-{STAGE_ID_DB}): {short summary from master_plan_title or Stage title}

Stage {STAGE_ID_DB} — {N} tasks: {comma-separated STAGE_TASK_IDS}

Pass A: implement + compile (all tasks)
Pass B: verify-loop pass
Closeout: {archived_task_count} tasks archived; ia_stages.status=done
```

Stage worktree (delta-only staging):

1. **Stage chain-scope paths only:**
   ```
   git add -- <STAGE_TOUCHED_PATHS>
   ```
   Pass paths as explicit arguments; never use `git add -A`, `git add .`, or any blanket-stage form. If `STAGE_TOUCHED_PATHS` is large, batch via repeated `git add --` calls (no shell expansion glob).
2. **Verify staged scope** — run `git diff --cached --name-only` and assert the result equals `STAGE_TOUCHED_PATHS` (no overlap with BASELINE_DIRTY-unchanged paths). Mismatch → STOPPED `SHIP_STAGE {STAGE_ID}: STOPPED at commit — staged scope drift: expected {N}, got {M}. Refusing contamination.` (do NOT commit; human investigates).
3. **Create commit:** `git commit -m "$(cat <<'EOF' ... EOF)"` (HEREDOC to preserve formatting).
4. Capture commit sha: `STAGE_COMMIT_SHA=$(git rev-parse HEAD)`.

**Hook failure:** if commit fails (pre-commit hook red), do NOT amend or retry blindly. Investigate, fix, re-stage chain-scope paths only, create NEW commit. Capture new sha.

**Rationale:** ship-stage may run on a worktree that already carries unrelated dirty changes (sibling master plans in flight, in-progress refactors, manual edits, untracked artifacts). Blanket-staging via `git add -A` would sweep all of those into the stage commit, contaminating its scope. Chain-scope delta vs `BASELINE_DIRTY` keeps the commit honest: it lands ONLY what this chain produced.

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
    "command": "/ship-stage|/stage-file|/stage-decompose|none",
    "args": "{SLUG} Stage X.Y",
    "shell": "claude-personal \"...\""
  }
}
```

Caveman summary follows JSON: tasks shipped, stage commit sha (short), verify outcome, next step.

---

## Step 10 — Next-stage resolver

Re-call `master_plan_state(slug=SLUG)`. Sort stages by **numeric tuple `(major, minor)`** parsed from `stage_id` (e.g. `8.1` → `(8,1)`, `19.2` → `(19,2)`). Iterate forward starting from the first stage with `(major, minor) > current STAGE_ID_DB`. Pick the **first** stage matching one of the 4 cases below — do NOT skip skeletons or pending stages to grab a later filed stage. `stage-decompose` + `stage-file` are part of the standard incremental flow.

**4 cases (sequential — first stage in numeric order wins, regardless of case):**

1. **Filed stage** — stage with ≥1 task `status ∈ {pending, implemented}` (real ids, not `_pending_`):
   → `Next: claude-personal "/ship-stage {SLUG} Stage X.Y"`

2. **Pending stage** — stage where tasks are `_pending_` placeholders (decomposed but not yet filed in DB):
   → `Next: claude-personal "/stage-file {SLUG} Stage X.Y"`

3. **Skeleton stage** — stage with no tasks at all (Objectives + Exit only, decomposition deferred):
   → `Next: claude-personal "/stage-decompose {SLUG} Stage X.Y"`

4. **Umbrella done** — no more stages after current:
   → `All stages done — plan complete (no further action; inline `stage_closeout_apply` already recorded per-stage).`

**Forbidden:** skipping a skeleton or pending stage in favor of a later filed stage. Sequential ordering preserves the user's authored progression.

---

## Exit lines

- **Success:** `SHIP_STAGE {STAGE_ID}: PASSED` + chain digest + `Next:` handoff. Only after Step 7 closeout + Step 8 stage commit + `stage_verification_flip` succeeded.
- **Readiness gate fail:** `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff.
- **Stale-DB stage not found:** `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` + `/stage-file` handoff.
- **PASS_B_ONLY worktree-clean inconsistency:** `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. DB says implemented but disk has nothing.` + manual repair directive.
- **Pass A implement failure:** `STOPPED at {ISSUE_ID} — implement: {reason}` + partial chain digest + `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID_DB}"` after fix.
- **Pass A compile failure:** `STOPPED at {ISSUE_ID} — compile_gate: {reason}` + partial chain digest + same `/ship-stage` re-entry.
- **Pass A scene-wiring failure:** `STOPPED at {ISSUE_ID} — scene_wiring: ...` + same `/ship-stage` re-entry after wiring.
- **Pass B verify failure:** `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` + human review directive. Worktree stays dirty.
- **Closeout failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` + chain digest + DB-drift repair directive.
- **Stage commit failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` + chain digest + repair directive (do NOT amend; investigate hook).

---

## Hard boundaries

- Sequential per-task dispatch only — tasks share files + invariants; no parallel.
- **Pass A NEVER commits.** No `git commit feat({ISSUE_ID})` per task. Single stage commit at Step 8.1.
- Resume gate (Step 4) queries `task_state` / `stage_bundle` — does NOT git-scan for commit subjects.
- Stop on first Pass A gate failure (compile, scene-wiring, implement); do NOT continue to next task.
- Do NOT roll back Pass A status flips on STAGE_VERIFY_FAIL — DB stays at `implemented`; worktree stays dirty; human repairs via re-run after fix.
- **No code-review in this chain.** Verify-loop + validation are the gate. Operator may invoke standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9) before re-running ship-stage; resume path will create a new stage commit if review fixes added new diff.
- Inline closeout (Step 7) is mandatory on green Pass B — Stage closeout always runs inline.
- **Stage commit at Step 8.1 stages ONLY chain-scope paths** (delta vs `BASELINE_DIRTY` snapshot taken at Step 1.5). NEVER use `git add -A` / `git add .` / any blanket-stage form. Pre-existing dirty files (sibling work streams, in-flight refactors, untracked artifacts) stay in the worktree, untouched by the stage commit. Closeout (Step 7) is DB-only — no filesystem diff.
- `domain-context-load` fires ONCE at chain start (Step 2); do NOT re-call per task.
- Do NOT auto-invoke `/stage-authoring` from inside `/ship-stage` — Step 3 is a readiness gate only, hands off if missing.
- DB is sole source of truth for master plans, stages, tasks, and task spec bodies. All reads go through MCP (`master_plan_state`, `stage_bundle`, `task_state`, `task_spec_section`, `task_spec_body`); all writes go through MCP (`task_status_flip`, `stage_closeout_apply`, `master_plan_change_log_append`, `task_commit_record`, `stage_verification_flip`). Do NOT read or edit any `ia/projects/**` markdown.
- DB is source of truth. Do NOT fall back to filesystem-only operations on `db_unavailable` — escalate to human; halt chain.

---

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{SLUG}` | Master-plan slug (e.g. `blip`, `citystats-overhaul`) |
| `{STAGE_ID}` | Stage identifier as user typed (e.g. `Stage 1.1`) |
| `{STAGE_ID_DB}` | `STAGE_ID` minus `Stage ` prefix (DB-canonical, e.g. `1.1`) |
| `{SESSION_ID}` | `ship-stage-{SLUG}-{STAGE_ID_DB}-{ISO8601_compact}` |
| `{CURRENT_TASK_ID}` | Active task id in Step 5 loop |
| `{STAGE_TASK_IDS}` | All task ids in stage (Step 1 result) |
| `{PENDING_TASKS}` | Tasks with `status ∈ {pending, implemented}` (drives Pass A + Pass B scope) |
| `{CHAIN_CONTEXT}` | `domain-context-load` payload `{glossary_anchors, router_domains, spec_sections, invariants}` |
| `{BASELINE_DIRTY}` | Pre-chain `git status --porcelain` snapshot captured at Step 1.5; chain-scope guard for Step 8.1 commit scope |
| `{STAGE_TOUCHED_PATHS}` | Step 8.1 chain-scope delta = `CURRENT_DIRTY - BASELINE_DIRTY`; the ONLY paths staged in the stage commit |
| `{STAGE_COMMIT_SHA}` | Captured `git rev-parse HEAD` after Step 8.1 commit |

---

## Open Questions

- Crash-survivable session journal: `journal_append` writes to `ia_ship_stage_journal` table — survives process crash. Resume on re-invocation reads journal by `session_id` to detect mid-Pass-B state (e.g. verify done but status-flip not). Currently Step 6 re-runs as a unit; finer sub-step resume deferred.

---

## Changelog

| date | change | friction_types |
|------|--------|---------------|
| 2026-04-29 | parallel-carcass Wave 0 Phase 3 PR 3.5 — added stage_claim / stage_claim_release / claim_heartbeat / arch_drift_scan to tools_extra; documented Pass A/B carcass hooks in agent-body (conditional on .parallel-section-claim.json sentinel; legacy linear plans skip all hooks) | feature-extension |
