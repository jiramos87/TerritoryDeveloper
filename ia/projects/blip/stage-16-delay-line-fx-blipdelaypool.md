### Stage 16 — Patches + integration + golden fixtures + promotion / Delay-line FX + BlipDelayPool


**Status:** Done (closed 2026-04-17 — TECH-270..TECH-275 all archived)

**Objectives:** `BlipDelayPool` plain-class service (owned by `BlipCatalog`) allocates float[] delay-line buffers outside `Render` — zero alloc in hot path. Implement comb, allpass, chorus, flanger in `BlipFxChain.ProcessFx` replacing Stage 5.1 stubs. `BlipVoice.Render` gains nullable delay buffer params via new overload.

**Exit:**

- `Assets/Scripts/Audio/Blip/BlipDelayPool.cs` (new): `internal sealed class BlipDelayPool` — `float[] Lease(int sampleRate, float maxDelayMs)` + `void Return(float[])` via `ArrayPool<float>.Shared`. `BlipCatalog` gains `private BlipDelayPool _delayPool = new BlipDelayPool()` (init in `Awake`; invariant #4 compliant).
- `BlipVoiceState` gains `int delayWritePos_0, delayWritePos_1, delayWritePos_2, delayWritePos_3` (circular write-head per FX slot, blittable).
- `BlipVoice` gains new `Render` overload with `float[]? d0, float[]? d1, float[]? d2, float[]? d3` nullable delay params; existing 7-param signature delegates with all-null. `BlipFxChain.ProcessFx` signature extended with `float[]? delayBuf, int bufLen, ref int writePos`.
- `BlipBaker.BakeOrGet` pre-leases delay buffers before `Render`; returns in `finally`.
- Comb: `y=x+g*d[(wp-D+len)%len]; d[wp]=x; wp=(wp+1)%len`; `g=p1` clamped 0..0.97, `D=(int)(p0/1000f*sampleRate)`. Allpass: Schroeder `v=d[(wp-D+len)%len]; d[wp]=x+g*v; y=v-g*d[wp]; wp=(wp+1)%len`.
- Chorus: 2-tap LFO-modulated delay (rate `p0` Hz, depth `p1` ms, mix `p2`). Flanger: same structure, depth 1–10 ms.
- `BlipNoAllocTests` gains chorus patch variant; buffers pre-leased outside measurement window; assert delta/call ≤ 0.
- Phase 1 — `BlipDelayPool` service + `BlipVoiceState` write-heads + `BlipCatalog` ownership + `BlipVoice.Render` overload + `BlipBaker` call-site.
- Phase 2 — Comb + allpass kernels in `BlipFxChain.ProcessFx`.
- Phase 3 — Chorus + flanger kernels + `BlipNoAllocTests` delay-FX variant.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | BlipDelayPool + catalog wiring + VoiceState write-heads | **TECH-270** | Done (archived) | New `Assets/Scripts/Audio/Blip/BlipDelayPool.cs`: `internal sealed class BlipDelayPool` with `float[] Lease(int sampleRate, float maxDelayMs)` (sizes to `(int)Math.Ceiling(maxDelayMs/1000f*sampleRate)+1`; delegates to `ArrayPool<float>.Shared.Rent`) + `void Return(float[])`. `BlipCatalog` gains `private BlipDelayPool _delayPool = new BlipDelayPool()`. `BlipVoiceState` gains `int delayWritePos_0, delayWritePos_1, delayWritePos_2, delayWritePos_3`. |
| T16.2 | BlipVoice.Render delay overload + BlipBaker lease | **TECH-271** | Done (archived) | `BlipVoice` gains `Render` overload with `float[]? d0, float[]? d1, float[]? d2, float[]? d3`; existing 7-param overload delegates with all-null (backward compat shim). `BlipFxChain.ProcessFx` extended: `float[]? delayBuf, int bufLen, ref int writePos` params (null = skip delay op). `BlipBaker.BakeOrGet` pre-leases up to 4 buffers from `_catalog._delayPool`; passes to `Render`; returns in `finally`. |
| T16.3 | Comb filter kernel | **TECH-272** | Done (archived) | `BlipFxChain.ProcessFx` Comb case: feedback comb `y=x+g*d[(wp-D+len)%len]; d[wp]=x; wp=(wp+1)%len`; `D=(int)(p0/1000f*sampleRate)`, `g=p1` clamped 0..0.97 (enforce in `BlipPatch.OnValidate` for Comb slots). EditMode test `BlipFxChainTests.Comb_FeedbackAttenuation`: impulse, 10 ms delay, g=0.5 — 2nd echo amplitude ≈ 0.5 ± 0.05 relative to 1st. |
| T16.4 | Allpass filter kernel | **TECH-273** | Done (archived) | `BlipFxChain.ProcessFx` Allpass case: Schroeder `v=d[(wp-D+len)%len]; d[wp]=x+g*v; y=v-g*d[wp]; wp=(wp+1)%len`. EditMode test `BlipFxChainTests.Allpass_FlatMagnitude`: 1024 samples pink noise through allpass, assert RMS output ≈ RMS input ± 15% (flat magnitude response of ideal allpass). |
| T16.5 | Chorus + flanger kernels | **TECH-274** | Done (archived) | Chorus (`BlipFxChain.ProcessFx` Chorus case): 2-tap read at `offset±(p1_samples*sin(ringModPhase_N))`; write input; output `=(1-p2)*x+p2*0.5*(tap0+tap1)`; `ringModPhase_N+=2π*p0/sampleRate` (ring-mod phase repurposed for LFO — ring-mod and chorus/flanger are mutually exclusive per slot; enforced in `BlipPatch.OnValidate`). Flanger: same, depth clamped 1..10 ms. |
| T16.6 | NoAlloc delay-FX test + Render overload clean-up | **TECH-275** | Done (archived) | `BlipNoAllocTests.Render_WithChorus_ZeroManagedAlloc`: pre-lease 1 chorus delay buf outside `GC.GetAllocatedBytesForCurrentThread` window; 10 renders; assert delta/call ≤ 0. Confirm 7-param `BlipVoice.Render` overload still compiles; `BlipBakerTests` + `BlipDeterminismTests` suites still green after overload addition. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
