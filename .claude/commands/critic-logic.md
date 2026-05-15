---
description: Logic critic. Input: cumulative diff + invariants summary. Scans: (1) data-flow — null-path gaps, unguarded casts, missing error branches; (2) invariant touchpoints — Unity rules 1–11 + universal rules 12–13 touched by diff without matching guard comment; (3) control-flow — unreachable branches, early-return skipping cleanup. Output: findings JSON via review_findings_write MCP. Triggered exclusively by /ship-final Pass B — not standalone.
argument-hint: "{SLUG} {DIFF_PATH_OR_INLINE}"
---

# /critic-logic — Logic critic subagent dispatched by /ship-final Pass B. Scans cumulative diff for data-flow violations, invariant-touchpoint gaps, and control-flow anomalies. Emits findings JSON conforming to ia_review_findings shape via review_findings_write MCP.

Drive `$ARGUMENTS` via the [`critic-logic`](../agents/critic-logic.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /critic-logic {SLUG} {DIFF}
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{SLUG} {DIFF_PATH_OR_INLINE}`. Logic critic dispatched by /ship-final Pass B.

## Mission

Scan cumulative diff for data-flow violations, invariant-touchpoint gaps, and control-flow anomalies. Emit findings via `review_findings_write` MCP. Return summary.

## Phase sequence

1. Load invariants + Unity rules.
2. Data-flow scan — null dereferences, unchecked casts, missing error branches.
3. Invariant-touchpoint scan — invariant-governed files modified without guard comment.
4. Control-flow scan — unreachable branches, cleanup-skipping early returns.
5. Emit each finding via `review_findings_write`.
6. Return `{ findings_count, high, medium, low }`.

## Hard boundaries

- Read-only — no source mutations.
- Do NOT block on findings — emit all, return. Blocking = ship-final's job.
