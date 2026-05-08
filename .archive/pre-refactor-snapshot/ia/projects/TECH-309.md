---
purpose: "TECH-309 — StudioRackBlock schema extension of UiTheme token ring."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-309 — StudioRackBlock schema (UiTheme extension)

> **Issue:** [TECH-309](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Extend `UiTheme` ScriptableObject w/ new `[Serializable] class StudioRackBlock` nested type + `public StudioRackBlock studioRack` field. Carries studio-rack visual tokens (LED hues, VU gradient, knob detent, fader track, oscilloscope trace/glow, shadow depth stops, glow, sparkle palette). Foundation for downstream rings — primitives (Step 2), studio controls (Step 3), juice helpers (Step 4) all read these fields by name. Satisfies Stage 1.1 Exit criterion "UiTheme.cs carries new `[Serializable] StudioRackBlock` nested class w/ named fields per exploration §Design Expansion".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `[Serializable] class StudioRackBlock` nested type to `UiTheme.cs` w/ 10 named fields: `ledHues` (`Color[]`), `vuGradientStops` (`Gradient`), `knobDetentColor` (`Color`), `faderTrackGradient` (`Gradient`), `oscilloscopeTrace` (`Color`), `oscilloscopeGlowColor` (`Color`), `shadowDepthStops` (`float[3]`), `glowRadius` (`float`), `glowColor` (`Color`), `sparklePalette` (`Color[]`).
2. Expose via `public StudioRackBlock studioRack` on `UiTheme`.
3. Keep all existing `UiTheme` fields untouched (additive edit only).
4. Compile-check green (`npm run unity:compile-check`).

### 2.2 Non-Goals (Out of Scope)

1. Populating asset defaults on `DefaultUiTheme.asset` — lives in TECH-311.
2. Extending motion block — lives in TECH-310.
3. Spec / glossary rows — lives in TECH-312 / TECH-313.
4. OnValidate broadcast — Stage 1.2.
5. Consumer primitives / helpers — Steps 2–4.

## 4. Current State

### 4.2 Systems map

Domain: **UI changes** (router: `ia/specs/ui-design-system.md` §Foundations, components). **Manager responsibilities** (router: `ia/specs/managers-reference.md`) — `UiTheme` is a ScriptableObject under `Assets/Scripts/Managers/GameManagers/UiTheme.cs`. No per-frame code paths, no `FindObjectOfType` — invariant #3 irrelevant at this stage (broadcast in Stage 1.2). Additive-only edit respects orchestrator Locked decision "existing fields untouched".

## 7. Implementation Plan

### Phase 1 — Schema addition

- [ ] Append `[Serializable] public class StudioRackBlock { ... }` nested inside `UiTheme` (or sibling `[Serializable]` class in same file) w/ the 10 fields above.
- [ ] Add `public StudioRackBlock studioRack = new StudioRackBlock();` field on `UiTheme`.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:dead-project-specs` green.

## 8. Acceptance Criteria

- [ ] `StudioRackBlock` nested class present w/ all 10 named fields + correct types.
- [ ] `UiTheme.studioRack` field exposed.
- [ ] Existing `UiTheme` fields untouched (diff shows only additions).
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling / schema only. Downstream consumers (TECH-311 asset defaults, Step 2+ primitives) bind semantic meaning.
