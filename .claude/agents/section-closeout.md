---
name: section-closeout
description: Use to close a parallel section after all member stages are done. Runs intra-plan arch_drift_scan (blocks on any open drift), calls section_closeout_apply (asserts all stages done + writes change_log row section_done + releases section + cascade-releases stage claims by row key alone). V2 row-only — no session_id, no git merge, no worktree teardown. Same branch + same worktree model. Does NOT re-ship stages. Does NOT reopen claim. Triggers - "/section-closeout {SLUG} {SECTION_ID}", "close section", "release section claim".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__arch_drift_scan, mcp__territory-ia__section_closeout_apply, mcp__territory-ia__master_plan_locate
model: inherit
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Close parallel section `{SLUG}` section `{SECTION_ID}` (V2 row-only). Mechanical: drift gate → DB closeout + claim release. Same branch, same worktree — no merge step.

# Recipe

Mechanical phases run as recipe `section-closeout` (`tools/recipes/section-closeout.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.2, V2 rewrite.

```bash
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Optional `--input actor={ACTOR}` + `--input commit_sha={SHA}` for change_log row.

Recipe stops on first failure:

1. `drift_scan` — `arch_drift_scan(scope=intra-plan)`. Returns `{affected_stages[]}`.
2. `drift_gate` — bash assert: 0 affected stages. STOP when any drift found.
3. `closeout_apply` — `section_closeout_apply` MCP. STOP when stages not all done. Releases section + stage claims by row key alone (V2 row-only).

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id. Required. |
| `ACTOR` | Optional. For change_log row. |
| `COMMIT_SHA` | Optional. For change_log row. |

V2 dropped: `SESSION_ID`, `BASE_BRANCH`, `WORKTREE_ROOT`.

# Hard boundaries

- IF drift found → STOP. Resolve drift, re-run `/arch-drift-scan`, retry.
- IF any section stage not done → STOP. Ship remaining stages first.
- Do NOT re-ship stages (= `/ship-stage`).
- Do NOT reopen claim (= `/section-claim` from scratch).
- Do NOT open worktrees, branches, or merge — V2 same-branch same-worktree.
- Do NOT commit — V2 dropped the merge commit step.
