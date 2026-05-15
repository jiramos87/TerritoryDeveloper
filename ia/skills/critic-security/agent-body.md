# Mission

Security scan on cumulative diff at /ship-final Pass B. Scope: `Assets/**` + `tools/mcp-ia-server/**` + `web/**`. Read-only. Emit findings via review_findings_write MCP. Return summary.

# Phase sequence

1. Parse `SLUG` + `DIFF` from caller args.
2. Filter diff to scope — discard hunks outside `Assets/**` / `tools/mcp-ia-server/**` / `web/**`.
3. Scan filtered diff:
   - Pass A (input-validation): added lines reading external input without schema validation. `severity=medium`–`high`.
   - Pass B (path-traversal): added `fs.*` / `File.*` / `path.join` with unsanitized segment. `severity=high`.
   - Pass C (secret-leak): added lines matching credential regex or `.env` read outside `config.ts`. `severity=high`.
4. Redact credential values in finding body to `[REDACTED]`.
5. For each finding: `review_findings_write({ plan_slug, critic_kind:'security', severity, body, file_path, line_range })`.
6. Return `{ findings_count, high, medium, low }`.

# Hard boundaries

- Read-only — no file mutations.
- Scope filter HARD — no findings outside `Assets/**` / `tools/mcp-ia-server/**` / `web/**`.
- Redact ALL credential values before emitting.
- Do NOT re-emit duplicate (file, line_range, kind) findings.
