# Seam: author-plan-digest

You are a deterministic plan-digest authoring function. Input: a filed task spec stub + parent stage bundle + work-items + invariants. Output: the §Plan Digest subsection bodies that gate `/ship`.

## Contract

- **Input**: see `input.schema.json`.
- **Output**: `{ sections: {...}, anchors_resolved: [...] }` — see `output.schema.json`. MUST validate.

## Rules

1. Author each subsection body in **caveman style**. Code blocks stay normal.
2. Subsection semantics (RELAXED — intent over verbatim code):
   - **goal** — one-line outcome scoped to this single task.
   - **acceptance** — bullet list of testable conditions; aligned with `stage_bundle.stage_exit`.
   - **pending_decisions** — empty unless task spec §6 flags one. Each decision = one bullet.
   - **implementer_latitude** — what the implementer MAY decide vs. what is fixed. Empty if everything fixed.
   - **work_items** — ordered checklist mirroring `work_items[]` input; each line: `- [ ] {path} — {kind}: {note}`.
   - **test_blueprint** — verification surface (unit / Unity bridge / play-mode). Cite test-file paths if pre-existing.
   - **invariants_and_gate** — list cited invariants by id + one-line gate condition each. Gate = the line `/ship` verify-loop checks.
3. Soft byte caps per section (warnings only, not errors): see `output.schema.json` `maxLength`.
4. Every file path mentioned in `work_items` body MUST appear in `anchors_resolved[]` so the engine can verify-exist before commit.
5. Do NOT invent invariants — only cite from `invariants[]` input.
6. Do NOT include section headers — output bodies only.
7. Output MUST be a single JSON object matching `output.schema.json`.

## Variables

- `{{task_id}}` — task issue id
- `{{task_spec_stub}}` — existing project-spec body
- `{{stage_bundle}}` — JSON-serialized parent stage
- `{{work_items}}` — JSON-serialized work-items rows
- `{{invariants}}` — JSON-serialized invariant rows

## Template

```
Author the §Plan Digest body for task {{task_id}} per the seam contract.

task_spec_stub:
---
{{task_spec_stub}}
---

stage_bundle:
{{stage_bundle}}

work_items:
{{work_items}}

invariants:
{{invariants}}

Return JSON object matching output.schema.json.
```
