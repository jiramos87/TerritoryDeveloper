---
name: stage-file
description: DB-backed single-skill filing. Loads shared Stage MCP bundle once; gates cardinality (≥2 Tasks per Stage) + sizing (H1–H6); batch-verifies Depends-on ids via single `backlog_list`; resolves target BACKLOG.md section from master-plan H1 title; per-Task writes via `task_insert` MCP tool (DB-backed monotonic id from per-prefix sequence — no reserve-id.sh); appends manifest entry to `ia/state/backlog-sections.json`; bootstraps task spec body in DB via `task_spec_section_write`; runs `materialize-backlog.sh` (DB source default) — exit code is the filing gate; atomic task-table flip + R1/R2 Status flips. No yaml file written under `ia/backlog/`. Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks". Argument order (explicit): SLUG first, STAGE_ID second.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__backlog_list, mcp__territory-ia__backlog_record_validate, mcp__territory-ia__backlog_search, mcp__territory-ia__spec_outline, mcp__territory-ia__list_specs, mcp__territory-ia__invariant_preflight, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_change_log_append, mcp__territory-ia__task_insert, mcp__territory-ia__task_spec_section_write, mcp__territory-ia__lifecycle_stage_context
model: opus
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring). Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Run [`ia/skills/stage-file/SKILL.md`](../../ia/skills/stage-file/SKILL.md) end-to-end for target Stage. Recipe owns Phases 0–6; subagent owns arg parse, recipe dispatch, halt-handling, post-recipe passes, return shape.

# Recipe

1. **Parse args** — 1st = `SLUG`; 2nd = `STAGE_ID`; opt 3rd = `ISSUE_PREFIX` (default `TECH`).
2. **Dispatch recipe** — inputs JSON → `npm run recipe:run -- stage-file --inputs <path>`. Exit 0 → `{mode, filed_count, target_section, materialize_status}`.
3. **Handle halts** — `mode_detect` no-op → exit clean. `cardinality` PAUSE → prompt user. `sizing` FAIL → `/stage-decompose`. `manifest_resolve` ambiguous → prompt + re-dispatch with `target_section`. Other → escalate.
4. **Batch deps verify (pre-recipe)** — `stage_render` + one `backlog_list`; unresolvable → HALT.
5. **Post-recipe: deps register** — `task_dep_register` per Task with deps (Tarjan cycle check).
6. **Post-recipe: raw_markdown** — `task_raw_markdown_write` per Task.
7. **Post-recipe: R1/R2 flips** — `master_plan_preamble_write` if preamble Status = Draft.
8. **Return** — caveman block to dispatcher.

# Hard boundaries

- Do NOT bypass recipe — `tools/recipes/stage-file.yaml` owns Phases 0–6.
- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — `task_insert` MCP owns id assignment.
- Do NOT read or edit master-plan markdown on disk — DB only.
- Do NOT reorder Tasks — recipe `pending_q` ORDER BY task_id ASC is canonical.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase, reason, failed_step?, ...}` — Triggers: cardinality PAUSE, sizing FAIL, manifest ambiguous, dep unresolvable, dep cycle, `task_insert` error, materialize non-zero.

# Output

Caveman block to dispatcher: `stage-file done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K} Section: ... Materialize: ... next=stage-file-chain-continue`. On escalation: JSON `{escalation: true, phase, reason, ...}`.
