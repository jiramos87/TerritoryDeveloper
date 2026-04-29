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

Mechanical phases (validate, cell flip, Change log append, handoff) run as recipe `release-rollout-track` (`tools/recipes/release-rollout-track.yaml`) — DEC-A19 Phase C recipify. Invoke:

```
npm run recipe:run -- release-rollout-track \
  --input tracker_spec={TRACKER_SPEC} \
  --input row_slug={ROW_SLUG} \
  --input target_col={a..g} \
  --input new_marker={✓|◐|—|❓|⚠️} \
  --input ticket={TICKET} \
  --input changelog_note={CHANGELOG_NOTE}
```

Recipe stops on first failure (validate row / column / marker; cell-flip header parse; row not matched). Both `cell_flip` and `changelog_append` are idempotent — re-runs return `noop` instead of duplicating edits.

# Caller responsibilities (NOT in recipe — defer to seam Phase D)

- Column (g) align verify when `target_col=g` OR `target_col=e` with (g) gate. Run `term-anchor-verify` subskill (`ia/skills/term-anchor-verify/SKILL.md`) over child orchestrator domain entities. `all_anchored=true` → marker `✓`; otherwise `—` + skill bug log entry. Caller picks final marker before invoking recipe.
- Column (f) filed-signal verify when `target_col=f`. Either run helper `tools/scripts/recipe-engine/release-rollout-track/filed-signal.sh --slug {ROW_SLUG}` for a coarse glyph, or inspect Glob output by hand. Caller passes resulting glyph as `new_marker`.

# Hard boundaries

- IF row not in tracker → recipe `validate_row` step STOPs; do not retry.
- IF `target_col` invalid → recipe STOPs.
- IF `new_marker` invalid glyph → recipe STOPs.
- IF (g) align verify fails AND `target_col = (e)` → caller passes `target_col=g` + `new_marker=—` + skill bug log entry. Do NOT tick (e).
- Do NOT touch other rows.
- Do NOT edit Disagreements appendix.
- Do NOT commit.
