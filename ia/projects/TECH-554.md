---
purpose: "TECH-554 — Placement-mode routing through `ZoneSService`."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.2"
---
# TECH-554 — Placement-mode routing through `ZoneSService`

> **Issue:** [TECH-554](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

When **Zone S** placement is active, grid clicks must call **`ZoneSService.PlaceStateServiceZone(cell, currentSubTypeId)`** instead of generic `PlaceZone`. Invalid sub-type id forces picker reopen.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Transient state holds `currentSubTypeId` (from picker); -1 means “must pick”.
2. On cell click: if id < 0 reopen picker; else delegate to `ZoneSService`.
3. R/C/I placement code paths unchanged.

### 2.2 Non-Goals (Out of Scope)

1. Changing `ZoneSService` spend rules — prior stages own that.
2. AUTO zoning — TECH-550 family.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Place S building after picking type | Cell click places via service |
| 2 | Developer | Single routing branch for S | Clear guard on zone type |

## 4. Current State

### 4.1 Domain behavior

**ZoneSService** owns validation, **TryDraw**, treasury floor; placement must funnel there per exploration **IP** pipeline.

### 4.2 Systems map

- `ZoneSService` — `PlaceStateServiceZone`
- `ZoneManager` — placement mode, `PlaceZone` call sites
- Placement input handler (mouse / grid pick)

### 4.3 Implementation investigation notes (optional)

Grep `PlaceZone` from zoning tool / `ZoneManager` UI entry.

## 5. Proposed Design

### 5.1 Target behavior (product)

No placement without valid sub-type; service enforces envelope + treasury.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Small coordinator or `ZoneManager` branch: `if (IsStateServiceZone(activeType))` → service call with `subTypeId`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | -1 sentinel for unpicked | Matches `Zone.subTypeId` default story | Nullable int (rejected — aligns with int sidecar) |

## 7. Implementation Plan

### Phase 1 — Trace call path

- [ ] Find code that handles zoning click → `PlaceZone`.

### Phase 2 — S branch

- [ ] If active type is state-service: guard id; call `PlaceStateServiceZone`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Node | `npm run unity:compile-check` | Required |
| Placement w/ id | EditMode optional | Mock grid + service | If harness exists |

## 8. Acceptance Criteria

- [ ] Valid id → `PlaceStateServiceZone` invoked with correct cell + id.
- [ ] id < 0 → picker reopen; no silent place.
- [ ] R/C/I paths unchanged.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: duplicate `PlaceZone` call sites — mitigation: centralize or grep all consumers.
- Invariant #5: no raw `gridArray` — use `GridManager` accessors.
- Coupling: depends on TECH-555/556 for id lifecycle — coordinate merge order.

### §Examples

| State | Click | Result |
|-------|-------|--------|
| activeType=S, id=2 | cell (5,3) | `PlaceStateServiceZone` with 2 |
| activeType=S, id=-1 | cell | picker reopen |
| activeType=R | cell | legacy `PlaceZone` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| s_route_uses_service | active S, id valid | service called once | Unit / integration |
| s_blocks_negative_id | id -1 | no place event | EditMode |

### §Acceptance

- [ ] Service routing live for S.
- [ ] Guard for -1.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Where `currentSubTypeId` lives — static on `ZoneSService`, `ZoneManager` field, or small `SPlacementContext` — implementing agent picks; document for TECH-555/556.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
