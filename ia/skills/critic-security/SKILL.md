---
name: critic-security
purpose: >-
  Security critic subagent dispatched by /ship-final Pass B. Scans cumulative diff
  filtered to Assets/**, tools/mcp-ia-server/**, and web/** for input-validation
  gaps, path-traversal risks, and secret-leak patterns. Emits findings JSON
  conforming to ia_review_findings shape via review_findings_write MCP.
audience: agent
loaded_by: "skill:critic-security"
slices_via: rule_content
description: >-
  Security critic. Input: cumulative diff filtered to Assets/**, tools/mcp-ia-server/**,
  web/**. Scans: (1) input-validation — unvalidated user/agent input reaching
  DB or filesystem; (2) path-traversal — unsanitized path segments in file ops;
  (3) secret-leak — credential/key literals, env-var dump patterns, .env exposure.
  Output: findings JSON via review_findings_write MCP. Triggered exclusively by
  /ship-final Pass B — not standalone.
triggers:
  - /critic-security {SLUG} {DIFF}
argument_hint: "{SLUG} {DIFF_PATH_OR_INLINE}"
model: sonnet
reasoning_effort: medium
input_token_budget: 80000
pre_split_threshold: 70000
tools_role: custom
tools_extra:
  - mcp__territory-ia__rule_content
  - mcp__territory-ia__review_findings_write
caveman_exceptions:
  - code
  - verbatim error/tool output
  - structured MCP payloads
  - security/auth
hard_boundaries:
  - Do NOT mutate any source file — read-only scan only.
  - Do NOT call review_findings_write with severity other than low|medium|high.
  - Do NOT block on findings — emit all findings then return.
  - Do NOT log credential values in finding body — redact to `[REDACTED]`.
caller_agent: critic-security
---

# critic-security — security scan at /ship-final Pass B

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller (ship-final) | Master-plan slug being closed. |
| `DIFF` | Caller (ship-final) | Cumulative diff string or path to diff file. |

## Scope filter

Only scan hunks touching: `Assets/**`, `tools/mcp-ia-server/**`, `web/**`. Skip `ia/**`, `docs/**`, `db/migrations/**`, `tests/**`.

## Scan targets

1. **Input validation** — added lines that read from external sources (HTTP request, MCP tool args, Unity bridge response) without schema validation. Severity: `medium`–`high`.
2. **Path traversal** — added `fs.*` / `File.*` / `path.join` calls where one segment derives from user/agent input without `path.normalize` + prefix-check. Severity: `high`.
3. **Secret leak** — added lines matching: API key literals, `process.env` dumps, base64-encoded credentials, `.env` file read outside `config.ts`. Severity: `high`.

## Output contract

```json
{
  "plan_slug": "{SLUG}",
  "critic_kind": "security",
  "severity": "high",
  "body": "Path traversal: unsanitized `inputPath` from MCP arg passed to fs.readFileSync. Add path.normalize + prefix guard.",
  "file_path": "tools/mcp-ia-server/src/tools/example.ts",
  "line_range": "L88-L92"
}
```

Redact credential values: body must contain `[REDACTED]`, never the literal.

## Phase sequence

1. Filter diff to scope (`Assets/**` + `tools/mcp-ia-server/**` + `web/**`).
2. Input-validation pass — added lines touching external read surfaces.
3. Path-traversal pass — added `fs.*` / `File.*` / `path.join` calls.
4. Secret-leak pass — regex: `(api[_-]?key|password|secret|token)\s*=\s*['"]`, base64 patterns, `.env` reads.
5. Emit each finding via `review_findings_write`.
6. Return `{ findings_count, high, medium, low }`.

## Guardrails

- Scope filter is HARD — do not emit findings for files outside the 3 scope patterns.
- Redact all credential values before emitting finding body.
- Never emit duplicate (file, line_range, kind) findings.
- False-positive preference: emit `low` finding with caveat rather than silently skip.
