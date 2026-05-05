---
name: arch-drift-scan
purpose: >-
  Manual drift detector for architecture-coherence-system. Calls arch_drift_scan MCP, renders drift
  report, polls user via AskUserQuestion per affected Stage, persists resolutions to plan via
  master_plan_change_log_append (kind=arch_drift_scan). Plan never auto-rewritten.
audience: both
loaded_by: on-demand
slices_via: ""
description: >-
  Use when the user runs /arch-drift-scan {slug?} to check whether open master plans have drifted from
  arch_changelog entries written after each Stage was filed. Phases: load plan → call arch_drift_scan
  → render drift report → AskUserQuestion polling per affected Stage → master_plan_change_log_append
  per resolution. Triggers: "/arch-drift-scan", "drift scan", "architecture drift". No plan rewrite —
  change-log only.
phases:
  - load_plan
  - call_arch_drift_scan
  - render_report
  - poll_per_stage
  - append_change_log
triggers:
  - /arch-drift-scan
  - drift scan
  - architecture drift
  - arch coherence check
argument_hint: >-
  {SLUG?} — optional master-plan slug. Omit → scan all open plans (one report per plan).
model: opus
reasoning_effort: high
input_token_budget: 160000
pre_split_threshold: 140000
tools_role: lifecycle-helper
tools_extra:
  - mcp__territory-ia__arch_drift_scan
  - mcp__territory-ia__arch_surface_resolve
  - mcp__territory-ia__arch_decision_get
  - mcp__territory-ia__arch_decision_list
  - mcp__territory-ia__arch_changelog_since
  - mcp__territory-ia__master_plan_render
  - mcp__territory-ia__master_plan_state
  - mcp__territory-ia__master_plan_change_log_append
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - AskUserQuestion question stems + option labels (product/domain wording per agent-human-polling.md)
hard_boundaries:
  - Do NOT auto-rewrite plan markdown — only append change-log rows.
  - Do NOT skip AskUserQuestion polling on affected Stages — every affected Stage gets a poll.
  - Do NOT batch multiple Stages into one question — one Stage per AskUserQuestion call.
  - Do NOT proceed to change-log append before user resolution returns from AskUserQuestion.
  - Do NOT load whole plan markdown when master_plan_state / master_plan_render slice covers it.
  - Do NOT commit — user decides when.
caller_agent: arch-drift-scan
---

# Architecture drift scan — detect, render, poll, persist

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). AskUserQuestion stems + option labels follow [`agent-human-polling.md`](../../rules/agent-human-polling.md) — product/domain wording, not tool/api jargon.

**Position in lifecycle:** fires _on demand_, after architecture decisions land in `arch_changelog`. Reads only — appends one change-log row per resolved Stage. Does NOT mutate `ia_master_plans` / `ia_stages` / `ia_tasks` directly.

**Related:** [`ship-plan`](../ship-plan/SKILL.md) · [`design-explore`](../design-explore/SKILL.md) · `arch_drift_scan` MCP tool · `arch_changelog` table.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | User prompt | Optional master-plan slug. Omit → scan all open plans. |

---

## Phase sequence (gated)

### Phase 1 — Load plan(s)

- `SLUG` set → call `mcp__territory-ia__master_plan_state(slug)` → grab plan title + stage list (status filter: `_pending_` or `in-progress`).
- `SLUG` unset → call `master_plan_state` per open plan (iterate via `master_plan_state` over each known slug; out of scope to enumerate via filesystem — caller passes slug or runs per-plan).

If plan not found → STOP, report `master_plan_not_found: {slug}`.

### Phase 2 — Call arch_drift_scan

For each plan: `mcp__territory-ia__arch_drift_scan({ slug })`. Returns array of affected stages:

```json
[
  { "stage_id": "1.2", "drifted_surfaces": ["decisions/db-relations-four-tables"], "changelog_kind": "decide", "question": "new decision DEC-A16 — re-plan Stage 1.2?" },
  ...
]
```

Empty array → no drift; emit one-liner "no drift" + skip Phases 3–5.

### Phase 3 — Render drift report

Markdown table per plan:

| Stage | Drifted surfaces | Kind | Suggested resolution |
|---|---|---|---|
| 1.2 | decisions/db-relations-four-tables | decide | re-plan |

Print verbatim before polling — user sees full picture before answering per-Stage questions.

### Phase 4 — AskUserQuestion polling per affected Stage

Per row in drift report → fire ONE `AskUserQuestion` call. Question stem + option labels follow product/domain wording:

- **stem**: brief description of what drifted (which decision / surface) + which Stage hit. NO tool names, NO db column names.
- **options** (3, fixed):
  - `acknowledge` — drift seen, Stage stays as-is, no rework.
  - `reword` — Stage objective / exit criteria need re-authoring (no task changes).
  - `re-plan` — Stage tasks need split / reorder / new tasks added.

Wait for user answer before moving to next Stage. Do NOT batch.

### Phase 5 — Append change-log per resolution

Per resolved Stage → `mcp__territory-ia__master_plan_change_log_append`:

```json
{
  "slug": "{SLUG}",
  "kind": "arch_drift_scan",
  "actor": "arch-drift-scan",
  "body": "Stage {stage_id} drift: {drifted_surfaces} | resolution: {acknowledge|reword|re-plan}"
}
```

Plan never auto-rewritten. User runs `/ship-plan --version-bump {SLUG}` next when resolution = `reword` / `re-plan`.

---

## Tool recipe

1. `mcp__territory-ia__master_plan_state({ slug })` — get plan title + stage list.
2. `mcp__territory-ia__arch_drift_scan({ slug })` — get affected stages + suggested questions.
3. `mcp__territory-ia__arch_decision_get` / `arch_decision_list` — fetch decision details for richer question stems (optional).
4. `mcp__territory-ia__arch_changelog_since` — context on what changed since plan was filed (optional, for narrative).
5. `mcp__territory-ia__master_plan_change_log_append` — one append per resolved Stage.

---

## Guardrails

- IF `arch_drift_scan` returns empty → no polling, no append, emit "no drift" line.
- IF AskUserQuestion times out / user cancels → STOP, do NOT append change-log for that Stage; carry pending Stages forward in output.
- IF `master_plan_change_log_append` rejects → STOP, surface error code.
- IF plan markdown referenced in stage body has been edited mid-scan → re-fetch via `master_plan_state` before polling that Stage.
- Never overwrite Stage objective / exit / task list — change-log is the only persistence path.

---

## Output

Single concise caveman message per scanned plan:

1. plan slug + title.
2. drift count (affected stages).
3. resolutions tally (acknowledge / reword / re-plan).
4. change-log row count appended.
5. next step — `claude-personal "/ship-plan --version-bump {SLUG}"` when any Stage resolved as `reword` / `re-plan`; else "no follow-up".

---

## Next step

After scan + persist:

- All Stages = `acknowledge` → no follow-up; close session.
- ≥1 Stage = `reword` → `claude-personal "/ship-plan --version-bump {SLUG}"` with reword scope.
- ≥1 Stage = `re-plan` → `claude-personal "/ship-plan --version-bump {SLUG}"` with re-plan scope (new task ids reserved + filed).
