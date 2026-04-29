# Seams

Named LLM seams for the recipe-runner abstraction (DEC-A19).

Each seam = `f: typed_input → typed_output`. Pure transformation. No tool use inside seam. Recipe engine treats as black box.

## Surface

```
tools/seams/{name}/
├── prompt.md              # LLM prompt template (jinja-style {{var}} substitution)
├── input.schema.json      # JSON Schema — input contract
├── output.schema.json     # JSON Schema — output contract
├── golden/                # Snapshot pairs for offline validation
│   └── example-N.json     # { "input": {...}, "output": {...} }
└── agent.yaml             # (Phase B) — model + plan-covered subagent regen source
```

## Catalog (DEC-A19)

| Seam | Role | Current home (pre-recipe) |
|---|---|---|
| `author-spec-body` | issue id + glossary slice + stage context → §1–§10 markdown | `plan-author` / `project-new-applier` |
| `author-plan-digest` | task spec + invariants + work items → §Plan Digest | `stage-authoring` Opus pass |
| `decompose-skeleton-stage` | stage objectives + exit + glossary → Task table rows | `master-plan-new` / `stage-decompose` |
| `align-glossary` | spec body + glossary table → term replacements + warnings | `plan-reviewer-mechanical` glossary scan |
| `review-semantic-drift` | filed task spec + master-plan stage → §Plan Fix tuples | `plan-reviewer-semantic` |

## Invariants

- Output MUST validate against `output.schema.json` — engine escalates on fail (Q5)
- Prompts versioned with seam dir; golden snapshots gate prompt drift
- Model assignment lives in `agent.yaml` (Phase B); no per-call override
- Goldens checked by `npm run validate:seam-golden` (Phase A scope)

## Phase A scope (this scaffold)

- Seam dirs + prompts + schemas + ≥1 golden each
- `seams.run` MCP tool — validates I/O, returns envelope (no subagent dispatch yet)
- `validate:seam-golden` — walks goldens, asserts schema match
- Wired into `validate:all`

Phase B adds: recipe engine + `seam.run` step kind + plan-covered subagent regen.
