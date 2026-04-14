---
purpose: "TECH-113 — OnValidate clamps on BlipPatch (anti-click + range guards)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-113 — OnValidate clamps on BlipPatch (anti-click + range guards)

> **Issue:** [TECH-113](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

`OnValidate` guards on `BlipPatch` — clamp AHDSR timings ≥ 1 ms (≈48 samples @ 48 kHz, kills snap-onset click), clamp `variantCount` / `voiceLimit` / `sustainLevel` / `cooldownMs`. Third task of Stage 1.2 Phase 1.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Clamp `envelope.attackMs`, `envelope.decayMs`, `envelope.releaseMs` to ≥ 1 ms.
2. Clamp `variantCount` to 1..8.
3. Clamp `voiceLimit` to 1..16.
4. Clamp `sustainLevel` to 0..1.
5. Clamp `cooldownMs` to ≥ 0.

### 2.2 Non-Goals

1. Hash recompute (T1.2.5 — separate concern; Hash-write in `OnValidate` added there).
2. Flatten guard (T1.2.4).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Can't author patches with snap-onset click | Authoring 0-ms attack auto-clamps to 1 ms on save / Inspector edit. |

## 4. Current State

### 4.2 Systems map

- Lives on `BlipPatch` partial or same class (`Assets/Scripts/Audio/Blip/BlipPatch.cs`).
- Unity invokes `OnValidate` on Inspector edits + domain reload.

## 5. Proposed Design

### 5.1 Target behavior

Authoring-side only; runtime values never see clamp re-entry. Sustain-only patches supported via A=1ms / D=0 / R=1ms fallback.

## 7. Implementation Plan

### Phase 1 — Clamp method

- [ ] Add `OnValidate()` w/ `Mathf.Max` / `Mathf.Clamp` calls per field.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| OnValidate clamps fire | Unity compile | `npm run unity:compile-check` | Runtime behavior covered by EditMode tests in Stage 1.4. |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] All 5 clamp ranges enforced in `OnValidate`.
- [ ] Sustain-only case (A=1ms / D=0 / R=1ms) compiles + authored OK.
- [ ] `validate:all` + `unity:compile-check` green.

## Open Questions

1. None — tooling-adjacent; thresholds fixed by Stage 1.2 Exit.
