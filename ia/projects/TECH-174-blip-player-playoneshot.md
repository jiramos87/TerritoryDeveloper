---
purpose: "TECH-174 — BlipPlayer.PlayOneShot round-robin dispatch."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-174 — BlipPlayer.PlayOneShot round-robin dispatch

> **Issue:** [TECH-174](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Adds `PlayOneShot(AudioClip, float pitch, float gain, AudioMixerGroup)` to `BlipPlayer`. Round-robin picks next source via `_cursor`, voice-steal overwrites prior clip if still playing (no crossfade — post-MVP per orchestration guardrails), configures source + calls `Play()`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)` method on `BlipPlayer`.
2. Round-robin cursor advance `_cursor = (_cursor + 1) % _pool.Length` after pick.
3. Voice-steal overwrite — stop prior clip if source.isPlaying before reassigning.
4. Configure `clip`, `pitch`, `volume`, `outputAudioMixerGroup`; call `source.Play()`.

### 2.2 Non-Goals

1. Crossfade — post-MVP.
2. `BlipEngine.Play` dispatch integration — Stage 2.3 T2.3.3.

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipPlayer.cs` — pool + cursor from TECH-173.
- Blip master plan Stage 2.2 Exit bullet 5 — round-robin contract.
- Orchestration guardrails — voice-steal overwrite (no crossfade MVP).

## 5. Proposed Design

### 5.2 Architecture

```csharp
public void PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)
{
    var source = _pool[_cursor];
    _cursor = (_cursor + 1) % _pool.Length;
    if (source.isPlaying) source.Stop();
    source.clip = clip;
    source.pitch = pitch;
    source.volume = gain;
    source.outputAudioMixerGroup = group;
    source.Play();
}
```

## 7. Implementation Plan

### Phase 1 — Dispatch method

- [ ] Add `PlayOneShot` method to `BlipPlayer`.
- [ ] Verify no exception on mid-playback overwrite.
- [ ] `unity:compile-check` + `validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` | |
| IA chain | Node | `npm run validate:all` | |
| Cursor wrap + voice-steal | PlayMode | Stage 2.4 T2.4.4 fixture (later stage) | Not tested here |

## 8. Acceptance Criteria

- [ ] 16 rapid calls wrap cursor once (wrap point `_cursor == 0`).
- [ ] Mid-playback call overwrites prior clip w/o exception.
- [ ] `source.outputAudioMixerGroup` matches passed arg.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — impl only; voice-steal overwrite policy locked by orchestration guardrails.
