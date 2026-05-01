---
name: master-plan-new
description: Use when an exploration doc under `docs/` carries a persisted `## Design Expansion` block and the work needs a multi-stage plan rather than a single BACKLOG issue. Produces `ia_master_plans` row + `ia_stages` rows (orchestrator is permanent — never closeable, never deleted by automation) with ALL Stages fully decomposed into Tasks (2-level hierarchy: `Stage > Task`). Tasks seeded `_pending_` for later `stage-file`. Canonical shape authority: `docs/MASTER-PLAN-STRUCTURE.md` — file shape, Stage block shape, 5-column Task table schema, Status enums, flip matrix. Triggers: "/master-plan-new {path}", "turn expanded design into master plan", "create orchestrator from exploration", "author master plan from design expansion".
tools: Read, Bash, mcp__territory-ia__master_plan_insert, mcp__territory-ia__stage_insert, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_description_write, mcp__territory-ia__master_plan_change_log_append
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

Dispatch `tools/recipes/master-plan-new.yaml` against the provided exploration doc. Recipe handles Phase A (ratify + seed arch_decisions), Phase B (preamble + description), Phase C (stage decomposition).

# Recipe pointer

Recipe: `tools/recipes/master-plan-new.yaml`. CLI: `npm run recipe:run -- master-plan-new --inputs <fixture.json>`. Inputs: `{slug, title, description, preamble, arch_decisions[], stage_skeletons[]}`.

# Hard boundaries

- IF recipe engine unavailable OR exploration doc shape unparseable → STOP, fall back to `ia/skills/master-plan-new/SKILL.md` interactive flow.
- IF `master_plan_insert` errors on duplicate slug → STOP, ask user confirm overwrite OR new slug.
- Do NOT commit — user decides.
