---
description: Security critic. Input: cumulative diff filtered to Assets/**, tools/mcp-ia-server/**, web/**. Scans: (1) input-validation — unvalidated user/agent input reaching DB or filesystem; (2) path-traversal — unsanitized path segments in file ops; (3) secret-leak — credential/key literals, env-var dump patterns, .env exposure. Output: findings JSON via review_findings_write MCP. Triggered exclusively by /ship-final Pass B — not standalone.
argument-hint: "{SLUG} {DIFF_PATH_OR_INLINE}"
---

# /critic-security — Security critic subagent dispatched by /ship-final Pass B. Scans cumulative diff filtered to Assets/**, tools/mcp-ia-server/**, and web/** for input-validation gaps, path-traversal risks, and secret-leak patterns. Emits findings JSON conforming to ia_review_findings shape via review_findings_write MCP.

Drive `$ARGUMENTS` via the [`critic-security`](../agents/critic-security.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, verbatim error/tool output, structured MCP payloads, security/auth. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /critic-security {SLUG} {DIFF}
<!-- skill-tools:body-override -->

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
