---
purpose: Compact self-review prose shape when the reviewer is the branch author (territory-developer IA-heavy branches)
audience: agent
loaded_by: ondemand
slices_via: none
description: Verdict-first self-review format, English kind tone, territory IA scan order — caveman default does not apply to this surface
alwaysApply: false
---

# Code review — self-review on your own branch

Use when reviewing a branch you authored (or when the user asks for the same compact shape). **Caveman style does not apply** to review prose — write normal English, kind and positive-first (same carve-out as PR bodies per `agent-output-caveman.md`).

## Shape

1. **Verdict** — one line + one reason.
2. **What went well** — 3–5 single-line bullets.
3. **Suggestions and questions** — no preamble, no PR recap, no trailing “want me to fix?” unless the user asked for remediation.

## Findings

- Format: `[severity] path:section — short summary` then one sentence problem + one sentence fix.
- Severities: `critical`, `major`, `minor`. Skip nits the linter or typecheck already catches.

## Territory scan order (priority)

1. `ia/skills/*/SKILL.md` — contract drift (id reservation, locks, batch vs single).
2. `ia/rules/*.md` — invariant drift (especially `HeightMap`, `GridManager`, roads, monotonic ids).
3. DB-backed master plans (`ia_master_plans` rows; render via `master_plan_render({slug})`) + tracker docs (`docs/*-rollout-tracker.md`) — orchestrator vs tracker; permanent-doc violations.
4. `docs/*-exploration.md` — locked decisions vs live code; duplicate ownership of the same subsystem across docs.
5. `tools/mcp-ia-server/src/**` — swallowed errors, schema-cache drift, hand-rolled parsers vs emitters.
6. `tools/scripts/*.sh` — `flock` on paths that mutate `ia/state/` or materialize `BACKLOG.md`.
7. `.claude/agents/*.md` — model pins, lifecycle alignment with `docs/agent-lifecycle.md`.

## Drop

Pre-existing issues not introduced by the branch, formatting the linter fixes, and generic quality complaints not pinned in `ia/rules/`.

## Ownership

Self-review does not auto-offer a remediation plan unless the user explicitly asks.
