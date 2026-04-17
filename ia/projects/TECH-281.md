---
purpose: "TECH-281 — Seed 7 ZoneSubTypeRegistry entries + asset file."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-281 — Seed 7 `ZoneSubTypeRegistry` entries + asset

> **Issue:** [TECH-281](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Create `Assets/ScriptableObjects/Economy/ZoneSubTypeRegistry.asset` with 7 seeded entries: police, fire, education, health, parks, public housing, public offices. Placeholder prefabs + icons acceptable — real art lands post-MVP. `baseCost` + `monthlyUpkeep` per exploration §IP-1 balancing intent.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Asset file at `Assets/ScriptableObjects/Economy/ZoneSubTypeRegistry.asset`.
2. 7 entries seeded with stable `id` values 0..6 matching exploration IP-1 ordering (police=0, fire=1, education=2, health=3, parks=4, public housing=5, public offices=6).
3. `baseCost` + `monthlyUpkeep` numbers chosen to match exploration Examples 1–3 (police baseCost 500, fire baseCost 600, etc. — final numbers per implementation).
4. Placeholder prefab + icon refs resolve (can be null or a generic placeholder prefab — no missing-ref warnings).

### 2.2 Non-Goals

1. No final art — colored-cube placeholders OK per umbrella Bucket 3 scope.
2. No balancing tuning — final numbers adjusted during Step 3 playtest.
3. No scene wiring — consumer injection lands in Stage 1.3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Designer | See 7 sub-types in Editor | Inspector lists 7 entries w/ labels |
| 2 | Developer | Reference registry asset via Inspector | Drag-drop wire resolves at play time |

## 4. Current State

### 4.1 Domain behavior

Registry class lands via TECH-280. No asset exists yet — runtime services have nothing to resolve.

### 4.2 Systems map

- `Assets/ScriptableObjects/Economy/ZoneSubTypeRegistry.asset` *(new)*.
- `Assets/ScriptableObjects/Economy/.meta` *(new dir meta)*.
- `docs/zone-s-economy-exploration.md` §IP-1 — canonical sub-type list + rough cost hints.
- Router domain: Zones, buildings, RCI.
- Depends on: TECH-280 (registry class must compile first).

## 5. Proposed Design

### 5.1 Target behavior

7 entries seeded via Editor w/ ids 0..6. Ids are stable contract — downstream services reference by int, never by displayName string.

### 5.2 Architecture / implementation

Create dir + .meta via Unity Editor (Create → Territory → Economy → Zone Sub-Type Registry). Populate entries array in Inspector. Placeholder prefab: reuse existing building prefab or create empty GO w/ sprite renderer. Placeholder icon: reuse existing zoning icon.

### 5.3 Seed values

| id | displayName | baseCost | monthlyUpkeep |
|----|-------------|----------|---------------|
| 0 | Police | 500 | 50 |
| 1 | Fire | 600 | 60 |
| 2 | Education | 800 | 80 |
| 3 | Health | 1000 | 100 |
| 4 | Parks | 300 | 30 |
| 5 | Public Housing | 700 | 70 |
| 6 | Public Offices | 900 | 90 |

Numbers chosen to exercise envelope exhaustion in exploration Example 2 (envelope=200, fire baseCost=600 → blocks).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Ids 0..6 stable, never renumbered | Save-file int channel; reorder would corrupt v4 saves | Hash-of-displayName (rejected — fragile) |
| 2026-04-17 | Placeholder prefabs OK at MVP | Art deferred to post-MVP bucket | Block stage on art (rejected — scope creep) |

## 7. Implementation Plan

### Phase 1 — Asset seed

- [ ] Create `Assets/ScriptableObjects/Economy/` directory.
- [ ] Create `ZoneSubTypeRegistry.asset` via Editor menu.
- [ ] Populate 7 entries per §5.3 table.
- [ ] Wire placeholder prefab + icon refs (can be existing zoning placeholders).
- [ ] Commit .meta file alongside .asset.
- [ ] Run `npm run unity:compile-check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Asset loadable + 7 entries present | EditMode test | covered in TECH-283 | Registry lookup round-trip |
| Compile-safe | Unity compile | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] `Assets/ScriptableObjects/Economy/ZoneSubTypeRegistry.asset` exists.
- [ ] 7 entries w/ ids 0..6 populated.
- [ ] `baseCost` + `monthlyUpkeep` match §5.3 table.
- [ ] Prefab + icon refs resolve (placeholder OK).
- [ ] `unity:compile-check` green.

## Open Questions

1. Are placeholder prefabs per sub-type required, or is a single shared placeholder acceptable MVP? (Default: single shared placeholder pending Step 3 art task.)
