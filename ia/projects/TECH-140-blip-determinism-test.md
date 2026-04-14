---
purpose: "TECH-140 — Blip determinism test."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-140 — Blip determinism test

> **Issue:** [TECH-140](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

EditMode test asserting two renders of same Blip patch + seed + variant index produce identical output within epsilon. Uses sum-of-abs hash (1e-6 tolerance) + first-256-sample byte equality as cheap early-signal gate. Satisfies Stage 1.4 Exit bullet 6.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New test file `Assets/Tests/EditMode/Audio/BlipDeterminismTests.cs`.
2. Render same patch + seed + variantIndex twice; assert:
   - `SumAbsHash(bufA) - SumAbsHash(bufB)| < 1e-6`;
   - first 256 samples exactly equal (byte comparison).
3. Validate voice-state reset + xorshift RNG determinism across invocations.
4. Tests green in Unity Test Runner.

### 2.2 Non-Goals (Out of Scope)

1. Cross-platform / cross-JIT byte-equality on full buffer — brittle against `Math.Sin` LSB drift; deferred to post-MVP LUT osc (per `docs/blip-post-mvp-extensions.md` §1).
2. Cross-variant-index determinism (different variants intentionally differ via jitter).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Regression guard against non-deterministic state leak (stale `z1`, RNG not seeded, rngState shared). | Test fails on determinism break. |

## 4. Current State

### 4.1 Domain behavior

Stage 1.3 `BlipVoice.Render` uses xorshift32 RNG seeded from `variantIndex * 0x9E3779B9 ^ voiceId` + honors `deterministic` flag. No automated regression coverage.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — RNG + voice-state reset under test.
- `Assets/Scripts/Audio/Blip/BlipVoiceState.cs` — `rngState` field.
- `Assets/Tests/EditMode/Audio/` — TECH-137 fixtures (`SumAbsHash`).

## 5. Proposed Design

### 5.1 Target behavior (product)

Tooling-only.

### 5.2 Architecture / implementation

- `[Test]` method — render twice with fresh `BlipVoiceState` each call but identical `patchFlat` + same `variantIndex`.
- Hash both buffers via `BlipTestFixtures.SumAbsHash`; `Assert.That(Math.Abs(hashA - hashB), Is.LessThan(1e-6))`.
- `Assert.That(bufA.Take(256).ToArray(), Is.EqualTo(bufB.Take(256).ToArray()))` (or indexed loop).
- Set `deterministic = true` on patch to force deterministic RNG path.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Hybrid sum-of-abs + first-256-byte equality | Catches deep drift (hash) + early state leak (byte-equal prefix); avoids JIT-LSB brittleness of full byte-equal | Full byte-equality; hash-only |

## 7. Implementation Plan

### Phase 1 — Test

- [ ] Create `BlipDeterminismTests.cs` w/ one `[Test]` method.
- [ ] Verify pass locally.
- [ ] `npm run unity:compile-check` + `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Test compiles + passes | Unity EditMode | Unity Test Runner → EditMode | |
| Repo health | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Determinism test passes locally.
- [ ] `unity:compile-check` + `validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
