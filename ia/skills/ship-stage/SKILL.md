---
purpose: "Two-pass chain: Pass 1 = per-Task implement + unity:compile-check fast-fail gate; Pass 2 = Stage-end bulk verify-loop (full Path A+B cumulative delta) + code-review (Stage diff) + audit + closeout. Approach B stateful chain subagent."
audience: agent
loaded_by: skill:ship-stage
slices_via: backlog_issue, router_for_task, spec_section, spec_sections, glossary_discover, glossary_lookup, invariants_summary
name: ship-stage
description: >
  Opus orchestrator. Drives every non-Done filed task row of one Stage X.Y through a
  two-pass chain. Pass 1 (per-Task): implement + unity:compile-check fast-fail gate +
  atomic Task-level commit. Pass 2 (Stage-end bulk): verify-loop full Path A+B on
  cumulative delta + code-review Stage diff (shared amortized context) + audit + closeout.
  --per-task-verify flag preserves pre-TECH-519 N× per-Task verify-loop + code-review shape.
  MCP context loaded once via domain-context-load subskill; cached payload passed
  to per-task inner dispatches. Emits SHIP_STAGE {STAGE_ID}: PASSED or STOPPED.
  Triggers: "/ship-stage", "ship stage", "chain stage tasks", "ship all stage tasks".
phases:
  - "Parse stage"
  - "Context load"
  - "Pass 1 per-Task"
  - "Pass 2 Stage-end"
  - "Chain digest"
  - "Next-stage resolver"
---

# Ship-stage — chain dispatcher skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Related:** [`ship.md`](../../../.claude/commands/ship.md) (single-task) · [`verify-loop`](../verify-loop/SKILL.md) (`--skip-path-b` flag) · [`domain-context-load`](../domain-context-load/SKILL.md) (MCP cache subskill) · [`project-stage-close`](../project-stage-close/SKILL.md) (per-spec stage close — fires inside each inner spec-implementer unchanged) · [`project-spec-close`](../project-spec-close/SKILL.md) (umbrella close, per task).

**Verification policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `MASTER_PLAN_PATH` | User prompt | Repo-relative path to `*-master-plan.md` (e.g. `ia/projects/citystats-overhaul-master-plan.md`). |
| `STAGE_ID` | User prompt | Stage identifier as it appears in the master plan header (e.g. `Stage 1.1`). |
| `--per-task-verify` | Optional flag | **Rollback / legacy flag.** When set: Pass 2 verify-loop + code-review are SKIPPED; Pass 1 is promoted to full `verify-loop --skip-path-b` + `code-review` per Task (pre-TECH-519 shape). Audit + closeout remain Stage-scoped N=1 regardless. Use as safety valve for Stages too large for bulk Pass 2 review (e.g. N≥5, wide surface). |

**Dispatch-shape agnostic:** identical behavior whether this skill is invoked as a Task-dispatched subagent (fresh context) or inline by an orchestrator (inherited context). Do not introduce subagent-only assumptions.

---

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

---

## Step 0 — Parse stage task table

**Algorithm (narrow regex, fails loud on schema drift):**

1. Read `{MASTER_PLAN_PATH}`.
2. Locate stage header: scan for a heading line matching `#### {STAGE_ID}` (any number of leading `#` followed by a space, then `{STAGE_ID}`). Accept `## Stage X.Y`, `### Stage X.Y`, `#### Stage X.Y` to be header-depth agnostic. Regex: `/^#{2,6}\s+Stage\s+X\.Y\b/` where X.Y comes from `STAGE_ID`.
3. Collect lines between that heading and the next heading of equal or lower depth.
4. Locate task table: find a Markdown table with header row containing columns `Issue` and `Status` (case-insensitive, any column order). Regex: `/\|\s*Issue\s*\|/i` on the header row.
5. **Schema drift guard:** only `Issue` + `Status` are required columns. Canonical master-plan schema is the 6-column superset `Task | Name | Phase | Issue | Status | Intent` — all other columns are advisory. If `Issue` OR `Status` column not found within the stage block → emit `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + diff showing required columns `[Issue, Status]` (canonical superset `[Task, Name, Phase, Issue, Status, Intent]`) vs found column headers. Stop.
6. Extract rows: for each data row, parse `Issue` column (must match `/\*\*?(TECH|BUG|FEAT|ART|AUDIO)-\d+\*\*?/` or bare id) and `Status` column.
7. Filter: keep rows where `Status` is NOT `Done` / `archived` / `skipped` (case-insensitive). These are the **pending tasks**.
8. If zero pending tasks → emit `SHIP_STAGE {STAGE_ID}: all tasks already Done. No work needed.` + next-stage resolver (Step 5).

**Parser fixtures (verify at authoring, not runtime):**

- `citystats-overhaul-master-plan.md` Stage 1.1 — `####` depth, 6-col schema `Task | Name | Phase | Issue | Status | Intent`.
- `multi-scale-master-plan.md` Stage 1.1 — `####` depth, same 6-col schema.
- `backlog-yaml-mcp-alignment-master-plan.md` Stage 1.1 — `####` depth, same schema.
- `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` Stage 1.1 — `####` depth, same schema. Two tasks TECH-314 + TECH-315 (first `/ship-stage` production run, 2026-04-18).

All current master plans use `####` headers and the 6-col schema. Parser accepts `##`–`######` to be forward-compatible; only `Issue` + `Status` columns are required, other columns ignored.

---

## Step 1 — Context load (once per chain)

Run [`domain-context-load`](../domain-context-load/SKILL.md) subskill once for the stage domain:

```
keywords: derive from master plan title + stage objectives (English)
tooling_only_flag: <auto-detect per heuristic below; default false>
context_label: "{MASTER_PLAN_PATH} {STAGE_ID}"
```

**`tooling_only_flag` auto-detect heuristic (pre-context-load):**

Flip to `true` (skips `invariants_summary` — runtime-C# invariants irrelevant for tooling stages) when ANY of these hold:

- `MASTER_PLAN_PATH` matches `/mcp-lifecycle-tools|ia-infrastructure|tooling|bridge-environment|backlog-yaml-mcp/`.
- Master plan H1 contains bracket label `(IA Infrastructure)`, `(MCP)`, or `(Tooling)`.
- Stage block under `{STAGE_ID}` touches only `tools/mcp-ia-server/**`, `tools/scripts/**`, `ia/**`, `.claude/**`, `docs/**` (no `Assets/**/*.cs`).

Otherwise keep `false` (most runtime stages touch Unity C# and need invariants). Manual override via explicit prompt param still wins.

Store returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` as `CHAIN_CONTEXT`. Pass to each per-task inner dispatch so kickoff / implementer / verify-loop don't re-query.

---

## Step 2 — Pass 1: per-Task loop (sequential, fail-fast)

For each pending task row in order (index `i`, total `N`):

```
CURRENT_TASK = task_rows[i]
ISSUE_ID = CURRENT_TASK.issue_id
```

### Step 2.1 — Implement

Dispatch `spec-implementer` subagent (Sonnet):

> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §7 Implementation Plan end-to-end, phase by phase. Pre-loaded context: {CHAIN_CONTEXT}. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → stop, emit STOPPED line + partial chain digest.

### Step 2.2 — Compile gate

Run `npm run unity:compile-check` (Path: repo root, ~15 s). Non-zero exit = compile failure.

**On failure:** emit:

```
STOPPED at {ISSUE_ID} — compile_gate: {reason}
```

Then emit partial chain digest (Step 4 shape with `tasks_stopped_at: "{ISSUE_ID}"`) listing:
- `tasks_completed`: issue ids of Tasks that passed Pass 1 before this Task.
- `uncommitted_tail`: this Task (implement done; commit NOT made — stop before commit on compile failure).
- `unstarted`: remaining Task ids after this Task.

Halt chain. `Next: claude-personal "/ship {ISSUE_ID}"` after user fixes compile error.

**On success:** continue.

### Step 2.3 — Atomic Task-level commit

Commit all changes for this Task as a single atomic commit. Message format:

```
feat({ISSUE_ID}): {short description from spec §1}

Pass 1 compile gate: passed
```

This commit is the bisection anchor for the Task.

### Step 2.4 — Per-Task verify-loop + code-review (--per-task-verify flag only)

**SKIP this step UNLESS `--per-task-verify` flag is set.** When flag set, run the legacy per-Task shape:

Dispatch `verify-loop` subagent (Sonnet) with `--skip-path-b` flag:

> Mission: Run verify-loop for {ISSUE_ID} with `--skip-path-b`. Path A compile gate runs; Path B skipped. JSON verdict `path_b: skipped_batched`. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict` must be `"pass"`. Failure → stop, emit STOPPED digest.

Then dispatch `opus-code-reviewer` subagent (Opus):

> Mission: Run opus-code-review for {ISSUE_ID}. STAGE_MCP_BUNDLE: {CHAIN_CONTEXT}. Emit verdict (PASS / minor / critical). On critical: write §Code Fix Plan; return `{verdict: "critical"}`.

Verdict `critical` → emit STOPPED digest (code-review gate); verdict `PASS` / `minor` → continue.

### Step 2.5 — Journal accumulation (Pass 1 entry)

After successful Step 2.2 (or Step 2.4 when flag set), append to `CHAIN_JOURNAL`:

```json
{
  "task_id": "{ISSUE_ID}",
  "pass1_compile_gate": "passed",
  "lessons": [],
  "decisions": [],
  "verify_iterations": 0
}
```

Lessons + decisions updated from closeout digest in Step 3.5 below.

### Step 2.6 — Re-read master plan

Re-read `{MASTER_PLAN_PATH}` to confirm task row status after commit. Continue to next task.

---

## Step 3 — Pass 2: Stage-end bulk (runs ONCE after all Tasks pass Pass 1)

**SKIP Pass 2 verify-loop + code-review when `--per-task-verify` flag is set.** Jump directly to Step 3.4 (audit).

### Step 3.1 — Verify-loop on cumulative Stage delta

Run `verify-loop` (full Path A+B, no `--skip-path-b`) on cumulative Stage delta:

**Cumulative delta anchor:** `git diff {FIRST_TASK_COMMIT_PARENT}..HEAD` — where `{FIRST_TASK_COMMIT_PARENT}` = the commit SHA immediately before the first Pass 1 Task commit. **EXCLUDE Stage closeout commits** (closeout runs after Pass 2; closeout commits not yet on HEAD at this point — so the anchor is naturally correct).

> Mission: Run full verify-loop (Path A + Path B) on cumulative stage delta. Issue context: last closed {ISSUE_ID} (for backlog context). Changed areas = all files touched across all Pass 1 Task commits. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict` must be `"pass"`.

**STAGE_VERIFY_FAIL handling:** if Pass 2 verify-loop fails:
- All Tasks committed in Pass 1 — no rollback.
- Emit `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` field + `escalation` object mirroring inner verify-loop `gap_reason` taxonomy (see `ia/skills/verify-loop/SKILL.md` § Step 7, `docs/agent-led-verification-policy.md` § Escalation taxonomy).
- `gap_reason` REQUIRED — pick `bridge_kind_missing` over `human_judgment_required` whenever a missing `unity_bridge_command` kind could close the loop.
- No automatic retry.

### Step 3.2 — Code-review on Stage-level diff

Dispatch `opus-code-reviewer` subagent (Opus) with Stage diff + shared context:

> Mission: Run opus-code-review on Stage-level diff (cumulative delta: same anchor as Step 3.1). STAGE_MCP_BUNDLE: {CHAIN_CONTEXT} — shared spec/invariant/glossary context cached from Phase 1 (do NOT re-query domain-context-load). All N §Plan Author sections from `{MASTER_PLAN_PATH}` are the acceptance reference. Emit verdict (PASS / minor / critical). On critical: write §Code Fix Plan tuples targeting the appropriate spec files.

**Verdict PASS / minor:** continue to Step 3.4.

**Verdict critical (first time):**
1. Run `code-fix-apply` Sonnet on `§Code Fix Plan` tuples.
2. Re-enter Step 3.1 verify-loop (one re-entry — cap = 1).
3. Run Step 3.2 code-review again.
4. Second critical verdict → exit `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest. Halt. Human review required.

### Step 3.3 — (Reserved)

No additional step between code-review and audit.

### Step 3.4 — Audit

Dispatch `opus-auditor` subagent (Opus) — Stage-scoped (unchanged):

> Mission: Run opus-audit Stage 1×N for Stage {STAGE_ID}. Issue ids: {all Task ids in Stage}. STAGE_MCP_BUNDLE: {CHAIN_CONTEXT}. Return audit report.

### Step 3.5 — Closeout

Dispatch `stage-closeout-planner` → `stage-closeout-applier` pair (Opus → Sonnet) — Stage-scoped (unchanged):

> Mission: Run Stage-scoped closeout for Stage {STAGE_ID} in {MASTER_PLAN_PATH}. All Task rows. Migrate lessons → delete specs → archive BACKLOG rows. No confirmation gate. Return full `project_spec_closeout_digest` JSON payload (including `lessons_migrated[]` and `decisions[]` per task) so chain journal can aggregate.

**Gate:** closeout digest JSON `validate_dead_specs_post.exit_code` == 0. Failure → STOPPED digest.

After closeout, update `CHAIN_JOURNAL` entries with lessons + decisions from closeout digest.

---

---

## Step 4 — Chain-level stage digest

Emit one chain-level stage digest at chain end (success or STAGE_VERIFY_FAIL). Distinct from per-spec `project-stage-close` which already fired inside each `spec-implementer`.

**Format:** mirrors `.claude/output-styles/closeout-digest.md` (JSON header + caveman summary) with additional `chain:` block.

```json
{
  "chain_stage_digest": true,
  "master_plan": "{MASTER_PLAN_PATH}",
  "stage_id": "{STAGE_ID}",
  "tasks_shipped": ["TECH-xxx", "TECH-yyy"],
  "tasks_stopped_at": null,
  "stage_verify": "passed|failed|skipped",
  "next_handoff": {
    "case": "filed|pending|skeleton|umbrella-done",
    "command": "/ship-stage|/stage-file|/stage-decompose|/closeout",
    "args": "ia/projects/{slug}-master-plan.md Stage X.Y",
    "shell": "claude-personal \"/ship-stage ia/projects/{slug}-master-plan.md Stage X.Y\""
  },
  "chain": {
    "tasks": [
      {
        "task_id": "TECH-xxx",
        "lessons": ["lesson1"],
        "decisions": ["decision1"],
        "verify_iterations": 0
      }
    ],
    "aggregate_lessons": ["..."],
    "aggregate_decisions": ["..."],
    "verify_iterations_total": 0
  }
}
```

`next_handoff.case` mirrors Step 5 resolver cases exactly — downstream drivers (`release-rollout`, dashboards) pick up the structured field without re-parsing caveman prose. On STOPPED / STAGE_VERIFY_FAIL, `next_handoff.case` is `"stopped"` or `"stage_verify_fail"` respectively and `command` / `args` reference the fix path (`/ship {ISSUE_ID}` or human-review directive).

Caveman summary follows JSON: tasks shipped, any stopped/failed, stage-level verify outcome, aggregate lesson count, next step.

---

## Step 5 — Next-stage resolver

Re-read `{MASTER_PLAN_PATH}` post-close. Scan for next stage after `{STAGE_ID}`:

**4 cases (in priority order):**

1. **Next filed stage** — next `####` Stage heading where task table has ≥1 row with `Status != Done/archived/skipped` AND issue ids are real (not `_pending_`):
   → `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage X.Y"`

2. **Next pending stage** — next `####` Stage heading where task table rows have `_pending_` issue ids (tasks not yet filed):
   → `Next: claude-personal "/stage-file {MASTER_PLAN_PATH} Stage X.Y"`

3. **Next skeleton step** — next Step section with no filed stages beneath it (fully unpopulated):
   → `Next: claude-personal "/stage-decompose {MASTER_PLAN_PATH} Step N"`

4. **Umbrella done** — no more stages/steps in any state:
   → `Next: claude-personal "/closeout {UMBRELLA_ISSUE_ID}"` (if identifiable from master plan header) OR print `All stages done — umbrella close pending.`

---

## Exit lines

- **Success:** `SHIP_STAGE {STAGE_ID}: PASSED` + chain digest + `Next:` handoff.
- **Pass 1 compile failure:** `STOPPED at {ISSUE_ID} — compile_gate: {reason}` + partial chain digest (tasks-completed + uncommitted tail + unstarted list) + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.
- **Pass 1 implement failure (--per-task-verify only):** `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — implement: {reason}` + partial chain digest + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.
- **Pass 2 verify failure:** `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` + human review directive.
- **Pass 2 code-review critical twice:** `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest + human review required (structural issue).
- **Parser error:** `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + expected-vs-found column diff.

---

## Hard boundaries

- Sequential task dispatch only — tasks share files + invariants; no parallel.
- Stop on first Pass 1 gate failure (compile or implement); do NOT continue to next task.
- Do NOT rollback committed Pass 1 Tasks on STAGE_VERIFY_FAIL or STAGE_CODE_REVIEW_CRITICAL_TWICE — commits already landed; emit digest + human directive only.
- `STAGE_CODE_REVIEW_CRITICAL` re-entry cap = 1 — second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`; do NOT re-enter a third time.
- Pass 2 cumulative delta anchor: first Task-commit parent → Stage-end HEAD, EXCLUDING closeout commits.
- Stage-scoped closeout (`stage-closeout-plan` → `stage-closeout-apply` pair) fires ONCE after Pass 2 completes — do NOT call per Task; do NOT inhibit.
- Chain-level stage digest is a NEW scope distinct from stage-closeout-apply's per-task digest aggregation.
- `domain-context-load` fires ONCE at chain start (Step 1); do NOT re-call per task.
- Do NOT exceed `/ship` single-task dispatch shape for inner stages — each dispatches the canonical subagent.

---

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{MASTER_PLAN_PATH}` | Repo-relative path to master plan (e.g. `ia/projects/citystats-overhaul-master-plan.md`) |
| `{STAGE_ID}` | Stage identifier matching master plan header (e.g. `Stage 1.1`) |
| `{ISSUE_ID}` | Active task BACKLOG id (BUG-/FEAT-/TECH-/ART-/AUDIO-) |
| `{CHAIN_CONTEXT}` | `domain-context-load` payload `{glossary_anchors, router_domains, spec_sections, invariants}` |
| `{CHAIN_JOURNAL}` | In-process accumulator list of `{task_id, lessons[], decisions[], verify_iterations}` |

---

## Open Questions

- Crash-survivable `{CHAIN_JOURNAL}` (disk-persisted + resume on re-invocation) — tracked by [TECH-493](../../projects/TECH-493.md). Implementation deferred to that issue's implementer; this skill currently treats `{CHAIN_JOURNAL}` as in-process only.

---

## Changelog

### 2026-04-19 — Subagent bailed "no Task tool in nested context" — premature 50.7k token burn

**Status:** fixed (agent body patched)

**Symptom:**
`/ship-stage ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md Stage 17` launched as subagent, bailed after 3 tool uses + 50.7k tokens + 37s, reporting "Subagent blocked — no Task tool in nested context". Re-dispatch with explicit "inline execution" instruction succeeded (100+ tool uses). Stage 8 production run (F9 entry below) had already proven inline execution works. Inconsistent behavior = misread of `tools:` frontmatter intent.

**Root cause:**
`.claude/agents/ship-stage.md` `tools:` frontmatter intentionally omits `Agent`/`Task` (subagent cannot nest-dispatch). Skill body Steps 2.1–2.4 phrase work as "Dispatch `spec-kickoff` subagent" / "Dispatch `spec-implementer`" etc. Subagent read "Dispatch X" literally, found no Task tool, bailed. SKILL.md §40 "Dispatch-shape agnostic" directive not reinforced in agent body.

Secondary drift: agent body + skill Steps 2.1/2.4 + Hard boundaries still referenced retired surfaces `spec-kickoff` + per-spec `project-stage-close` (M6 collapse folded both into `/author` Stage 1×N + Stage-scoped `/closeout` pair).

**Fix:**
Added explicit "Execution model (CRITICAL)" section to `.claude/agents/ship-stage.md` stating: subagent runs ALL phase work inline using native tools; "Dispatch X subagent" phrasing in SKILL.md is shorthand for "execute the work that subagent would do"; do NOT bail on missing Task tool. Updated retired-surface refs (`spec-kickoff` → `/author`; `project-stage-close` → Stage-scoped `/closeout`). Added hard boundary: "Do NOT bail with 'no Task tool in nested context'."

Deeper rewrite of skill Steps 2.1–2.4 + Step 3 to canonical rev-3 lifecycle surfaces (author → plan-review → per-task implement/verify/code-review → audit → closeout) deferred — current shorthand-with-translation-directive unblocks the immediate 50.7k token regression.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Self-referential dry-run scope diverged from T8.1 intent (F7 finding)

**Status:** pending (deferred — T8.1b external-plan re-run row 9)

**Symptom:**
T8.1 verbatim asked for "small _pending_ Task from any open master plan". M8 dry-run actually exercised Stage 8 of `lifecycle-refactor-master-plan.md` itself (filed TECH-485..488 into the plan-under-refactor). Self-referential. Stress-test broader (5 surfaces vs 3) but no isolation from refactor-churn — F4 sampling bias amplified.

**Root cause:**
Process gap, not skill code bug — T8.1 dispatch did not enforce external-plan target.

**Fix:**
pending — re-run T8.1b against external open master plan with _pending_ Task for steady-state yield sample. Locks F4/F5 re-measurement.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Clean end-to-end Stage chain ship (F9 positive signal)

**Status:** observed (no fix required — validates rev-3 collapse)

**Symptom:**
Single `/ship-stage ia/projects/lifecycle-refactor-master-plan.md 8` invocation: 68 tool uses, ~103.1k tokens, 8m 37s wall. 4 tasks (TECH-485–488) shipped through author → implement → verify-loop → code-review → audit → closeout. Stage verify passed (`validate:all` + `unity:compile-check` + `db:bridge-preflight`). All yamls archived; project specs deleted. M7 flipped `done` in migration JSON. No pair-contract escalations.

**Root cause:**
Positive signal — rev-3 Stage-scoped chain works end-to-end. Validates lifecycle-refactor M6 collapse (Stage-scoped bulk pair shape for author/audit/closeout).

**Fix:**
none required.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Migration-JSON polling via ad-hoc python3 awkward (F11 finding)

**Status:** pending (deferred — Fix #11 optional)

**Symptom:**
M8 dry-run agent ran 4 trial-and-error `python3 -c "...json..."` Bash calls to inspect `ia/state/lifecycle-refactor-migration.json` phases section before yielding usable output.

**Root cause:**
No typed surface for migration-JSON status query. Agent fell back to ad-hoc python.

**Fix:**
pending — candidate MCP tool `lifecycle_migration_status {phase?}` returning `{phase_id, status, flipped_at, notes}` OR documented `jq '.phases | to_entries | map(...)'` pattern in `ship-stage` SKILL §evidence gathering. Low priority.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — STAGE_ID argument syntax drift (F12 finding)

**Status:** pending (deferred — Fix #10)

**Symptom:**
Original user invocation: `/ship-stage ia/projects/... 8` (bare numeric). Agent suggestion drifted to `/ship-stage ia/projects/... Stage 9` (word + number). `/ship-stage` subagent description = `{MASTER_PLAN_PATH} {STAGE_ID}` — `STAGE_ID` format spec ambiguous (`8` vs `8.1` vs `Stage 8` vs `Stage 8.1`).

**Root cause:**
`STAGE_ID` accepted-format spec underdefined; subagent suggestion prose drifted across surfaces.

**Fix:**
pending — lock `STAGE_ID` format in `.claude/agents/ship-stage.md` frontmatter description (pick one canonical form; reject ambiguous); align all subagent suggestion prose. Low priority.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
