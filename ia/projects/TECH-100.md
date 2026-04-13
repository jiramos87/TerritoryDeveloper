---
purpose: "TECH-100 — BlipBootstrap prefab + DontDestroyOnLoad + MainMenu.unity placement."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-100 — BlipBootstrap prefab + DontDestroyOnLoad + MainMenu.unity placement

> **Issue:** [TECH-100](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Author `BlipBootstrap` GameObject prefab. `Awake` calls `DontDestroyOnLoad(transform.root.gameObject)` following `GameNotificationManager.cs` pattern. Empty child slots for `BlipCatalog` / `BlipPlayer` / `BlipMixerRouter` / `BlipCooldownRegistry` — populated Step 2. Place at root of `MainMenu.unity` (boot scene, build index 0 per `MainMenuController.cs`). Satisfies Stage 1.1 Exit criterion "`BlipBootstrap` GameObject prefab at `MainMenu.unity` root".

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipBootstrap` prefab lives under `Assets/Prefabs/Audio/` (or similar).
2. `Awake` calls `DontDestroyOnLoad(transform.root.gameObject)` — survives all scene loads from `MainMenu` onward.
3. Empty child slots present (GameObjects w/ correct names): `BlipCatalog`, `BlipPlayer`, `BlipMixerRouter`, `BlipCooldownRegistry`. No components beyond `Transform` MVP.
4. Prefab instance placed at root of `MainMenu.unity`.

### 2.2 Non-Goals (Out of Scope)

1. No `BlipCatalog` / `BlipPlayer` logic — Step 2.
2. No mixer binding — TECH-99 owns `Awake` SFX volume block (merged together).
3. No scene-load suppression flag wiring — TECH-101 documents policy; full ready-flag lands Step 2.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer I want a persistent boot GameObject so Step 2 can populate child slots w/o scene-lookup races. | Enter `MainMenu` → load `Game.unity` → `BlipBootstrap` still alive (one instance). |

## 4. Current State

### 4.1 Domain behavior

No persistent audio bootstrap today. `GameNotificationManager.cs` uses `DontDestroyOnLoad(transform.root.gameObject)` pattern — mirror it.

### 4.2 Systems map

- New prefab: `Assets/Prefabs/Audio/BlipBootstrap.prefab`.
- New script: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (shared w/ TECH-99 — merge: TECH-99 owns volume binding block, TECH-100 owns `DontDestroyOnLoad` + child refs).
- Scene edit: `Assets/Scenes/MainMenu.unity` — add prefab instance at root.
- Reference pattern: `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` (`DontDestroyOnLoad` call site).

## 5. Proposed Design

### 5.2 Architecture / implementation

```
public class BlipBootstrap : MonoBehaviour {
    [SerializeField] private Transform catalogSlot;       // empty Step 1; BlipCatalog Step 2
    [SerializeField] private Transform playerSlot;        // empty Step 1; BlipPlayer Step 2
    [SerializeField] private Transform mixerRouterSlot;   // empty Step 1
    [SerializeField] private Transform cooldownSlot;      // empty Step 1
    // Awake body: DontDestroyOnLoad + volume binding (TECH-99 merge)
}
```

Honors invariants #3 (no `FindObjectOfType` in `Update` — all caching in `Awake`) + #4 (no new singleton; scene-component pattern).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Prefab at `MainMenu.unity` root (not `Game.unity`) | MVP boot entry per `MainMenuController.cs` build index 0 | Game scene root (rejected — already loaded means no bootstrap before menu SFX) |

## 7. Implementation Plan

### Phase 1 — Prefab + scene placement

- [ ] Create `Assets/Prefabs/Audio/BlipBootstrap.prefab` w/ empty child GameObjects for Catalog / Player / MixerRouter / Cooldown.
- [ ] `BlipBootstrap.cs` Awake calls `DontDestroyOnLoad(transform.root.gameObject)` — merge w/ TECH-99 volume-binding block.
- [ ] Drag prefab instance into `MainMenu.unity` at scene root.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` | |
| Persistence | PlayMode | Deferred — Step 2 smoke asserts survival across scene load | |

## 8. Acceptance Criteria

- [ ] `BlipBootstrap.prefab` + `.meta` committed.
- [ ] `MainMenu.unity` contains instance at root.
- [ ] `Awake` calls `DontDestroyOnLoad(transform.root.gameObject)`.
- [ ] Four empty child slots present w/ correct names.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 10. Lessons Learned

- …

## Open Questions

None — prefab authoring + lifetime only; no game logic.
