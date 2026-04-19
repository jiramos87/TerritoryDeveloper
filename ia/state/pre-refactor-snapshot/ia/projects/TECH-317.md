---
purpose: "TECH-317 — Music — Expose MusicVolume + MasterVolume params."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-317 — Music — Expose MusicVolume + MasterVolume params

> **Issue:** [TECH-317](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Expose two dB params on `BlipMixer.mixer` — `MusicVolume` (binds `Blip-Music` group volume) + `MasterVolume` (binds Master group volume). Defaults 0 dB. Enables headless `AudioMixer.SetFloat` binding from `MusicBootstrap.Awake` (TECH-319) + Settings sliders (Step 3). `SfxVolume` param stays as-is (Blip-owned — no re-declaration).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `MusicVolume` param exposed + bound to `Blip-Music` group volume slider.
2. `MasterVolume` param exposed + bound to Master group volume slider.
3. Exposed Parameters panel shows 3 entries total: `MasterVolume`, `MusicVolume`, `SfxVolume`.
4. `SfxVolume` untouched (Blip-owned).

### 2.2 Non-Goals (Out of Scope)

1. C# binding (→ TECH-319).
2. Settings UI sliders (→ Stage 3.1).

## 4. Current State

### 4.2 Systems map

- `Assets/Audio/BlipMixer.mixer` — extended by TECH-316 (new `Blip-Music` group).
- `ia/specs/audio-blip.md §5.4` — existing `SfxVolume` param reference (Blip-owned).
- Unity Editor Audio Mixer → right-click group volume → "Expose 'Volume (of Blip-Music)' to script" → rename to `MusicVolume`.

## 5. Proposed Design

### 5.2 Architecture / implementation

Unity Editor: select `BlipMixer` → Master group → right-click `Volume` parameter → "Expose 'Volume (of Master)' to script" → Exposed Parameters panel → rename to `MasterVolume`. Repeat for `Blip-Music` group → rename exposed name to `MusicVolume`. Default values stay 0 dB.

## 7. Implementation Plan

### Phase 1 — Expose params

- [ ] Expose Master group volume → rename to `MasterVolume`.
- [ ] Expose `Blip-Music` group volume → rename to `MusicVolume`.
- [ ] Verify Exposed Parameters panel lists exactly: `MasterVolume`, `MusicVolume`, `SfxVolume`.
- [ ] Commit `.mixer` + `.meta` diff.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Asset-only change compiles clean | Node | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] `MusicVolume` param exposed + binds `Blip-Music` group.
- [ ] `MasterVolume` param exposed + binds Master group.
- [ ] Exposed panel: 3 entries (`MasterVolume`, `MusicVolume`, `SfxVolume`).
- [ ] `SfxVolume` untouched.
- [ ] `npm run unity:compile-check` green.

## Open Questions

1. None — asset edit only.
