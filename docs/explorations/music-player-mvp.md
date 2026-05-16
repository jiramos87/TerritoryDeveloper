---
slug: music-player-mvp
status: seed-stub
supersedes_master_plan: music-player (closed 2026-05-16 — pre-arch drift, unstarted)
parent_exploration: docs/mvp-scope.md §3.30 (D35 lock)
related_master_plans:
  - region-scene-prototype (closed — region ambient layer)
  - city-region-zoom-transition (open — audio crossfade on scale switch)
  - full-game-mvp (umbrella)
related_specs:
  - docs/mvp-scope.md §3.30 Audio (D35 lock — 4 SFX cue groups + 3-5 looping music tracks + city ambient bed)
  - docs/mvp-scope.md §3.24 Settings menu (D14 lock — 5-slider audio panel)
arch_decisions_inherited:
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
  - DEC-A29 (iso-scene-core-shared-foundation — music persists across scale switch)
  - DEC-A30 (corescene-persistent-shell — AudioManager lives in CoreScene, not CityScene)
arch_surfaces_touched:
  - data-flows/persistence (audio settings persist across saves)
  - layers/system-layers (Territory.Audio new layer)
---

# Music Player + Audio Mixer — Exploration Seed Stub (MVP)

**Status:** Seed stub. `/design-explore` to expand.

**Replaces:** Closed master plan `music-player` (8 stages, never started, drifted).

---

## Problem Statement

D35 (2026-05-07) locked the audio scope: 4 UI-SFX cue groups + music playlist (3–5 looping tracks rotating on track-end) + single city ambient bed (traffic + birds + wind + distant crowd) layered on top. AudioMixer with 5 channels per D14 settings (Master / Music / Ambient / UI-SFX / Game-SFX).

Closed `music-player` plan pre-dated D35 cue inventory + DEC-A30 (AudioManager in CoreScene so music persists across CityScene ↔ RegionScene scale switch + main-menu return).

No music plays today. SFX stubs exist in `SubtypePickerController.cs` (`sfxPanelOpen` / `sfxPanelClose` / `sfxPickerConfirm`) but no wired AudioMixer. Settings sliders (D14) have no audio target. AudioManager scaffold doesn't exist.

---

## Open Questions (resolve at /design-explore Phase 1)

### Track + asset inventory

1. **3–5 looping music tracks — final count + style direction?** D35 says "3–5" — pick 3, 4, or 5 for MVP. Genre (electronic / orchestral / minimal-ambient / lo-fi)?
2. **City ambient bed composition.** Single loop or layered loops (traffic + birds + wind + crowd as separate stems for dynamic mixing)?
3. **Asset source.** License + commission, royalty-free library, or commission new tracks? Asset workstream timing.

### Architecture

4. **AudioManager lifecycle.** Lives in CoreScene per DEC-A30 (persistent across scale switch + main-menu return). Lifetime: load on first scene entry, persist via DontDestroyOnLoad, never destroyed?
5. **AudioMixer integration.** Unity AudioMixer asset under `Assets/Audio/`? 5 channels expose volume parameters bound to D14 settings sliders. Mixer routing per AudioSource.
6. **Track rotation logic.** Random shuffle? Sequential? Persist last-played-index across saves so resume continues where left off?
7. **Crossfade behavior.** On track-end transition: hard cut, fade-out + fade-in overlap, or seamless gapless loop joining?
8. **Scale-switch audio.** CityScene → RegionScene transition: ambient bed crossfades (city traffic out, region wind/highway in)? Music continues uninterrupted?

### Settings integration

9. **5-slider binding.** Each D14 slider maps to one AudioMixer parameter (Master/Music/Ambient/UI-SFX/Game-SFX). Slider 0..100 → mixer dB scale (logarithmic).
10. **Persistence.** Settings persist in save (per-save preference) or PlayerPrefs (global across saves)? D14 implies per-save (settings-modal is in-game).
11. **Track-skip button.** D35 mentions "track-skip button" as post-MVP — drop from MVP scope or include now?

### SFX cue inventory

12. **Cue assignment.** D35 lists 4 cue groups: Toolbar + picker (already stubbed) / Paint commit per family (zone/road/utility/service/forest/landmark + demolish) / HUD buttons (play-pause/speed-cycle/map/stats) / Error validation. Total ~15–20 distinct cues. Each = a separate clip or shared across cues?
13. **Cue routing.** All SFX through UI-SFX or Game-SFX channel? D35 splits UI-SFX vs Game-SFX in 5-slider settings — define which cue group routes where.

---

## Scope NOT in this seed

- **Adaptive music** (single shared track + ambient only — §5 hard exclusion).
- **Voice acting / narration**.
- **Sound effect editor / per-effect tuning UI**.
- **Music store / DLC packs**.
- **Subtitles** (§5 accessibility excluded).
- **Music slider on HUD** (settings-only).

## Pre-conditions for `/design-explore`

- `region-scene-prototype` closed (yes) — RegionScene shell exists for region ambient.
- `city-region-zoom-transition` open — scale-switch audio behavior depends on transition mechanics; can be designed in parallel with cross-references.
- Asset workstream picks track style + commissions/sources audio before /ship-plan can author code-side tasks.

## Next step

`/design-explore docs/explorations/music-player-mvp.md`
