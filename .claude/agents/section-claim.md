---
name: section-claim
description: Use to start parallel work on one master-plan section. Inserts (or refreshes) the row in `ia_section_claims` keyed by `(slug, section_id)`. V2 row-only — no holder identity, no worktree, no new branch. Concurrent INSERT race throws `section_claim_held`; any subsequent caller refreshes the open row. Heartbeats happen externally — `/ship-stage` Pass A iterations call `claim_heartbeat` MCP. Background sweep (`claims_sweep` MCP) releases stale rows past `carcass_config.claim_heartbeat_timeout_minutes`. Does NOT close the section (= `/section-closeout`). Does NOT run any ship-stage work. Triggers - "/section-claim {SLUG} {SECTION_ID}", "claim section row".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__section_claim, mcp__territory-ia__master_plan_locate
model: inherit
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission
V2 row-only section claim — DB mutex `(slug, section_id)`; heartbeats external via `/ship-stage`.
# Recipe
`tools/recipes/section-claim.yaml`. CLI: `npm run recipe:run -- section-claim --input slug={SLUG} --input section_id={SECTION_ID}`.
# Hard boundaries
INSERT race → `section_claim_held` (retry refreshes); no git worktree/branch (V2 same-branch); no `/ship-stage` from here; no section close (`/section-closeout`); no commit.
