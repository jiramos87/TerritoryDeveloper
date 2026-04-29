# Seam: decompose-skeleton-stage

You are a deterministic stage-decomposition function. Input: a skeleton master-plan stage (objectives + exit + hints) plus glossary + relevant surfaces + invariants. Output: a Task table (≥2 rows) + §Stage File Plan + §Plan Fix bodies.

## Contract

- **Input**: see `input.schema.json`.
- **Output**: `{ tasks, stage_file_plan, plan_fix }` — see `output.schema.json`. MUST validate.

## Rules

1. Cardinality: emit **≥2 tasks**. If natural decomposition yields 1, split by surface (e.g., schema vs. UI vs. test) until ≥2.
2. Sizing: each task `size` ∈ {H1..H6}. Prefer H2–H4 (sweet spot). H5/H6 = warning sign — consider re-splitting.
3. Task titles caveman — `[verb] [object] [scope]`, ≤80 chars.
4. Use canonical glossary terms only — no ad-hoc synonyms.
5. `depends_on[]` may reference sibling task titles (recipe engine resolves to ids at file-time) or pre-existing issue ids from `relevant_surfaces[]`.
6. `summary` body = caveman; mirrors what §1 Goal of the eventual filed task spec will say.
7. `stage_file_plan` body — list filing-time considerations: BACKLOG section anchor, prefix counter increments, depends-on resolution order. Caveman.
8. `plan_fix` body — usually empty `""`. Populate ONLY when decomposition surfaces a known glossary/invariant drift in `stage_objectives` or `stage_exit` text.
9. `tasks[]` order matches intended filing order (engine consumes top-to-bottom).
10. Output MUST be a single JSON object matching `output.schema.json`.

## Variables

- `{{master_plan_slug}}` — parent plan slug
- `{{stage_id}}` — `Stage X.Y`
- `{{stage_objectives}}` — skeleton stage's Objectives text
- `{{stage_exit}}` — skeleton stage's Exit text
- `{{deferred_decomposition_hints}}` — optional hints from author
- `{{relevant_surfaces}}` — JSON-serialized surface list
- `{{glossary_slice}}` — JSON-serialized glossary rows
- `{{invariants}}` — JSON-serialized invariant rows

## Template

```
Decompose {{stage_id}} of master plan {{master_plan_slug}} into a Task table per the seam contract.

stage_objectives:
{{stage_objectives}}

stage_exit:
{{stage_exit}}

deferred_decomposition_hints:
{{deferred_decomposition_hints}}

relevant_surfaces:
{{relevant_surfaces}}

glossary_slice:
{{glossary_slice}}

invariants:
{{invariants}}

Return JSON object matching output.schema.json.
```
