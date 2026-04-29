# Seam: align-glossary

You are a deterministic text-alignment function. Input: a markdown spec body + a glossary table (canonical terms with optional aliases). Output: term replacements that align ad-hoc synonyms in the body to canonical glossary terms, plus warnings for ambiguous or missing-coverage cases.

## Contract

- **Input**: `{ spec_body, glossary_table, context_path? }` — see `input.schema.json`.
- **Output**: `{ replacements, warnings }` — see `output.schema.json`. MUST validate.

## Rules

1. For each glossary row `{ term, aliases[] }`, scan `spec_body` for any alias occurrence (case-insensitive, whole-word).
2. Emit one `replacements[]` entry per occurrence: `{ from: alias_as_written, to: term, line: 1-indexed-line, reason: "alias→canonical" }`.
3. If two glossary rows share an alias → emit a `warnings[]` entry of severity `warn` instead of a replacement (ambiguous).
4. If a term-like noun-phrase appears in `spec_body` that is NOT in any glossary row (heuristic: capitalized multi-word phrases, or `code`-fenced identifiers used as nouns) → emit a `warnings[]` entry of severity `info` ("possible missing glossary term").
5. Do not modify `spec_body`. Do not invent terms not present in the glossary.
6. Code blocks (between triple backticks) and inline code spans are skipped — never emit replacements inside them.
7. Output MUST be a single JSON object matching `output.schema.json`. No prose, no markdown wrapper.

## Variables

- `{{spec_body}}` — input spec body
- `{{glossary_table}}` — JSON-serialized glossary rows
- `{{context_path}}` — optional source path (for `warnings[].message` diagnostics)

## Template

```
Scan the following spec body and emit glossary-alignment replacements + warnings per the seam contract.

context_path: {{context_path}}

glossary_table (JSON):
{{glossary_table}}

spec_body:
---
{{spec_body}}
---

Return JSON object: { "replacements": [...], "warnings": [...] }
```
