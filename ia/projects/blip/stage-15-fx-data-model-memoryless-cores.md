### Stage 15 — Patches + integration + golden fixtures + promotion / FX data model + memoryless cores


**Status:** Done (all 5 tasks TECH-256..TECH-260 archived)

**Objectives:** New `BlipFxKind` enum + `BlipFxSlot` / `BlipFxSlotFlat` structs establish the per-patch FX chain data model. `BlipPatch.fxChain` + `BlipPatchFlat` inline FX fields added. `BlipVoiceState` gains per-slot FX state. New `BlipFxChain.cs` implements 4 no-delay-buffer processors (bit-crush, ring-mod, soft-clip, DC blocker); Comb/Allpass/Chorus/Flanger return passthrough stubs until Stage 5.2. `BlipVoice.Render` FX loop wired post-envelope — empty chain = passthrough, MVP golden fixtures unaffected.

**Exit:**

- `BlipFxKind` enum in `BlipPatchTypes.cs`: None=0/BitCrush=1/RingMod=2/SoftClip=3/DcBlocker=4/Comb=5/Allpass=6/Chorus=7/Flanger=8 (full set; delay-line kinds implemented in Stage 5.2).
- `BlipFxSlot [Serializable] struct` (BlipFxKind kind; float param0, param1, param2) + `BlipFxSlotFlat readonly struct` (mirrors scalars, blittable) — both in `BlipPatchTypes.cs`.
- `BlipPatch` gains `[SerializeField] private BlipFxSlot[] fxChain` (max 4, truncated in `OnValidate`); `BlipPatchFlat` gains `BlipFxSlotFlat fx0,fx1,fx2,fx3` + `int fxSlotCount` inline (matching oscillator inline-triplet pattern at lines 170–181 of `BlipPatchFlat.cs`) + ctor extension.
- `BlipVoiceState` gains `float dcZ1_0..3` (DC blocker per-slot input z-1), `float dcY1_0..3` (DC blocker output z-1), `float ringModPhase_0..3` (ring-mod carrier phase 0..2π). All blittable.
- `Assets/Scripts/Audio/Blip/BlipFxChain.cs` (new): `internal static class BlipFxChain` with `ProcessFx(ref float x, BlipFxKind kind, float p0, float p1, ref float dcZ1, ref float dcY1, ref float ringPhase, int sampleRate)`: BitCrush/RingMod/SoftClip/DcBlocker implemented; Comb/Allpass/Chorus/Flanger return passthrough. Zero allocs; no Unity API.
- `BlipVoice.Render` post-envelope FX loop: unrolled 4-slot dispatch; `BlipNoAllocTests` still green.
- Phase 1 — Types + data model: `BlipFxKind` / `BlipFxSlot` / `BlipFxSlotFlat` in `BlipPatchTypes.cs`; `BlipPatch.fxChain` + `BlipPatchFlat` FX inline fields; `BlipVoiceState` FX state extension.
- Phase 2 — FX kernel + render wire: `BlipFxChain.cs` memoryless cores + `BlipVoice.Render` FX loop + `BlipNoAllocTests` FX variant.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | FX types | **TECH-256** | Done | `BlipFxKind` enum (None=0/BitCrush=1/RingMod=2/SoftClip=3/DcBlocker=4/Comb=5/Allpass=6/Chorus=7/Flanger=8) + `BlipFxSlot [Serializable] struct` (BlipFxKind kind; float param0, param1, param2) + `BlipFxSlotFlat readonly struct` (mirrors scalars, blittable copy ctor) — all added to `BlipPatchTypes.cs`. |
| T15.2 | BlipPatch fxChain + BlipPatchFlat FX inline | **TECH-257** | Done (archived) | `BlipPatch` gains `[SerializeField] private BlipFxSlot[] fxChain`; `OnValidate` truncates to max 4 slots. `BlipPatchFlat` gains `BlipFxSlotFlat fx0,fx1,fx2,fx3` + `int fxSlotCount` inline (matching oscillator inline-triplet at lines 170–181 of `BlipPatchFlat.cs`). `BlipPatchFlat(BlipPatch so, …)` ctor extended to flatten `fxChain`. |
| T15.3 | BlipVoiceState FX state fields | **TECH-258** | Done (archived) | `BlipVoiceState` extended: `float dcZ1_0, dcZ1_1, dcZ1_2, dcZ1_3` (DC blocker input z-1 per slot) + `float dcY1_0, dcY1_1, dcY1_2, dcY1_3` (DC blocker output z-1) + `float ringModPhase_0, ringModPhase_1, ringModPhase_2, ringModPhase_3` (ring-mod carrier phase). All blittable. Delay write-heads (`delayWritePos_N`) land in Stage 5.2 T5.2.1; LFO phases in Stage 5.3 T5.3.2. |
| T15.4 | BlipFxChain.cs memoryless cores | **TECH-259** | Done (archived) | New `Assets/Scripts/Audio/Blip/BlipFxChain.cs`: `internal static class BlipFxChain`. `static void ProcessFx(ref float x, BlipFxKind kind, float p0, float p1, ref float dcZ1, ref float dcY1, ref float ringPhase, int sampleRate)`: BitCrush `x=Mathf.Round(x*steps)/steps, steps=1<<(int)p0`; RingMod `ringPhase+=2π*p0/sampleRate; x*=Mathf.Sin(ringPhase)`; SoftClip `x=x/(1f+Mathf.Abs(x))`; DcBlocker `float y=x-dcZ1+0.9995f*dcY1; dcZ1=x; dcY1=y; x=y`; Comb/Allpass/Chorus/Flanger → passthrough (stubs). Zero allocs; no Unity API. |
| T15.5 | BlipVoice.Render FX loop + NoAlloc extension | **TECH-260** | Done (archived) | Post-envelope FX dispatch in `BlipVoice.Render`: unrolled `if (patch.fxSlotCount >= 1) BlipFxChain.ProcessFx(ref sample, patch.fx0.kind, patch.fx0.param0, patch.fx0.param1, ref state.dcZ1_0, ref state.dcY1_0, ref state.ringModPhase_0, sampleRate)` … (4 slots, no array alloc). Empty chain (`fxSlotCount=0`) fast-exits. `BlipNoAllocTests` gains `Render_WithFxChain_ZeroManagedAlloc` — 2-slot BitCrush+DcBlocker patch; assert delta/call ≤ 0. |

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
