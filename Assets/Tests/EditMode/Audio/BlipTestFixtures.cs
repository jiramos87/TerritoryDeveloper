using System;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// Static fixture helpers for Blip EditMode tests.
    /// All methods are pure (no MonoBehaviour, no Unity API, no per-call allocs
    /// beyond the output buffer created by <see cref="RenderPatch"/>).
    /// </summary>
    public static class BlipTestFixtures
    {
        // -----------------------------------------------------------------------
        // RenderPatch — render a full blip voice into a fresh float[]
        // -----------------------------------------------------------------------

        /// <summary>
        /// Allocates <c>sampleRate * seconds</c> samples, renders one blip voice
        /// into it from a fresh <see cref="BlipVoiceState"/>, and returns the buffer.
        /// </summary>
        /// <param name="patch">Immutable patch parameters.</param>
        /// <param name="sampleRate">Sample rate in Hz (e.g. 48000).</param>
        /// <param name="seconds">Duration in whole seconds.</param>
        /// <param name="variantIndex">Round-robin variant selector (0-based).</param>
        /// <returns>PCM float buffer of length <c>sampleRate * seconds</c>.</returns>
        public static float[] RenderPatch(
            in BlipPatchFlat patch,
            int sampleRate,
            int seconds,
            int variantIndex = 0)
        {
            var buf = new float[sampleRate * seconds];
            var state = default(BlipVoiceState);
            BlipVoice.Render(buf, 0, buf.Length, sampleRate, in patch, variantIndex, ref state);
            return buf;
        }

        // -----------------------------------------------------------------------
        // CountZeroCrossings — deterministic sign-flip counter
        // -----------------------------------------------------------------------

        /// <summary>
        /// Counts sign changes between adjacent non-zero samples.
        /// Exact-zero samples are skipped to avoid double-counting on sine
        /// zero crossings that land on integer-period boundaries.
        /// </summary>
        /// <param name="buffer">PCM float buffer to analyse.</param>
        /// <returns>Number of zero crossings.</returns>
        public static int CountZeroCrossings(float[] buffer)
        {
            int crossings = 0;
            int n = buffer.Length;
            float prev = 0f;

            for (int i = 0; i < n; i++)
            {
                float s = buffer[i];
                if (s == 0f)
                    continue;

                if (prev != 0f && ((prev < 0f) != (s < 0f)))
                    crossings++;

                prev = s;
            }

            return crossings;
        }

        // -----------------------------------------------------------------------
        // SampleEnvelopeLevels — abs-value stride subsample
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns a sub-sampled abs-value envelope from <paramref name="buffer"/>.
        /// Element <c>i</c> of the result is <c>Math.Abs(buffer[i * stride])</c>.
        /// Rectified so that bipolar oscillator output yields a monotonic envelope
        /// suitable for slope assertions (TECH-139).
        /// </summary>
        /// <param name="buffer">PCM float buffer to analyse.</param>
        /// <param name="stride">Sample step between envelope readings.</param>
        /// <returns>Array of length <c>buffer.Length / stride</c>.</returns>
        public static float[] SampleEnvelopeLevels(float[] buffer, int stride)
        {
            int count = buffer.Length / stride;
            var out_ = new float[count];
            for (int i = 0; i < count; i++)
                out_[i] = Math.Abs(buffer[i * stride]);
            return out_;
        }

        // -----------------------------------------------------------------------
        // SumAbsHash — L1-norm fingerprint
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the sum of absolute values of all samples in <paramref name="buffer"/>.
        /// Used as a lightweight determinism fingerprint (TECH-140).
        /// </summary>
        /// <param name="buffer">PCM float buffer to hash.</param>
        /// <returns>Sum of absolute sample values (double precision accumulator).</returns>
        public static double SumAbsHash(float[] buffer)
        {
            double acc = 0;
            foreach (float s in buffer)
                acc += Math.Abs(s);
            return acc;
        }
    }
}
