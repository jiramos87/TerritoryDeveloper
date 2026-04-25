---
description: Use AFTER a downstream subagent returns success from `/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` to flip the corresponding tracker cell `— → ◐` or `◐ → ✓`, append the completion ticket (SHA / doc path / issue id), and add a Change log row. Read-only verification pass via MCP (`glossary_lookup`, `router_for_task`, `spec_section`) for column (g) align gate. Does NOT decide cell targets (umbrella skill owns). Does NOT dispatch subagents (= umbrella skill). Triggers: "track cell flip", "update tracker after stage-file", "release-rollout-track {row-slug} {col} {ticket}".
argument-hint: ""
---

# /release-rollout-track — Update tracker cells after a downstream subagent (design-explore / master-plan-new / master-plan-extend / stage-decompose / stage-file) returns success. Mechanical cell flip + ticket append + change log entry. No decisions.

Drive `$ARGUMENTS` via the [`release-rollout-track`](../agents/release-rollout-track.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- track cell flip
- update tracker after stage-file
- release-rollout-track {row-slug} {col} {ticket}
## Dispatch

Single Agent invocation with `subagent_type: "release-rollout-track"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/release-rollout-track/SKILL.md`](../../ia/skills/release-rollout-track/SKILL.md) §Hard boundaries.
