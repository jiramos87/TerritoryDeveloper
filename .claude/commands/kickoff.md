---
description: Review or enrich a project spec before implementation (Stage 4 stub — wires to spec-kickoff subagent)
argument-hint: "{ISSUE_ID} (e.g. TECH-85)"
---

# /kickoff — Stage 1 stub

This slash command is a **Stage 1 stub** for [TECH-85](../../ia/projects/TECH-85-ia-migration.md). The real implementation lands in **Stage 4 / Phase 4.3**, when it will dispatch the `spec-kickoff` subagent against `ia/projects/{ID}*.md`.

**Not yet wired — coming in Stage 4.**

For now, run the kickoff workflow inline using:

- [`.cursor/skills/project-spec-kickoff/SKILL.md`](../../.cursor/skills/project-spec-kickoff/SKILL.md)
- territory-ia MCP tools: `backlog_issue` → `router_for_task` → `spec_section` / `glossary_lookup`

Argument received: `$ARGUMENTS`
