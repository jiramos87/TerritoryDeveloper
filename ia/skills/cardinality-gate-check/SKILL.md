---
purpose: "Check phase → task cardinality in a stage: flag phases_lt_2, phases_gt_6, single-file tasks, >3-subsystem tasks. Return structured verdict + pause-or-proceed."
audience: agent
loaded_by: skill:cardinality-gate-check
slices_via: none
name: cardinality-gate-check
description: >
  Sonnet subskill. Given a phase→tasks map from a stage being authored or filed, returns
  `{phases_lt_2, phases_gt_6, single_file_tasks, oversized_tasks, verdict}` and a
  pause-or-proceed signal. Centralizes the cardinality rule from `ia/rules/project-hierarchy.md`
  (≥2 tasks/phase, ≤6 soft) so all master-plan authoring + stage-file skills agree on the same
  gate. Triggers: "cardinality gate", "check phase task counts", "cardinality-gate-check subskill",
  "phase cardinality validation".
---

# Cardinality gate check — Sonnet subskill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Rule source:** `ia/rules/project-hierarchy.md` — ≥2 tasks per phase (hard), ≤6 tasks per phase (soft warn).

---

## Inputs

| Field | Type | Notes |
|-------|------|-------|
| `phases` | `{phase_id: string, tasks: [{id, name, intent}]}[]` | Task lists per phase in the stage being gated. |
| `stage_id` | string | e.g. `"1.2"` — for report labelling only. |

---

## Output

```json
{
  "stage_id": "...",
  "phases_lt_2": ["Phase N — only 1 task: {id} {name}"],
  "phases_gt_6": ["Phase M — 7 tasks (list ids)"],
  "single_file_tasks": ["{id} {name} — covers only 1 file/function/struct"],
  "oversized_tasks": ["{id} {name} — spans >3 unrelated subsystems"],
  "verdict": "proceed | pause"
}
```

`verdict = pause` when `phases_lt_2`, `phases_gt_6`, `single_file_tasks`, or `oversized_tasks` is non-empty.

---

## Contract

1. For each phase in input: count tasks.
   - Count < 2 → add to `phases_lt_2`.
   - Count > 6 → add to `phases_gt_6`.
2. For each task: read `intent` field.
   - Intent covers ≤1 file, ≤1 function, ≤1 struct with no logic → add to `single_file_tasks`.
   - Intent spans >3 unrelated subsystems → add to `oversized_tasks`.
3. Set `verdict`:
   - Any violation list non-empty → `pause`.
   - All lists empty → `proceed`.
4. Return JSON block.

**Caller responsibility:** on `verdict = pause`, surface violations to user (split / justify / merge
as appropriate), re-run with corrected map, then proceed only after user confirms or fixes.
Caller polls the human in product-easy wording — phrase split/merge/justify question in terms of
player/designer-visible outcomes (releasable slices, user-visible checkpoints), not phase ids or
task-count math. Ids / counts go on a trailing `Context:` line. Full rule:
[`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).

---

## Guardrails

- Do NOT decide split boundaries — surface violations, let caller + user resolve.
- Do NOT modify the task table — read-only classifier.
- Do NOT gate on `phases_gt_6` violations without user confirm — it is a soft warn, not a hard block.

---

## Callers

`master-plan-new` Phase 6 · `master-plan-extend` Phase 6 · `stage-decompose` Phase 3 ·
`stage-file` Cardinality gate section.
