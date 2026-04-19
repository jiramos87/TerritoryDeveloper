---
purpose: "Compress mode: audits Draft tasks in a Stage against sizing heuristic; proposes merge groups; harvests + closes source specs; creates consolidated issues. Cold path — only for already-filed stages with over-granular Draft tasks."
audience: agent
loaded_by: skill:stage-compress
slices_via: none
name: stage-compress
description: >
  Compress mode cold path (extracted from legacy stage-file). Merges over-granular Draft
  tasks into consolidated issues before kickoff. Operates only on Draft status rows.
  Never touches In Review, In Progress, or Done tasks. User must confirm merge plan before
  any destructive action (harvest + close source specs). Mode-detection contract: stage-file
  dispatcher routes here when 0 _pending_ tasks + ≥1 Draft tasks in Stage.
  Triggers: "stage-compress", "/stage-compress {ORCHESTRATOR_SPEC} {STAGE_ID}",
  "compress stage", "merge draft tasks", "consolidate stage tasks".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
phases:
  - "Load tool recipe"
  - "Scope audit"
  - "Propose merge plan"
  - "Execute merges"
  - "Post-compress validation"
---

# Stage-compress skill (Compress mode)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Compress mode cold path. Audits `Draft` tasks in a Stage for over-granularity; merges groups into consolidated issues before kickoff. User-gated at Step 3 — no destructive action without explicit confirmation.

Routing: [`stage-file/SKILL.md`](../stage-file/SKILL.md) dispatcher routes here when Stage has 0 `_pending_` + ≥1 `Draft` tasks. File mode → [`stage-file-plan`](../stage-file-plan/SKILL.md) + [`stage-file-apply`](../stage-file-apply/SKILL.md) pair.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg | Repo-relative path to `ia/projects/{master-plan}.md`. |
| `STAGE_ID` | 2nd arg | e.g. `7.2` or `Stage 7.2`. |

---

## Step 1 — Load tool recipe

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)):

- `keywords` = English tokens from Stage Objectives + Exit criteria text.
- `brownfield_flag = false`.
- `tooling_only_flag = false`.
- `context_label` = `"stage-compress Stage {STAGE_ID}"`.

Shared context applies to all candidate merges in the Stage.

---

## Step 2 — Scope audit

For each `Draft` task in the target Stage, evaluate against the sizing heuristic (`ia/skills/master-plan-new/SKILL.md` Phase 5):

| Signal | Judgment | Action |
|--------|----------|--------|
| Task intent covers ≤1 file, ≤1 function, or ≤1 struct with no logic | Too small | Flag for merge |
| Task intent spans >3 unrelated subsystems | Too large | Flag for split (warning; user decides) |
| Task intent spans 2–5 files forming one algorithm layer | Correct scope | Keep as-is |

Group adjacent same-phase "too small" tasks sharing a domain into **candidate merge groups**. Cross-phase merge = hard boundary violation (never allowed).

---

## Step 3 — Propose merge plan

Present to user **before any destructive action**:

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

Frame question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not task ids or phase numbers. Ids/phase numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).

Do **NOT** proceed to Step 4 without explicit user confirmation. User may adjust proposed intents or skip individual groups.

---

## Step 4 — Execute merges (one group at a time)

For each confirmed merge group:

### 4a. Harvest source specs

Before deleting, read each `ia/projects/{ISSUE_ID}.md` in the group. Collect anything not already captured in Stage context:
- §7 Implementation Plan phases with non-trivial content (beyond single-phase stub boilerplate).
- §4.2 Systems map entries that name specific files or methods.
- §2.1 Goals that add precision beyond the task intent line.

Fold harvested content into merged spec seed prompt (Step 4b). Stubs containing only template boilerplate contribute nothing — discard without notes.

### 4b. Close source specs

For each task in the group:
1. Delete `ia/projects/{ISSUE_ID}.md`.
2. Move `ia/backlog/{ISSUE_ID}.yaml` → `ia/backlog-archive/{ISSUE_ID}.yaml`; set `status: closed`; update Notes: `superseded by {new-id} — stage compress ({STAGE_ID})`.
3. Run `npm run validate:dead-project-specs` after each deletion.

After all source specs closed, run `bash tools/scripts/materialize-backlog.sh` once to regenerate `BACKLOG.md`.

### 4c. Create merged issue

Run `project-new` workflow with confirmed merged intent + Stage context block + harvested spec content from Step 4a. Merged spec absorbs combined scope of all source tasks.

### 4d. Update orchestrator task table

Replace merged task rows with single consolidated row:
- New issue id in Issue column.
- `Draft` in Status column.
- Phase = dominant source task (first in group, unless user specifies).
- Intent = confirmed merged intent from Step 3.

---

## Step 5 — Post-compress validation

After all merge groups executed:

1. **Regenerate progress dashboard** — run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block step 2.
2. **Run `npm run validate:all`**.
3. **Offer next step:**
   - ≥2 tasks compressed into stage: `claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"`.
   - Single task: `claude-personal "/ship {first_draft_id}"`.

---

## Hard boundaries

- Do NOT run compress on `In Review`, `In Progress`, or `Done` tasks — active work is off-limits.
- Do NOT delete source specs or BACKLOG rows before user confirms merge plan (Step 3).
- Do NOT merge tasks across different phases — phase boundaries are preserved.
- Do NOT merge tasks with divergent domains even if adjacent — same-domain grouping only.
- Do NOT split tasks in compress mode — flag splits as warnings; user handles splits manually via master plan edit + re-run.

---

## Mode-detection contract

`/stage-file` command + `stage-file` dispatcher skill inspect Stage task-status counts and route:

| Condition | Route |
|-----------|-------|
| ≥1 `_pending_` task, 0 `Draft` | File mode → `stage-file-plan` + `stage-file-apply` |
| 0 `_pending_`, ≥1 `Draft` | Compress mode → this skill (`stage-compress`) |
| ≥1 `_pending_` + ≥1 `Draft` | File pending first (File mode), then offer Compress on resulting Drafts |
| 0 `_pending_`, 0 `Draft` | No-op — report stage state + exit |

Compress prose (`stage-compress/SKILL.md`) is NOT loaded in File-mode hot path — cold path only. File-mode context = `stage-file-plan/SKILL.md` + `stage-file-apply/SKILL.md` only.

---

## §Changelog emitter

## Changelog
