---
purpose: "TECH-109 — Testmode smoke: stub placed + binding after interstate build."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-109 — Testmode smoke: stub at border after new-game + binding intact after interstate build

> **Issue:** [TECH-109](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.3 Phase 4 closer + regression gate. Testmode scenario: new-game places ≥1 stub; scripted interstate to border triggers binding; assert stub exists + binding resolves via `GridManager.GetNeighborStub`. Closes Stage 1.3 exit (≥1 stub per city + inert read API verified live).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Testmode scenario exercises new-game → stub present.
2. Scripted interstate-to-border → binding present.
3. `GridManager.GetNeighborStub(boundSide)` returns stub (non-null).
4. City sim tick runs zero exceptions (no regression).

### 2.2 Non-Goals

1. Multi-stub / multi-border scenarios (MVP = 1 stub).
2. Flow-consumer validation (post-MVP).

## 4. Current State

### 4.2 Systems map

- Testmode batch scenario (`AgentTestModeBatchRunner`).
- Depends: TECH-104 (seed), TECH-106 (read API). Binding recorder shipped w/ closed sibling.
- Orchestrator: Stage 1.3 exit rollup.

## 7. Implementation Plan

### Phase 1 — Smoke

- [ ] Author testmode scenario covering new-game + scripted interstate build.
- [ ] Assertions: stub count ≥1, binding present, `GetNeighborStub(boundSide) != null`, ≥1 sim tick exception-free.
- [ ] Exit 0 + report.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Stub + binding live | Testmode | `npm run unity:testmode-batch --scenario neighbor-stub-smoke` | exit 0, zero exceptions |

## 8. Acceptance Criteria

- [ ] Testmode scenario exit 0.
- [ ] `stub_count >= 1` post-new-game; binding present post-interstate-build.
- [ ] `GetNeighborStub(boundSide)` non-null.
- [ ] Zero C# exceptions across run.
- [ ] Report artifact attached.
- [ ] `validate:all` green.

## Open Questions

1. None — verification scenario; closes Stage 1.3.
