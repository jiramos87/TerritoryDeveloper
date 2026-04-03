# TECH-32 — Urban centroid / growth rings recompute what-if (research tooling)

> **Issue:** [TECH-32](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **24**. Coordinates with **FEAT-43** / **FEAT-36** tuning.

## 1. Summary

Build **offline or Editor** experiments comparing **UrbanCentroidService.RecalculateFromGrid** strategies: **full recompute every simulation tick** vs **throttled** (every K ticks) vs **incremental approximation** (if any). Output a **report** (JSON/CSV) quantifying cost and **desync metrics** vs baseline full recompute — without changing shipped gameplay until **FEAT-43** / design approves.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Reproducible runs on fixed seed + scene.
2. Metrics: wall time per tick, optional max drift in ring assignments vs baseline (define drift in **Decision Log**).
3. Non-player-facing; no change to default game without follow-up issue.

### 2.2 Non-Goals (Out of Scope)

1. Shipping throttled recompute in production in this issue.
2. Replacing **simulation-system** definitions of **urban centroid** / **growth rings**.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Designer | I want evidence for **FEAT-43** tuning. | Report compares strategies on same seed. |
| 2 | Developer | I want numbers before optimizing **TECH-16**. | JSON includes per-tick timings. |

## 4. Current State

### 4.1 Domain behavior

**sim §Rings:** each tick, rings recomputed from weighted center; **AutoRoadBuilder** / **AutoZoningManager** use them — drift may affect **AUTO** growth patterns; report must flag behavioral risk.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Code | `UrbanCentroidService.cs`, `AutoRoadBuilder`, `AutoZoningManager` |
| Spec | `simulation-system.md` — **Urban centroid and growth rings** |

## 5. Proposed Design

### 5.1 Target behavior (product)

Default game unchanged.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Fork or instrument service behind `#if UNITY_EDITOR` or dedicated test assembly.
- Run N ticks × M strategies; aggregate.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Research-only | Risk to **AUTO** | Production throttle |

## 7. Implementation Plan

- [ ] Define drift metric (e.g. Hamming distance on ring bucket per cell).
- [ ] Implement comparison harness.
- [ ] Document results path under `tools/reports/`.

## 8. Acceptance Criteria

- [ ] Report generated on reference scene; methodology documented.
- [ ] Explicit “not for merge to gameplay” until product sign-off in **FEAT-43** or new issue.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

If drift metric implies a **definition** of acceptable **AUTO** bias, escalate to **FEAT-43** / product owner — this spec only implements **measurement** options.
