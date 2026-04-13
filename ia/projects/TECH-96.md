---
purpose: "TECH-96 — Testmode smoke: city load + sim tick, no regression after cell-type split."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-96 — Testmode smoke: city load + sim tick regression gate

> **Issue:** [TECH-96](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 4 — regression gate: run testmode batch smoke scenario after full cell-type split (TECH-90 through TECH-95). Confirms city load + at least one sim tick complete without errors. Zero behavior regression.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Testmode batch scenario loads city save + runs sim tick; no Unity errors or exceptions.
2. Result confirms cell-type split (TECH-90–95) introduced zero behavior regression.

### 2.2 Non-Goals (Out of Scope)

1. `HeightMap` / `CityCell.height` integrity assertion — that is TECH-97.
2. Any new test scenario content; reuse existing smoke scenario.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Automated smoke confirms refactor did not break city sim | Testmode passes; no errors in console |

## 4. Current State

### 4.1 Domain behavior

Post-TECH-95: `CityCell` rename + typed `GetCell` in place. City sim should run identically to pre-refactor. Testmode batch infrastructure exists (TECH-89 established round-trip scenario).

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `npm run unity:testmode-batch` | Test runner |
| Existing smoke scenario (TECH-89 scenario or equivalent) | Scenario to reuse/extend |
| `ia/skills/agent-test-mode-verify/SKILL.md` | Verification policy |
| `docs/agent-led-verification-policy.md` | Canonical verification policy |

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. Testmode passes green.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Run `npm run unity:testmode-batch` with appropriate scenario flag. Confirm: no C# exceptions, city load succeeds, one sim tick fires without error.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Reuse existing smoke scenario | No new behavior introduced; regression gate is sufficient | New dedicated scenario (higher cost, same signal) |

## 7. Implementation Plan

### Phase 1 — Run testmode smoke

- [ ] Run `npm run unity:testmode-batch` with smoke/round-trip scenario.
- [ ] Confirm zero Unity errors / exceptions in output.
- [ ] Confirm city load + one sim tick complete.
- [ ] Document pass/fail + any anomalies in Issues Found.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| City load + sim tick no regression | Agent | `npm run unity:testmode-batch` | See `ia/skills/agent-test-mode-verify/SKILL.md` for Path A/B policy |

## 8. Acceptance Criteria

- [ ] Testmode batch smoke passes (zero errors).
- [ ] City load + sim tick confirmed in output.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
