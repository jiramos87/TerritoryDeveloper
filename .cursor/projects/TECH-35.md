# TECH-35 — Research spike: property-based invariant fuzzing (optional)

> **Issue:** [TECH-35](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **38**. **High setup cost**; schedule only if geometric / ordering bugs justify.

**Spec pipeline program:** [TECH-60](TECH-60.md) lists this issue as an optional **prerequisite** for **invariant** fuzzing beyond **Node**-side checks — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md).

## 1. Summary

Explore **property-based** or **randomized mutation** tests that assert **invariants** from [`.cursor/rules/invariants.mdc`](../../.cursor/rules/invariants.mdc): e.g. **HeightMap** / **cell** height sync, **InvalidateRoadCache** after **road** edits, **shore band** constraints, **H_bed** monotonicity along **rivers**. Deliverable is a **spike report** (this spec **Decision Log** + optional prototype under `Tests/` or `tools/`) recommending whether to promote to full **TECH-** issue or abandon.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Time-box (e.g. 2–3 days) documented in **Implementation Plan**.
2. List which invariants are **testable** without full Unity play (pure helpers vs integration).
3. Prototype **one** predicate on synthetic small grids if feasible.

### 2.2 Non-Goals (Out of Scope)

1. Production fuzzing in player builds.
2. Covering all invariants in this spike.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Tech lead | I want a go/no-go for heavy test investment. | Spike doc with recommendation. |

## 4. Current State

### 4.1 Domain behavior

Predicates must use **glossary** terms; violations reference invariant text.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Rules | `invariants.mdc` |
| Related | **TECH-31** fixtures, **TECH-16** harness |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Evaluate FsCheck-style (C#) vs custom random grid mutator.
- Prefer tests on **pure** static helpers if extracted; avoid **MonoBehaviour** in first spike if too heavy.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spike-only issue | Cost | Full suite |

## 7. Implementation Plan

- [ ] Read **invariants**; shortlist 3 predicates.
- [ ] Prototype one; measure effort.
- [ ] Write recommendation: promote / defer / abandon.

## 8. Acceptance Criteria

- [ ] **Decision Log** contains recommendation with rationale.
- [ ] If prototype merged: runs in CI or locally with one command; documented.
- [ ] No regression to existing tests.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — research tooling; game **rules** for edge cases remain in reference specs, not this spike.
