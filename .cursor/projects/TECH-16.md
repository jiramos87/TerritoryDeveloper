# TECH-16 — Simulation tick harness, ProfilerMarkers, and profiling outputs

> **Issue:** [TECH-16](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **4**, **22**, **25**. **TECH-29** consumes stable phase **ids** from this work.

**Spec pipeline program:** [TECH-60](TECH-60.md) lists this issue as a **prerequisite** for **simulation tick** harness **JSON** and **UTF**-oriented validation — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md).

## 1. Summary

Provide a **simulation tick harness** that runs **N** `ProcessSimulationTick()` iterations on a fixed scene/seed and writes **JSON** with phases labeled to match **Tick execution order** in `.cursor/specs/simulation-system.md`: (1) `GrowthBudgetManager.EnsureBudgetValid` (when present), (2) `UrbanCentroidService.RecalculateFromGrid`, (3) `AutoRoadBuilder`, (4) `AutoZoningManager`, (5) `AutoResourcePlanner`. Add **`ProfilerMarker`** / marker names mirroring those steps for deep profiling and optional post-run grouping (**task 25**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. JSON report: per-tick or aggregated wall time and optional GC per **spec-labeled** phase.
2. **ProfilerMarker** names stable enough for **TECH-29** manifest and human profiler search.
3. Optional: same harness attaches **validation_samples** (read-only) as in **TECH-15** spec pattern.

### 2.2 Non-Goals (Out of Scope)

1. Changing **AUTO** gameplay or **tick execution order** (order changes are separate bugs/specs).
2. Full replacement of Unity Profiler — summary JSON first.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want to see which **AUTO** step costs time each tick. | JSON lists all five steps with timings. |
| 2 | Developer | I want **Deep Profile** to show the same names. | Markers exist in code for each step. |

## 4. Current State

### 4.1 Domain behavior

**Urban centroid** and **growth rings** (**sim §Rings**) are recomputed each tick before road/zoning steps — harness must not assume a different order.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Code | `SimulationManager.cs` — `ProcessSimulationTick` |
| Spec | `.cursor/specs/simulation-system.md` — **Tick execution order** |

### 4.3 Implementation investigation notes (optional)

- territory-ia **`spec_section`** `sim` + tick execution order for canonical numbering.

## 5. Proposed Design

### 5.1 Target behavior (product)

No change when harness is disabled.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Editor or test assembly: enter play mode, advance **N** ticks, write `sim-tick-profile-{timestamp}.json`.
- Suggested marker names: `SimTick.EnsureBudget`, `SimTick.RecalculateCentroidRings`, `SimTick.AutoRoadBuilder`, `SimTick.AutoZoning`, `SimTick.AutoResourcePlanner` (exact strings in Decision Log once chosen).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Phase ids must match simulation spec order | **TECH-29** drift detector | Ad-hoc names |

## 7. Implementation Plan

### Phase 1 — Harness + JSON

- [ ] Implement N-tick run and JSON output to `tools/reports/`.
- [ ] Label five phases per spec.

### Phase 2 — ProfilerMarker

- [ ] Wrap each step in `ProfilerMarker` or `AutoProfiler` pattern.
- [ ] Document marker names in this spec and **TECH-29** manifest source.

### Phase 3 — Optional invariant samples

- [ ] Optional JSON block for read-only checks (shared pattern with **TECH-15**).

## 8. Acceptance Criteria

- [ ] JSON `phases` include all five **tick execution order** steps (skip budget phase only when `GrowthBudgetManager` absent — document in output).
- [ ] **Unity:** Run harness on a small scene; no exception; **simulation tick** still advances correctly.
- [ ] **TECH-29** can import or duplicate the ordered phase id list without ambiguity.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; acceptance in §8.
