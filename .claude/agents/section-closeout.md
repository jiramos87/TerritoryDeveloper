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
V2 row-only section close — drift_scan + drift_gate + closeout_apply; same branch, no merge.
# Recipe
`tools/recipes/section-closeout.yaml`. CLI: `npm run recipe:run -- section-closeout --input slug={SLUG} --input section_id={SECTION_ID}` (optional `--input actor={ACTOR} --input commit_sha={SHA}`).
# Hard boundaries
Drift found → STOP (re-run `/arch-drift-scan`); stages not done → STOP (ship first); no re-ship, no reopen claim, no worktree/branch/merge (V2 same-branch), no commit.
