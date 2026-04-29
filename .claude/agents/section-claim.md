---
name: section-claim
description: Use to start parallel work on one master-plan section. Opens a git worktree at `../territory-developer.section-{section_id}` on branch `feature/{slug}-section-{section_id}`, takes the section claim row in `ia_section_claims`, and writes a `.parallel-section-claim.json` sentinel inside the worktree so downstream `/ship-stage` + `/section-closeout` calls can read the same `session_id`. Heartbeats happen externally — `/ship-stage` Pass A iterations call `claim_heartbeat` MCP. Does NOT close the section (= `/section-closeout`). Does NOT run any ship-stage work. Triggers - "/section-claim {SLUG} {SECTION_ID}", "claim section worktree".
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

Open parallel section worktree + take section claim for `{SLUG}` section `{SECTION_ID}`. Mechanical — no decisions. Heartbeats happen externally via `/ship-stage`.

# Recipe

Mechanical phases (worktree open, claim, sentinel write) run as recipe `section-claim` (`tools/recipes/section-claim.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.1. Invoke:

```
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id={SESSION_ID}
```

Optional `--input worktree_root={ABS_PATH}` + `--input base_branch={REF}` for non-default layout.

Recipe stops on first failure:

1. `open_worktree` — STOPs when path exists on different branch. Noop on same-branch re-entry.
2. `claim` — STOPs `section_claim_held` when held by another session. Same session = heartbeat refresh.
3. `write_sentinel` — writes `.parallel-section-claim.json` inside worktree. Idempotent.

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id (matches `ia_stages.section_id`). Required. |
| `SESSION_ID` | Stable id reused across `/ship-stage` + `/section-closeout`. Convention `section-claim-{SLUG}-{SECTION_ID}-{ISO8601_compact}`. |
| `WORKTREE_ROOT` | Optional override. Default `{repo_parent}/{repo_name}.section-{SECTION_ID}`. |
| `BASE_BRANCH` | Optional fork ref. Default = current HEAD. |

# Hard boundaries

- IF section claimed by another session → recipe `claim` step raises `section_claim_held`. Do not force.
- IF worktree path exists on different branch → `open_worktree` STOPs. Resolve clash manually.
- Do NOT run `/ship-stage` from this skill (caller invokes after recipe returns).
- Do NOT close the section (`/section-closeout` owns drift gate + merge + release).
- Do NOT commit.

# Next step

```
cd {worktree_root}
/ship-stage {SLUG} {SECTION_ID}.1
```

Sentinel carries `session_id` for `/ship-stage` Pass A `stage_claim` + `claim_heartbeat`.
