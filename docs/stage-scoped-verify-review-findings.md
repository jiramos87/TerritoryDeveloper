# Stage-Scoped Verify + Review — Findings

**Date**: 2026-04-19
**Status**: Proposal — issues filed, implementation gated on sibling landing.
**Issues**: [TECH-518](../BACKLOG-ARCHIVE.md) (stage sizing gate) → [TECH-519](../BACKLOG-ARCHIVE.md) (ship-stage Pass 1/Pass 2 split).

## Problem

Current `/ship-stage` runs `implement → verify-loop → code-review` per-Task. For a Stage with N tasks:
- N× Unity Path A boot (~30-60s each cold).
- N× Path B preflight (~20-40s each).
- N× Opus code-review (spec + invariants + glossary reloaded each time).
- N× Sonnet verify-loop (redundant Bash output per run).

Stage-end `audit + closeout` already bulk; verify + review lag behind.

## Proposal

Two-issue split:

1. **TECH-518 — Stage sizing gate** (author-time). Heuristic in `stage-decompose` + `stage-file-planner` decides whether Stage task cluster is bulk-verify + bulk-review feasible. Fail → recurse split into X.A / X.B. Heuristics: subsystem cohesion, verify-path overlap (Path A scenario / Path B bridge cluster), diff LOC budget (~800 LOC / 25 files soft), depends-on DAG linearity, invariant-hotspot density. Not an N-cap.

2. **TECH-519 — `/ship-stage` Pass 1/Pass 2 split** (execution-time). Pass 1 per-Task (`implement` + `unity:compile-check` fast-fail only). Pass 2 Stage-end (verify-loop full Path A+B on cumulative delta → code-review Stage diff → audit → closeout). `--per-task-verify` legacy flag for rollback.

## Gains (N=4 reference Stage)

| Cost line | Current | Proposed | Delta |
|---|---|---|---|
| Opus code-review tokens | 4× full load | 1× shared-context load | ~45% |
| Sonnet verify-loop tokens | 4× | 1× | ~60% |
| Unity Path A boots | 4× (2-4min) | 1× (30-60s) | 1.5-3min wall |
| Path B preflights | 4× (1.5-2.5min) | 1× (20-40s) | 1-2min wall |
| Per-Task `unity:compile-check` | 0 | 4× (~60s total) | -1min (cost, not save) |

**Net per Stage**: ~40-50% token reduction, ~3.5-8min wall saved.

## Risks + mitigations

- **Late break discovery** — Stage-end verify surfaces first failing Task. Mitigation: per-Task `unity:compile-check` catches public-API / syntax / invariant-preflight breaks (~80% of failures) at ~15s each. Accept Unity-runtime-only breaks surfacing late.
- **Cluster fix-tuple anchor drift** — fix on Task A may shift lines in Task B's diff. Mitigation: `code-fix-applier` already resolves anchors at apply time (pair contract); single-Edit-per-tuple atomic.
- **Debug bisection harder** — Stage verify fail doesn't pinpoint Task. Mitigation: Task atomic commits preserved + git bisect at commit granularity.
- **Stage-decompose misjudging feasibility** — gate too loose = unmanageable cluster; too tight = churn. Mitigation: TECH-518 first + observe 2-3 Stages empirical before TECH-519 default-flip. Skill iteration log captures drift.

## Order

**Ship TECH-518 first.** Observe 2-3 Stages filed under new gate. Empirical data on cluster manageability before flipping TECH-519 execution flow. TECH-519 depends_on TECH-518.

Rollback path for TECH-519: `--per-task-verify` flag preserves current N× behavior.

## Alternative considered

`--batch-review` flag on `/ship-stage` (no sizing gate). Rejected: without feasibility gate upstream, bulk clusters grow unbounded; fix-applier blast radius degrades. Author-time gate is structurally cleaner than runtime flag.

## Surfaces touched (forward reference)

- `ia/rules/stage-sizing-gate.md` (new, TECH-518).
- `ia/skills/stage-decompose/SKILL.md` + `ia/skills/stage-file-plan/SKILL.md` (TECH-518 phase hook).
- `.claude/agents/ship-stage.md` + `ia/skills/ship-stage/SKILL.md` (TECH-519 Pass 1/2).
- `ia/skills/verify-loop/SKILL.md` (TECH-519 Stage-scoped input).
- `ia/skills/opus-code-review/SKILL.md` (TECH-519 Stage-diff input).
- `ia/rules/agent-lifecycle.md` (TECH-518 + TECH-519 flow diagram + hard-rule update).
