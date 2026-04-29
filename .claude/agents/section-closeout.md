---
name: section-closeout
description: Use to close a parallel section after all member stages are done. Runs intra-plan arch_drift_scan (blocks on any open drift), calls section_closeout_apply (asserts all stages done + writes change_log row section_done + releases claims), then merges the section branch into base_branch and removes the git worktree. Read `.parallel-section-claim.json` in the worktree for session_id. Does NOT re-ship stages. Does NOT reopen claim. Triggers - "/section-closeout {SLUG} {SECTION_ID}", "close section worktree", "merge section branch".
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

Close parallel section `{SLUG}` section `{SECTION_ID}`. Mechanical: drift gate → DB closeout + claim release → git merge + worktree remove.

# Recipe

Mechanical phases run as recipe `section-closeout` (`tools/recipes/section-closeout.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.2. Read session_id from sentinel first:

```bash
SESSION_ID=$(cat {worktree}/.parallel-section-claim.json | jq -r '.session_id')
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id="$SESSION_ID" \
  --input base_branch={BASE_BRANCH}
```

Recipe stops on first failure:

1. `drift_scan` — `arch_drift_scan(scope=intra-plan)`. Returns `{affected_stages[]}`.
2. `drift_gate` — bash assert: 0 affected stages. STOP when any drift found.
3. `closeout_apply` — `section_closeout_apply` MCP. STOP when stages not all done.
4. `git_merge` — merge `feature/{SLUG}-section-{SECTION_ID}` → `{BASE_BRANCH}` + `git worktree remove`.

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id. Required. |
| `SESSION_ID` | From `.parallel-section-claim.json` sentinel in worktree. Required. |
| `BASE_BRANCH` | Target merge branch. Must be current branch in main worktree. Required. |
| `ACTOR` | Optional. For change_log row. |
| `WORKTREE_ROOT` | Optional override. Default `{repo_parent}/{repo_name}.section-{SECTION_ID}`. |

# Hard boundaries

- IF drift found → STOP. Resolve drift, re-run `/arch-drift-scan`, retry.
- IF any section stage not done → STOP. Ship remaining stages first.
- IF main worktree not on base_branch → STOP. `git checkout {BASE_BRANCH}` + retry.
- Do NOT re-ship stages (= `/ship-stage`).
- Do NOT reopen claim (= `/section-claim` from scratch).
