using System;

namespace Territory.Audio
{
    // =========================================================================
    // BlipEnvelopeStepper — AHDSR envelope state-machine step helper
    // =========================================================================
    // Pure static helper: zero allocs, no Unity API, no singletons.
    // Caller (BlipVoice.Render) invokes Advance once per sample.
    // NOTE: Named BlipEnvelopeStepper (not BlipEnvelope) to avoid CS0101 collision
    //       with the BlipEnvelope patch-data struct in BlipPatchTypes.cs.
    // =========================================================================

    /// <summary>
    /// AHDSR envelope state-machine stepper for <see cref="BlipVoice"/>.
    /// All methods are pure static; state is mutated via <c>ref BlipVoiceState</c>.
    /// No Unity API, no allocs, no managed references.
    /// </summary>
    public static class BlipEnvelopeStepper
    {
        // -----------------------------------------------------------------
        // Phase 1 — Stage budget helper
        // -----------------------------------------------------------------

        /// <summary>
        /// Converts a duration in milliseconds to a sample count.
        /// Returns 0 when <paramref name="ms"/> &lt;= 0 (enables Decay/Hold skip).
        /// Returns at least 1 sample for any positive <paramref name="ms"/>.
        /// </summary>
        /// <param name="ms">Duration in milliseconds.</param>
        /// <param name="sampleRate">Audio sample rate in Hz.</param>
        /// <returns>Sample count (&gt;= 0).</returns>
        public static int MsToSamples(float ms, int sampleRate)
        {
            if (ms <= 0f) return 0;
            int samples = (int)Math.Round(sampleRate * ms / 1000.0, MidpointRounding.AwayFromZero);
            return samples < 1 ? 1 : samples;
        }

        // -----------------------------------------------------------------
        // Phase 2 helpers — budget + next-stage mapping
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns the sample budget for <paramref name="stage"/>.
        /// Sustain returns 0 (unbounded — exits only on Release trigger).
        /// Idle returns 0 (transitions immediately to Attack on first tick).
        /// </summary>
        private static int BudgetFor(BlipEnvStage stage, in BlipEnvelopeFlat env, int sampleRate)
        {
            switch (stage)
            {
                case BlipEnvStage.Idle:    return 0;
                case BlipEnvStage.Attack:  return MsToSamples(env.attackMs,  sampleRate);
                case BlipEnvStage.Hold:    return MsToSamples(env.holdMs,    sampleRate);
                case BlipEnvStage.Decay:   return MsToSamples(env.decayMs,   sampleRate);
                case BlipEnvStage.Sustain: return 0; // unbounded; no elapsed check
                case BlipEnvStage.Release: return MsToSamples(env.releaseMs, sampleRate);
                default:                   return 0;
            }
        }

        /// <summary>
        /// Returns the next stage after <paramref name="current"/> expires its budget.
        /// Applies skip rules: Hold budget 0 skips to Decay/Sustain; Decay budget 0 skips to Sustain.
        /// </summary>
        private static BlipEnvStage NextStage(BlipEnvStage current, in BlipEnvelopeFlat env, int sampleRate)
        {
            switch (current)
            {
                case BlipEnvStage.Idle:
                    return BlipEnvStage.Attack;

                case BlipEnvStage.Attack:
                    // Hold budget 0 → skip Hold
                    if (MsToSamples(env.holdMs, sampleRate) == 0)
                        return MsToSamples(env.decayMs, sampleRate) == 0
                            ? BlipEnvStage.Sustain
                            : BlipEnvStage.Decay;
                    return BlipEnvStage.Hold;

                case BlipEnvStage.Hold:
                    // Decay budget 0 → skip Decay (sustain-only fallback)
                    return MsToSamples(env.decayMs, sampleRate) == 0
                        ? BlipEnvStage.Sustain
                        : BlipEnvStage.Decay;

                case BlipEnvStage.Decay:
                    return BlipEnvStage.Sustain;

                case BlipEnvStage.Sustain:
                    return BlipEnvStage.Release;

                case BlipEnvStage.Release:
                    return BlipEnvStage.Idle;

                default:
                    return BlipEnvStage.Idle;
            }
        }

        // -----------------------------------------------------------------
        // ComputeLevel — per-sample envelope level math (Linear + Exponential)
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns the envelope level in [0, 1] for the current stage and sample position.
        /// Pure static, allocation-free, no Unity API.
        /// Flat-level stages: Idle → 0, Hold → 1, Sustain → env.sustainLevel.
        /// Ramping stages (Attack / Decay / Release): routed by per-stage shape selector
        /// (Linear straight ramp; Exponential target+(start-target)*exp(-t/τ), τ=stageDur/4).
        /// When <paramref name="stageDurationSamples"/> &lt;= 0, returns <c>target</c> instantly
        /// (matches TECH-118 budget-0 skip semantics; prevents divide-by-zero).
        /// </summary>
        /// <param name="env">Immutable blittable envelope parameters (shapes, sustainLevel).</param>
        /// <param name="stage">Current envelope stage.</param>
        /// <param name="samplesElapsed">Samples elapsed within the current stage.</param>
        /// <param name="stageDurationSamples">Total sample budget for the current stage.</param>
        /// <param name="releaseStartLevel">
        /// Envelope level at the moment Release was entered; caller (TECH-121 render driver)
        /// snapshots <c>BlipVoiceState.envLevel</c> when transitioning to Release.
        /// </param>
        /// <returns>Envelope level in [0, 1].</returns>
        public static float ComputeLevel(
            in BlipEnvelopeFlat env,
            BlipEnvStage        stage,
            int                 samplesElapsed,
            int                 stageDurationSamples,
            float               releaseStartLevel)
        {
            // Phase 1 — Flat-level stages
            switch (stage)
            {
                case BlipEnvStage.Idle:    return 0f;
                case BlipEnvStage.Hold:    return 1f;
                case BlipEnvStage.Sustain: return env.sustainLevel;
            }

            // Phase 2 + Phase 3 — Ramping stages (Attack, Decay, Release)
            float start, target;
            BlipEnvShape shape;

            switch (stage)
            {
                case BlipEnvStage.Attack:
                    start  = 0f;
                    target = 1f;
                    shape  = env.attackShape;
                    break;
                case BlipEnvStage.Decay:
                    start  = 1f;
                    target = env.sustainLevel;
                    shape  = env.decayShape;
                    break;
                case BlipEnvStage.Release:
                    start  = releaseStartLevel;
                    target = 0f;
                    shape  = env.releaseShape;
                    break;
                default:
                    return 0f;
            }

            // Guard: zero-duration stage → instant settle
            if (stageDurationSamples <= 0)
                return target;

            switch (shape)
            {
                case BlipEnvShape.Linear:
                {
                    float t = samplesElapsed / (float)stageDurationSamples;
                    if (t < 0f) t = 0f;
                    if (t > 1f) t = 1f;
                    return start + (target - start) * t;
                }
                case BlipEnvShape.Exponential:
                {
                    float tau   = stageDurationSamples / 4f;
                    float level = target + (start - target) * (float)Math.Exp(-samplesElapsed / tau);
                    return level;
                }
                default:
                    return target;
            }
        }

        // -----------------------------------------------------------------
        // Phase 2 + Phase 3 — Advance
        // -----------------------------------------------------------------

        /// <summary>
        /// Advances the AHDSR envelope state machine by one sample.
        /// Mutates <paramref name="state"/>.envStage and <paramref name="state"/>.samplesElapsed in place.
        /// </summary>
        /// <param name="state">Per-voice DSP state (mutated via ref).</param>
        /// <param name="env">Immutable blittable envelope parameters from the flattened patch.</param>
        /// <param name="durationSeconds">One-shot voice duration in seconds; Release fires when
        /// <paramref name="voiceElapsedSamples"/> &gt;= durationSeconds * sampleRate.</param>
        /// <param name="sampleRate">Audio sample rate in Hz.</param>
        /// <param name="voiceElapsedSamples">Total samples rendered for this voice invocation
        /// (maintained by the caller, not stored on BlipVoiceState).</param>
        public static void Advance(
            ref BlipVoiceState state,
            in BlipEnvelopeFlat env,
            float durationSeconds,
            int sampleRate,
            int voiceElapsedSamples)
        {
            // --- Phase 3: one-shot release trigger ---
            // Check before stage budget so that release fires even if still in Attack/Hold/Decay.
            int oneShotEndSamples = MsToSamples(durationSeconds * 1000f, sampleRate);
            bool releaseDue = oneShotEndSamples > 0
                && voiceElapsedSamples >= oneShotEndSamples
                && state.envStage != BlipEnvStage.Release
                && state.envStage != BlipEnvStage.Idle;

            if (releaseDue)
            {
                state.envStage      = BlipEnvStage.Release;
                state.samplesElapsed = 0;
                // Fall through to process the first Release sample this tick.
            }

            // --- Phase 2: per-sample counter + stage transition ---
            // Idle transitions immediately (budget == 0, so elapsed check triggers right away).
            state.samplesElapsed++;
            int budget = BudgetFor(state.envStage, env, sampleRate);

            // Sustain is unbounded (no budget check); all other stages advance when elapsed >= budget.
            if (state.envStage != BlipEnvStage.Sustain && state.samplesElapsed >= budget)
            {
                state.envStage       = NextStage(state.envStage, env, sampleRate);
                state.samplesElapsed = 0;
            }
        }
    }
}
