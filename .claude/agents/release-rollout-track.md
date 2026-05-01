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
Mechanical tracker cell flip — idempotent. validate_row + cell_flip + changelog_append + handoff via recipe.
# Recipe
`tools/recipes/release-rollout-track.yaml`. CLI: `npm run recipe:run -- release-rollout-track --input tracker_spec={SPEC} --input row_slug={ROW} --input target_col={a..g} --input new_marker={glyph} --input ticket={TICKET} --input changelog_note={NOTE}`. Caller responsibilities ((g) align verify + (f) filed-signal): see `ia/skills/release-rollout-track/SKILL.md` §Caller responsibilities.
# Hard boundaries
validate_row STOP on row/col/marker invalid; (g) align fail + col=e → pass col=g + marker=— + skill bug log; no other rows; no Disagreements appendix; no commit.
