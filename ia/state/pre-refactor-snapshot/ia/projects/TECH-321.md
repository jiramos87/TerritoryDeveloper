---
purpose: "TECH-321 — Music — MainMenu scene placement + compile verify."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-321 — Music — MainMenu scene placement + compile verify

> **Issue:** [TECH-321](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Place `MusicBootstrap.prefab` instance at `MainMenu.unity` root (sibling to existing `BlipBootstrap` prefab instance). Commit scene diff. Run `npm run unity:compile-check` + manual smoke — verify `MusicBootstrap.Instance != null` post-Awake + no null-mixer warn in log. Closes Stage 1.1 of music-player orchestrator. Invariant #4 satisfied at stage close (MB Inspector-placed via scene, not `new`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `MusicBootstrap.prefab` instance at `MainMenu.unity` root, sibling to `BlipBootstrap`.
2. `MainMenu.unity` scene diff committed.
3. `npm run unity:compile-check` green.
4. Manual smoke — `MusicBootstrap.Instance != null` after Awake; null-mixer warn absent.
5. Invariant #4 satisfied at stage close.

### 2.2 Non-Goals

1. Playlist + `MusicPlayer` handoff (→ Stage 2.1).
2. First-run toast (→ Stage 3.3).

## 4. Current State

### 4.2 Systems map

- `Assets/Scenes/MainMenu.unity` — hosts persistent bootstrap GO roots (`BlipBootstrap` already present; add `MusicBootstrap` sibling).
- `Assets/Prefabs/Audio/MusicBootstrap.prefab` — created by TECH-320.
- `ia/specs/unity-development-context.md §6` — SEO + `DontDestroyOnLoad` Awake timing (no specific ordering constraint vs Blip; both run Awake in arbitrary order — neither depends on the other at stage 1.1 boundary).
- `ia/rules/invariants.md` #4 — MB Inspector-placed.

## 5. Proposed Design

### 5.2 Architecture / implementation

Unity Editor: open `MainMenu.unity` → drag `Assets/Prefabs/Audio/MusicBootstrap.prefab` into scene Hierarchy root (sibling to existing `BlipBootstrap` GO) → save scene. Smoke verify via Editor Play Mode — look for `"[Music] MusicBootstrap: blipMixer ref missing"` warn (should be absent).

## 7. Implementation Plan

### Phase 1 — Scene placement + verify

- [ ] Open `MainMenu.unity`.
- [ ] Drag `MusicBootstrap.prefab` into Hierarchy root.
- [ ] Save scene.
- [ ] `npm run unity:compile-check` green.
- [ ] Manual Play Mode smoke — `MusicBootstrap.Instance != null`; no null-mixer warn.
- [ ] Commit scene diff.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| C# + scene compile clean | Node | `npm run unity:compile-check` | |
| Play Mode: Instance set + no warn | Agent report | Manual smoke in Editor Play Mode | Record in Verification block |

## 8. Acceptance Criteria

- [ ] `MainMenu.unity` hosts `MusicBootstrap` prefab instance at root (sibling to `BlipBootstrap`).
- [ ] Scene diff committed.
- [ ] `npm run unity:compile-check` green.
- [ ] Play Mode smoke: `Instance != null` post-Awake; no null-mixer warn.
- [ ] Invariant #4 satisfied (MB Inspector-placed).

## Open Questions

1. None — scene placement + verify.
