---
purpose: "Bulk-file all pending tasks of a single orchestrator stage as individual BACKLOG issues + project spec stubs, sharing stage context and strict alignment. Idempotent: re-running on an already-filed stage enters Compress mode, merging over-granular Draft tasks before kickoff."
audience: agent
loaded_by: skill:stage-file
slices_via: none
name: stage-file
description: >
  Bulk-create BACKLOG rows + ia/projects/{ISSUE_ID}.md stubs for all _pending_ tasks in a
  given stage of an orchestrator spec. Each task becomes one issue (project-new workflow).
  Shared stage context loaded once; strict phase/task cardinality enforced. Idempotent:
  re-running on a fully-filed stage enters Compress mode — audits Draft tasks against the
  sizing heuristic and merges over-granular ones before kickoff. Triggers:
  "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues",
  "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks",
  "compress stage tasks", "merge draft tasks".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
---

# Bulk stage task filing (idempotent)

Files all `_pending_` tasks of one orchestrator stage as BACKLOG rows + project spec stubs.
Re-running on an already-filed stage enters **Compress mode**: audits `Draft` tasks against
the sizing heuristic and merges over-granular ones into consolidated issues before kickoff.
Each task delegates to **[`project-new`](../project-new/SKILL.md)** for the individual
file+backlog checklist. Shared stage context pre-loaded once; task table updated after all
issues created.

**vs project-new:** `project-new` = one issue from user prompt. This skill = bulk project-new
for tasks sharing a stage's context, with strict alignment to the orchestrator spec and
phase/task cardinality enforcement.

## Inputs

**Argument order (positional, explicit preferred):** `ORCHESTRATOR_SPEC` first, `STAGE_ID` second.
When the repo has multiple orchestrator docs under `ia/projects/`, always pass the path explicitly to avoid ambiguity.

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ORCHESTRATOR_SPEC` | **1st arg** (explicit path preferred) or Glob resolve | Path to `ia/projects/{master-plan}.md`. Glob fallback only when exactly one `*-master-plan.md` exists. |
| `STAGE_ID` | **2nd arg** | e.g. `1.2` or `Stage 1.2` |
| `ISSUE_PREFIX` | 3rd arg or default | `TECH-` / `FEAT-` / `BUG-` / `ART-` / `AUDIO-` — default `TECH-` |

## Mode detection

Scan the target stage's task table **before any other action**. Classify by task status counts:

| Mode | Condition | Action |
|---|---|---|
| **File mode** | ≥1 `_pending_` task, 0 `Draft` tasks | Run filing loop (existing behavior) |
| **Compress mode** | 0 `_pending_` tasks, ≥1 `Draft` tasks | Run compress workflow (below) |
| **Mixed mode** | ≥1 `_pending_` + ≥1 `Draft` tasks | File pending first (File mode), then offer Compress mode on resulting Drafts |
| **No-op** | 0 `_pending_`, 0 `Draft` tasks | Report stage state (In Review / In Progress / Done tasks present) — exit. Active or closed tasks cannot be touched. |

`In Review`, `In Progress`, and `Done` tasks: **skip in all modes**. Never touch active or closed work.

## Cardinality gate

Before filing, count tasks per phase in the target stage.
- Phase with 1 task → **warn user** and pause. Ask: split the task, or confirm single-task phase with justification for Decision Log.
- Phase with 0 tasks → skip silently (nothing to file).

**Task scope check (last-chance sizing pass before project-new runs):**
- Task covers only 1 file, 1 function, or 1 struct with no logic → **warn user** and pause. Suggest merge with adjacent same-phase task. Rationale: each filed task = 5 orchestration steps (project-new → kickoff → implement → verify-loop → closeout); single-function tasks multiply that overhead without reducing risk.
- Task spans >3 unrelated subsystems → **warn user** and pause. Suggest split at the subsystem seam.
- Reference: `ia/skills/master-plan-new/SKILL.md` Phase 5 "Task sizing heuristic" for the merge/split decision guide.

Proceed only after user confirms, merges, or splits.

## Tool recipe (territory-ia) — run ONCE for the whole stage

Load shared context before iterating tasks. All tasks in a stage share domain, subsystems, and invariants.

1. **`glossary_discover`** — `keywords` JSON array from stage Objectives + Exit criteria text (English tokens).
2. **`glossary_lookup`** — high-confidence terms from discover.
3. **`router_for_task`** — 1–3 domains matching agent-router table vocabulary, derived from stage Objectives.
4. **`invariants_summary`** — if stage touches runtime C# / game subsystems. Skip for doc/IA-only stages.
5. **`spec_section`** — relevant spec sections implied by stage Objectives (set `max_chars`).
6. **`backlog_issue`** — for any Depends-on ids listed in stage or step Exit criteria.

## Seed prompt construction (per task)

Parameterize `project-new`'s seed prompt for each task. Include the shared context block:

```markdown
Create a new backlog issue and initial project spec from this description:

**Title / intent:** {TASK_INTENT}
**Issue type:** {ISSUE_PREFIX}
**User / product prompt:**

{TASK_INTENT}

**Stage context (shared — do NOT file separately):**
- Stage: {STAGE_ID} — {STAGE_TITLE}
- Stage Objectives: {STAGE_OBJECTIVES}
- Stage Exit criteria: {STAGE_EXIT}
- Phase this task belongs to: Phase {PHASE_NUM} — {PHASE_NAME}
- Other tasks in this phase: {SIBLING_TASK_INTENTS}
- Pre-loaded glossary terms: {GLOSSARY_TERMS}
- Pre-loaded router domains: {ROUTER_DOMAINS}
- Relevant invariants: {INVARIANTS}

Follow `ia/skills/project-new/SKILL.md` § "Stage context injection" + § "File and backlog
checklist". Use stage context to populate §1 Summary, §2.1 Goals, §4.2 Systems map.
Skip re-running glossary_discover / router_for_task / invariants_summary (pre-loaded above)
unless task intent clearly diverges. Run `npm run validate:dead-project-specs` only (NOT
validate:all — stage-file runs that once at end). Do NOT update the orchestrator task table.
```

## Filing loop

Run in task-table order (T{stage}.1, T{stage}.2, …).

For each `_pending_` task:

1. Scan `BACKLOG.md` + `BACKLOG-ARCHIVE.md` for highest id in `ISSUE_PREFIX` → assign `max+1`.
2. Construct seed prompt (above) with task-specific fields + shared context block.
3. Follow `project-new` **File and backlog checklist** (steps 1–7 minus validate:all):
   - Add BACKLOG row under correct lane (match orchestrator's lane — `§ Multi-scale simulation lane` for multi-scale orchestrator tasks).
   - Bootstrap `ia/projects/{ISSUE_ID}.md` from `ia/templates/project-spec-template.md`.
   - Fill §1 Summary + §2.1 Goals from task intent + stage context.
   - Fill §4.2 Systems map from router domains + pre-loaded spec sections.
   - Fill §7 Implementation Plan sketch from task intent (single phase is fine at stub level).
   - Set `Spec: ia/projects/{ISSUE_ID}.md` on backlog row.
   - Verify any Depends-on ids via `backlog_issue` (stage-level deps apply to all tasks).
   - Run `npm run validate:dead-project-specs` per task.
4. Record assigned issue id + file path for task table update.

## Compress mode

Merges over-granular `Draft` tasks into consolidated issues before kickoff. Operates only on `Draft` status rows. Never touches `In Review`, `In Progress`, or `Done`.

### Step 1 — Load tool recipe

Same as File mode: `glossary_discover` → `glossary_lookup` → `router_for_task` → `invariants_summary` → `spec_section`. Shared context applies to all candidate merges in the stage.

### Step 2 — Scope audit

For each `Draft` task in the target stage, evaluate against the sizing heuristic (`ia/skills/master-plan-new/SKILL.md` Phase 5):

- **Too small:** task intent covers ≤1 file, ≤1 function, or ≤1 struct with no logic → flag for merge.
- **Too large:** task intent spans >3 unrelated subsystems → flag for split (treat as a cardinality warning; user decides split boundary).
- **Correct scope:** 2–5 files forming one algorithm layer → keep as-is.

Group adjacent same-phase "too small" tasks sharing a domain into **candidate merge groups**.

### Step 3 — Propose merge plan

Present to user before any destructive action:

```
Compress mode — Stage {STAGE_ID} audit
───────────────────────────────────────
Merge group A (Phase {N}):
  {ISSUE_ID_1} — {task intent}
  {ISSUE_ID_2} — {task intent}
  → Proposed merged intent: {combined scope, 1–2 sentences}

Merge group B (Phase {N}):
  {ISSUE_ID_3} — {task intent}
  {ISSUE_ID_4} — {task intent}
  {ISSUE_ID_5} — {task intent}
  → Proposed merged intent: {combined scope, 1–2 sentences}

Keep as-is:
  {ISSUE_ID_6} — {task intent} (correct scope)

Confirm merge groups? (y / adjust / skip group)
```

Do NOT proceed to Step 4 without explicit user confirmation. User may adjust proposed intents or skip individual groups.

### Step 4 — Execute merges (one group at a time)

For each confirmed merge group:

1. **Harvest source specs:** before deleting, read each `ia/projects/{ISSUE_ID}.md` in the group. Collect anything not already captured in the stage context block:
   - §7 Implementation Plan phases with non-trivial content (beyond the single-phase stub boilerplate).
   - §4.2 Systems map entries that name specific files or methods.
   - §2.1 Goals that add precision beyond the task intent line.
   Fold harvested content into the merged spec seed prompt (Step 2 below). Stubs that contain only template boilerplate contribute nothing — discard without notes.

2. **Close source specs:** for each task in the group:
   - Delete `ia/projects/{ISSUE_ID}.md`.
   - Move BACKLOG row to `BACKLOG-ARCHIVE.md` with note: `superseded by {new-id} — stage compress ({STAGE_ID})`.
   - Run `npm run validate:dead-project-specs` after each deletion.

3. **Create merged issue:** run `project-new` workflow with the confirmed merged intent + stage context block + harvested spec content (from step 1). Merged spec absorbs the combined scope of all source tasks.

4. **Update orchestrator task table:** replace the merged task rows with a single consolidated row:
   - New issue id in Issue column.
   - `Draft` in Status column.
   - Phase assignment = dominant source task (first in group, unless user specifies otherwise).
   - Intent = confirmed merged intent from Step 3.

### Step 5 — Post-compress validation

After all merge groups executed:

1. **Regenerate progress dashboard** — `npm run progress`.
2. **Run `npm run validate:all`**.
3. **Offer next step** — `/kickoff {first_draft_id}`.

## Post-loop

After all tasks filed:

1. **Update orchestrator task table** — for each task row: replace `_pending_` in Issue column with `**{ISSUE_ID}**`; replace `_pending_` in Status column with `Draft`.
1b. **Regenerate progress dashboard** — `npm run progress` (repo root). Reflects `Draft` status flip in `docs/progress.html`. Deterministic; failure does NOT block step 2 — log exit code and continue.
2. **Run `npm run validate:all`** — chains validate:dead-project-specs + test:ia + validate:fixtures + generate:ia-indexes --check.
3. **Offer next step** — `/kickoff {first_issue_id}` to enrich and refine the first spec before implementation.

## Hard boundaries

**File mode:**
- Do NOT update orchestrator task table until ALL tasks are filed (atomic update).
- Do NOT skip validate:dead-project-specs per task — catches broken spec paths immediately.
- Do NOT run validate:all per task — runs once at end.
- Do NOT file tasks for phases not in the target stage.
- Do NOT file tasks outside the target stage.
- Do NOT touch `project-hierarchy.md` cardinality rule from here — surface violations as warnings only.

**Compress mode:**
- Do NOT run compress on `In Review`, `In Progress`, or `Done` tasks — active work is off-limits.
- Do NOT delete source specs or BACKLOG rows before user confirms the merge plan (Step 3).
- Do NOT merge tasks across different phases — phase boundaries are preserved.
- Do NOT merge tasks with divergent domains even if adjacent — same-domain grouping only.
- Do NOT split tasks in compress mode — flag splits as warnings; user handles splits manually via master plan edit + re-run.
