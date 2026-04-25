using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Territory.Audio
{
    /// <summary>
    /// Authored ScriptableObject holding all MVP scalar fields for a Blip sound patch.
    /// No AnimationCurve fields. No mode/BlipMode field (post-MVP).
    /// Clamp guards enforced in OnValidate (see method doc).
    /// Content hash (`patchHash`) recomputed at end of OnValidate.
    /// </summary>
    [CreateAssetMenu(menuName = "Territory/Audio/Blip Patch", fileName = "BlipPatch")]
    public sealed class BlipPatch : ScriptableObject
    {
        // -----------------------------------------------------------------------
        // Oscillators (0..3 layers; array size capped in OnValidate)
        // -----------------------------------------------------------------------
        [SerializeField] private BlipOscillator[] oscillators = new BlipOscillator[0];

        /// <summary>Oscillator layers for this patch (0..3).</summary>
        public BlipOscillator[] Oscillators => oscillators;

        // -----------------------------------------------------------------------
        // FX chain (0..4 slots; array size capped in OnValidate)
        // -----------------------------------------------------------------------
        [SerializeField] private BlipFxSlot[] fxChain = new BlipFxSlot[0];

        /// <summary>FX chain slots for this patch (0..4).</summary>
        public BlipFxSlot[] FxChain => fxChain;

        // -----------------------------------------------------------------------
        // Envelope — AHDSR, per-stage shape; no AnimationCurve
        // -----------------------------------------------------------------------
        [SerializeField] private BlipEnvelope envelope;

        /// <summary>AHDSR envelope parameters.</summary>
        public BlipEnvelope Envelope => envelope;

        // -----------------------------------------------------------------------
        // Filter — one-pole low-pass; no cutoff envelope
        // -----------------------------------------------------------------------
        [SerializeField] private BlipFilter filter;

        /// <summary>One-pole low-pass filter parameters.</summary>
        public BlipFilter Filter => filter;

        // -----------------------------------------------------------------------
        // LFO slots (2 fixed slots; blittable-friendly — TECH-285)
        // -----------------------------------------------------------------------
        [SerializeField] private BlipLfo lfo0;

        /// <summary>First LFO slot. kind=Off disables modulation.</summary>
        public BlipLfo Lfo0 => lfo0;

        [SerializeField] private BlipLfo lfo1;

        /// <summary>Second LFO slot. kind=Off disables modulation.</summary>
        public BlipLfo Lfo1 => lfo1;

        // -----------------------------------------------------------------------
        // Variation
        // -----------------------------------------------------------------------
        [SerializeField] private int variantCount = 1;

        /// <summary>Number of round-robin variants (1..8; clamped in OnValidate).</summary>
        public int VariantCount => variantCount;

        // -----------------------------------------------------------------------
        // Per-invocation jitter triplet
        // -----------------------------------------------------------------------
        [SerializeField] private float pitchJitterCents;

        /// <summary>Per-invocation pitch jitter range in cents (±).</summary>
        public float PitchJitterCents => pitchJitterCents;

        [SerializeField] private float gainJitterDb;

        /// <summary>Per-invocation gain jitter range in dB (±).</summary>
        public float GainJitterDb => gainJitterDb;

        [SerializeField] private float panJitter;

        /// <summary>Per-invocation pan jitter range ([-1..1]).</summary>
        public float PanJitter => panJitter;

        // -----------------------------------------------------------------------
        // Voice management
        // -----------------------------------------------------------------------
        [SerializeField] private int voiceLimit = 1;

        /// <summary>Max concurrent voices for this patch (1..16; clamped in OnValidate).</summary>
        public int VoiceLimit => voiceLimit;

        [SerializeField] private int priority;

        /// <summary>Voice-steal ranking — higher survives steal.</summary>
        public int Priority => priority;

        [SerializeField] private float cooldownMs;

        /// <summary>Minimum inter-play gap in ms (>= 0; clamped in OnValidate).</summary>
        public float CooldownMs => cooldownMs;

        // -----------------------------------------------------------------------
        // Determinism / fixture flag
        // -----------------------------------------------------------------------
        [SerializeField] private bool deterministic;

        /// <summary>When true, jitter is disabled for test fixtures and golden runs.</summary>
        public bool Deterministic => deterministic;

        // -----------------------------------------------------------------------
        // Mixer routing (authoring-only; NOT flattened into BlipPatchFlat)
        // BlipMixerRouter keeps flat struct blittable — Decision Log 2026-04-14.
        // -----------------------------------------------------------------------
        [SerializeField] private AudioMixerGroup mixerGroup;

        /// <summary>
        /// Target mixer group. Authoring-only reference; not copied into BlipPatchFlat.
        /// BlipMixerRouter (Step 2) holds the BlipId → AudioMixerGroup map at runtime.
        /// </summary>
        public AudioMixerGroup MixerGroup => mixerGroup;

        // -----------------------------------------------------------------------
        // Bake parameters
        // -----------------------------------------------------------------------
        [SerializeField] private float durationSeconds = 1f;

        /// <summary>Offline bake length in seconds.</summary>
        public float DurationSeconds => durationSeconds;

        // -----------------------------------------------------------------------
        // Reserved / future
        // -----------------------------------------------------------------------
        [SerializeField] private bool useLutOscillators;

        /// <summary>
        /// Reserved: when true, DSP kernel will use LUT oscillators for bit-exact output.
        /// Unread in MVP — wiring deferred to Stage 1.3+.
        /// </summary>
        public bool UseLutOscillators => useLutOscillators;

        // -----------------------------------------------------------------------
        // Content hash (FNV-1a 32-bit; persisted across Editor reloads)
        // -----------------------------------------------------------------------
        [SerializeField] private int patchHash;

        /// <summary>
        /// FNV-1a 32-bit over scalar fields. Recomputed in OnValidate.
        /// Used as cache key by BlipBaker.
        /// </summary>
        public int PatchHash => patchHash;

        // -----------------------------------------------------------------------
        // Authoring guards — clamped on Inspector edit + domain reload
        // -----------------------------------------------------------------------

        /// <summary>
        /// Clamp authoring fields to safe ranges:
        /// <list type="bullet">
        ///   <item><term>attackMs</term><description>≥ 1 ms (prevents snap-onset click ≈ 48 samples @ 48 kHz).</description></item>
        ///   <item><term>decayMs</term><description>≥ 0 ms (0 = instant drop to sustain level; sustain-only patches supported).</description></item>
        ///   <item><term>releaseMs</term><description>≥ 1 ms (prevents tail click).</description></item>
        ///   <item><term>sustainLevel</term><description>0..1.</description></item>
        ///   <item><term>variantCount</term><description>1..8.</description></item>
        ///   <item><term>voiceLimit</term><description>1..16.</description></item>
        ///   <item><term>cooldownMs</term><description>≥ 0 ms.</description></item>
        ///   <item><term>oscillators</term><description>length capped at 3 (BlipPatchFlat MVP budget).</description></item>
        /// </list>
        /// Appends `patchHash = BlipPatchHash.Compute(this)` at tail.
        /// </summary>
        private void OnValidate()
        {
            envelope.attackMs  = Mathf.Max(1f, envelope.attackMs);
            envelope.decayMs   = Mathf.Max(0f, envelope.decayMs);
            envelope.releaseMs = Mathf.Max(1f, envelope.releaseMs);
            envelope.sustainLevel = Mathf.Clamp01(envelope.sustainLevel);

            variantCount = Mathf.Clamp(variantCount, 1, 8);
            voiceLimit   = Mathf.Clamp(voiceLimit,   1, 16);
            cooldownMs   = Mathf.Max(0f, cooldownMs);

            if (oscillators != null && oscillators.Length > 3)
                Array.Resize(ref oscillators, 3);

            if (fxChain != null && fxChain.Length > 4)
                Array.Resize(ref fxChain, 4);

            // Clamp per-kind params — inserted after length cap, before patchHash recompute.
            // Comb param1 (feedback gain) clamped [0, 0.97] — BIBO stability margin.
            // Flanger param1 (depth ms) clamped [1, 10] — classic flange-sweep range.
            if (fxChain != null)
            {
                for (int i = 0; i < fxChain.Length; i++)
                {
                    if (fxChain[i].kind == BlipFxKind.Comb)
                        fxChain[i].param1 = Mathf.Clamp(fxChain[i].param1, 0f, 0.97f);
                    else if (fxChain[i].kind == BlipFxKind.Flanger)
                        fxChain[i].param1 = Mathf.Clamp(fxChain[i].param1, 1f, 10f);
                }
            }

            // LFO rate clamp (TECH-285). Inserted before patchHash recompute.
            lfo0.rateHz = Mathf.Max(0f, lfo0.rateHz);
            lfo1.rateHz = Mathf.Max(0f, lfo1.rateHz);

            // Biquad BP resonance Q clamp (TECH-434). Must clamp BEFORE patchHash recompute.
            filter.resonanceQ = Mathf.Clamp(filter.resonanceQ, 0.1f, 20f);

            patchHash = BlipPatchHash.Compute(this);
        }

        private void Awake()
        {
            int recomputed = BlipPatchHash.Compute(this);
            if (recomputed != patchHash)
            {
                Debug.LogWarning(
                    $"[BlipPatch] '{name}' patchHash mismatch: stored={patchHash} recomputed={recomputed}. " +
                    "Re-save the asset in the Inspector to fix.");
            }
        }

        private void OnEnable()
        {
            int recomputed = BlipPatchHash.Compute(this);
            if (recomputed != patchHash)
            {
                Debug.LogWarning(
                    $"[BlipPatch] '{name}' patchHash mismatch on enable: stored={patchHash} recomputed={recomputed}. " +
                    "Re-save the asset in the Inspector to fix.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // BlipPatchHash — FNV-1a 32-bit content hash over BlipPatch scalar fields.
    // Field order is frozen. Do NOT reorder — invalidates BlipBaker cache
    // across builds. Future fields append at tail + bump HashVersion.
    // -------------------------------------------------------------------------
    internal static class BlipPatchHash
    {
        private const uint FnvOffsetBasis = 0x811C9DC5u;
        private const uint FnvPrime       = 0x01000193u;

        /// <summary>
        /// Compute FNV-1a 32-bit hash over all scalar fields of <paramref name="so"/>.
        /// Excludes <c>mixerGroup</c> (managed ref) and <c>patchHash</c> itself (circular).
        /// Returns the hash cast to <c>int</c> (bit-identical; sign ignored by cache consumers).
        /// </summary>
        public static int Compute(BlipPatch so)
        {
            uint h = FnvOffsetBasis;

            // 1. oscillatorCount
            int oscCount = so.Oscillators != null ? Mathf.Min(so.Oscillators.Length, 3) : 0;
            FeedInt(ref h, oscCount);

            // 2. Per-oscillator fields
            for (int i = 0; i < oscCount; i++)
            {
                BlipOscillator osc = so.Oscillators[i];
                FeedEnum(ref h, osc.waveform);
                FeedFloat(ref h, osc.frequencyHz);
                FeedFloat(ref h, osc.detuneCents);
                FeedFloat(ref h, osc.pulseDuty);
                FeedFloat(ref h, osc.gain);
            }

            // 3. Envelope
            BlipEnvelope env = so.Envelope;
            FeedFloat(ref h, env.attackMs);
            FeedEnum(ref h, env.attackShape);
            FeedFloat(ref h, env.holdMs);
            FeedFloat(ref h, env.decayMs);
            FeedEnum(ref h, env.decayShape);
            FeedFloat(ref h, env.sustainLevel);
            FeedFloat(ref h, env.releaseMs);
            FeedEnum(ref h, env.releaseShape);

            // 4. Filter
            BlipFilter flt = so.Filter;
            FeedEnum(ref h, flt.kind);
            FeedFloat(ref h, flt.cutoffHz);

            // 5. Variation
            FeedInt(ref h, so.VariantCount);

            // 6. Jitter triplet
            FeedFloat(ref h, so.PitchJitterCents);
            FeedFloat(ref h, so.GainJitterDb);
            FeedFloat(ref h, so.PanJitter);

            // 7. Voice management
            FeedInt(ref h, so.VoiceLimit);
            FeedInt(ref h, so.Priority);
            FeedFloat(ref h, so.CooldownMs);

            // 8. Flags / bake params
            FeedBool(ref h, so.Deterministic);
            FeedFloat(ref h, so.DurationSeconds);
            FeedBool(ref h, so.UseLutOscillators);

            // 9. FX chain (append-only; never reorder preceding sections)
            var fxArr = so.FxChain;
            int fxN = fxArr != null ? Mathf.Min(fxArr.Length, 4) : 0;
            FeedInt(ref h, fxN);
            for (int i = 0; i < fxN; i++)
            {
                BlipFxSlot s = fxArr[i];
                FeedEnum(ref h, s.kind);
                FeedFloat(ref h, s.param0);
                FeedFloat(ref h, s.param1);
                FeedFloat(ref h, s.param2);
            }

            // 10. LFO slots (append-only, TECH-285). Existing assets: one-time stale patchHash
            //     → Awake/OnEnable warning; re-save in Inspector fixes. Never re-order §1-9.
            BlipLfo l0 = so.Lfo0;
            FeedEnum(ref h, l0.kind);
            FeedFloat(ref h, l0.rateHz);
            FeedFloat(ref h, l0.depth);
            FeedEnum(ref h, l0.route);
            BlipLfo l1 = so.Lfo1;
            FeedEnum(ref h, l1.kind);
            FeedFloat(ref h, l1.rateHz);
            FeedFloat(ref h, l1.depth);
            FeedEnum(ref h, l1.route);

            return (int)h;
        }

        private static void FeedInt(ref uint h, int v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            foreach (byte b in bytes)
                h = (h ^ b) * FnvPrime;
        }

        private static void FeedFloat(ref uint h, float v)
        {
            FeedInt(ref h, BitConverter.SingleToInt32Bits(v));
        }

        private static void FeedBool(ref uint h, bool v)
        {
            h = (h ^ (v ? (byte)1 : (byte)0)) * FnvPrime;
        }

        private static void FeedEnum<T>(ref uint h, T e) where T : Enum
        {
            FeedInt(ref h, Convert.ToInt32(e));
        }
    }
}
