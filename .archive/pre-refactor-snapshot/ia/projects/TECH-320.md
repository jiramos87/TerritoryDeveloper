---
purpose: "TECH-320 — Music — MusicBootstrap prefab creation."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-320 — Music — MusicBootstrap prefab creation

> **Issue:** [TECH-320](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Create `Assets/Prefabs/Audio/MusicBootstrap.prefab` — new asset sibling to `BlipBootstrap.prefab`. Attach `MusicBootstrap.cs` component. Inspector wires `blipMixer` serialized ref → `Assets/Audio/BlipMixer.mixer`. Commit `.prefab` + `.meta`. No scene diff (→ TECH-321).

## 2. Goals and Non-Goals

### 2.1 Goals

1. New prefab asset at `Assets/Prefabs/Audio/MusicBootstrap.prefab`.
2. `MusicBootstrap` component attached.
3. `blipMixer` Inspector ref wired to `BlipMixer.mixer`.
4. `.prefab` + `.meta` committed.

### 2.2 Non-Goals

1. Scene placement (→ TECH-321).
2. Playlist ref wiring (→ Stage 2.1).

## 4. Current State

### 4.2 Systems map

- `Assets/Prefabs/Audio/` — existing directory; currently hosts `BlipBootstrap.prefab`.
- `Assets/Prefabs/Audio/BlipBootstrap.prefab` — sibling pattern (single GO w/ `BlipBootstrap` component + `blipMixer` ref + 4 child Transform slots).
- `Assets/Audio/BlipMixer.mixer` — target ref (shared w/ Blip).

## 5. Proposed Design

### 5.2 Architecture / implementation

Unity Editor workflow: Create empty GameObject → name `MusicBootstrap` → add component `MusicBootstrap` → drag `Assets/Audio/BlipMixer.mixer` into `blipMixer` Inspector slot → drag GO into `Assets/Prefabs/Audio/` to create prefab → delete scene copy.

## 7. Implementation Plan

### Phase 1 — Prefab authoring

- [ ] Create empty GO in temp scene.
- [ ] Add `MusicBootstrap` component.
- [ ] Wire `blipMixer` Inspector slot → `BlipMixer.mixer`.
- [ ] Save as prefab at `Assets/Prefabs/Audio/MusicBootstrap.prefab`.
- [ ] Delete temp GO.
- [ ] Commit `.prefab` + `.meta`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Prefab ref survives compile + import | Node | `npm run unity:compile-check` | Runtime smoke → TECH-321 |

## 8. Acceptance Criteria

- [ ] `Assets/Prefabs/Audio/MusicBootstrap.prefab` exists.
- [ ] `MusicBootstrap` component attached.
- [ ] `blipMixer` ref wired (Inspector shows `BlipMixer`).
- [ ] `.prefab` + `.meta` committed.
- [ ] No scene diff.

## Open Questions

1. None — prefab authoring.
