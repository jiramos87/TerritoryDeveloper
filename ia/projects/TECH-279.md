---
purpose: "TECH-279 — Add Zone.subTypeId sidecar field."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-279 — Add `Zone.subTypeId` sidecar field

> **Issue:** [TECH-279](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add `subTypeId` int sidecar field to `Zone` component. Default `-1` means "RCI, no sub-type". Value identifies a **Zone S** sub-type row in the forthcoming `ZoneSubTypeRegistry`. No save plumbing yet — save bump lands in Stage 1.3.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `[SerializeField] private int subTypeId = -1;` on `Zone.cs` with public getter/setter.
2. Default `-1` propagates on all existing (RCI) `Zone` instances — no breaking change.
3. Value persists via existing Unity serialization (in-scene prefabs, instantiated copies).
4. `unity:compile-check` green.

### 2.2 Non-Goals

1. No `GameSaveManager` wiring — v3→v4 bump lands in Stage 1.3.
2. No `ZoneSubTypeRegistry` lookup — registry lands in TECH-280.
3. No validation of id range — `ZoneSService` placement enforces valid id at call site.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Tag a `Zone` with its S sub-type | `zone.SubTypeId = 3` assigns + reads back as 3 |
| 2 | Developer | Leave RCI zones untagged | Default `-1` reads cleanly on all legacy zones |

## 4. Current State

### 4.1 Domain behavior

`Zone` component holds `ZoneType` + density + owning `Cell` ref. No sub-type channel — RCI has no such concept.

### 4.2 Systems map

- `Assets/Scripts/Managers/UnitManagers/Zone.cs` — `Zone` MonoBehaviour declaration.
- Router domain: Zones, buildings, RCI.
- Relevant invariant: none directly; respects Unity serialization conventions.
- Depends on: TECH-278 (consumer convention assumes enum extension; not hard-compile dep).

## 5. Proposed Design

### 5.1 Target behavior

Sidecar int field, default `-1`, readable/writable via property. Unity serializes automatically. Downstream consumers (Stage 2.3 `ZoneSService`) read + write during placement.

### 5.2 Architecture / implementation

```csharp
[SerializeField] private int subTypeId = -1;
public int SubTypeId { get => subTypeId; set => subTypeId = value; }
```

Place near existing `zoneType` + `density` fields in `Zone.cs`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Sidecar int vs enum expansion | Keeps `ZoneType` lean; 7 sub-types × 3 tiers would bloat enum to 21 entries | Enum expansion (rejected — 21-value blow-up + save int drift) |

## 7. Implementation Plan

### Phase 1 — Sidecar field

- [ ] Add `[SerializeField] private int subTypeId = -1;` to `Zone.cs`.
- [ ] Add `public int SubTypeId { get; set; }` property.
- [ ] Run `npm run unity:compile-check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Field compiles + defaults to -1 | Unity compile | `npm run unity:compile-check` | |
| Serialization round-trip | EditMode test | covered in TECH-283 | |

## 8. Acceptance Criteria

- [ ] `subTypeId` field lands on `Zone` w/ default `-1`.
- [ ] Public getter/setter accessible.
- [ ] Existing RCI zones read `-1` with no migration.
- [ ] `unity:compile-check` green.

## Open Questions

1. None — scaffolding task, no gameplay change.
