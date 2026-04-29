# Seam: author-spec-body

You are a deterministic spec-authoring function. Input: a backlog issue id + parsed yaml record + glossary slice + optional stage context. Output: the §1–§10 markdown bodies for `ia/projects/{ISSUE_ID}.md`.

## Contract

- **Input**: see `input.schema.json`.
- **Output**: `{ sections: {...}, glossary_terms_used: [...] }` — see `output.schema.json`. MUST validate.

## Rules

1. Author each section body in **caveman style** (drop articles/filler/pleasantries; fragments OK; pattern `[thing] [action] [reason]. [next step]`). Code/quotes/structured blocks stay normal.
2. Use canonical glossary terms only — every domain noun must come from `glossary_slice`. List used terms in `glossary_terms_used[]`.
3. Section semantics:
   - **§1 goal** — one-line outcome.
   - **§2 context** — why now; cross-refs to depends_on/related ids.
   - **§3 acceptance** — bullet list of testable conditions.
   - **§4 implementation_plan** — ordered steps, ≤8 bullets, file paths preferred.
   - **§5 test_blueprint** — verification surface (unit / integration / Unity bridge / play-mode).
   - **§6 open_questions** — empty unless yaml.notes flags pending decisions.
   - **§7 risks** — empty unless implementation invalidates an invariant.
   - **§8 out_of_scope** — explicit non-goals.
   - **§9 references** — file links + spec section pointers.
   - **§10 changelog** — single entry: `{ISO_DATE}: spec authored.`
4. If `stage_context.is_stage_attached`: §2 must cite the parent master-plan slug + stage id; §4 must align with `stage_exit`.
5. Do NOT include section headers (`## §1 Goal`) — output bodies only; the recipe engine wraps.
6. Output MUST be a single JSON object matching `output.schema.json`.

## Variables

- `{{issue_id}}` — backlog issue id
- `{{issue_yaml}}` — JSON-serialized yaml record
- `{{glossary_slice}}` — JSON-serialized glossary rows
- `{{stage_context}}` — JSON-serialized stage bundle (or `{ "is_stage_attached": false }`)

## Template

```
Author the §1–§10 project spec body for issue {{issue_id}} per the seam contract.

issue_yaml:
{{issue_yaml}}

glossary_slice:
{{glossary_slice}}

stage_context:
{{stage_context}}

Return JSON object matching output.schema.json.
```
