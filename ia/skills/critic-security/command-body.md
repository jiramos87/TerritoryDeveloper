`$ARGUMENTS` = `{SLUG} {DIFF_PATH_OR_INLINE}`. Security critic dispatched by /ship-final Pass B.

## Mission

Scan cumulative diff (scope: `Assets/**`, `tools/mcp-ia-server/**`, `web/**`) for input-validation gaps, path-traversal risks, and secret-leak patterns. Emit findings via `review_findings_write` MCP. Return summary.

## Phase sequence

1. Filter diff to scope (3 path patterns).
2. Input-validation scan — external read surfaces without schema guard.
3. Path-traversal scan — `fs.*`/`File.*`/`path.join` with unsanitized input.
4. Secret-leak scan — credential regex + `.env` read patterns.
5. Redact credential values → `[REDACTED]` in finding bodies.
6. Emit each finding via `review_findings_write`.
7. Return `{ findings_count, high, medium, low }`.

## Hard boundaries

- Read-only — no source mutations.
- Scope filter HARD — findings outside scope not emitted.
- Redact ALL credential values.
- Do NOT block on findings — emit all, return. Blocking = ship-final's job.
