using System;

namespace Territory.Audio
{
    // =========================================================================
    // BlipVoice — per-sample DSP render kernel for Blip procedural SFX.
    // =========================================================================
    // Static class; zero managed allocs inside Render; no Unity API.
    // All per-voice state is carried in ref BlipVoiceState (caller-owned).
    // Shared kernel: used by BlipBaker (Step 2) + BlipLiveHost (post-MVP).
    //
    // One-pole LP filter (α coefficient + per-sample z1 recursion).
    // Render driver (signature, osc bank, envelope, filter, write).
    // =========================================================================

    /// <summary>
    /// Static DSP kernel for blip voice rendering.
    /// No Unity API, no managed allocations inside <see cref="Render"/>.
    /// </summary>
    public static class BlipVoice
    {
        private const double TwoPi = 2.0 * Math.PI;

        // -----------------------------------------------------------------------
        // Render — per-sample driver loop
        // -----------------------------------------------------------------------

        /// <summary>
        /// Renders <paramref name="count"/> samples of a blip voice into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Samples are ADDED to the existing buffer contents (mix-in semantics).
        /// </summary>
        /// <param name="buffer">Target sample buffer (float PCM, any sample rate).</param>
        /// <param name="offset">First sample index to write within <paramref name="buffer"/>.</param>
        /// <param name="count">Number of samples to render. Must be &gt;= 0.</param>
        /// <param name="sampleRate">Audio sample rate in Hz. Must be &gt; 0.</param>
        /// <param name="patch">Immutable blittable patch parameters.</param>
        /// <param name="variantIndex">Round-robin variant selector (0-based).</param>
        /// <param name="state">Per-voice DSP state, mutated in place.</param>
        public static void Render(
            Span<float>       buffer,
            int               offset,
            int               count,
            int               sampleRate,
            in BlipPatchFlat  patch,
            int               variantIndex,
            ref BlipVoiceState state)
        {
            // Back-compat shim: delegates to full overload with all-null delay buffers.
            // All-null + zero lens → delay-line kinds skip-op (passthrough).
            // Four throwaway writePos locals satisfy the ref requirement.
            int wp0 = 0, wp1 = 0, wp2 = 0, wp3 = 0;
            Render(buffer, offset, count, sampleRate, in patch, variantIndex, ref state,
                null, null, null, null, 0, 0, 0, 0,
                ref wp0, ref wp1, ref wp2, ref wp3);
        }

        /// <summary>
        /// Renders <paramref name="count"/> samples of a blip voice into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Samples are ADDED to the existing buffer contents (mix-in semantics).
        /// Accepts pre-leased delay-line buffers for FX slots 0..3; pass null per slot
        /// to keep that slot in passthrough mode (safe skip-op — no kernel yet).
        /// </summary>
        /// <param name="buffer">Target sample buffer (float PCM, any sample rate).</param>
        /// <param name="offset">First sample index to write within <paramref name="buffer"/>.</param>
        /// <param name="count">Number of samples to render. Must be &gt;= 0.</param>
        /// <param name="sampleRate">Audio sample rate in Hz. Must be &gt; 0.</param>
        /// <param name="patch">Immutable blittable patch parameters.</param>
        /// <param name="variantIndex">Round-robin variant selector (0-based).</param>
        /// <param name="state">Per-voice DSP state, mutated in place.</param>
        /// <param name="d0">Pre-leased delay buffer for FX slot 0, or null for passthrough.</param>
        /// <param name="d1">Pre-leased delay buffer for FX slot 1, or null for passthrough.</param>
        /// <param name="d2">Pre-leased delay buffer for FX slot 2, or null for passthrough.</param>
        /// <param name="d3">Pre-leased delay buffer for FX slot 3, or null for passthrough.</param>
        /// <param name="len0">Usable sample count in <paramref name="d0"/>.</param>
        /// <param name="len1">Usable sample count in <paramref name="d1"/>.</param>
        /// <param name="len2">Usable sample count in <paramref name="d2"/>.</param>
        /// <param name="len3">Usable sample count in <paramref name="d3"/>.</param>
        /// <param name="writePos0">Write-head position for <paramref name="d0"/> (read/write).</param>
        /// <param name="writePos1">Write-head position for <paramref name="d1"/> (read/write).</param>
        /// <param name="writePos2">Write-head position for <paramref name="d2"/> (read/write).</param>
        /// <param name="writePos3">Write-head position for <paramref name="d3"/> (read/write).</param>
        public static void Render(
            Span<float>        buffer,
            int                offset,
            int                count,
            int                sampleRate,
            in BlipPatchFlat   patch,
            int                variantIndex,
            ref BlipVoiceState state,
            float[]?           d0,
            float[]?           d1,
            float[]?           d2,
            float[]?           d3,
            int                len0,
            int                len1,
            int                len2,
            int                len3,
            ref int            writePos0,
            ref int            writePos1,
            ref int            writePos2,
            ref int            writePos3)
        {
            if (count <= 0 || sampleRate <= 0)
                return;

            // -----------------------------------------------------------------
            // Phase 1 — Coefficient pre-compute (outside sample loop)
            // -----------------------------------------------------------------
            // α for one-pole LP filter: 1 - exp(-2π * cutoffHz / sampleRate).
            // BlipFilterKind.None → α = 1 (passthrough: z1 = x each sample).
            // Clamped to [0, 1] to guard degenerate cutoffHz >= sampleRate/2.
            // Math.Exp called once per Render invocation — not per sample.
            float alpha;
            if (patch.filter.kind == BlipFilterKind.LowPass)
            {
                alpha = 1f - (float)Math.Exp(-TwoPi * patch.filter.cutoffHz / sampleRate);
                if (alpha < 0f) alpha = 0f;
                if (alpha > 1f) alpha = 1f;
            }
            else
            {
                // None → α = 1 collapses z1 = z1 + 1*(x-z1) = x (passthrough).
                // Single kernel path: no branch inside the sample loop.
                alpha = 1f;
            }

            // -----------------------------------------------------------------
            // TECH-435 — Biquad BP coefficient pre-compute (outside sample loop)
            // -----------------------------------------------------------------
            // RBJ constant-skirt-gain BP cookbook. One Math.Sin + one Math.Cos
            // per Render (NOT per sample). a1n/a2n/b0n consumed by per-sample
            // BP kernel (TECH-436). When kind != BandPass these stay 0 and the
            // BP branch is dead; LP / None paths unchanged.
            float a1n = 0f, a2n = 0f, b0n = 0f;
            if (patch.filter.kind == BlipFilterKind.BandPass)
            {
                double w0    = TwoPi * patch.filter.cutoffHz / sampleRate;
                double cosW0 = Math.Cos(w0);
                double sinW0 = Math.Sin(w0);
                double q     = patch.filter.resonanceQ;
                if (q < 0.0001) q = 0.0001;       // guard: clamp upstream is [0.1, 20]
                double alphaB = sinW0 / (2.0 * q);
                double a0     = 1.0 + alphaB;
                double inv    = 1.0 / a0;
                b0n = (float)(alphaB * inv);
                a1n = (float)((-2.0 * cosW0) * inv);
                a2n = (float)((1.0 - alphaB) * inv);
            }

            // -----------------------------------------------------------------
            // TECH-121 Phase 1 — Pre-compute per-invocation envelope budgets
            // -----------------------------------------------------------------
            // Stage sample budgets computed once so the per-sample loop avoids
            // repeated MsToSamples calls (which call Math.Round).
            int attackSamples  = BlipEnvelopeStepper.MsToSamples(patch.envelope.attackMs,  sampleRate);
            int holdSamples    = BlipEnvelopeStepper.MsToSamples(patch.envelope.holdMs,    sampleRate);
            int decaySamples   = BlipEnvelopeStepper.MsToSamples(patch.envelope.decayMs,   sampleRate);
            int releaseSamples = BlipEnvelopeStepper.MsToSamples(patch.envelope.releaseMs, sampleRate);

            // Release-start level snapshot — updated when state enters Release.
            // Initialized to current envLevel so a mid-render transition is smooth.
            float releaseStartLevel = state.envLevel;

            // -----------------------------------------------------------------
            // Stage 5.3 Phase 2 — LFO pre-computes (outside sample loop)
            // -----------------------------------------------------------------
            // lfoSmCoef: 1-pole coefficient for ~20 ms param smoothing (fc = 50 Hz).
            // lfoPhaseInc0/1: phase advance per sample — pre-computed to avoid
            // per-sample divide (hot path at 48 kHz × N voices).
            float  lfoSmCoef    = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate);
            double lfoPhaseInc0 = TwoPi * patch.lfo0Flat.rateHz / sampleRate;
            double lfoPhaseInc1 = TwoPi * patch.lfo1Flat.rateHz / sampleRate;

            // Short-circuit flags: skip entire slot when kind == Off.
            bool lfo0Active = patch.lfo0Flat.kind != BlipLfoKind.Off;
            bool lfo1Active = patch.lfo1Flat.kind != BlipLfoKind.Off;

            // -----------------------------------------------------------------
            // Per-invocation jitter pre-compute (outside loop)
            // -----------------------------------------------------------------
            // Computed once per Render call — NOT per sample.
            // Option B pitch fold: snapshot local BlipOscillatorFlat copies with
            // detuneCents += pitchCents so SampleOsc signature stays frozen.
            // gainMult applied to (oscSum * envLevel) before filter write.
            // panOffset stashed on state for Step 2 BlipBaker mixer consumption.
            float gainMult;
            float panOffset;
            // pitchCents folded into per-slot local copies below.

            if (patch.deterministic)
            {
                // Deterministic path — bypass jitter sampling entirely.
                // Fixed non-zero seed ensures xorshift32 produces a valid sequence
                // (xorshift32 is undefined when state == 0).
                gainMult  = 1f;
                panOffset = 0f;
                state.rngState = (uint)(variantIndex + 1);

                // Pitch: no adjustment — pass patch oscillators directly via locals.
                BlipOscillatorFlat loc0 = patch.osc0;
                BlipOscillatorFlat loc1 = patch.osc1;
                BlipOscillatorFlat loc2 = patch.osc2;

                state.panOffset = panOffset;

                // -----------------------------------------------------------------
                // TECH-121 Phase 1 — Per-sample loop (deterministic)
                // -----------------------------------------------------------------
                for (int i = 0; i < count; i++)
                {
                    // ---------------------------------------------------------
                    // TECH-288 — LFO routing matrix (deterministic branch)
                    // Applies PRE-oscillator (pitch) and PRE-filter (cutoff, gain, pan).
                    // Short-circuit: kind == Off → skip entire slot (preserves golden bit-exactness).
                    // ---------------------------------------------------------
                    float pitchModCents = 0f;
                    float gainModMult   = 1f;
                    float cutoffModHz   = 0f;
                    float panModOffset  = 0f;

                    if (lfo0Active)
                    {
                        // Phase-wrap edge detect for S&H (before advance).
                        double phaseBefore0 = state.lfoPhase0;
                        state.lfoPhase0 += lfoPhaseInc0;
                        bool wrapped0 = state.lfoPhase0 >= TwoPi;
                        if (wrapped0) state.lfoPhase0 -= TwoPi;

                        float lfo0Raw;
                        switch (patch.lfo0Flat.kind)
                        {
                            case BlipLfoKind.Sine:
                                lfo0Raw = (float)Math.Sin(phaseBefore0);
                                break;
                            case BlipLfoKind.Triangle:
                                lfo0Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore0)));
                                break;
                            case BlipLfoKind.Square:
                                lfo0Raw = (float)Math.Sign(Math.Sin(phaseBefore0));
                                break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped0) state.lfoShVal0 = SampleJitterUniform(ref state.rngState);
                                lfo0Raw = state.lfoShVal0;
                                break;
                            default:
                                lfo0Raw = 0f;
                                break;
                        }
                        lfo0Raw *= patch.lfo0Flat.depth;

                        switch (patch.lfo0Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm0Pitch, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm0Gain, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm0Cutoff, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm0Pan, lfo0Raw, lfoSmCoef);
                                break;
                        }
                    }
                    else
                    {
                        // Off — advance phase only (no routing).
                        state.lfoPhase0 += lfoPhaseInc0;
                        if (state.lfoPhase0 >= TwoPi) state.lfoPhase0 -= TwoPi;
                    }

                    if (lfo1Active)
                    {
                        double phaseBefore1 = state.lfoPhase1;
                        state.lfoPhase1 += lfoPhaseInc1;
                        bool wrapped1 = state.lfoPhase1 >= TwoPi;
                        if (wrapped1) state.lfoPhase1 -= TwoPi;

                        float lfo1Raw;
                        switch (patch.lfo1Flat.kind)
                        {
                            case BlipLfoKind.Sine:
                                lfo1Raw = (float)Math.Sin(phaseBefore1);
                                break;
                            case BlipLfoKind.Triangle:
                                lfo1Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore1)));
                                break;
                            case BlipLfoKind.Square:
                                lfo1Raw = (float)Math.Sign(Math.Sin(phaseBefore1));
                                break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped1) state.lfoShVal1 = SampleJitterUniform(ref state.rngState);
                                lfo1Raw = state.lfoShVal1;
                                break;
                            default:
                                lfo1Raw = 0f;
                                break;
                        }
                        lfo1Raw *= patch.lfo1Flat.depth;

                        switch (patch.lfo1Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm1Pitch, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm1Gain, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm1Cutoff, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm1Pan, lfo1Raw, lfoSmCoef);
                                break;
                        }
                    }
                    else
                    {
                        state.lfoPhase1 += lfoPhaseInc1;
                        if (state.lfoPhase1 >= TwoPi) state.lfoPhase1 -= TwoPi;
                    }

                    // Apply LFO pitch mod: re-tune local osc copies for this sample.
                    // BlipOscillatorFlat is a readonly struct — stack copy, zero heap alloc.
                    BlipOscillatorFlat s0 = pitchModCents == 0f ? loc0 : new BlipOscillatorFlat(loc0, loc0.detuneCents + pitchModCents);
                    BlipOscillatorFlat s1 = pitchModCents == 0f ? loc1 : new BlipOscillatorFlat(loc1, loc1.detuneCents + pitchModCents);
                    BlipOscillatorFlat s2 = pitchModCents == 0f ? loc2 : new BlipOscillatorFlat(loc2, loc2.detuneCents + pitchModCents);

                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += s0.gain * BlipOscillatorBank.SampleOsc(
                            s0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += s1.gain * BlipOscillatorBank.SampleOsc(
                            s1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += s2.gain * BlipOscillatorBank.SampleOsc(
                            s2, sampleRate, ref state.phaseC, ref state.rngState);

                    BlipEnvStage stageBefore = state.envStage;
                    BlipEnvelopeStepper.Advance(
                        ref state,
                        patch.envelope,
                        patch.durationSeconds,
                        sampleRate,
                        offset + i);

                    if (stageBefore != BlipEnvStage.Release && state.envStage == BlipEnvStage.Release)
                        releaseStartLevel = state.envLevel;

                    int stageBudget;
                    switch (state.envStage)
                    {
                        case BlipEnvStage.Attack:  stageBudget = attackSamples;  break;
                        case BlipEnvStage.Hold:    stageBudget = holdSamples;    break;
                        case BlipEnvStage.Decay:   stageBudget = decaySamples;   break;
                        case BlipEnvStage.Release: stageBudget = releaseSamples; break;
                        default:                   stageBudget = 0;              break;
                    }

                    state.envLevel = BlipEnvelopeStepper.ComputeLevel(
                        patch.envelope,
                        state.envStage,
                        state.samplesElapsed,
                        stageBudget,
                        releaseStartLevel);

                    // gainMult == 1f in deterministic path; gainModMult applies LFO Gain route.
                    float x = oscSum * state.envLevel * gainMult * gainModMult;

                    // Post-envelope FX dispatch (unrolled 4-slot cascade).
                    // Empty chain (fxSlotCount == 0) fast-exits through all guards — bit-exact with pre-FX path.
                    if (patch.fxSlotCount >= 1)
                        BlipFxChain.ProcessFx(ref x, patch.fx0.kind, patch.fx0.param0, patch.fx0.param1, patch.fx0.param2,
                            ref state.dcZ1_0, ref state.dcY1_0, ref state.ringModPhase_0, sampleRate,
                            d0, len0, ref writePos0);
                    if (patch.fxSlotCount >= 2)
                        BlipFxChain.ProcessFx(ref x, patch.fx1.kind, patch.fx1.param0, patch.fx1.param1, patch.fx1.param2,
                            ref state.dcZ1_1, ref state.dcY1_1, ref state.ringModPhase_1, sampleRate,
                            d1, len1, ref writePos1);
                    if (patch.fxSlotCount >= 3)
                        BlipFxChain.ProcessFx(ref x, patch.fx2.kind, patch.fx2.param0, patch.fx2.param1, patch.fx2.param2,
                            ref state.dcZ1_2, ref state.dcY1_2, ref state.ringModPhase_2, sampleRate,
                            d2, len2, ref writePos2);
                    if (patch.fxSlotCount >= 4)
                        BlipFxChain.ProcessFx(ref x, patch.fx3.kind, patch.fx3.param0, patch.fx3.param1, patch.fx3.param2,
                            ref state.dcZ1_3, ref state.dcY1_3, ref state.ringModPhase_3, sampleRate,
                            d3, len3, ref writePos3);

                    // Apply LFO FilterCutoff mod: compute per-sample α when active.
                    float alphaThis = alpha;
                    if (cutoffModHz != 0f && patch.filter.kind == BlipFilterKind.LowPass)
                    {
                        float modCutoff = patch.filter.cutoffHz + cutoffModHz;
                        if (modCutoff < 1f) modCutoff = 1f;
                        alphaThis = 1f - (float)Math.Exp(-TwoPi * modCutoff / sampleRate);
                        if (alphaThis < 0f) alphaThis = 0f;
                        if (alphaThis > 1f) alphaThis = 1f;
                    }

                    // Update pan offset on state (consumed by BlipBaker mixer).
                    state.panOffset = panOffset + panModOffset;

                    if (patch.filter.kind == BlipFilterKind.BandPass)
                    {
                        float v = x - a1n * state.biquadZ1 - a2n * state.biquadZ2;
                        float y = b0n * v - b0n * state.biquadZ2;
                        state.biquadZ2 = state.biquadZ1;
                        state.biquadZ1 = v;
                        buffer[offset + i] += y;
                    }
                    else
                    {
                        state.filterZ1 += alphaThis * (x - state.filterZ1);
                        buffer[offset + i] += state.filterZ1;
                    }
                }
            }
            else
            {
                // Live path — sample jitter scalars once, apply per-invocation only.
                // Seed mix: (variantIndex * golden-ratio prime) XOR caller voice-hash.
                // Zero-guard: xorshift32 is undefined at state == 0.
                uint seed = (uint)(variantIndex * 0x9E3779B9) ^ state.rngState;
                if (seed == 0u) seed = 0x9E3779B9u;
                state.rngState = seed;

                float cents = SampleJitter(ref state.rngState, patch.pitchJitterCents);
                float db    = SampleJitter(ref state.rngState, patch.gainJitterDb);
                panOffset   = SampleJitter(ref state.rngState, patch.panJitter);

                gainMult = (float)Math.Pow(10.0, db / 20.0);

                // Option B: fold pitchCents into per-slot local osc copies.
                // detuneCents is in cents; pitchMult baked into freq advance inside
                // SampleOsc via its own detuneCents fold — adding cents here is
                // additive in log-frequency space, which is correct.
                // No allocation: BlipOscillatorFlat is a readonly struct (stack copy).
                BlipOscillatorFlat loc0 = new BlipOscillatorFlat(patch.osc0, patch.osc0.detuneCents + cents);
                BlipOscillatorFlat loc1 = new BlipOscillatorFlat(patch.osc1, patch.osc1.detuneCents + cents);
                BlipOscillatorFlat loc2 = new BlipOscillatorFlat(patch.osc2, patch.osc2.detuneCents + cents);

                state.panOffset = panOffset;

                // -----------------------------------------------------------------
                // TECH-121 Phase 1 — Per-sample loop (live / jitter enabled)
                // -----------------------------------------------------------------
                for (int i = 0; i < count; i++)
                {
                    // ---------------------------------------------------------
                    // TECH-288 — LFO routing matrix (live branch)
                    // Structural mirror of deterministic branch above.
                    // ---------------------------------------------------------
                    float pitchModCents = 0f;
                    float gainModMult   = 1f;
                    float cutoffModHz   = 0f;
                    float panModOffset  = 0f;

                    if (lfo0Active)
                    {
                        double phaseBefore0 = state.lfoPhase0;
                        state.lfoPhase0 += lfoPhaseInc0;
                        bool wrapped0 = state.lfoPhase0 >= TwoPi;
                        if (wrapped0) state.lfoPhase0 -= TwoPi;

                        float lfo0Raw;
                        switch (patch.lfo0Flat.kind)
                        {
                            case BlipLfoKind.Sine:
                                lfo0Raw = (float)Math.Sin(phaseBefore0);
                                break;
                            case BlipLfoKind.Triangle:
                                lfo0Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore0)));
                                break;
                            case BlipLfoKind.Square:
                                lfo0Raw = (float)Math.Sign(Math.Sin(phaseBefore0));
                                break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped0) state.lfoShVal0 = SampleJitterUniform(ref state.rngState);
                                lfo0Raw = state.lfoShVal0;
                                break;
                            default:
                                lfo0Raw = 0f;
                                break;
                        }
                        lfo0Raw *= patch.lfo0Flat.depth;

                        switch (patch.lfo0Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm0Pitch, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm0Gain, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm0Cutoff, lfo0Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm0Pan, lfo0Raw, lfoSmCoef);
                                break;
                        }
                    }
                    else
                    {
                        state.lfoPhase0 += lfoPhaseInc0;
                        if (state.lfoPhase0 >= TwoPi) state.lfoPhase0 -= TwoPi;
                    }

                    if (lfo1Active)
                    {
                        double phaseBefore1 = state.lfoPhase1;
                        state.lfoPhase1 += lfoPhaseInc1;
                        bool wrapped1 = state.lfoPhase1 >= TwoPi;
                        if (wrapped1) state.lfoPhase1 -= TwoPi;

                        float lfo1Raw;
                        switch (patch.lfo1Flat.kind)
                        {
                            case BlipLfoKind.Sine:
                                lfo1Raw = (float)Math.Sin(phaseBefore1);
                                break;
                            case BlipLfoKind.Triangle:
                                lfo1Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore1)));
                                break;
                            case BlipLfoKind.Square:
                                lfo1Raw = (float)Math.Sign(Math.Sin(phaseBefore1));
                                break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped1) state.lfoShVal1 = SampleJitterUniform(ref state.rngState);
                                lfo1Raw = state.lfoShVal1;
                                break;
                            default:
                                lfo1Raw = 0f;
                                break;
                        }
                        lfo1Raw *= patch.lfo1Flat.depth;

                        switch (patch.lfo1Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm1Pitch, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm1Gain, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm1Cutoff, lfo1Raw, lfoSmCoef);
                                break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm1Pan, lfo1Raw, lfoSmCoef);
                                break;
                        }
                    }
                    else
                    {
                        state.lfoPhase1 += lfoPhaseInc1;
                        if (state.lfoPhase1 >= TwoPi) state.lfoPhase1 -= TwoPi;
                    }

                    // Apply LFO pitch mod: re-tune local osc copies for this sample.
                    BlipOscillatorFlat s0 = pitchModCents == 0f ? loc0 : new BlipOscillatorFlat(loc0, loc0.detuneCents + pitchModCents);
                    BlipOscillatorFlat s1 = pitchModCents == 0f ? loc1 : new BlipOscillatorFlat(loc1, loc1.detuneCents + pitchModCents);
                    BlipOscillatorFlat s2 = pitchModCents == 0f ? loc2 : new BlipOscillatorFlat(loc2, loc2.detuneCents + pitchModCents);

                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += s0.gain * BlipOscillatorBank.SampleOsc(
                            s0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += s1.gain * BlipOscillatorBank.SampleOsc(
                            s1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += s2.gain * BlipOscillatorBank.SampleOsc(
                            s2, sampleRate, ref state.phaseC, ref state.rngState);

                    BlipEnvStage stageBefore = state.envStage;
                    BlipEnvelopeStepper.Advance(
                        ref state,
                        patch.envelope,
                        patch.durationSeconds,
                        sampleRate,
                        offset + i);

                    if (stageBefore != BlipEnvStage.Release && state.envStage == BlipEnvStage.Release)
                        releaseStartLevel = state.envLevel;

                    int stageBudget;
                    switch (state.envStage)
                    {
                        case BlipEnvStage.Attack:  stageBudget = attackSamples;  break;
                        case BlipEnvStage.Hold:    stageBudget = holdSamples;    break;
                        case BlipEnvStage.Decay:   stageBudget = decaySamples;   break;
                        case BlipEnvStage.Release: stageBudget = releaseSamples; break;
                        default:                   stageBudget = 0;              break;
                    }

                    state.envLevel = BlipEnvelopeStepper.ComputeLevel(
                        patch.envelope,
                        state.envStage,
                        state.samplesElapsed,
                        stageBudget,
                        releaseStartLevel);

                    // Apply gain jitter multiplier + LFO gain mod to pre-filter sample.
                    float x = oscSum * state.envLevel * gainMult * gainModMult;

                    // Post-envelope FX dispatch (unrolled 4-slot cascade).
                    // Empty chain (fxSlotCount == 0) fast-exits through all guards — bit-exact with pre-FX path.
                    if (patch.fxSlotCount >= 1)
                        BlipFxChain.ProcessFx(ref x, patch.fx0.kind, patch.fx0.param0, patch.fx0.param1, patch.fx0.param2,
                            ref state.dcZ1_0, ref state.dcY1_0, ref state.ringModPhase_0, sampleRate,
                            d0, len0, ref writePos0);
                    if (patch.fxSlotCount >= 2)
                        BlipFxChain.ProcessFx(ref x, patch.fx1.kind, patch.fx1.param0, patch.fx1.param1, patch.fx1.param2,
                            ref state.dcZ1_1, ref state.dcY1_1, ref state.ringModPhase_1, sampleRate,
                            d1, len1, ref writePos1);
                    if (patch.fxSlotCount >= 3)
                        BlipFxChain.ProcessFx(ref x, patch.fx2.kind, patch.fx2.param0, patch.fx2.param1, patch.fx2.param2,
                            ref state.dcZ1_2, ref state.dcY1_2, ref state.ringModPhase_2, sampleRate,
                            d2, len2, ref writePos2);
                    if (patch.fxSlotCount >= 4)
                        BlipFxChain.ProcessFx(ref x, patch.fx3.kind, patch.fx3.param0, patch.fx3.param1, patch.fx3.param2,
                            ref state.dcZ1_3, ref state.dcY1_3, ref state.ringModPhase_3, sampleRate,
                            d3, len3, ref writePos3);

                    // Apply LFO FilterCutoff mod: compute per-sample α when active.
                    float alphaThis = alpha;
                    if (cutoffModHz != 0f && patch.filter.kind == BlipFilterKind.LowPass)
                    {
                        float modCutoff = patch.filter.cutoffHz + cutoffModHz;
                        if (modCutoff < 1f) modCutoff = 1f;
                        alphaThis = 1f - (float)Math.Exp(-TwoPi * modCutoff / sampleRate);
                        if (alphaThis < 0f) alphaThis = 0f;
                        if (alphaThis > 1f) alphaThis = 1f;
                    }

                    // Update pan offset on state (consumed by BlipBaker mixer).
                    state.panOffset = panOffset + panModOffset;

                    if (patch.filter.kind == BlipFilterKind.BandPass)
                    {
                        float v = x - a1n * state.biquadZ1 - a2n * state.biquadZ2;
                        float y = b0n * v - b0n * state.biquadZ2;
                        state.biquadZ2 = state.biquadZ1;
                        state.biquadZ1 = v;
                        buffer[offset + i] += y;
                    }
                    else
                    {
                        state.filterZ1 += alphaThis * (x - state.filterZ1);
                        buffer[offset + i] += state.filterZ1;
                    }
                }
            }
        }

        // -----------------------------------------------------------------------
        // SmoothOnePole — 1-pole param-smoothing helper (Stage 5.3)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Advances a one-pole smoothing filter one step toward <paramref name="target"/>.
        /// <paramref name="coef"/> = 1 - exp(-2π * fc / sampleRate); fc ≈ 50 Hz → ~20 ms τ.
        /// Pass <paramref name="z"/> by ref; returns the updated state for convenience.
        /// Zero allocs; pure static — callable from Render + future FX code.
        /// </summary>
        public static float SmoothOnePole(ref float z, float target, float coef)
        {
            z += coef * (target - z);
            return z;
        }

        // -----------------------------------------------------------------------
        // Jitter sampling helpers (private, static, zero alloc)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Samples a uniform value in [-1, 1) via one xorshift32 step.
        /// Used by S&amp;H LFO waveform for held-value resampling (TECH-288).
        /// </summary>
        private static float SampleJitterUniform(ref uint rngState)
        {
            // xorshift32 step (Marsaglia 2003).
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;

            float t = (rngState & 0x7FFFFFFFu) * (1f / 0x80000000u); // [0, 1)
            return t * 2f - 1f;                                        // [-1, 1)
        }

        /// <summary>
        /// Samples a uniform ± jitter value in [-<paramref name="range"/>, <paramref name="range"/>).
        /// Uses one xorshift32 step on <paramref name="rngState"/>.
        /// Returns 0 when <paramref name="range"/> == 0 (no RNG step consumed).
        /// </summary>
        private static float SampleJitter(ref uint rngState, float range)
        {
            if (range == 0f) return 0f;

            // xorshift32 step (Marsaglia 2003).
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;

            // Map uint [0, uint.MaxValue] → float [0, 1), then scale to [-range, range).
            float t = (rngState & 0x7FFFFFFFu) * (1f / 0x80000000u); // [0, 1)
            return (t * 2f - 1f) * range;                             // [-range, range)
        }
    }
}
