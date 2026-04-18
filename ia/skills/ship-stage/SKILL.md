---
purpose: "Chain kickoff → implement → verify-loop → closeout across every non-Done filed task row of one Stage X.Y in a master plan, end-to-end and autonomously. Approach B stateful chain subagent."
audience: agent
loaded_by: skill:ship-stage
slices_via: backlog_issue, router_for_task, spec_section, spec_sections, glossary_discover, glossary_lookup, invariants_summary
name: ship-stage
description: >
  Opus orchestrator. Drives every non-Done filed task row of one Stage X.Y through
  spec-kickoff → spec-implementer → verify-loop (--skip-path-b) → closeout,
  then runs one batched Path B at stage end and emits a chain-level stage digest.
  MCP context loaded once via domain-context-load subskill; cached payload passed
  to per-task inner dispatches. Emits SHIP_STAGE {STAGE_ID}: PASSED or STOPPED.
  Triggers: "/ship-stage", "ship stage", "chain stage tasks", "ship all stage tasks".
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

**Dispatch-shape agnostic:** identical behavior whether this skill is invoked as a Task-dispatched subagent (fresh context) or inline by an orchestrator (inherited context). Do not introduce subagent-only assumptions.

---

## Phase 0 — Parse stage task table

**Algorithm (narrow regex, fails loud on schema drift):**

1. Read `{MASTER_PLAN_PATH}`.
2. Locate stage header: scan for a heading line matching `#### {STAGE_ID}` (any number of leading `#` followed by a space, then `{STAGE_ID}`). Accept `## Stage X.Y`, `### Stage X.Y`, `#### Stage X.Y` to be header-depth agnostic. Regex: `/^#{2,6}\s+Stage\s+X\.Y\b/` where X.Y comes from `STAGE_ID`.
3. Collect lines between that heading and the next heading of equal or lower depth.
4. Locate task table: find a Markdown table with header row containing columns `Issue` and `Status` (case-insensitive, any column order). Regex: `/\|\s*Issue\s*\|/i` on the header row.
5. **Schema drift guard:** only `Issue` + `Status` are required columns. Canonical master-plan schema is the 6-column superset `Task | Name | Phase | Issue | Status | Intent` — all other columns are advisory. If `Issue` OR `Status` column not found within the stage block → emit `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + diff showing required columns `[Issue, Status]` (canonical superset `[Task, Name, Phase, Issue, Status, Intent]`) vs found column headers. Stop.
6. Extract rows: for each data row, parse `Issue` column (must match `/\*\*?(TECH|BUG|FEAT|ART|AUDIO)-\d+\*\*?/` or bare id) and `Status` column.
7. Filter: keep rows where `Status` is NOT `Done` / `archived` / `skipped` (case-insensitive). These are the **pending tasks**.
8. If zero pending tasks → emit `SHIP_STAGE {STAGE_ID}: all tasks already Done. No work needed.` + next-stage resolver (Phase 5).

**Parser fixtures (verify at authoring, not runtime):**

- `citystats-overhaul-master-plan.md` Stage 1.1 — `####` depth, 6-col schema `Task | Name | Phase | Issue | Status | Intent`.
- `multi-scale-master-plan.md` Stage 1.1 — `####` depth, same 6-col schema.
- `backlog-yaml-mcp-alignment-master-plan.md` Stage 1.1 — `####` depth, same schema.
- `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` Stage 1.1 — `####` depth, same schema. Two tasks TECH-314 + TECH-315 (first `/ship-stage` production run, 2026-04-18).

All current master plans use `####` headers and the 6-col schema. Parser accepts `##`–`######` to be forward-compatible; only `Issue` + `Status` columns are required, other columns ignored.

---

## Phase 1 — Context load (once per chain)

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

## Phase 2 — Task loop (sequential, fail-fast)

For each pending task row in order (index `i`, total `N`):

```
CURRENT_TASK = task_rows[i]
ISSUE_ID = CURRENT_TASK.issue_id
```

### Step 2.1 — Kickoff

Dispatch `spec-kickoff` subagent (Opus):

> Mission: Run `project-spec-kickoff` on `ia/projects/{ISSUE_ID}*.md`. Resolve filename via Glob. Pre-loaded context: {CHAIN_CONTEXT} — skip MCP glossary/router/invariants calls where context already covers. End with `KICKOFF_DONE`.

**Gate:** subagent output must contain `KICKOFF_DONE`. Failure → stop, emit:

```
SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — kickoff: {reason}
```

Then emit chain digest (Phase 4) for tasks already closed + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.

### Step 2.2 — Implement

Dispatch `spec-implementer` subagent (Sonnet):

> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §7 Implementation Plan end-to-end, phase by phase. Pre-loaded context: {CHAIN_CONTEXT}. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → stop, same STOPPED digest pattern.

### Step 2.3 — Verify-loop (--skip-path-b)

Dispatch `verify-loop` subagent (Sonnet) with `--skip-path-b` flag:

> Mission: Run verify-loop for {ISSUE_ID} with `--skip-path-b`. Path A compile gate runs; Path B skipped (batched at stage end). JSON verdict `path_b: skipped_batched`. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict` must be `"pass"`. `"fail"` or `"escalated"` → stop, emit STOPPED digest.

### Step 2.4 — Closeout

Dispatch `closeout` subagent (Opus):

> Mission: Run `project-spec-close` on verified issue {ISSUE_ID}. Migrate lessons → delete spec → archive BACKLOG row → purge id. No confirmation gate — execute all ops. Return full `project_spec_closeout_digest` JSON payload (including `lessons_migrated[]` and `decisions[]`) so chain journal can aggregate.

**Gate:** closeout digest JSON `spec_deleted.ok` must be `true` + `validate_dead_specs_post.exit_code` == 0. Failure → STOPPED digest.

### Step 2.5 — Journal accumulation

After successful closeout, append to `CHAIN_JOURNAL`:

```json
{
  "task_id": "{ISSUE_ID}",
  "lessons": ["..."],
  "decisions": ["..."],
  "verify_iterations": 0
}
```

Source of truth (ordering-safe — spec file is already deleted by Step 2.4):

- `lessons[]` ← closeout digest `lessons_migrated[]` (post-migration canonical form, not the pre-delete spec §10 text).
- `decisions[]` ← closeout digest `decisions[]`.
- `verify_iterations` ← verify-loop JSON verdict `fix_iterations` field from Step 2.3.

Do NOT re-read `ia/projects/{ISSUE_ID}.md` — closeout has already deleted it.

### Step 2.6 — Re-read master plan

After closeout, re-read `{MASTER_PLAN_PATH}` to get latest task status (closeout flips the row). Continue to next task.

---

## Phase 3 — Batched Path B verify (stage end)

After all tasks closed successfully:

Run `verify-loop` subagent (Sonnet) **without** `--skip-path-b` (normal mode) on cumulative delta:

> Mission: Run full verify-loop (Path A + Path B) on cumulative stage delta. Issue context: last closed {ISSUE_ID} (for backlog context). Changed areas = all files touched across the stage.

**STAGE_VERIFY_FAIL handling:** if batched Path B fails:
- All tasks already closed + archived — no rollback.
- Emit `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` field + `escalation` object mirroring the inner verify-loop `gap_reason` taxonomy (see `ia/skills/verify-loop/SKILL.md` § Step 7, `docs/agent-led-verification-policy.md` § Escalation taxonomy).
- `gap_reason` REQUIRED — pick `bridge_kind_missing` over `human_judgment_required` whenever a missing `unity_bridge_command` kind could close the loop; cite `missing_kind` + `tooling_issue_id` (TECH-412 landed the initial 20 mutation kinds — file a new TECH for genuinely missing successors).
- Directive wording MUST include the concrete gap — e.g. "STAGE_VERIFY_FAIL: `bridge_kind_missing` — `some_missing_kind` — tracked in TECH-###. Human: one-shot Editor action to unblock while kind lands; do NOT reopen tasks."
- No automatic retry.

---

## Phase 4 — Chain-level stage digest

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

`next_handoff.case` mirrors Phase 5 resolver cases exactly — downstream drivers (`release-rollout`, dashboards) pick up the structured field without re-parsing caveman prose. On STOPPED / STAGE_VERIFY_FAIL, `next_handoff.case` is `"stopped"` or `"stage_verify_fail"` respectively and `command` / `args` reference the fix path (`/ship {ISSUE_ID}` or human-review directive).

Caveman summary follows JSON: tasks shipped, any stopped/failed, stage-level verify outcome, aggregate lesson count, next step.

---

## Phase 5 — Next-stage resolver

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
- **Mid-chain failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}` + partial chain digest (tasks already closed) + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.
- **Stage verify failure:** `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` + human review directive.
- **Parser error:** `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + expected-vs-found column diff.

---

## Hard boundaries

- Sequential task dispatch only — tasks share files + invariants; no parallel.
- Stop on first per-task gate failure; do NOT continue to next task.
- Do NOT rollback closed tasks on STAGE_VERIFY_FAIL — destructive ops already committed; emit digest + human directive only.
- Per-spec `project-stage-close` fires inside each inner `spec-implementer` normally — do NOT inhibit it. Chain-level stage digest is a NEW separate scope.
- `domain-context-load` fires ONCE at chain start (Phase 1); do NOT re-call per task.
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
