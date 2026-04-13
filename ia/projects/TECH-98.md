---
purpose: "TECH-98 — BlipMixer.mixer asset + three groups + exposed SfxVolume param."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-98 — BlipMixer.mixer asset + three groups + exposed SfxVolume param

> **Issue:** [TECH-98](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Author `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML asset, not hand-written). Three child groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) routed through master. Expose master `SfxVolume` dB param via `Exposed Parameters` panel, default 0 dB. Satisfies first Stage 1.1 exit criterion — mixer asset + routing surface ready for Step 2 player pool + router to consume.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `Assets/Audio/BlipMixer.mixer` lands as binary YAML asset under version control.
2. Three child groups present: `Blip-UI`, `Blip-World`, `Blip-Ambient` — each routed through master.
3. Master exposes `SfxVolume` dB param (default 0 dB) via `Exposed Parameters` panel; accessible headless via `AudioMixer.SetFloat("SfxVolume", db)`.

### 2.2 Non-Goals (Out of Scope)

1. No Settings UI slider / mute toggle — deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4.
2. No headless `PlayerPrefs` binding — TECH-99 lands that.
3. No FX (reverb / compressor) on any group — post-MVP.
4. No duck / side-chain routing.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer I want a mixer asset w/ three named groups so Step 2 `BlipMixerRouter` can resolve `BlipId → AudioMixerGroup`. | Asset opens in Audio Mixer window; three groups visible; `SfxVolume` reachable by name. |

## 4. Current State

### 4.1 Domain behavior

No audio mixer exists under `Assets/Audio/`. Game plays no SFX today. Blip subsystem entirely greenfield.

### 4.2 Systems map

- New asset: `Assets/Audio/BlipMixer.mixer` (binary YAML).
- Consumers (future steps): `BlipMixerRouter` (Step 2), `BlipBootstrap.Awake` (TECH-99), `BlipPlayer.PlayOneShot` (Step 2).
- Reference: orchestrator `ia/projects/blip-master-plan.md` Stage 1.1 Exit; exploration `docs/blip-procedural-sfx-exploration.md` §7, §14.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change this task. Mixer asset only.

### 5.2 Architecture / implementation

Unity Editor authoring — `Window → Audio → Audio Mixer` → create `BlipMixer` → add three child groups → expose master `SfxVolume` (right-click attenuation → `Expose to script`). Save. Commit binary YAML asset + `.meta`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Three fixed groups (UI / World / Ambient) | Locked in exploration §13 | Per-id groups (rejected — explosion); single group (rejected — no routing flex) |

## 7. Implementation Plan

### Phase 1 — Authoring

- [ ] Create `Assets/Audio/` folder if absent.
- [ ] Author `BlipMixer.mixer` via Unity Editor; add `Blip-UI`, `Blip-World`, `Blip-Ambient` groups.
- [ ] Expose master `SfxVolume` dB param (default 0 dB).
- [ ] Verify groups visible in mixer window + `SfxVolume` listed under `Exposed Parameters`.
- [ ] Commit `.mixer` + `.meta`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Mixer asset + groups exist | Manual Editor | Open `BlipMixer` in Audio Mixer window | Binary YAML; no pure unit test path |
| Exposed param reachable | Headless | TECH-99 verifies via `SetFloat` return true | Deferred to TECH-99 |

## 8. Acceptance Criteria

- [ ] `Assets/Audio/BlipMixer.mixer` + `.meta` committed.
- [ ] Three groups present (`Blip-UI`, `Blip-World`, `Blip-Ambient`).
- [ ] `SfxVolume` exposed (default 0 dB).
- [ ] `npm run validate:all` green.

## 10. Lessons Learned

- …

## Open Questions

None — tooling / asset authoring only; no game logic.
