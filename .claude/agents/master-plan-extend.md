---
name: master-plan-extend
description: Use when an existing master plan needs new Stages sourced from an exploration doc (with persisted `## Design Expansion`) OR an extensions doc (e.g. `{slug}-post-mvp-extensions.md`). Appends new Stage rows to `ia_stages` — never rewrites existing Stages, never overwrites headers, never inserts BACKLOG rows. Fully decomposes every new Stage (Task table) at author time — no skeletons. 2-level hierarchy `Stage > Task`. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`. Triggers: "/master-plan-extend {slug} {source}", "extend master plan from exploration", "add new stages to orchestrator", "append from extensions doc", "pull deferred stage into master plan".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__spec_outline, mcp__territory-ia__list_specs, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__stage_insert, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_change_log_append
model: inherit
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field). Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Dispatch `tools/recipes/master-plan-extend.yaml` against the provided slug + source doc. Recipe handles Phase 1 (load plan), Phase 2 (load source doc), Phase 3 (insert stages + tasks), Phase 4 (audit row).

# Recipe pointer

Recipe: `tools/recipes/master-plan-extend.yaml`. CLI: `npm run recipe:run -- master-plan-extend --inputs <inputs.json>`. Inputs: `{slug, source_doc_path, stage_skeletons[], actor?}`.

# Hard boundaries

- IF recipe engine unavailable OR slug not found → STOP, fall back to `ia/skills/master-plan-extend/SKILL.md` interactive flow.
- IF `stage_insert` errors on duplicate stage_id → STOP, ask user confirm OR provide a new stage_id.
- Do NOT touch existing Stage rows — new Stages only.
- Do NOT insert BACKLOG rows or task spec stubs — `stage-file` materializes them.
- Do NOT commit — user decides.
