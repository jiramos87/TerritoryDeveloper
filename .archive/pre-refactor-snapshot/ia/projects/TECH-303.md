---
purpose: "TECH-303 — Add IStatsReadModel + StatKey typed read-model contract."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-303 — Add `IStatsReadModel` + `StatKey` typed read-model contract

> **Issue:** [TECH-303](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add typed pull contract `IStatsReadModel` + metric id enum `StatKey` before touching any MonoBehaviour. First task of citystats-overhaul Stage 1.1 — bootstraps the read-model shape so `ColumnarStatsStore` (TECH-304) + `CityStatsFacade` (Stage 1.2) can compose against it. Satisfies Stage 1.1 Exit criterion 1 and unblocks every consumer migration in Step 2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `Assets/Scripts/Managers/UnitManagers/IStatsReadModel.cs` compiles w/ signatures `GetScalar(StatKey) → float`, `GetSeries(StatKey, int windowTicks) → float[]`, `EnumerateRows(string dimension, Predicate<object> filter) → IEnumerable<object>`.
2. `Assets/Scripts/Managers/GameManagers/StatKey.cs` enum entry per current `CityStats` public field — covers population, money, happiness, forestCoverage, unemployment, jobs, demand r/c/i, etc. — grep `CityStats.cs` for exhaustive list.
3. Stubs `RegionPopulation` + `CountryPopulation` present (filled in Stage 3.1).
4. No runtime wiring — compile-only.

### 2.2 Non-Goals

1. No `ColumnarStatsStore` changes — TECH-304.
2. No `CityStatsFacade` MonoBehaviour — Stage 1.2.
3. No `CityStats` shim wrappers — Stage 1.3.
4. No consumer migration — Step 2.
5. No region/country rollup logic — Stage 3.1.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As future implementer of `ColumnarStatsStore`, want `StatKey` enum + `IStatsReadModel` interface in place so I compose against stable types. | Both files compile; enum coverage exhaustive vs `CityStats` public fields. |

## 4. Current State

### 4.1 Domain behavior

`CityStats` (god-class) exposes public fields directly. No typed pull contract. `StatisticsManager.StatisticTrend` is the only ring buffer, soon deprecated. No metric id enum — consumers reference fields by name.

### 4.2 Systems map

- `Assets/Scripts/Managers/UnitManagers/ICityStats.cs:9` — existing interface; preserved verbatim (shim work in Stage 1.3).
- `Assets/Scripts/Managers/GameManagers/CityStats.cs` — source of truth for public-field enumeration → one `StatKey` entry each.
- Spec refs: `ia/specs/managers-reference.md §Helper Services`; `docs/citystats-overhaul-exploration.md §Design Expansion`.

## 5. Proposed Design

### 5.1 Target behavior (product)

Contract-only. No player-visible change. Runtime behavior identical post-landing.

### 5.2 Architecture / implementation

- `IStatsReadModel` under `UnitManagers/` (alongside `ICityStats`).
- `StatKey` under `GameManagers/` (alongside future `ColumnarStatsStore` + `CityStatsFacade`).
- Enum backing type: default `int` (fine for <256 keys).
- Order entries by domain cluster (pop / money / happiness / demand / employment / environment / scale stubs) — stable naming, not alphabetic.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Enum (not string keys) | Compile-safe, zero alloc, fast dict lookup | `Dictionary<string,float>` — rejected, typo-prone |

## 7. Implementation Plan

### Phase 1 — Author contract + enum

- [ ] Grep `CityStats.cs` public fields; enumerate exhaustively.
- [ ] Write `IStatsReadModel.cs` w/ three method signatures.
- [ ] Write `StatKey.cs` enum — one entry per public field + `RegionPopulation` + `CountryPopulation` stubs.
- [ ] `npm run unity:compile-check`.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean typed contract | Agent report | `npm run unity:compile-check` | No runtime wiring yet |
| IA alignment | Node | `npm run validate:all` | Frontmatter + dead-spec check |

## 8. Acceptance Criteria

- [ ] `IStatsReadModel.cs` compiles w/ three signatures matching Stage 1.1 Exit.
- [ ] `StatKey.cs` compiles; one entry per current `CityStats` public field.
- [ ] `RegionPopulation` + `CountryPopulation` stubs present.
- [ ] `npm run unity:compile-check` clean.
- [ ] `npm run validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling / type scaffolding only. Any `CityStats` public field gap surfaces at Stage 1.3 (shim wrappers) + Stage 2.2 (producer managers grep).
