// long-file-allowed: complex algorithm — split would fragment logic without clean boundary
using System;
using Territory.Audio;

namespace Territory.Audio
{
    /// <summary>
    /// POCO service extracted from BlipVoice (Stage 5.2 Tier-C NO-PORT).
    /// Full DSP render kernel: oscillator bank, envelope, LFO routing, FX chain, filter.
    /// Hub (BlipVoice static class) delegates Render + SmoothOnePole here.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// autoReferenced:true on TerritoryDeveloper.Game provides Territory.Audio types implicitly.
    /// </summary>
    public static class BlipVoiceService
    {
        private const double TwoPi = 2.0 * Math.PI;

        /// <summary>
        /// Renders <paramref name="count"/> samples into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Back-compat overload — no delay buffers (passthrough FX slots).
        /// </summary>
        public static void Render(
            Span<float>       buffer,
            int               offset,
            int               count,
            int               sampleRate,
            in BlipPatchFlat  patch,
            int               variantIndex,
            ref BlipVoiceState state)
        {
            int wp0 = 0, wp1 = 0, wp2 = 0, wp3 = 0;
            Render(buffer, offset, count, sampleRate, in patch, variantIndex, ref state,
                null, null, null, null, 0, 0, 0, 0,
                ref wp0, ref wp1, ref wp2, ref wp3);
        }

        /// <summary>
        /// Renders <paramref name="count"/> samples into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Full overload with pre-leased delay-line buffers for FX slots 0..3.
        /// </summary>
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
            float alpha;
            if (patch.filter.kind == BlipFilterKind.LowPass)
            {
                alpha = 1f - (float)Math.Exp(-TwoPi * patch.filter.cutoffHz / sampleRate);
                if (alpha < 0f) alpha = 0f;
                if (alpha > 1f) alpha = 1f;
            }
            else
            {
                alpha = 1f;
            }

            // -----------------------------------------------------------------
            // TECH-435 — Biquad BP coefficient pre-compute
            // -----------------------------------------------------------------
            float a1n = 0f, a2n = 0f, b0n = 0f;
            if (patch.filter.kind == BlipFilterKind.BandPass)
            {
                double w0    = TwoPi * patch.filter.cutoffHz / sampleRate;
                double cosW0 = Math.Cos(w0);
                double sinW0 = Math.Sin(w0);
                double q     = patch.filter.resonanceQ;
                if (q < 0.0001) q = 0.0001;
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
            int attackSamples  = BlipEnvelopeStepper.MsToSamples(patch.envelope.attackMs,  sampleRate);
            int holdSamples    = BlipEnvelopeStepper.MsToSamples(patch.envelope.holdMs,    sampleRate);
            int decaySamples   = BlipEnvelopeStepper.MsToSamples(patch.envelope.decayMs,   sampleRate);
            int releaseSamples = BlipEnvelopeStepper.MsToSamples(patch.envelope.releaseMs, sampleRate);

            float releaseStartLevel = state.envLevel;

            // -----------------------------------------------------------------
            // Stage 5.3 Phase 2 — LFO pre-computes
            // -----------------------------------------------------------------
            float  lfoSmCoef    = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate);
            double lfoPhaseInc0 = TwoPi * patch.lfo0Flat.rateHz / sampleRate;
            double lfoPhaseInc1 = TwoPi * patch.lfo1Flat.rateHz / sampleRate;

            bool lfo0Active = patch.lfo0Flat.kind != BlipLfoKind.Off;
            bool lfo1Active = patch.lfo1Flat.kind != BlipLfoKind.Off;

            float gainMult;
            float panOffset;

            if (patch.deterministic)
            {
                gainMult  = 1f;
                panOffset = 0f;
                state.rngState = (uint)(variantIndex + 1);

                BlipOscillatorFlat loc0 = patch.osc0;
                BlipOscillatorFlat loc1 = patch.osc1;
                BlipOscillatorFlat loc2 = patch.osc2;

                state.panOffset = panOffset;

                for (int i = 0; i < count; i++)
                {
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
                                lfo0Raw = (float)Math.Sin(phaseBefore0); break;
                            case BlipLfoKind.Triangle:
                                lfo0Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore0))); break;
                            case BlipLfoKind.Square:
                                lfo0Raw = (float)Math.Sign(Math.Sin(phaseBefore0)); break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped0) state.lfoShVal0 = SampleJitterUniform(ref state.rngState);
                                lfo0Raw = state.lfoShVal0; break;
                            default:
                                lfo0Raw = 0f; break;
                        }
                        lfo0Raw *= patch.lfo0Flat.depth;

                        switch (patch.lfo0Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm0Pitch, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm0Gain, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm0Cutoff, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm0Pan, lfo0Raw, lfoSmCoef); break;
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
                                lfo1Raw = (float)Math.Sin(phaseBefore1); break;
                            case BlipLfoKind.Triangle:
                                lfo1Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore1))); break;
                            case BlipLfoKind.Square:
                                lfo1Raw = (float)Math.Sign(Math.Sin(phaseBefore1)); break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped1) state.lfoShVal1 = SampleJitterUniform(ref state.rngState);
                                lfo1Raw = state.lfoShVal1; break;
                            default:
                                lfo1Raw = 0f; break;
                        }
                        lfo1Raw *= patch.lfo1Flat.depth;

                        switch (patch.lfo1Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm1Pitch, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm1Gain, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm1Cutoff, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm1Pan, lfo1Raw, lfoSmCoef); break;
                        }
                    }
                    else
                    {
                        state.lfoPhase1 += lfoPhaseInc1;
                        if (state.lfoPhase1 >= TwoPi) state.lfoPhase1 -= TwoPi;
                    }

                    BlipOscillatorFlat s0 = pitchModCents == 0f ? loc0 : new BlipOscillatorFlat(loc0, loc0.detuneCents + pitchModCents);
                    BlipOscillatorFlat s1 = pitchModCents == 0f ? loc1 : new BlipOscillatorFlat(loc1, loc1.detuneCents + pitchModCents);
                    BlipOscillatorFlat s2 = pitchModCents == 0f ? loc2 : new BlipOscillatorFlat(loc2, loc2.detuneCents + pitchModCents);

                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += s0.gain * BlipOscillatorBank.SampleOsc(s0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += s1.gain * BlipOscillatorBank.SampleOsc(s1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += s2.gain * BlipOscillatorBank.SampleOsc(s2, sampleRate, ref state.phaseC, ref state.rngState);

                    BlipEnvStage stageBefore = state.envStage;
                    BlipEnvelopeStepper.Advance(ref state, patch.envelope, patch.durationSeconds, sampleRate, offset + i);

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
                        patch.envelope, state.envStage, state.samplesElapsed, stageBudget, releaseStartLevel);

                    float x = oscSum * state.envLevel * gainMult * gainModMult;

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

                    float alphaThis = alpha;
                    if (cutoffModHz != 0f && patch.filter.kind == BlipFilterKind.LowPass)
                    {
                        float modCutoff = patch.filter.cutoffHz + cutoffModHz;
                        if (modCutoff < 1f) modCutoff = 1f;
                        alphaThis = 1f - (float)Math.Exp(-TwoPi * modCutoff / sampleRate);
                        if (alphaThis < 0f) alphaThis = 0f;
                        if (alphaThis > 1f) alphaThis = 1f;
                    }

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
                uint seed = (uint)(variantIndex * 0x9E3779B9) ^ state.rngState;
                if (seed == 0u) seed = 0x9E3779B9u;
                state.rngState = seed;

                float cents = SampleJitter(ref state.rngState, patch.pitchJitterCents);
                float db    = SampleJitter(ref state.rngState, patch.gainJitterDb);
                panOffset   = SampleJitter(ref state.rngState, patch.panJitter);

                gainMult = (float)Math.Pow(10.0, db / 20.0);

                BlipOscillatorFlat loc0 = new BlipOscillatorFlat(patch.osc0, patch.osc0.detuneCents + cents);
                BlipOscillatorFlat loc1 = new BlipOscillatorFlat(patch.osc1, patch.osc1.detuneCents + cents);
                BlipOscillatorFlat loc2 = new BlipOscillatorFlat(patch.osc2, patch.osc2.detuneCents + cents);

                state.panOffset = panOffset;

                for (int i = 0; i < count; i++)
                {
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
                                lfo0Raw = (float)Math.Sin(phaseBefore0); break;
                            case BlipLfoKind.Triangle:
                                lfo0Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore0))); break;
                            case BlipLfoKind.Square:
                                lfo0Raw = (float)Math.Sign(Math.Sin(phaseBefore0)); break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped0) state.lfoShVal0 = SampleJitterUniform(ref state.rngState);
                                lfo0Raw = state.lfoShVal0; break;
                            default:
                                lfo0Raw = 0f; break;
                        }
                        lfo0Raw *= patch.lfo0Flat.depth;

                        switch (patch.lfo0Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm0Pitch, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm0Gain, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm0Cutoff, lfo0Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm0Pan, lfo0Raw, lfoSmCoef); break;
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
                                lfo1Raw = (float)Math.Sin(phaseBefore1); break;
                            case BlipLfoKind.Triangle:
                                lfo1Raw = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phaseBefore1))); break;
                            case BlipLfoKind.Square:
                                lfo1Raw = (float)Math.Sign(Math.Sin(phaseBefore1)); break;
                            case BlipLfoKind.SampleAndHold:
                                if (wrapped1) state.lfoShVal1 = SampleJitterUniform(ref state.rngState);
                                lfo1Raw = state.lfoShVal1; break;
                            default:
                                lfo1Raw = 0f; break;
                        }
                        lfo1Raw *= patch.lfo1Flat.depth;

                        switch (patch.lfo1Flat.route)
                        {
                            case BlipLfoRoute.Pitch:
                                pitchModCents += SmoothOnePole(ref state.lfoSm1Pitch, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Gain:
                                gainModMult *= 1f + SmoothOnePole(ref state.lfoSm1Gain, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.FilterCutoff:
                                cutoffModHz += SmoothOnePole(ref state.lfoSm1Cutoff, lfo1Raw, lfoSmCoef); break;
                            case BlipLfoRoute.Pan:
                                panModOffset += SmoothOnePole(ref state.lfoSm1Pan, lfo1Raw, lfoSmCoef); break;
                        }
                    }
                    else
                    {
                        state.lfoPhase1 += lfoPhaseInc1;
                        if (state.lfoPhase1 >= TwoPi) state.lfoPhase1 -= TwoPi;
                    }

                    BlipOscillatorFlat s0 = pitchModCents == 0f ? loc0 : new BlipOscillatorFlat(loc0, loc0.detuneCents + pitchModCents);
                    BlipOscillatorFlat s1 = pitchModCents == 0f ? loc1 : new BlipOscillatorFlat(loc1, loc1.detuneCents + pitchModCents);
                    BlipOscillatorFlat s2 = pitchModCents == 0f ? loc2 : new BlipOscillatorFlat(loc2, loc2.detuneCents + pitchModCents);

                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += s0.gain * BlipOscillatorBank.SampleOsc(s0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += s1.gain * BlipOscillatorBank.SampleOsc(s1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += s2.gain * BlipOscillatorBank.SampleOsc(s2, sampleRate, ref state.phaseC, ref state.rngState);

                    BlipEnvStage stageBefore = state.envStage;
                    BlipEnvelopeStepper.Advance(ref state, patch.envelope, patch.durationSeconds, sampleRate, offset + i);

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
                        patch.envelope, state.envStage, state.samplesElapsed, stageBudget, releaseStartLevel);

                    float x = oscSum * state.envLevel * gainMult * gainModMult;

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

                    float alphaThis = alpha;
                    if (cutoffModHz != 0f && patch.filter.kind == BlipFilterKind.LowPass)
                    {
                        float modCutoff = patch.filter.cutoffHz + cutoffModHz;
                        if (modCutoff < 1f) modCutoff = 1f;
                        alphaThis = 1f - (float)Math.Exp(-TwoPi * modCutoff / sampleRate);
                        if (alphaThis < 0f) alphaThis = 0f;
                        if (alphaThis > 1f) alphaThis = 1f;
                    }

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

        /// <summary>
        /// Advances a one-pole smoothing filter one step toward <paramref name="target"/>.
        /// <paramref name="coef"/> = 1 - exp(-2π * fc / sampleRate); fc ≈ 50 Hz → ~20 ms τ.
        /// </summary>
        public static float SmoothOnePole(ref float z, float target, float coef)
        {
            z += coef * (target - z);
            return z;
        }

        // -----------------------------------------------------------------------
        // Jitter sampling helpers (internal — zero alloc)
        // -----------------------------------------------------------------------

        internal static float SampleJitterUniform(ref uint rngState)
        {
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            float t = (rngState & 0x7FFFFFFFu) * (1f / 0x80000000u);
            return t * 2f - 1f;
        }

        internal static float SampleJitter(ref uint rngState, float range)
        {
            if (range == 0f) return 0f;
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            float t = (rngState & 0x7FFFFFFFu) * (1f / 0x80000000u);
            return (t * 2f - 1f) * range;
        }
    }
}
