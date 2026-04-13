---
purpose: "TECH-97 — Testmode assertion: HeightMap / CityCell.height integrity (invariant #1)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-97 — Testmode `HeightMap` / `CityCell.height` integrity assertion

> **Issue:** [TECH-97](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 4 — targeted testmode assertion confirming `HeightMap[x,y] == CityCell.height` (invariant #1) across all grid cells after city load + sim tick. Confirms the cell-type split refactor (TECH-90–95) did not break the dual-write invariant.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Testmode scenario asserts `HeightMap[x,y] == CityCell.height` for every grid cell after load.
2. Assertion passes with zero violations on a standard 32×32 test map.
3. Stage 1.2 exit criteria for invariant #1 satisfied.

### 2.2 Non-Goals (Out of Scope)

1. General smoke (city load + tick) — that is TECH-96.
2. Any other invariant assertions beyond #1.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Automated proof that HeightMap ↔ CityCell.height sync survived refactor | Assertion passes on all grid cells; zero violations |

## 4. Current State

### 4.1 Domain behavior

`HeightMap[x,y]` and `CityCell.height` must be in sync on every write (invariant #1). The cell-type split rename could have introduced a breakage if any dual-write site was missed. This task provides the assertion gate.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/UnitManagers/HeightMap.cs` | Source of truth for height array |
| `Assets/Scripts/Managers/UnitManagers/CityCell.cs` | Source of `height` field |
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | `GetCell<CityCell>(x,y)` access path |
| Testmode batch scenario infrastructure | Assertion runner |
| `ia/skills/agent-test-mode-verify/SKILL.md` | Verification policy |
| `docs/agent-led-verification-policy.md` | Canonical verification policy |

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. Testmode assertion passes green.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Testmode scenario (new or extended) iterates all grid cells post-load: `Assert.AreEqual(heightMap[x,y], gridManager.GetCell<CityCell>(x,y).height)`. Log violations with cell coordinates. Run via `npm run unity:testmode-batch`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Assertion in testmode batch (not Play Mode only) | Automated; no manual Unity run required | Play Mode manual check (slower; agent-inaccessible) |

## 7. Implementation Plan

### Phase 1 — Implement + run HeightMap integrity assertion

- [ ] Add or extend testmode scenario: iterate all cells; assert `HeightMap[x,y] == CityCell.height`.
- [ ] Run `npm run unity:testmode-batch` with the assertion scenario.
- [ ] Confirm zero violations logged.
- [ ] Document result in Issues Found if any violation detected.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| HeightMap ↔ CityCell.height invariant #1 | Agent | `npm run unity:testmode-batch` (assertion scenario) | See `ia/skills/agent-test-mode-verify/SKILL.md` for Path A/B |

## 8. Acceptance Criteria

- [ ] Testmode assertion scenario runs; zero `HeightMap` / `CityCell.height` mismatches.
- [ ] Stage 1.2 invariant #1 gate: confirmed clean after full cell-type split.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
