### Stage 4 — DSP foundations + audio infra / EditMode DSP tests


**Status:** Done (closed 2026-04-15 — all 5 tasks archived)

**Objectives:** Unity Test Runner EditMode harness covering `BlipVoice.Render`. Owner has no prior game-audio testing experience — tasks scaffolded w/ explicit fixture helpers + assertion patterns. Tests run headless; no Unity audio system dependency (pure `float[]` math). Determinism test uses sum-of-abs tolerance hash (not byte equality — byte-equality within-run is brittle against JIT / `Math.Sin` LSB drift; bit-exact path lands post-MVP w/ LUT osc per `docs/blip-post-mvp-extensions.md` §1).

**Exit:**

- `Assets/Tests/EditMode/Audio/` asmdef w/ refs to `Blip` runtime asmdef + `UnityEngine.TestRunner` + `nunit.framework`.
- Test fixture helpers — render-to-buffer util, zero-crossing counter, envelope-slope sampler, sum-of-abs hash.
- Oscillator tests pass — sine / triangle / square / pulse @ known freq × duration ≈ expected zero-crossings (± tolerance).
- Envelope tests pass — `Linear` shape: attack rises monotonic, hold flat, decay falls, sustain holds, release tails to zero. `Exponential` shape: same monotonicity; additionally attack slope peaks in first quarter (ear-perceived-linear signature).
- Silence test passes — `gainMult = 0` → all samples == 0 (exact).
- Determinism test passes — same seed + patch + variant twice → sum-of-abs hash equal within epsilon (1e-6) + first 256 samples byte-equal (cheap early-signal gate).
- No-alloc regression test passes — steady-state `GC.GetAllocatedBytesForCurrentThread` delta per render call == 0 after warm-up.
- `npm run unity:compile-check` green (loads `.env` / `.env.local` per `CLAUDE.md` §5 — do not skip on empty `$UNITY_EDITOR_PATH`).
- Phase 1 — Test asmdef + fixture helpers.
- Phase 2 — Oscillator + envelope + silence assertions.
- Phase 3 — Determinism + no-alloc regression tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | asmdef + fixture helpers bootstrap | **TECH-137** | Done (archived) | `Assets/Tests/EditMode/Audio/Blip.Tests.EditMode.asmdef` (Editor-only; refs `Blip` runtime + `UnityEngine.TestRunner` + `nunit.framework`) + fixture helper utilities — `RenderPatch(in BlipPatchFlat, int sampleRate, int seconds) → float[]`, `CountZeroCrossings(float[]) → int`, `SampleEnvelopeLevels(float[], int stride) → float[]`, `SumAbsHash(float[]) → double`. Consolidates former T1.4.1 (asmdef) + T1.4.2 (helpers) per stage compress (2026-04-14). |
| T4.2 | Oscillator crossing tests | **TECH-138** | Done (archived) | Oscillator zero-crossing tests — sine @ 440 Hz × 1 s @ 48 kHz ≈ 880 crossings (± 2). Repeat triangle / square / pulse duty=0.5. |
| T4.3 | Envelope shape + silence tests | **TECH-139** | Done (archived) | Envelope shape tests — both `Linear` + `Exponential` shapes. A=50ms/H=0/D=50ms/S=0.5/R=50ms. Assert attack monotonic rising, decay monotonic falling to sustain, release monotonic falling to zero. Exponential-shape extra assert — attack slope in first quarter > slope in last quarter. Silence case — `gainMult = 0` → all-zero buffer (exact equality, not tolerance). Consolidates former T1.4.4 (envelope) + T1.4.5 (silence) per stage compress (2026-04-14). |
| T4.4 | Determinism test | **TECH-140** | Done (archived) | Determinism test — render same patch + seed + variant twice; assert `SumAbsHash` equal within 1e-6 + first 256 samples byte-equal. Validates voice-state reset + RNG determinism without depending on JIT stability of trailing samples. |
| T4.5 | No-alloc regression | **TECH-141** | Done (archived) | No-alloc regression — warm-up loop (3 renders, discard allocation), then measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state renders; assert delta constant ≤ 0 bytes/call (tolerates NUnit infra alloc outside the measured window). |

**Backlog state (Step 1):** All Step 1 task rows stay in this doc as `_pending_`. File BACKLOG rows + project specs when parent stage → `In Progress` via `stage-file` skill. Stages 2.x + 3.x task decomposition deferred until Step 2 + Step 3 open.

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._


**Status:** Final
