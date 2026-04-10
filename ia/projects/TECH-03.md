---
purpose: "Project spec for TECH-03 — Extract magic numbers to constants or ScriptableObjects."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-03 — Extract magic numbers to constants or ScriptableObjects

> **Issue:** [TECH-03](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **34** (magic-number extraction report) optional planning aid.

## 1. Summary

**Economic**, **sorting**, **pathfinding**, **height generation**, and **UI** code contain many **magic numbers**. Extract to **`const`**, **`static readonly`**, named fields, or **ScriptableObjects** for tuning — **without changing gameplay** unless explicitly agreed.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Improve readability and safe tuning for hot files (**`GridManager`**, **`CityStats`**, **`RoadManager`**, **`UIManager`**, **`TimeManager`**, **`TerrainManager`**, **`WaterManager`**, **`EconomyManager`**, **`ForestManager`**, **`InterstateManager`** — per backlog).
2. Group related constants (e.g. **`sortingOrder`** offsets, **pathfinding** weights) in **`static`** or dedicated **`*Constants`** / **`RoadPathCostConstants`**-style classes where they already exist.

### 2.2 Non-Goals (Out of Scope)

1. Rebalancing **economy** or **simulation** (numeric changes belong to design tickets).
2. **ScriptableObject** infrastructure for every trivial literal in one pass.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Designer | I want one place to tweak obvious tuning knobs. | Named constants or SO fields for chosen literals. |
| 2 | Developer | I want diffs to show intent. | `const int DefaultResidentialTax = 9` beats raw `9` scattered. |

## 4. Current State

### 4.1 Domain behavior

N/A (maintain identical behavior for pure extractions).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — TECH-03 |
| Existing patterns | `RoadPathCostConstants`, **`AutoSimulationRoadRules`**, etc. |

### 4.3 Implementation investigation notes (optional)

- **Agent-friendly slices:** one subsystem per PR (e.g. **sorting** offsets only, or **EconomyManager** only).
- After each slice: Unity smoke test relevant feature.

## 5. Proposed Design

### 5.1 Target behavior (product)

Identical **player-visible** behavior for mechanical renames; any intentional balance change requires explicit **Decision Log** entry and approval.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Choose a file or region (e.g. **`DEPTH_MULTIPLIER`** neighborhood).
2. Introduce **`private const`** or shared **`static`** class.
3. Replace literals; run compile.
4. Document in **Decision Log** if a value was corrected (bugfix) vs pure extract.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Phased extraction | Reduces regression risk | Big-bang constant file |

## 7. Implementation Plan

### Phase 1 — Pilot region

- [ ] Select one file (e.g. **`RoadManager`** path costs or **`GridManager`** sorting constants).
- [ ] Extract 5–15 related literals to named constants.

### Phase 2 — Expand by domain

- [ ] Economy / **CityStats** numeric defaults.
- [ ] **Terrain** / **water** generation thresholds (coordinate with **geography** spec terms).

### Phase 3 — Optional ScriptableObjects

- [ ] Only where backlog/design requests runtime-tunable data.

## 8. Acceptance Criteria

- [ ] At least one merged slice demonstrates pattern; backlog can stay open for remaining files.
- [ ] No unintentional numeric drift (compare before/after for extracted values).
- [ ] **English** names for constants.
- [ ] **Unity:** Compile and short play test on touched flows (no behavior drift).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **simulation rules** — pure refactor. If a literal’s meaning is unclear (ambiguous between two design intents), clarify with product owner before encoding a name.
