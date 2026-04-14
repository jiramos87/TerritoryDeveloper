---
purpose: "TECH-139 — Blip envelope shape + silence tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-139 — Blip envelope shape + silence tests

> **Issue:** [TECH-139](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

EditMode assertions covering Blip AHDSR envelope behavior for both `Linear` + `Exponential` shapes, plus silence guard for `gainMult = 0`. Satisfies Stage 1.4 Exit bullets 4+5.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New test file `Assets/Tests/EditMode/Audio/BlipEnvelopeTests.cs`.
2. Per-shape tests (Linear + Exponential) — patch A=50ms / H=0 / D=50ms / S=0.5 / R=50ms. Assert via `SampleEnvelopeLevels`:
   - attack segment monotonic rising;
   - decay segment monotonic falling toward sustain;
   - release segment monotonic falling to zero.
3. Exponential-shape extra assert — attack slope first quarter > last quarter (perceptual-linear signature).
4. Silence test — patch w/ `gainMult = 0` → all buffer samples exactly 0 (byte equality — `Assert.That(buf, Is.All.EqualTo(0f))`).
5. Tests green in Unity Test Runner.

### 2.2 Non-Goals (Out of Scope)

1. No coverage of envelope stage timing precision beyond monotonicity (millisecond-accurate segment boundary deferred).
2. No hold-segment assertions when hold duration = 0 (skipped in patch).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Catch envelope math regressions (sign flip, shape swap, silence leak). | Tests fail if monotonicity or silence break. |

## 4. Current State

### 4.1 Domain behavior

Stage 1.3 shipped AHDSR FSM (TECH-118) + envelope level math (TECH-119) covering Linear + Exponential shapes. No automated coverage — visual / ear inspection only.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipEnvelope.cs` — static `BlipEnvelopeStepper` + `ComputeLevel` under test.
- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — renders envelope × osc × filter.
- `Assets/Tests/EditMode/Audio/` — TECH-137 fixtures.

## 5. Proposed Design

### 5.1 Target behavior (product)

Tooling-only.

### 5.2 Architecture / implementation

- Shape-parametrized test or two near-identical `[Test]` methods.
- Render patch @ 48 kHz × total duration 200 ms (covers A+D+R + small sustain tail).
- Extract slope via `SampleEnvelopeLevels(buf, stride=64)`; walk each segment boundary (attack 0–50 ms, decay 50–100 ms, sustain 100–150 ms, release 150–200 ms approx — boundaries from patch durations).
- Monotonic assertion — `for (i = 1; i < seg.Length; i++) Assert.That(seg[i], Is.GreaterThanOrEqualTo(seg[i-1]))` (or flipped per segment).
- Exponential quarter-slope — compare `(seg[q1] - seg[0])` vs `(seg[end] - seg[q3])` on attack segment.
- Silence — separate `[Test]`; patch with `gainMult = 0`; `Assert.That(buf, Is.All.EqualTo(0f))`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Merge T1.4.4 (envelope) + T1.4.5 (silence) | Both share envelope math surface + fixture setup; phase 2 compress per user stage-file plan | File separately |

## 7. Implementation Plan

### Phase 1 — Tests

- [ ] Create `BlipEnvelopeTests.cs` w/ Linear + Exponential shape tests.
- [ ] Add silence `[Test]` in same file.
- [ ] Verify all pass locally.
- [ ] `npm run unity:compile-check` + `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tests compile + pass | Unity EditMode | Unity Test Runner → EditMode → `Blip.Tests.EditMode` | |
| Repo health | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Linear envelope monotonicity tests pass.
- [ ] Exponential envelope monotonicity + quarter-slope test passes.
- [ ] Silence test passes w/ exact-equality assertion.
- [ ] `unity:compile-check` + `validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
