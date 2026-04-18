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

`/master-plan-extend` is the append-only companion to `/master-plan-new`: run it when an existing orchestrator needs new Steps sourced from a fresh exploration doc or an extensions doc (`{slug}-post-mvp-extensions.md`). Never rewrites existing Steps. Full decomposition of every new Step at author time (same cardinality gate as `/master-plan-new`). **Pre-condition:** source doc must have a `## Design Expansion` block (or semantic equivalent). Missing block → `master-plan-extend` Phase 0 stops and routes to `/design-explore {SOURCE_DOC}` — if the source doc is a locked design, add `--against {UMBRELLA_DOC}` to run gap-analysis mode first.

`/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` is a stage-scoped chain dispatcher sitting BETWEEN `/verify-loop` and `project-stage-close`: run it when a master plan Stage X.Y has ≥1 non-Done filed task row and you want one command to drive all of them through kickoff → implement → verify-loop (per-task Path A; `--skip-path-b`) → closeout, then run one batched Path B at stage end. Emits a chain-level stage digest (separate from per-spec `project-stage-close` which fires inside each inner `spec-implementer` unchanged). Next-stage handoff auto-resolved for all 4 cases. Per-task Path A is mandatory; batched Path B runs once at stage end on cumulative delta. Chain stops on first per-task gate failure (no continue-on-error).

`/release-rollout` is an umbrella-level driver ABOVE the single-issue flow: run it when a multi-bucket umbrella master-plan (e.g. `full-game-mvp-master-plan.md`) has a sibling rollout tracker (`ia/projects/{umbrella-slug}-rollout-tracker.md`) and needs to advance one row through the 7-column lifecycle (a)–(g) toward step (f) ≥1-task-filed. Dispatches to the same single-issue commands (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`) per target cell. Does NOT close issues (= `/closeout`). Tracker is seeded by `release-rollout-enumerate` helper (one-shot per umbrella).

## Surface map (one row per stage)

| Stage | Slash command | Subagent | Skill | Model |
|-------|---------------|----------|-------|-------|
| Explore | `/design-explore` | `design-explore` | `design-explore` | Opus |
| Orchestrate | `/master-plan-new` | `master-plan-new` | `master-plan-new` | Opus |
| Extend orchestrator | `/master-plan-extend` | `master-plan-extend` | `master-plan-extend` | Opus |
| Decompose step | `/stage-decompose` | `stage-decompose` | `stage-decompose` | Opus |
| Bulk-file stage | `/stage-file` | `stage-file` | `stage-file` | Opus |
| Single issue | `/project-new` | `project-new` | `project-new` | Opus |
| Refine | `/kickoff` | `spec-kickoff` | `project-spec-kickoff` | Opus |
| Implement | `/implement` | `spec-implementer` | `project-spec-implement` | Sonnet |
| Verify (single-pass) | `/verify` | `verifier` | composed | Sonnet |
| Verify (closed-loop) | `/verify-loop` | `verify-loop` | `verify-loop` | Sonnet |
| Test-mode ad-hoc | `/testmode` | `test-mode-loop` | `agent-test-mode-verify` | Sonnet |
| Stage-scoped chain ship | `/ship-stage` | `ship-stage` | `ship-stage` | Opus |
| Rollout umbrella | `/release-rollout` | `release-rollout` | `release-rollout` (+ `-enumerate`, `-track`, `-skill-bug-log` helpers) | Opus |
| Close stage | *(none)* | *(none)* | `project-stage-close` | — |
| Close issue | `/closeout` | `closeout` | `project-spec-close` | Opus |

## Hard rules

- **`design-explore --against` for locked docs** — when `/design-explore` is called on a doc that has locked decisions but no Approaches list, pass `--against {REFERENCE_DOC}` (path to umbrella orchestrator or master plan) to activate gap-analysis mode. Without it the skill stops and asks. Useful when a child orchestrator's exploration doc needs alignment-checking against the full-game MVP or any umbrella before `/master-plan-extend`.
- **`/ship-stage` vs `/ship`** — after `/stage-file` files ≥2 tasks in a Stage X.Y, default next step is `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` (chains all tasks kickoff → implement → verify-loop `--skip-path-b` → closeout; batched Path B at stage end). Fall back to `/ship {ISSUE_ID}` for single-task stages and standalone issues (no master plan).
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
