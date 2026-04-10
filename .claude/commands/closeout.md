---
description: Close an issue end-to-end — umbrella close (Stage 4 stub — wires to closeout subagent)
argument-hint: "{ISSUE_ID} (e.g. TECH-85)"
---

# /closeout — Stage 1 stub

This slash command is a **Stage 1 stub** for [TECH-85](../../ia/projects/TECH-85-ia-migration.md). The real implementation lands in **Stage 4 / Phase 4.3**, when it will dispatch the `closeout` subagent (umbrella close — **not** per-stage close) with an explicit confirmation gate before destructive operations (per Q6 resolution).

**Not yet wired — coming in Stage 4.**

For now, run closeout inline using:

- [`.cursor/skills/project-spec-close/SKILL.md`](../../.cursor/skills/project-spec-close/SKILL.md) — umbrella close
- For **per-stage** closes during a multi-stage spec, use [`.cursor/skills/project-stage-close/SKILL.md`](../../.cursor/skills/project-stage-close/SKILL.md) instead.

Argument received: `$ARGUMENTS`
