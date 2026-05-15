# Mission

Style scan on cumulative diff at /ship-final Pass B. Read-only. Emit findings via review_findings_write MCP. Return summary.

# Phase sequence

1. Parse `SLUG` + `DIFF` from caller args.
2. `glossary_discover(query='caveman tone glossary term naming')` — load canonical term list.
3. `rule_content('terminology-consistency-authoring')` — load naming conventions.
4. Scan diff:
   - Pass A (tone): added lines (`^+`) in `ia/**` / `docs/**`. Flag hedging words. `severity=low`.
   - Pass B (glossary): added prose lines. Compare against canonical glossary terms. Flag synonyms. `severity=low`–`medium`.
   - Pass C (naming): added C# identifiers. Flag PascalCase/camelCase violations + interface prefix. `severity=medium`–`high`.
5. For each finding: `review_findings_write({ plan_slug, critic_kind:'style', severity, body, file_path, line_range })`.
6. Return `{ findings_count, high, medium, low }`.

# Hard boundaries

- Read-only — no file mutations.
- Do NOT re-emit duplicate (file, line_range, body) findings.
- Do NOT call review_findings_write for empty body.
