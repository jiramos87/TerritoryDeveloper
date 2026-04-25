---
name: release-rollout-track
description: Use AFTER a downstream subagent returns success from `/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` to flip the corresponding tracker cell `— → ◐` or `◐ → ✓`, append the completion ticket (SHA / doc path / issue id), and add a Change log row. Read-only verification pass via MCP (`glossary_lookup`, `router_for_task`, `spec_section`) for column (g) align gate. Does NOT decide cell targets (umbrella skill owns). Does NOT dispatch subagents (= umbrella skill). Triggers: "track cell flip", "update tracker after stage-file", "release-rollout-track {row-slug} {col} {ticket}".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content
model: inherit
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Flip one cell in `{TRACKER_SPEC}` for `{ROW_SLUG}` at `{TARGET_COL}` to `{NEW_MARKER}`. Idempotent. Append Change log row. No decisions — mechanical cell flip only.

# Recipe

Follow `ia/skills/release-rollout-track/SKILL.md` end-to-end.

Phase 0 — Load + validate: Read `{TRACKER_SPEC}`. Grep for `| {ROW_SLUG} |`. Missing row → STOP. Confirm `TARGET_COL` ∈ (a)–(g). Confirm `NEW_MARKER` ∈ {✓, ◐, —, ❓, ⚠️}.

Phase 1 — Column (g) align verify (only when `TARGET_COL = (g)` OR `TARGET_COL = (e)` with (g) gate): run `term-anchor-verify` subskill (`ia/skills/term-anchor-verify/SKILL.md`) for every NEW domain entity introduced by this row. Inputs: `terms` = English entity names from child orchestrator Objectives / Exit criteria. `all_anchored = true` → (g) `✓`. `all_anchored = false` → (g) `—` + Skill Iteration Log note naming `unresolved_terms`.

Phase 1b — Column (f) filed-signal verify (only when `TARGET_COL = (f)` AND `NEW_MARKER` = `✓` or `◐`): Glob `ia/backlog/*.yaml` + `ia/projects/{id}*.md` pairs for slug. Both present for all records → `✓`; any yaml without spec → `◐`; zero records → `—`.

Phase 2 — Cell flip: Edit `{TRACKER_SPEC}`. Find row `| {ROW_SLUG} |`. Replace `TARGET_COL` cell with `{NEW_MARKER} ({TICKET})`. Idempotent: if already at target marker + same ticket → no-op + skip Phase 3.

Phase 3 — Change log append: append row to `## Change log` table:
`| {YYYY-MM-DD} | {ROW_SLUG} cell ({TARGET_COL}) → {NEW_MARKER}; ticket: {TICKET} ({CHANGELOG_NOTE}) | release-rollout-track |`

Phase 4 — Handoff: single caveman line: `{TRACKER_SPEC} {ROW_SLUG}({TARGET_COL}) → {NEW_MARKER} ({TICKET}).`

# Hard boundaries

- IF row not in tracker → STOP.
- IF `TARGET_COL` invalid → STOP.
- IF `NEW_MARKER` invalid glyph → STOP.
- IF (g) align verify fails AND `TARGET_COL = (e)` → STOP. Fall back to (g) = `—` + skill bug log entry. Do NOT tick (e).
- Do NOT touch other rows.
- Do NOT edit Disagreements appendix.
- Do NOT commit.
