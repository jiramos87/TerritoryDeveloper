using System.Buffers;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipLutPool — ArrayPool<float> wrapper for waveform LUT buffers.
    // Owned by BlipCatalog; callers lease before use, return after.
    // Zero managed alloc in audio hot path (Stage 5.3 no-alloc contract).
    // Invariant #4 compliant: plain internal sealed class, field-init owned
    // by BlipCatalog — not a singleton, not a MonoBehaviour.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plain service class that leases and returns <c>float[]</c> waveform LUT
    /// buffers via <see cref="ArrayPool{T}.Shared"/>.
    /// <para>
    /// Owned by <see cref="BlipCatalog"/> (field-initializer; no MonoBehaviour
    /// lifecycle needed — invariant #4 compliant).
    /// </para>
    /// <para>
    /// Stub: LUT population (sine/tri/etc. baking) is post-Stage-5.3.
    /// <see cref="Lease"/> returns a rented zeroed array; callers must
    /// populate before use.
    /// </para>
    /// </summary>
    internal sealed class BlipLutPool
    {
        /// <summary>
        /// Rents a LUT buffer of at least <paramref name="size"/> floats.
        /// </summary>
        /// <param name="size">Minimum number of samples required.</param>
        /// <returns>
        /// A rented array from <see cref="ArrayPool{T}.Shared"/>. Caller must
        /// return it via <see cref="Return"/> when done. Length may exceed
        /// <paramref name="size"/> — always use the requested size, not
        /// <c>arr.Length</c>.
        /// </returns>
        public float[] Lease(int size) =>
            ArrayPool<float>.Shared.Rent(size);

        /// <summary>
        /// Returns a previously rented LUT buffer to <see cref="ArrayPool{T}.Shared"/>.
        /// Always clears the array to prevent stale-sample leakage across lease reuse.
        /// </summary>
        /// <param name="arr">Buffer previously obtained from <see cref="Lease"/>; null is a no-op.</param>
        public void Return(float[] arr)
        {
            if (arr == null) return;
            ArrayPool<float>.Shared.Return(arr, clearArray: true);
        }
    }
}
