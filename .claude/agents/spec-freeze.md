---
name: spec-freeze
description: Single-step skill: calls master_plan_spec_freeze MCP with {slug, source_doc_path}. Fails fast if open_questions_count > 0. Emits frozen artifact path + spec_id to user. Bypass via --skip-freeze logs arch_changelog kind=spec_freeze_bypass. Prerequisite for /ship-plan Phase A gate.
tools: Read, Edit, Glob, mcp__territory-ia__master_plan_spec_freeze, mcp__territory-ia__cron_arch_changelog_append_enqueue
model: sonnet
reasoning_effort: low
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Run `ia/skills/spec-freeze/SKILL.md` end-to-end for plan slug `{SLUG}`.

# Phase sequence

1. **Phase 1 — Resolve source path.** Default `docs/explorations/{SLUG}.md`. Override via `--source {path}`.
2. **Phase 2 — Call `master_plan_spec_freeze` MCP.** Args: `{slug, source_doc_path, version, force}`. `force=true` when `--skip-freeze` flag present.
3. **Phase 3 — Emit result.** On success: `spec-freeze done. SLUG=... SPEC_ID=... FROZEN_AT=... Next: /ship-plan {SLUG}`. On bypass: warn open_questions_count + `Next: /ship-plan {SLUG} --skip-freeze`.

# Hard boundaries

- Do NOT proceed to ship-plan — this skill is a gate only.
- Do NOT freeze if open_questions_count > 0 unless `--skip-freeze` flag explicitly provided.
