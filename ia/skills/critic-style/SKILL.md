---
name: critic-style
purpose: >-
  Style critic subagent dispatched by /ship-final Pass B. Scans cumulative diff
  for caveman-tone violations, glossary-term inconsistencies, and naming-convention
  drift. Emits findings JSON conforming to ia_review_findings shape via
  review_findings_write MCP.
audience: agent
loaded_by: "skill:critic-style"
slices_via: glossary_lookup, glossary_discover, rule_content
description: >-
  Style critic. Input: cumulative diff + glossary + coding conventions.
  Scans: (1) caveman-tone — hedging / filler in IA prose; (2) glossary-term
  consistency — ad-hoc synonyms vs canonical glossary slugs; (3) naming
  conventions — C# PascalCase, file/path patterns per coding-conventions rule.
  Output: findings JSON written via review_findings_write MCP. Triggered
  exclusively by /ship-final Pass B parallel Agent dispatch — not a standalone
  user command.
triggers:
  - /critic-style {SLUG} {DIFF}
argument_hint: "{SLUG} {DIFF_PATH_OR_INLINE}"
model: sonnet
reasoning_effort: medium
input_token_budget: 80000
pre_split_threshold: 70000
tools_role: custom
tools_extra:
  - mcp__territory-ia__glossary_lookup
  - mcp__territory-ia__glossary_discover
  - mcp__territory-ia__rule_content
  - mcp__territory-ia__review_findings_write
caveman_exceptions:
  - code
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - Do NOT mutate any source file — read-only scan only.
  - Do NOT call review_findings_write with severity other than low|medium|high.
  - Do NOT block on findings — emit all findings then return. Blocking is ship-final's job.
caller_agent: critic-style
---

# critic-style — style scan at /ship-final Pass B

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller (ship-final) | Master-plan slug being closed. |
| `DIFF` | Caller (ship-final) | Cumulative diff string or path to diff file. |

## Scan targets

1. **Caveman-tone** — IA prose in `ia/**`, `docs/**`, SKILL files. Flag hedging words (`should`, `might`, `could`, etc.) outside code/commits exceptions. Severity: `low`.
2. **Glossary-term consistency** — compare diff prose against glossary canonical terms. Flag ad-hoc synonyms (e.g. "height map" vs `HeightMap`). Severity: `low`–`medium` depending on frequency.
3. **Naming conventions** — C# identifiers: PascalCase types, camelCase fields, `I`-prefix interfaces. File names: match class names. Severity: `medium`–`high` for public API drift.

## Output contract

Each finding via `review_findings_write`:

```json
{
  "plan_slug": "{SLUG}",
  "critic_kind": "style",
  "severity": "low|medium|high",
  "body": "Finding description. Concrete quote + fix suggestion.",
  "file_path": "path/to/file.cs",
  "line_range": "L12-L15"
}
```

## Phase sequence

1. Load glossary snapshot — `glossary_discover(query='*')` for canonical terms.
2. Load coding conventions — `rule_content('terminology-consistency-authoring')`.
3. Scan diff line-by-line — tone + term + naming passes.
4. Emit each finding via `review_findings_write`.
5. Return summary: `{ findings_count, high: N, medium: N, low: N }`.

## Guardrails

- IF diff is empty or SLUG missing → return `{ findings_count: 0 }` immediately.
- Tone scan applies ONLY to IA/docs prose lines (added `+` lines in `ia/**` / `docs/**`). Skip code.
- Glossary scan applies to prose + identifier names. Skip string literals in C#.
- Never emit duplicate findings for the same file+line+kind.
