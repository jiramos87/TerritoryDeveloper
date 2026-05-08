---
purpose: "TECH-310 — MotionBlock schema + curves on UiTheme."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-310 — MotionBlock schema + curves (UiTheme extension)

> **Issue:** [TECH-310](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Extend `UiTheme` ScriptableObject w/ new `[Serializable] class MotionBlock` nested type + `[Serializable] struct MotionEntry { float durationSeconds; AnimationCurve easing; }` helper. Six semantic entries — `moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`. Expose via `public MotionBlock motion` on `UiTheme`. Consumer rings (primitives Step 2, studio controls Step 3, juice Step 4) read duration + easing from these entries — no hard-coded values downstream. Satisfies Stage 1.1 Exit criterion "motion block (`moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`, easing curves)".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `[Serializable] struct MotionEntry { float durationSeconds; AnimationCurve easing; }` to `UiTheme.cs`.
2. Add `[Serializable] class MotionBlock` w/ six `MotionEntry` fields: `moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`.
3. Expose `public MotionBlock motion` on `UiTheme`.
4. Additive only — existing fields untouched.
5. Compile-check green.

### 2.2 Non-Goals

1. Asset defaults for durations / curves — TECH-311.
2. Studio-rack visual block — TECH-309.
3. Spec / glossary rows — TECH-312 / TECH-313.
4. Consumer helpers (`TweenCounter`, `NeedleBallistics`, etc.) — Step 4.

## 4. Current State

### 4.2 Systems map

Domain: **UI changes** + **Manager responsibilities** (router match). `UiTheme.cs` owns semantic motion vocabulary — consumer rings will never hard-code duration / easing. `AnimationCurve` is a Unity serializable type — Inspector-editable on `DefaultUiTheme.asset`. No per-frame code in this issue.

## 7. Implementation Plan

### Phase 1 — Schema addition

- [ ] Define `[Serializable] struct MotionEntry` (or `class` if reference semantics preferred — struct matches value-type intent).
- [ ] Define `[Serializable] class MotionBlock` w/ six named `MotionEntry` fields.
- [ ] Append `public MotionBlock motion = new MotionBlock();` on `UiTheme`.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:dead-project-specs` green.

## 8. Acceptance Criteria

- [ ] `MotionEntry` + `MotionBlock` types defined w/ correct serializable signatures.
- [ ] Six motion entries present on `MotionBlock` w/ exact field names above.
- [ ] `UiTheme.motion` exposed.
- [ ] Existing `UiTheme` fields untouched.
- [ ] `npm run unity:compile-check` + `validate:all` green.

## Open Questions

1. None — tooling / schema only.
