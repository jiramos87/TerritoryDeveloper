# TECH-29 — Simulation tick call-order drift detector

> **Issue:** [TECH-29](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **5**. Phase **ids** must match **TECH-16** harness and `.cursor/specs/simulation-system.md` **Tick execution order**.

## 1. Summary

Maintain a **checked-in ordered manifest** of the five **simulation tick** steps (plus optional **GrowthBudget** when present) matching **simulation-system** **Tick execution order**. A **script** (Node, Python, or shell + `rg`) compares `SimulationManager.ProcessSimulationTick` (or equivalent) **call sequence** to that manifest and **fails CI** (or prints a clear diff) when code order diverges without a matching spec update.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Deterministic check runnable in CI and locally.
2. Error message cites **simulation-system** and **TECH-16** marker names.
3. Manifest is human-editable when spec changes.

### 2.2 Non-Goals (Out of Scope)

1. Proving behavioral equivalence of each step — **order only**.
2. Parsing full C# AST unless team prefers — heuristic extraction acceptable with tests on fixture snippets.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want accidental reorder caught in CI. | Drift fails build. |
| 2 | Maintainer | I want to update manifest when spec changes. | Single YAML/JSON file in `tools/`. |

## 4. Current State

### 4.1 Domain behavior

Canonical order (from spec): **EnsureBudgetValid** (optional) → **RecalculateFromGrid** (centroid + rings) → **AutoRoadBuilder** → **AutoZoningManager** → **AutoResourcePlanner**.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Code | `SimulationManager.cs` |
| Spec | `.cursor/specs/simulation-system.md` |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- `tools/sim-tick-order-manifest.json` — array of `{ "id": "...", "callee_hint": "..." }`.
- Script extracts ordered method calls from `ProcessSimulationTick` body (regex between braces or Roslyn).
- CI: optional advisory mode for first PR — **Decision Log**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Soft dependency on TECH-16 | Naming parity | Hard gate |

## 7. Implementation Plan

### Phase 1 — Manifest + extractor

- [ ] Add manifest file aligned to spec.
- [ ] Script prints diff.

### Phase 2 — CI

- [ ] Add workflow step or `npm run check:tick-order`.

## 8. Acceptance Criteria

- [ ] Intentional reorder in a test branch is detected.
- [ ] Manifest update process documented in this spec **§7**.
- [ ] **TECH-16** ProfilerMarker names referenced in manifest or mapping table.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; advisory vs blocking in **Decision Log**.
