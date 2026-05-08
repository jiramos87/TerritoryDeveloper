---
purpose: "TECH-316 — Music — Add Blip-Music mixer group."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-316 — Music — Add Blip-Music mixer group

> **Issue:** [TECH-316](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Extend `Assets/Audio/BlipMixer.mixer` — add new `Blip-Music` group under Master, routed through Master. First task of music-player Stage 1.1 (mixer extension + persistent bootstrap). Binary YAML asset edit via Unity Editor `Window → Audio → Audio Mixer`. No code. Satisfies master plan Step 1 Exit criterion "BlipMixer extended w/ `Blip-Music` group under Master".

## 2. Goals and Non-Goals

### 2.1 Goals

1. New group `Blip-Music` present under Master on `BlipMixer.mixer`.
2. Existing groups `Blip-UI` / `Blip-World` / `Blip-Ambient` untouched.
3. Asset diff committed (`.mixer` binary YAML + `.meta`).

### 2.2 Non-Goals (Out of Scope)

1. Exposing volume params (→ TECH-317).
2. Any C# code (→ TECH-318 / TECH-319).
3. Prefab + scene placement (→ TECH-320 / TECH-321).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Music subsystem needs its own mixer routing group to isolate volume from SFX. | `Blip-Music` group visible under Master in Audio Mixer window; routes through Master. |

## 4. Current State

### 4.1 Domain behavior

`BlipMixer.mixer` currently hosts Master + 3 child groups (Blip-UI / Blip-World / Blip-Ambient) per glossary **Blip mixer group** + `ia/specs/audio-blip.md §5.4`. No Music routing.

### 4.2 Systems map

- `Assets/Audio/BlipMixer.mixer` — Unity AudioMixer asset (binary YAML). Edit via Editor; do not hand-patch YAML.
- `ia/specs/audio-blip.md §5.4` — Blip mixer group layout reference.
- `ia/projects/blip-master-plan.md` — sibling orchestrator; mixer asset stable at Music Stage 1.1 landing (Blip Step 2/3 Final).

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change at this task boundary (mixer group addition is structural). Enables downstream `MusicVolume` param exposure (TECH-317) + C# binding (TECH-319).

### 5.2 Architecture / implementation

Unity Editor workflow: `Window → Audio → Audio Mixer` → select `BlipMixer` → right-click Master → Add child group → name `Blip-Music` → confirm routing "Output → Master".

## 7. Implementation Plan

### Phase 1 — Group add

- [ ] Open `BlipMixer.mixer` in Audio Mixer window.
- [ ] Add child group `Blip-Music` under Master (routed through Master).
- [ ] Verify no edits to Blip-UI / Blip-World / Blip-Ambient.
- [ ] Commit `.mixer` + `.meta` diff.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Asset-only change compiles clean | Node | `npm run unity:compile-check` | No C# — compile check confirms no indirect break |

## 8. Acceptance Criteria

- [ ] `Blip-Music` group visible under Master in Audio Mixer window.
- [ ] Existing Blip-UI / Blip-World / Blip-Ambient untouched.
- [ ] `npm run unity:compile-check` green.

## Open Questions (resolve before / during implementation)

1. None — asset edit only.
