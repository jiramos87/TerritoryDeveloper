---
purpose: "Thin always-loaded anchor for the agent lifecycle (exploration → close). Names every slash command + subagent + skill in one place and points at the canonical doc."
audience: agent
loaded_by: always
slices_via: none
description: "Canonical ordered flow for agents + slash commands + skills. Defers to docs/agent-lifecycle.md for the full matrix."
alwaysApply: true
---

# Agent lifecycle — canonical flow

Full canonical doc: [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) (flow diagram, stage → surface matrix, handoff contract, decision tree).

## Ordered flow (one issue end-to-end)

```
/design-explore  →  /master-plan-new  →  [/stage-decompose (re-decompose only)]  →  /stage-file  →  /project-new  →  /kickoff  →  /implement  →  /verify-loop  →  project-stage-close (skill, non-final stage)  →  /closeout
```

Single-issue path skips the first three stages: `/project-new → /kickoff → /implement → /verify-loop → /closeout`.

`/stage-decompose` is optional: run it when an already-decomposed step needs re-decomposition (scope change, design pivot). `master-plan-new` now fully decomposes ALL steps — no skeletons. Does NOT create BACKLOG rows.

## Surface map (one row per stage)

| Stage | Slash command | Subagent | Skill |
|-------|---------------|----------|-------|
| Explore | `/design-explore` | `design-explore` | `design-explore` |
| Orchestrate | `/master-plan-new` | `master-plan-new` | `master-plan-new` |
| Decompose step | `/stage-decompose` | `stage-decompose` | `stage-decompose` |
| Bulk-file stage | `/stage-file` | `stage-file` | `stage-file` |
| Single issue | `/project-new` | `project-new` | `project-new` |
| Refine | `/kickoff` | `spec-kickoff` | `project-spec-kickoff` |
| Implement | `/implement` | `spec-implementer` | `project-spec-implement` |
| Verify (single-pass) | `/verify` | `verifier` | composed |
| Verify (closed-loop) | `/verify-loop` | `verify-loop` | `verify-loop` |
| Test-mode ad-hoc | `/testmode` | `test-mode-loop` | `agent-test-mode-verify` |
| Close stage | *(none)* | *(none)* | `project-stage-close` |
| Close issue | `/closeout` | `closeout` | `project-spec-close` |

## Hard rules

- **`/verify` vs `/verify-loop`** — `/verify` = single pass, read-only, no fix iteration. `/verify-loop` = 7-step closed loop with bounded fix iteration (`MAX_ITERATIONS` default 2).
- **Orchestrator docs are permanent.** `master-plan-new` output (`ia/projects/{slug}-master-plan.md`) is NEVER closeable via `/closeout`. See [`ia/rules/orchestrator-vs-spec.md`](orchestrator-vs-spec.md).
- **Stage close ≠ umbrella close.** `project-stage-close` skill ticks one stage inside a multi-stage spec (no BACKLOG or spec-file changes). `/closeout` is the umbrella close (deletes spec, archives row, purges id).
- **Verification policy is single canonical.** All verify agents defer to [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). Do not restate timeout escalation, Path A lock release, or Path B preflight in skill / agent / command bodies.
- **Handoff artifact required per stage.** Missing artifact → next stage refuses to start. Full contract: `docs/agent-lifecycle.md` §3.
- **Monotonic ids per prefix.** `BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-` ids never reused across BACKLOG + BACKLOG-ARCHIVE ([`AGENTS.md`](../../AGENTS.md) §7).

## Authoritative neighbors

- [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) — full canonical doc.
- [`ia/rules/project-hierarchy.md`](project-hierarchy.md) — step > stage > phase > task.
- [`ia/rules/orchestrator-vs-spec.md`](orchestrator-vs-spec.md) — permanent vs temporary split.
- [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md) — Verification block policy.
- [`AGENTS.md`](../../AGENTS.md) §2 — lifecycle entry for human-facing agents.
- [`CLAUDE.md`](../../CLAUDE.md) §3 — Claude Code host surface (hooks, subagents, commands).
