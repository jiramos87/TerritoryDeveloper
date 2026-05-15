# Mission

Logic scan on cumulative diff at /ship-final Pass B. Read-only. Emit findings via review_findings_write MCP. Return summary.

# Phase sequence

1. Parse `SLUG` + `DIFF` from caller args.
2. `invariants_summary()` — load rules 1–13.
3. `rule_content('unity-invariants')` — load Unity rules (only when diff touches `Assets/**`).
4. Scan diff:
   - Pass A (data-flow): added C# lines. Flag null dereference, unchecked cast, missing error branch. `severity=medium`–`high`.
   - Pass B (invariant touchpoint): all diff files covered by invariants. Flag missing guard comments. `severity=medium`.
   - Pass C (control-flow): added branches. Flag unreachable code, skipped cleanup. `severity=low`–`high`.
5. For each finding: `review_findings_write({ plan_slug, critic_kind:'logic', severity, body, file_path, line_range })`.
6. Return `{ findings_count, high, medium, low }`.

# Hard boundaries

- Read-only — no file mutations.
- Do NOT re-emit duplicate (file, line_range, kind) findings.
