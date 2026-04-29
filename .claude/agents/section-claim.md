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

Take V2 row-only section claim for `{SLUG}` section `{SECTION_ID}`. Pure DB mutex on `(slug, section_id)`. Same branch, same worktree — no git worktree, no per-section branch. Heartbeats happen externally via `/ship-stage`.

# Recipe

Mechanical phase (claim) runs as recipe `section-claim` (`tools/recipes/section-claim.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.1, V2 rewrite. Invoke:

```
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Recipe stops on first failure:

1. `claim` — STOPs `section_claim_held` only on concurrent INSERT race. Subsequent caller refreshes heartbeat (V2 row-only — section IS the holder).

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id (matches `ia_stages.section_id`). Required. |

V2 dropped: `SESSION_ID`, `WORKTREE_ROOT`, `BASE_BRANCH`. Section is the holder.

# Hard boundaries

- IF concurrent INSERT race → `section_claim_held`. Retry — second call refreshes heartbeat.
- Do NOT open git worktrees or branches — V2 same-branch same-worktree model.
- Do NOT write `.parallel-section-claim.json` — V2 dropped sentinel.
- Do NOT run `/ship-stage` from this skill (caller invokes after recipe returns).
- Do NOT close the section (`/section-closeout` owns drift gate + DB closeout).
- Do NOT commit.

# Next step

```
/ship-stage {SLUG} {SECTION_ID}.1
```

`/ship-stage` Pass A refreshes claim heartbeat per stage via `claim_heartbeat({slug, stage_id})` MCP.
