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
