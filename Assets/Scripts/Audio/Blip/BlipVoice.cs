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
                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += loc0.gain * BlipOscillatorBank.SampleOsc(
                            loc0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += loc1.gain * BlipOscillatorBank.SampleOsc(
                            loc1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += loc2.gain * BlipOscillatorBank.SampleOsc(
                            loc2, sampleRate, ref state.phaseC, ref state.rngState);

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

                    // gainMult == 1f in deterministic path — multiply is identity.
                    float x = oscSum * state.envLevel * gainMult;
                    state.filterZ1 += alpha * (x - state.filterZ1);
                    buffer[offset + i] += state.filterZ1;
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
                    float oscSum = 0f;
                    if (patch.oscillatorCount > 0)
                        oscSum += loc0.gain * BlipOscillatorBank.SampleOsc(
                            loc0, sampleRate, ref state.phaseA, ref state.rngState);
                    if (patch.oscillatorCount > 1)
                        oscSum += loc1.gain * BlipOscillatorBank.SampleOsc(
                            loc1, sampleRate, ref state.phaseB, ref state.rngState);
                    if (patch.oscillatorCount > 2)
                        oscSum += loc2.gain * BlipOscillatorBank.SampleOsc(
                            loc2, sampleRate, ref state.phaseC, ref state.rngState);

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

                    // Apply gain jitter multiplier to pre-filter sample.
                    float x = oscSum * state.envLevel * gainMult;
                    state.filterZ1 += alpha * (x - state.filterZ1);
                    buffer[offset + i] += state.filterZ1;
                }
            }
        }

        // -----------------------------------------------------------------------
        // Jitter sampling helpers (private, static, zero alloc)
        // -----------------------------------------------------------------------

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
