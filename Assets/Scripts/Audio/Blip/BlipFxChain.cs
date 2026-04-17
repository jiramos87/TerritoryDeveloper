using System;
using UnityEngine;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipFxChain — per-sample FX dispatcher for BlipVoice kernel.
    // Static class; zero allocs; blittable-only inputs; ref state for statefull
    // cores (DcBlocker, RingMod, Chorus, Flanger). Memoryless cores: BitCrush,
    // RingMod, SoftClip, DcBlocker. All six delay-line kinds active: Comb, Allpass,
    // Chorus, Flanger.
    // -------------------------------------------------------------------------

    internal static class BlipFxChain
    {
        private const double TwoPi = 2.0 * Math.PI;

        /// <summary>
        /// Apply one FX slot in-place on sample <paramref name="x"/>.
        /// </summary>
        /// <param name="x">Audio sample to process (read/write).</param>
        /// <param name="kind">Effect type. <see cref="BlipFxKind.None"/> is passthrough.
        /// Comb, Allpass, Chorus, Flanger all active.</param>
        /// <param name="p0">
        /// Kind-specific param 0.
        /// BitCrush: bit-depth [1..16] — caller must clamp (DL-1; BlipPatch OnValidate).
        /// RingMod: carrier frequency in Hz.
        /// SoftClip/DcBlocker/passthrough: unused.
        /// </param>
        /// <param name="p1">Kind-specific param 1. RingMod: unused. Comb: feedback gain g. Allpass: feedback/feedforward gain g. Chorus/Flanger: depth in ms.</param>
        /// <param name="p2">Kind-specific param 2. Chorus: wet/dry mix [0..1]. Reserved for all other kinds.</param>
        /// <param name="dcZ1">DcBlocker: input z-1 state (per-slot, read/write).</param>
        /// <param name="dcY1">DcBlocker: output z-1 state (per-slot, read/write).</param>
        /// <param name="ringPhase">RingMod: carrier phase in radians [0..2π) (per-slot, read/write).</param>
        /// <param name="sampleRate">Sample rate in Hz. RingMod guard: if &lt;= 0 the arm is a no-op.</param>
        /// <param name="delayBuf">
        /// Optional delay-line buffer pre-leased by <c>BlipBaker</c> for this FX slot.
        /// <c>null</c> → passthrough for all delay-line kinds (Comb/Allpass/Chorus/Flanger).
        /// Memoryless kinds (BitCrush/RingMod/SoftClip/DcBlocker) ignore this parameter.
        /// </param>
        /// <param name="bufLen">Usable sample count in <paramref name="delayBuf"/> (≤ array length).</param>
        /// <param name="writePos">
        /// Per-slot write-head position into <paramref name="delayBuf"/> (read/write).
        /// Ignored when <paramref name="delayBuf"/> is null.
        /// </param>
        internal static void ProcessFx(
            ref float x,
            BlipFxKind kind,
            float p0,
            float p1,
            float p2,
            ref float dcZ1,
            ref float dcY1,
            ref float ringPhase,
            int sampleRate,
            float[]? delayBuf,
            int bufLen,
            ref int writePos)
        {
            switch (kind)
            {
                case BlipFxKind.BitCrush:
                {
                    // steps = 2^bit-depth; caller ensures p0 in [1..16] (DL-1).
                    int steps = 1 << (int)p0;
                    x = Mathf.Round(x * steps) / steps;
                    break;
                }

                case BlipFxKind.RingMod:
                {
                    // Guard matches BlipOscillatorBank.SampleOsc style (DL-2).
                    if (sampleRate <= 0)
                        break;

                    ringPhase += (float)(TwoPi * p0 / sampleRate);
                    if (ringPhase > (float)TwoPi)
                        ringPhase -= (float)TwoPi;

                    x *= Mathf.Sin(ringPhase);
                    break;
                }

                case BlipFxKind.SoftClip:
                {
                    // x / (1 + |x|) — monotonic, odd, bounded (-1,1). No Math lib (DL-4).
                    x = x / (1f + Mathf.Abs(x));
                    break;
                }

                case BlipFxKind.DcBlocker:
                {
                    // Single-pole HP blocker; pole at 0.9995 ~ -3 dB @ 3.5 Hz / 44.1 kHz (DL-3).
                    float y = x - dcZ1 + 0.9995f * dcY1;
                    dcZ1 = x;
                    dcY1 = y;
                    x    = y;
                    break;
                }

                case BlipFxKind.Comb:
                {
                    // Feedback comb: y[n] = x[n] + g·y[n-D]; d[w] = y (wet write feeds recursion); w = (w+1)%len.
                    // p0 = delay ms → samples D; p1 = feedback gain g (clamped [0,0.97] in OnValidate).
                    // Null/out-of-range guard → passthrough (matches Stage 5.1 convention).
                    if (delayBuf == null || bufLen <= 0) break;
                    int D = (int)(p0 / 1000f * sampleRate);
                    if (D < 1 || D >= bufLen) break;
                    int readIdx = writePos - D;
                    if (readIdx < 0) readIdx += bufLen;
                    float delayed = delayBuf[readIdx];
                    float y = x + p1 * delayed;
                    delayBuf[writePos] = y;   // wet write — y[n-D] feeds next tap
                    writePos = (writePos + 1) % bufLen;
                    x = y;
                    break;
                }

                case BlipFxKind.Allpass:
                {
                    // Schroeder all-pass: unity magnitude, phase-only modification.
                    // v = d[r]; d[w] = x + g*v; y = v - g*d[w]; w = (w+1)%len; x = y.
                    // p0 = delay ms → samples D; p1 = feedback/feedforward gain g.
                    // no clamp — Schroeder stable for |g| < 1
                    if (delayBuf == null || bufLen <= 0) break;
                    int D = (int)(p0 / 1000f * sampleRate);
                    if (D < 1 || D >= bufLen) break;
                    float g = p1;
                    int readIdx = writePos - D;
                    if (readIdx < 0) readIdx += bufLen;
                    float v = delayBuf[readIdx];
                    delayBuf[writePos] = x + g * v;
                    float y = v - g * delayBuf[writePos];
                    writePos = (writePos + 1) % bufLen;
                    x = y;
                    break;
                }

                case BlipFxKind.Chorus:
                {
                    // 2-tap LFO-modulated delay.
                    // p0 = rate Hz, p1 = depth ms, p2 = wet/dry mix [0..1].
                    // ringPhase reused as LFO phase (mutually exclusive w/ RingMod per slot — one kind per slot).
                    // Nearest-neighbour taps; linear interp deferred to a later Stage 5.x revisit.
                    if (delayBuf == null || bufLen <= 0) break;

                    ringPhase += (float)(TwoPi * p0 / sampleRate);
                    if (ringPhase >= (float)TwoPi) ringPhase -= (float)TwoPi;

                    float depthSamples = p1 / 1000f * sampleRate;
                    float lfoOffset    = depthSamples * (float)Math.Sin(ringPhase);

                    // Base delay at buffer midpoint so ±lfoOffset stays in range for depth ≤ bufLen/2 samples.
                    float center = bufLen * 0.5f;
                    int tap0 = ((int)Math.Floor(writePos - center - lfoOffset) % bufLen + bufLen) % bufLen;
                    int tap1 = ((int)Math.Floor(writePos - center + lfoOffset) % bufLen + bufLen) % bufLen;

                    float wet = 0.5f * (delayBuf[tap0] + delayBuf[tap1]);
                    delayBuf[writePos] = x;
                    writePos = (writePos + 1) % bufLen;
                    x = (1f - p2) * x + p2 * wet;
                    break;
                }

                case BlipFxKind.Flanger:
                {
                    // Identical kernel to Chorus; p1 already clamped [1f, 10f] ms by OnValidate.
                    // p0 = rate Hz, p1 = depth ms (1..10), p2 = wet/dry mix [0..1].
                    // ringPhase reused as LFO phase (mutually exclusive w/ RingMod per slot).
                    if (delayBuf == null || bufLen <= 0) break;

                    ringPhase += (float)(TwoPi * p0 / sampleRate);
                    if (ringPhase >= (float)TwoPi) ringPhase -= (float)TwoPi;

                    float depthSamples = p1 / 1000f * sampleRate;
                    float lfoOffset    = depthSamples * (float)Math.Sin(ringPhase);

                    float center = bufLen * 0.5f;
                    int tap0 = ((int)Math.Floor(writePos - center - lfoOffset) % bufLen + bufLen) % bufLen;
                    int tap1 = ((int)Math.Floor(writePos - center + lfoOffset) % bufLen + bufLen) % bufLen;

                    float wet = 0.5f * (delayBuf[tap0] + delayBuf[tap1]);
                    delayBuf[writePos] = x;
                    writePos = (writePos + 1) % bufLen;
                    x = (1f - p2) * x + p2 * wet;
                    break;
                }

                // None + any future unrecognised value → passthrough (DL-5).
                default:
                    break;
            }
        }
    }
}
