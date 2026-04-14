using System;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipOscillatorBank — per-sample oscillator helpers for BlipVoice kernel.
    // Static class; zero allocs; no Unity API; blittable-only inputs.
    // Phase state: double radians [0..2π), advanced + wrapped per sample.
    // -------------------------------------------------------------------------

    internal static class BlipOscillatorBank
    {
        private const double TwoPi = 2.0 * Math.PI;

        // -----------------------------------------------------------------------
        // Public entry point
        // -----------------------------------------------------------------------

        /// <summary>
        /// Compute one output sample for <paramref name="osc"/>, advancing
        /// <paramref name="phase"/> by one sample step.
        /// </summary>
        /// <param name="osc">Oscillator parameters (waveform, frequency, detune, duty).</param>
        /// <param name="sampleRate">Audio sample rate in Hz. Zero or negative → returns 0.</param>
        /// <param name="phase">
        /// Per-voice phase accumulator in radians [0..2π). Mutated in-place.
        /// Maps to <c>BlipVoiceState.phaseA/B/C</c>.
        /// </param>
        /// <param name="rngState">xorshift32 RNG state; mutated only for <c>NoiseWhite</c>.</param>
        /// <returns>Sample in [-1, 1].</returns>
        internal static float SampleOsc(
            in BlipOscillatorFlat osc,
            int sampleRate,
            ref double phase,
            ref uint rngState)
        {
            // Guard: invalid sample rate → silent, no phase advance.
            if (sampleRate <= 0)
                return 0f;

            // Effective frequency: base Hz × pitch multiplier from detune.
            // detuneCents folded here per-slot (patch-level pitchJitter applied
            // upstream by Render driver T1.3.6 — out of scope).
            double pitchMult = Math.Pow(2.0, osc.detuneCents / 1200.0);
            double effectiveFreq = osc.frequencyHz * pitchMult;

            // Phase advance and wrap at 2π (single-step; freq bounded by patch).
            phase += TwoPi * effectiveFreq / sampleRate;
            if (phase >= TwoPi)
                phase -= TwoPi;

            // Dispatch to per-kind sample math.
            switch (osc.waveform)
            {
                case BlipWaveform.Sine:
                    return SampleSine(phase);

                case BlipWaveform.Triangle:
                    return SampleTriangle(phase);

                case BlipWaveform.Square:
                    return SampleSquare(phase);

                case BlipWaveform.Pulse:
                    return SamplePulse(phase, osc.pulseDuty);

                case BlipWaveform.NoiseWhite:
                    return SampleNoise(ref rngState);

                default:
                    return 0f;
            }
        }

        // -----------------------------------------------------------------------
        // Per-kind sample math
        // -----------------------------------------------------------------------

        /// <summary>Sine — <c>Math.Sin(phase)</c>. Range [-1, 1].</summary>
        private static float SampleSine(double phase)
        {
            return (float)Math.Sin(phase);
        }

        /// <summary>
        /// Triangle — normalize phase to p∈[0,1); return <c>4·|p−0.5|−1</c>.
        /// Range [-1, 1].
        /// </summary>
        private static float SampleTriangle(double phase)
        {
            double p = phase / TwoPi;
            return (float)(4.0 * Math.Abs(p - 0.5) - 1.0);
        }

        /// <summary>
        /// Square — normalize; return +1 when p&lt;0.5, else -1.
        /// Range {-1, +1}.
        /// </summary>
        private static float SampleSquare(double phase)
        {
            double p = phase / TwoPi;
            return p < 0.5 ? 1f : -1f;
        }

        /// <summary>
        /// Pulse — normalize; clamp <paramref name="pulseDuty"/> to [0,1];
        /// return +1 when p&lt;duty, else -1. Range {-1, +1}.
        /// </summary>
        private static float SamplePulse(double phase, float pulseDuty)
        {
            double p = phase / TwoPi;
            // Clamp duty to [0, 1].
            double duty = pulseDuty < 0f ? 0.0 : pulseDuty > 1f ? 1.0 : (double)pulseDuty;
            return p < duty ? 1f : -1f;
        }

        /// <summary>
        /// Noise-white — xorshift32 step on <paramref name="rngState"/>;
        /// map to [-1, 1] via <c>(int)rngState * (1f / int.MaxValue)</c>.
        /// Ignores phase.
        /// Determinism: same initial rngState → identical output sequence.
        /// </summary>
        private static float SampleNoise(ref uint rngState)
        {
            // xorshift32 (Marsaglia 2003).
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            return (int)rngState * (1f / int.MaxValue);
        }
    }
}
