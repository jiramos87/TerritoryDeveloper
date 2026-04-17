---
purpose: "TECH-286 — BlipLutPool stub + BlipVoiceState LFO phase fields."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-286 — BlipLutPool stub + BlipVoiceState LFO phase fields

> **Issue:** [TECH-286](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Blip Step 5 Stage 5.3 Phase 1 second task. Add `BlipLutPool` plain-class stub wired into `BlipCatalog` (LUT lease/return stub; full LUT baking lands post-Stage-5.3). Rename `BlipVoiceState.phaseD` → `lfoPhase0` + add `double lfoPhase1`. Zero kernel logic — phase fields consumed by TECH-287/288.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New `Assets/Scripts/Audio/Blip/BlipLutPool.cs` — `internal sealed class BlipLutPool` w/ `float[] Lease(int size)` + `void Return(float[])` via `ArrayPool<float>.Shared` (clear on return).
2. `BlipCatalog` gains `private BlipLutPool _lutPool = new BlipLutPool()` (field-init; invariant #4 — no new singleton).
3. `BlipVoiceState.phaseD` renamed → `lfoPhase0`; all refs in `BlipVoice.cs` + test files updated.
4. `BlipVoiceState` gains `double lfoPhase1` (blittable; `default = 0.0`).
5. `npm run unity:compile-check` + `npm run validate:all` green.

### 2.2 Non-Goals

1. LUT population / sine-table baking — post-Stage-5.3 (stub returns empty arrays).
2. LFO per-sample advance — TECH-287.
3. Routing matrix — TECH-288.
4. Glossary **Blip LUT pool** row — TECH-288.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Runtime | Voice holds 2 LFO phase accumulators | `BlipVoiceState.lfoPhase0` / `lfoPhase1` present + blittable |
| 2 | Catalog | Hosts LUT pool ref for future table-based LFO | `_lutPool` field initialized; no singleton |

## 4. Current State

### 4.1 Domain behavior

`BlipVoiceState.phaseD` exists as unused 4th oscillator phase slot (legacy). No LUT pool anywhere. Catalog holds `_delayPool` (Stage 5.2 precedent for plain-class + field-init).

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipLutPool.cs` (new) — mirrors `BlipDelayPool` pattern (`ArrayPool<float>.Shared` lease/return).
- `Assets/Scripts/Audio/Blip/BlipCatalog.cs` — append `_lutPool` field-init next to existing `_delayPool`.
- `Assets/Scripts/Audio/Blip/BlipVoiceState.cs` — rename + add phase field.
- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — rename refs (`state.phaseD` → `state.lfoPhase0`).
- Reference spec: `ia/specs/audio-blip.md §3.2` (BlipVoiceState default-zero invariant).
- Invariant #4 (no new singletons) — plain `internal sealed class` + field-init compliance.

## 5. Proposed Design

### 5.1 Target behavior (product)

LUT pool = future cache of pre-sampled waveform tables (sine/tri/etc). Stub signatures only — lease returns `ArrayPool<float>.Shared.Rent(size)`, return clears + releases. Voice state gains two `double` phase accumulators (wide range, avoid Hz drift over long voices).

### 5.2 Architecture / implementation

`BlipLutPool` = 1:1 clone of `BlipDelayPool` API shape minus sample-rate param. `BlipCatalog._lutPool` field-init (not Awake-assigned) per Stage 5.2 `_delayPool` precedent. `phaseD` rename is mechanical — no logic change. Rationale for rename: `phaseD` was dead code (oscillator slot 4 never shipped); repurposing for LFO 0 keeps struct size stable + preserves blittability.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Repurpose `phaseD` → `lfoPhase0` (rename, not add+drop) | Struct size stable; `default=0` invariant held; zero blit change | Add new field, leave `phaseD` — grows struct unnecessarily |
| 2026-04-17 | Plain `internal sealed class` not MonoBehaviour | Matches `BlipDelayPool`; invariant #4; data-only infra | MonoBehaviour — needless scene object |
| 2026-04-17 | `ArrayPool<float>.Shared` (same as delay pool) | Proven zero-alloc lease; existing pattern | Custom pool — reinventing Stage 5.2 |

## 7. Implementation Plan

### Phase 1 — BlipLutPool + BlipCatalog wiring

- [ ] Author `Assets/Scripts/Audio/Blip/BlipLutPool.cs` — `internal sealed class BlipLutPool` w/ `Lease(int size)` + `Return(float[])` (clearArray: true).
- [ ] `BlipCatalog`: add `private BlipLutPool _lutPool = new BlipLutPool();` next to `_delayPool`.

### Phase 2 — BlipVoiceState LFO phase fields

- [ ] `BlipVoiceState.cs`: rename `phaseD` → `lfoPhase0` (keep `double` type).
- [ ] Add `public double lfoPhase1;` after `lfoPhase0`.
- [ ] `BlipVoice.cs`: update all `state.phaseD` refs → `state.lfoPhase0` (search + replace; expect dead-code reference only).
- [ ] EditMode tests: update any `phaseD` refs (likely none — was dead).
- [ ] Run `npm run unity:compile-check` + `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| New class + renamed + added fields compile | Unity compile | `npm run unity:compile-check` | Struct blittability preserved |
| No allocs from pool lease/return | EditMode | Covered downstream TECH-287/288 | Stub-only here |
| Existing goldens stay green (phase rename inert) | EditMode | `BlipGoldenFixtureTests` | `phaseD` was unused; rename behavior-neutral |
| IA indexes unaffected | Node | `npm run validate:all` | exit 0 |

## 8. Acceptance Criteria

- [ ] `BlipLutPool.cs` present; `Lease`/`Return` via `ArrayPool<float>.Shared` w/ clear-on-return.
- [ ] `BlipCatalog._lutPool` field-init; no singleton; invariant #4 held.
- [ ] `BlipVoiceState.phaseD` → `lfoPhase0`; `lfoPhase1` added; blittable + `default = 0.0`.
- [ ] All `phaseD` refs migrated in `BlipVoice.cs` + tests.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` exit 0.
- [ ] `BlipGoldenFixtureTests` / `BlipNoAllocTests` still green.

## Open Questions

1. None — infra-only stub + mechanical rename; no gameplay-rule ambiguity.
