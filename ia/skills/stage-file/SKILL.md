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

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass phase → tasks map for the target stage's `_pending_` tasks. Cardinality rule: ≥2 tasks/phase (hard), ≤6 soft. Phase with 0 tasks → skip silently (nothing to file).

Subskill returns `{phases_lt_2, phases_gt_6, single_file_tasks, oversized_tasks, verdict}`:
- `verdict = pause` → surface violations to user; ask split, merge, or justify. Each filed task = 5 orchestration steps — single-function tasks multiply overhead without reducing risk. Proceed only after user confirms, merges, or splits. Phrase split/merge question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not task ids or phase numbers. Ids / phase numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to filing loop.

Reference: `ia/skills/master-plan-new/SKILL.md` Phase 5 "Task sizing heuristic" for merge/split guide.

## Tool recipe (territory-ia) — run ONCE for the whole stage

Load shared context before iterating tasks. All tasks in a stage share domain, subsystems, and invariants.

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from stage Objectives + Exit criteria text; `brownfield_flag = false` for stages touching existing subsystems; `tooling_only_flag = true` for doc/IA-only stages. Use returned `glossary_anchors`, `router_domains`, `spec_sections`, `invariants` as shared context block for all per-task `project-new` seed prompts.

Also run **`backlog_issue`** for any Depends-on ids listed in stage or step Exit criteria.

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

1. **Batch-reserve ids first.** Call `mcp__territory-ia__reserve_backlog_ids(prefix: "{ISSUE_PREFIX}", count: N)` once to reserve all N ids in a single MCP call **before filing any task**. Collect the returned id array. **MCP unavailable fallback:** run `bash tools/scripts/reserve-id.sh {ISSUE_PREFIX}` once per `_pending_` task (N separate calls) and collect ids. Invariant #13: `reserve-id.sh` is the atomic `flock` writer; MCP tool wraps it — no hand-editing the counter.
2. Construct seed prompt (above) with task-specific fields + shared context block. **Append `--reserved-id {ID}` at the end** using the pre-reserved id for this task. `project-new` will use the forwarded id verbatim and skip calling `reserve-id.sh` itself.
3. Follow `project-new` **File and backlog checklist** (steps 1–7 minus validate:all):
   - Author the yaml body for `ia/backlog/{ISSUE_ID}.yaml` (id, type, title, priority, status: open, section, spec, files, notes, acceptance, depends_on, depends_on_raw, related, created, raw_markdown). Every cited Depends-on id must exist in `ia/backlog/` or `ia/backlog-archive/`. Before writing to disk, call `mcp__territory-ia__backlog_record_validate(record: {yaml body})` and fix any reported schema errors. **MCP unavailable fallback:** skip the validate call; end-of-stage `validate:all` catches schema drift. Write the validated yaml to `ia/backlog/{ISSUE_ID}.yaml`. **Do NOT** edit `BACKLOG.md` directly — the materialize post-hook regenerates it.
   - Bootstrap `ia/projects/{ISSUE_ID}.md` from `ia/templates/project-spec-template.md`.
   - Fill §1 Summary + §2.1 Goals from task intent + stage context.
   - Fill §4.2 Systems map from router domains + pre-loaded spec sections.
   - Fill §7 Implementation Plan sketch from task intent (single phase is fine at stub level).
   - Set `spec` field on `ia/backlog/{ISSUE_ID}.yaml` to `ia/projects/{ISSUE_ID}.md`.
   - Verify any Depends-on ids via `backlog_issue` (stage-level deps apply to all tasks).
   - Run `npm run validate:dead-project-specs` per task.
4. After all yaml records written, run `bash tools/scripts/materialize-backlog.sh` once to regenerate `BACKLOG.md`.
5. Record assigned issue id + file path for task table update.

## Compress mode

Merges over-granular `Draft` tasks into consolidated issues before kickoff. Operates only on `Draft` status rows. Never touches `In Review`, `In Progress`, or `Done`.

### Step 1 — Load tool recipe

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from stage Objectives + Exit criteria text; `brownfield_flag = false`; `tooling_only_flag = false`. Shared context applies to all candidate merges in the stage.

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
   - Move `ia/backlog/{ISSUE_ID}.yaml` → `ia/backlog-archive/{ISSUE_ID}.yaml`; set `status: closed`; update Notes: `superseded by {new-id} — stage compress ({STAGE_ID})`.
   - Run `npm run validate:dead-project-specs` after each deletion.
   After all source specs closed, run `bash tools/scripts/materialize-backlog.sh` once to regenerate `BACKLOG.md`.

3. **Create merged issue:** run `project-new` workflow with the confirmed merged intent + stage context block + harvested spec content (from step 1). Merged spec absorbs the combined scope of all source tasks.

4. **Update orchestrator task table:** replace the merged task rows with a single consolidated row:
   - New issue id in Issue column.
   - `Draft` in Status column.
   - Phase assignment = dominant source task (first in group, unless user specifies otherwise).
   - Intent = confirmed merged intent from Step 3.

### Step 5 — Post-compress validation

After all merge groups executed:

1. **Regenerate progress dashboard** — run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)).
2. **Run `npm run validate:all`**.
3. **Offer next step** — if ≥2 tasks compressed into this stage: `claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` (chains all tasks kickoff → implement → verify-loop → closeout with batched Path B at stage end). Single task: `claude-personal "/ship {first_draft_id}"`.

## Post-loop

After all tasks filed:

1. **Update orchestrator task table** — for each task row: replace `_pending_` in Issue column with `**{ISSUE_ID}**`; replace `_pending_` in Status column with `Draft`.
1b. **Flip header Status lines** (R1 + R2 — encode status lifecycle rules): edit `{ORCHESTRATOR_SPEC}` in place.
   - **R2 — Stage header:** find the `#### Stage {STAGE_ID} — {Title}` block; rewrite its `**Status:**` line from `Draft` or `Planned` → `In Progress`.
   - **R1 — Plan top Status:** read the top-of-file `> **Status:**` line. If it equals `Draft` (any variant, e.g. `Draft — Step 1 / Stage 1.1 pending`), rewrite it to `In Progress — Step {STEP_N} / Stage {STAGE_ID}` where `STEP_N` = the parent step number of the target stage. This flip applies on the **first ever task filed** for the plan; if top Status already contains `In Progress`, leave it unchanged.
   - Both flips are idempotent — re-running when Status is already `In Progress` produces zero diff.
1c. **Regenerate progress dashboard** — run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block step 2; log exit code and continue.
2. **Run `npm run validate:all`** — chains validate:dead-project-specs + test:ia + validate:fixtures + generate:ia-indexes --check.
3. **Offer next step** — if ≥2 tasks were filed in this stage: `claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` (chains all tasks kickoff → implement → verify-loop → closeout; batched Path B at stage end). Single-task stage or standalone: `claude-personal "/ship {first_issue_id}"`.

**Step 1 — Friction-condition check**

Evaluate:

```
friction_fires = (guardrail_hits.length > 0) OR (phase_deviations.length > 0) OR (missing_inputs.length > 0)
```

Clean-run rule: if all conditions are false → skip Steps 2–3; no-op. §Changelog untouched.

**Step 2 — Construct `skill_self_report` JSON**

Build JSON per §Schema. Set `skill: stage-file`, `run_date: {YYYY-MM-DD}` (today), `schema_version: 2026-04-18` (date of this emitter stanza template). Populate `friction_types[]`, `guardrail_hits[]`, `phase_deviations[]`, `missing_inputs[]`, `severity` from phase execution data.

**Step 3 — Append §Changelog entry**

Append to `## Changelog` section of `ia/skills/stage-file/SKILL.md`:

```markdown
### {YYYY-MM-DD} — self-report

**source:** self-report

**schema_version:** 2026-04-18

```json
{
  "skill": "stage-file",
  "run_date": "{YYYY-MM-DD}",
  "schema_version": "2026-04-18",
  "friction_types": [],
  "guardrail_hits": [],
  "phase_deviations": [],
  "missing_inputs": [],
  "severity": "low"
}
```

---
```

## Hard boundaries

**File mode:**
- Do NOT gate filing on parent step Status — only task Status matters (`_pending_` = file; `Draft` / `In Review` / `In Progress` / `Done` = skip).
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

## Changelog
