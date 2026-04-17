---
purpose: "TECH-280 — Author ZoneSubTypeRegistry ScriptableObject class."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-280 — `ZoneSubTypeRegistry` ScriptableObject class

> **Issue:** [TECH-280](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

New ScriptableObject class `ZoneSubTypeRegistry` cataloging 7 **Zone S** sub-types (police, fire, education, health, parks, public housing, public offices). Fields per entry: `id`, `displayName`, `prefab`, `baseCost`, `monthlyUpkeep`, `icon`. `GetById(int)` lookup. Class only — asset seeding lands in TECH-281.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ZoneSubTypeEntry` serializable nested class: `int id`, `string displayName`, `GameObject prefab`, `int baseCost`, `int monthlyUpkeep`, `Sprite icon`.
2. `ZoneSubTypeRegistry` ScriptableObject: `ZoneSubTypeEntry[] entries` field; `GetById(int) → ZoneSubTypeEntry` lookup; `[CreateAssetMenu]` attribute.
3. `unity:compile-check` green.

### 2.2 Non-Goals

1. No asset file — seeding lands in TECH-281.
2. No Inspector wiring on manager GO — consumer wiring lands in Stage 1.3.
3. No registry immutability enforcement — entries are content data, hand-edited in Editor.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Look up sub-type metadata by id | `registry.GetById(3)` returns matching entry or null |
| 2 | Designer | Edit 7 sub-types in Inspector | Entries array editable via Unity ScriptableObject inspector |

## 4. Current State

### 4.1 Domain behavior

No sub-type catalog exists. Exploration doc §IP-1 names 7 sub-types; no runtime handle yet.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` *(new)* — class + entry struct.
- `Assets/ScriptableObjects/Economy/` *(new dir)* — asset location for TECH-281.
- Router domain: Zones, buildings, RCI.
- Relevant invariant: #12 (project-scoped spec per task).

## 5. Proposed Design

### 5.1 Target behavior

ScriptableObject asset authored in Editor. Game code resolves sub-type via `GetById(int)`. Registry lives as a single `.asset` under `Assets/ScriptableObjects/Economy/`.

### 5.2 Architecture / implementation

```csharp
[CreateAssetMenu(fileName = "ZoneSubTypeRegistry", menuName = "Territory/Economy/Zone Sub-Type Registry")]
public class ZoneSubTypeRegistry : ScriptableObject {
    [Serializable] public class ZoneSubTypeEntry {
        public int id;
        public string displayName;
        public GameObject prefab;
        public int baseCost;
        public int monthlyUpkeep;
        public Sprite icon;
    }
    [SerializeField] private ZoneSubTypeEntry[] entries;
    public ZoneSubTypeEntry GetById(int id) { /* linear scan, null on miss */ }
    public IReadOnlyList<ZoneSubTypeEntry> Entries => entries;
}
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | ScriptableObject vs plain struct array | Editor editability + asset identity + future content-pack split | Static C# array (rejected — no Editor UX, no late-bind) |
| 2026-04-17 | Linear scan in `GetById` | 7 entries — dict overhead unjustified | Dictionary cache (rejected — premature) |

## 7. Implementation Plan

### Phase 1 — Class scaffolding

- [ ] Create `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs`.
- [ ] Declare nested `ZoneSubTypeEntry` serializable class.
- [ ] Declare `ZoneSubTypeRegistry` SO with entries array + `GetById`.
- [ ] Add `[CreateAssetMenu]` attribute.
- [ ] Run `npm run unity:compile-check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Class compiles + menu entry registered | Unity compile | `npm run unity:compile-check` | |
| `GetById` lookup correctness | EditMode test | covered in TECH-283 | |

## 8. Acceptance Criteria

- [ ] `ZoneSubTypeRegistry` SO class lands under `Assets/Scripts/Managers/GameManagers/`.
- [ ] Entry struct has 6 fields per spec.
- [ ] `GetById(int)` returns matching entry or null.
- [ ] `[CreateAssetMenu]` exposes menu entry.
- [ ] `unity:compile-check` green.

## Open Questions

1. None — class scaffolding only, content authoring in sibling task.
