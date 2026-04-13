---
purpose: "Bulk-file all pending tasks of a single orchestrator stage as individual BACKLOG issues + project spec stubs, sharing stage context and strict alignment."
audience: agent
loaded_by: skill:stage-file
slices_via: none
name: stage-file
description: >
  Bulk-create BACKLOG rows + ia/projects/{ISSUE_ID}.md stubs for all _pending_ tasks in a
  given stage of an orchestrator spec. Each task becomes one issue (project-new workflow).
  Shared stage context loaded once; strict phase/task cardinality enforced. Triggers:
  "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues",
  "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
---

# Bulk stage task filing

Files all `_pending_` tasks of one orchestrator stage as BACKLOG rows + project spec stubs.
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

## Cardinality gate

Before filing, count tasks per phase in the target stage.
- Phase with 1 task → **warn user** and pause. Ask: split the task, or confirm single-task phase with justification for Decision Log.
- Phase with 0 tasks → skip silently (nothing to file).
- Proceed only after user confirms or splits.

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

## Post-loop

After all tasks filed:

1. **Update orchestrator task table** — for each task row: replace `_pending_` in Issue column with `**{ISSUE_ID}**`; replace `_pending_` in Status column with `Draft`.
2. **Run `npm run validate:all`** — chains validate:dead-project-specs + test:ia + validate:fixtures + generate:ia-indexes --check.
3. **Offer next step** — `/kickoff {first_issue_id}` to enrich and refine the first spec before implementation.

## Hard boundaries

- Do NOT update orchestrator task table until ALL tasks are filed (atomic update).
- Do NOT skip validate:dead-project-specs per task — catches broken spec paths immediately.
- Do NOT run validate:all per task — runs once at end.
- Do NOT file tasks for phases not in the target stage.
- Do NOT file tasks outside the target stage.
- Do NOT touch `project-hierarchy.md` cardinality rule from here — surface violations as warnings only.
