using System;
using System.Buffers;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipDelayPool — ArrayPool<float> wrapper for delay-line FX buffers.
    // Owned by BlipCatalog; callers (BlipBaker) lease before Render, return after.
    // Zero managed alloc in audio hot path (Stage 5.2 no-alloc contract).
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plain service class that leases and returns <c>float[]</c> delay-line
    /// buffers via <see cref="ArrayPool{T}.Shared"/>.
    /// <para>
    /// Owned by <see cref="BlipCatalog"/> (field-initializer; no MonoBehaviour
    /// lifecycle needed — invariant #4 compliant).
    /// </para>
    /// </summary>
    internal sealed class BlipDelayPool
    {
        /// <summary>
        /// Rents a delay-line buffer sized to hold at least
        /// <paramref name="maxDelayMs"/> milliseconds at <paramref name="sampleRate"/>
        /// plus one guard sample.
        /// </summary>
        /// <param name="sampleRate">DSP sample rate (e.g. 44100, 48000).</param>
        /// <param name="maxDelayMs">Maximum delay in milliseconds the buffer must accommodate.</param>
        /// <returns>
        /// A rented array from <see cref="ArrayPool{T}.Shared"/>. Caller must
        /// return it via <see cref="Return"/> when done. Length may exceed the
        /// minimum — always use the computed length, not <c>arr.Length</c>.
        /// </returns>
        public float[] Lease(int sampleRate, float maxDelayMs)
        {
            int len = (int)Math.Ceiling(maxDelayMs / 1000f * sampleRate) + 1;
            return ArrayPool<float>.Shared.Rent(len);
        }

        /// <summary>
        /// Returns a previously rented buffer to <see cref="ArrayPool{T}.Shared"/>.
        /// Always clears the array to prevent stale-sample leakage across lease reuse.
        /// </summary>
        /// <param name="buf">Buffer previously obtained from <see cref="Lease"/>.</param>
        public void Return(float[] buf) =>
            ArrayPool<float>.Shared.Return(buf, clearArray: true);
    }
}
