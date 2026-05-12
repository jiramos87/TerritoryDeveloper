using System;

namespace Territory.Audio
{
    // =========================================================================
    // BlipVoice — thin delegate hub for DSP render kernel.
    // Stage 5.2 Tier-C NO-PORT: implementation moved to BlipVoiceService.
    // File path UNCHANGED. Class name UNCHANGED. Namespace UNCHANGED.
    // All public API signatures UNCHANGED — callers unaffected.
    // =========================================================================

    /// <summary>
    /// Static DSP kernel for blip voice rendering.
    /// Delegates to <see cref="BlipVoiceService"/> (Stage 5.2 extract).
    /// No Unity API, no managed allocations inside <see cref="Render"/>.
    /// </summary>
    public static class BlipVoice
    {
        /// <summary>
        /// Renders <paramref name="count"/> samples of a blip voice into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Samples are ADDED to the existing buffer contents (mix-in semantics).
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
            => BlipVoiceService.Render(buffer, offset, count, sampleRate, in patch, variantIndex, ref state);

        /// <summary>
        /// Renders <paramref name="count"/> samples of a blip voice into
        /// <paramref name="buffer"/>[<paramref name="offset"/> .. offset+count).
        /// Samples are ADDED to the existing buffer contents (mix-in semantics).
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
            => BlipVoiceService.Render(
                buffer, offset, count, sampleRate, in patch, variantIndex, ref state,
                d0, d1, d2, d3, len0, len1, len2, len3,
                ref writePos0, ref writePos1, ref writePos2, ref writePos3);

        /// <summary>
        /// Advances a one-pole smoothing filter one step toward <paramref name="target"/>.
        /// Delegates to <see cref="BlipVoiceService.SmoothOnePole"/>.
        /// </summary>
        public static float SmoothOnePole(ref float z, float target, float coef)
            => BlipVoiceService.SmoothOnePole(ref z, target, coef);
    }
}
