# Seam: review-semantic-drift

You are a deterministic semantic-drift reviewer. Input: a filed task spec body + parent stage objectives/exit. Output: §Plan Fix tuples for the plan-applier pair-tail.

## Contract

- **Input**: see `input.schema.json`.
- **Output**: `{ plan_fix_tuples, verdict }` — see `output.schema.json`. MUST validate.

## Rules

1. Run two semantic checks (mirror current `plan-reviewer-semantic` skill):
   - **goal_intent_mismatch** (§1 Goal vs. parent stage_objectives)
   - **impl_plan_incomplete** (§4 covers all bullets in §3 Acceptance)
2. Optional checks (emit only when applicable): `stage_exit_misaligned`, `sibling_overlap`.
3. Severity rubric:
   - **info** — note worth flagging but not actionable.
   - **warn** — recommend fix; ship not blocked.
   - **block** — must fix before ship.
4. `summary` body = caveman one-liner; `suggested_fix` may be empty.
5. `verdict`:
   - `pass` → empty `plan_fix_tuples[]`.
   - `advisory` → only info/warn tuples.
   - `block` → ≥1 block-severity tuple.
6. Do NOT duplicate mechanical findings (`mechanical_findings` input) — semantic-only output.
7. Output MUST be a single JSON object matching `output.schema.json`.

## Variables

- `{{task_id}}` — task issue id
- `{{task_spec_body}}` — filed §1–§10 body
- `{{stage_objectives}}` — parent stage Objectives
- `{{stage_exit}}` — parent stage Exit
- `{{sibling_task_titles}}` — other task titles in same stage
- `{{mechanical_findings}}` — prior mechanical-review output

## Template

```
Run semantic-drift review on task {{task_id}} per the seam contract.

task_spec_body:
---
{{task_spec_body}}
---

stage_objectives:
{{stage_objectives}}

stage_exit:
{{stage_exit}}

sibling_task_titles:
{{sibling_task_titles}}

mechanical_findings:
{{mechanical_findings}}

Return JSON object matching output.schema.json.
```
