---
description: Style critic. Input: cumulative diff + glossary + coding conventions. Scans: (1) caveman-tone — hedging / filler in IA prose; (2) glossary-term consistency — ad-hoc synonyms vs canonical glossary slugs; (3) naming conventions — C# PascalCase, file/path patterns per coding-conventions rule. Output: findings JSON written via review_findings_write MCP. Triggered exclusively by /ship-final Pass B parallel Agent dispatch — not a standalone user command.
argument-hint: "{SLUG} {DIFF_PATH_OR_INLINE}"
---

# /critic-style — Style critic subagent dispatched by /ship-final Pass B. Scans cumulative diff for caveman-tone violations, glossary-term inconsistencies, and naming-convention drift. Emits findings JSON conforming to ia_review_findings shape via review_findings_write MCP.

Drive `$ARGUMENTS` via the [`critic-style`](../agents/critic-style.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /critic-style {SLUG} {DIFF}
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{SLUG} {DIFF_PATH_OR_INLINE}`. Style critic dispatched by /ship-final Pass B.

## Mission

Scan cumulative diff for caveman-tone violations, glossary-term inconsistencies, and naming-convention drift. Emit findings via `review_findings_write` MCP. Return summary.

## Phase sequence

1. Load glossary + coding conventions.
2. Tone scan — added IA/docs prose lines only.
3. Glossary-term scan — canonical term vs ad-hoc synonym.
4. Naming-convention scan — C# public API identifiers.
5. Emit each finding via `review_findings_write`.
6. Return `{ findings_count, high, medium, low }`.

## Hard boundaries

- Read-only — no source mutations.
- Do NOT block on findings — emit all, return. Blocking = ship-final's job.
