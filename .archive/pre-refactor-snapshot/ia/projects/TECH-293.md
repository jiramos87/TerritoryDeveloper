---
purpose: "TECH-293 — Tick budget EditMode test."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-293 — Tick budget EditMode test

> **Issue:** [TECH-293](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

New EditMode test `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs` invokes isolated `SimulationManager` tick; asserts completion under configured budget threshold (ms sourced from `docs/city-tick-perf-notes.md` baseline). Threshold constant documents Step 3 **parity budget** target. Baseline recorded for Step 3 parity harness.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `TickBudgetTests.cs` (new) — EditMode test invoking isolated tick path.
2. Threshold constant references perf-notes baseline (w/ headroom factor, e.g. 1.5×).
3. Test annotation documents Step 3 parity target (one dormant + one active city credible).
4. `unity:testmode-batch` picks up new test.
5. Compile clean.

### 2.2 Non-Goals

1. Parity harness itself — Step 3 owns.
2. Perf baseline capture — TECH-290.
3. Allocator patch — TECH-291.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Catch tick regressions in CI before Step 3 parity harness lands | `TickBudgetTests` fails if tick exceeds threshold |

## 4. Current State

### 4.1 Domain behavior

No EditMode perf test exists for tick path. Perf regressions caught only by manual profiler runs.

### 4.2 Systems map

- `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs` (new).
- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — unit under test (isolated tick invocation).
- `docs/city-tick-perf-notes.md` — threshold source.
- Reference: `ia/specs/simulation-system.md` §tick-loop.

## 5. Proposed Design

### 5.1 Target behavior (product)

No runtime behavior change. Regression gate only.

### 5.2 Architecture / implementation

1. Set up EditMode test harness w/ minimal grid (small N × N, no rendering).
2. Inject tick-path manager mocks / real scene components as needed.
3. Warm up (≥5 ticks) then sample median ms across ≥20 ticks.
4. Assert median ≤ threshold constant (e.g. `BUDGET_MS_MEDIAN = 2.5 * baseline_from_notes`).
5. XML doc on threshold constant cites perf notes + Step 3 parity target.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Median across ≥20 sampled ticks | Single-tick variance too noisy in EditMode | Max-ms — flaky; mean — GC outliers skew |
| 2026-04-17 | 1.5–2.5× baseline as threshold | Headroom for dev-machine variance | Exact baseline — brittle |

## 7. Implementation Plan

### Phase 1 — EditMode test

- [ ] Scaffold `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs`.
- [ ] Wire minimal tick invocation; warmup + measurement loop.
- [ ] Threshold constant w/ perf-notes citation + Step 3 target doc comment.
- [ ] `npm run unity:testmode-batch` — test discovered + passes.
- [ ] `unity:compile-check` clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| EditMode test passes | Testmode | `npm run unity:testmode-batch` | EditMode runner |
| Compile clean | Unity | `npm run unity:compile-check` | |
| IA consistency | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `TickBudgetTests.cs` lands under `Assets/Tests/EditMode/Simulation/`.
- [ ] Threshold constant cites `docs/city-tick-perf-notes.md` baseline.
- [ ] XML doc notes Step 3 parity target.
- [ ] Test passes w/ current code under `unity:testmode-batch`.
- [ ] `validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — tooling only; threshold ratio + sample count tunable in Decision Log.
