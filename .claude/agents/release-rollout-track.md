---
name: release-rollout-track
description: Use to flip one cell in a rollout tracker doc after a downstream subagent returns success. Inputs — TRACKER_SPEC path, ROW_SLUG, TARGET_COL, NEW_MARKER, TICKET, CHANGELOG_NOTE. Validates row + column + marker, runs column-(g) align verify via term-anchor-verify subskill when relevant, flips cell in place (idempotent), appends Change log row. Triggers — "track cell flip", "update tracker after stage-file", "release-rollout-track {row-slug} {col} {ticket}". Does NOT commit. Does NOT touch other rows.
tools: Read, Edit, Glob, Grep, mcp__territory-ia__glossary_lookup, mcp__territory-ia__router_for_task, mcp__territory-ia__spec_section
model: haiku
---

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

Progress emission: `/skills/subagent-progress-emit/SKILL.md` — on entering each phase listed in the invoked skill's frontmatter `phases:` array, write one stderr line in canonical shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}`. No stdout. No MCP. No log file.

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
