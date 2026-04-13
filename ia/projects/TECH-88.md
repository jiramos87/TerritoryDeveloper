---
purpose: "TECH-88 — GridManager parent-id surface + new-game placeholder allocation."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-88 — `GridManager` parent-id surface + new-game placeholder allocation

> **Issue:** [TECH-88](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-12
> **Last updated:** 2026-04-12
> **Orchestrator:** [`multi-scale-master-plan.md`](multi-scale-master-plan.md) — Step 1 / Stage 1.1 / Phase 2.

## 1. Summary

`GridManager` exposes read-only `ParentRegionId` + `ParentCountryId`. Values hydrated from `GameSaveData` on load; new-game init allocates placeholder GUIDs + writes to save. No consumers yet — surface only.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GridManager.ParentRegionId` + `.ParentCountryId` readable from any city-sim code path.
2. New-game init allocates placeholder GUIDs + persists via `GameSaveData`.
3. Load path rehydrates ids from save.
4. No behavior change in city sim (surface only).

### 2.2 Non-Goals

1. Any consumer logic reading the ids.
2. Write access from outside `GridManager`.
3. Setter API (ids immutable post-init).
4. UI exposure.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want `GridManager` to expose parent region / country ids so Step 2 consumers have a stable read surface without touching `GameSaveData` directly. | Properties readable; values match save after round-trip. |

## 4. Current State

### 4.1 Domain behavior

`GridManager` has no parent-scale awareness. Post TECH-87, `GameSaveData` carries parent ids but nothing surfaces them at runtime.

### 4.2 Systems map

- `Assets/Scripts/GridManager.cs` — target for new properties.
- `Assets/Scripts/SaveSystem/SaveManager.cs` — load path wires ids into `GridManager`.
- `Assets/Scripts/NewGame/` (or equivalent new-game init) — allocates placeholders at generation.
- Depends on TECH-87 landing save fields.

### 4.3 Implementation investigation notes

- Follow existing `GridManager` read-only property pattern (e.g. width / height getters).
- Init ordering: `GridManager.Awake` / `Start` must run before consumers; confirm load-order matches existing patterns.
- Invariant #6 (no new responsibilities on `GridManager`): read-only identity properties are borderline — acceptable because they carry no logic, just expose save state already present. If logic grows, extract to helper.

## 5. Proposed Design

### 5.1 Target behavior (product)

At any point after grid init, `GridManager.ParentRegionId` + `.ParentCountryId` return non-null GUID strings. New-game → save → reload preserves both.

### 5.2 Architecture / implementation

- `GridManager`:
  - `public string ParentRegionId { get; private set; }`
  - `public string ParentCountryId { get; private set; }`
  - `internal void HydrateParentIds(string regionId, string countryId)` — called by `SaveManager` load path + new-game init.
- New-game init: allocate `Guid.NewGuid().ToString()` pair, call `HydrateParentIds`, persist via `GameSaveData`.
- `SaveManager` load path: read `GameSaveData.RegionId` / `.CountryId`, call `HydrateParentIds`.

### 5.3 Method / algorithm notes

Hydration is one-shot. Re-hydration during session = error (assert or log). Re-entry only on new scene load / new game.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-12 | Read-only property on `GridManager` (not new manager) | Invariant #4 (no new singletons); ids belong w/ grid identity | New `ParentScaleManager` (rejected — premature; zero logic to justify) |
| 2026-04-12 | Hydration via `internal` method, not constructor | Matches existing `GridManager` init pattern (MonoBehaviour + `FindObjectOfType`) | Constructor injection (rejected — not MonoBehaviour-friendly) |

## 7. Implementation Plan

### Phase 1 — `GridManager` surface

- [ ] Add `ParentRegionId` + `ParentCountryId` read-only properties.
- [ ] Add `internal void HydrateParentIds(string, string)`.

### Phase 2 — New-game init

- [ ] New-game code path allocates placeholder GUIDs + calls `HydrateParentIds`.
- [ ] `GameSaveData` populated w/ ids before first save.

### Phase 3 — Load path wiring

- [ ] `SaveManager` load calls `GridManager.HydrateParentIds` w/ values from `GameSaveData`.
- [ ] Order: grid init before save rehydration, per existing pattern.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA edits consistent | Node | `npm run validate:all` | |
| C# compiles | Node | `npm run unity:compile-check` | `Assets/**/*.cs` touched |
| New-game → save → load preserves ids | Agent report | `npm run unity:testmode-batch` (TECH-89) | Covered by TECH-89 |
| Properties readable from city-sim call site | Agent report | testmode scenario assertion (TECH-89) | |

## 8. Acceptance Criteria

- [ ] `GridManager.ParentRegionId` + `.ParentCountryId` readable post-init.
- [ ] New-game path allocates + persists placeholder GUIDs.
- [ ] Load path rehydrates ids; no null post-load.
- [ ] Invariant #6 respected (no sim logic added to `GridManager`).
- [ ] `npm run validate:all` + `unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. Does `GridManager` already have a canonical hydration hook called by `SaveManager`, or does this surface a new load-order edge? Resolve during Phase 3.
