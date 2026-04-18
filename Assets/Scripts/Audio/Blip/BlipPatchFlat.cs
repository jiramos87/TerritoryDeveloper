namespace Territory.Audio
{
    // =========================================================================
    // BlipOscillatorFlat
    // =========================================================================

    /// <summary>
    /// Blittable readonly mirror of <see cref="BlipOscillator"/>.
    /// Contains value-type scalars only — no managed references.
    /// Source of truth for field semantics: <see cref="BlipOscillator"/>.
    /// </summary>
    public readonly struct BlipOscillatorFlat
    {
        /// <summary>Waveform shape for this oscillator layer.</summary>
        public readonly BlipWaveform waveform;

        /// <summary>Base frequency in Hz.</summary>
        public readonly float frequencyHz;

        /// <summary>Fine-tuning offset in cents (+-1200 max).</summary>
        public readonly float detuneCents;

        /// <summary>Pulse-width duty cycle [0..1]; only meaningful when waveform == Pulse.</summary>
        public readonly float pulseDuty;

        /// <summary>Linear gain multiplier [0..1].</summary>
        public readonly float gain;

        /// <summary>
        /// Constructs a <see cref="BlipOscillatorFlat"/> by copying scalars from <paramref name="src"/>.
        /// Blittable contract: all fields are value types.
        /// </summary>
        public BlipOscillatorFlat(in BlipOscillator src)
        {
            waveform    = src.waveform;
            frequencyHz = src.frequencyHz;
            detuneCents = src.detuneCents;
            pulseDuty   = src.pulseDuty;
            gain        = src.gain;
        }

        /// <summary>
        /// Copy constructor — clones <paramref name="src"/> with an overridden
        /// <paramref name="detuneCents"/> value. Used by <c>BlipVoice.Render</c>
        /// for Option-B pitch-jitter fold: snapshots a local copy per osc slot
        /// without modifying the canonical patch data.
        /// Zero managed allocs (readonly struct on the stack).
        /// </summary>
        internal BlipOscillatorFlat(in BlipOscillatorFlat src, float detuneCents)
        {
            waveform    = src.waveform;
            frequencyHz = src.frequencyHz;
            this.detuneCents = detuneCents;
            pulseDuty   = src.pulseDuty;
            gain        = src.gain;
        }
    }

    // =========================================================================
    // BlipEnvelopeFlat
    // =========================================================================

    /// <summary>
    /// Blittable readonly mirror of <see cref="BlipEnvelope"/>.
    /// Contains value-type scalars only — no managed references.
    /// Source of truth for field semantics: <see cref="BlipEnvelope"/>.
    /// </summary>
    public readonly struct BlipEnvelopeFlat
    {
        /// <summary>Attack time in ms (clamped >= 1 by <see cref="BlipPatch"/> OnValidate).</summary>
        public readonly float attackMs;

        /// <summary>Shape applied during attack stage.</summary>
        public readonly BlipEnvShape attackShape;

        /// <summary>Hold time in ms.</summary>
        public readonly float holdMs;

        /// <summary>Decay time in ms (clamped >= 0; 0 = instant drop to sustain).</summary>
        public readonly float decayMs;

        /// <summary>Shape applied during decay stage.</summary>
        public readonly BlipEnvShape decayShape;

        /// <summary>Sustain level [0..1].</summary>
        public readonly float sustainLevel;

        /// <summary>Release time in ms (clamped >= 1 by <see cref="BlipPatch"/> OnValidate).</summary>
        public readonly float releaseMs;

        /// <summary>Shape applied during release stage.</summary>
        public readonly BlipEnvShape releaseShape;

        /// <summary>
        /// Constructs a <see cref="BlipEnvelopeFlat"/> by copying scalars from <paramref name="src"/>.
        /// Blittable contract: all fields are value types.
        /// </summary>
        public BlipEnvelopeFlat(in BlipEnvelope src)
        {
            attackMs      = src.attackMs;
            attackShape   = src.attackShape;
            holdMs        = src.holdMs;
            decayMs       = src.decayMs;
            decayShape    = src.decayShape;
            sustainLevel  = src.sustainLevel;
            releaseMs     = src.releaseMs;
            releaseShape  = src.releaseShape;
        }
    }

    // =========================================================================
    // BlipFilterFlat
    // =========================================================================

    /// <summary>
    /// Blittable readonly mirror of <see cref="BlipFilter"/>.
    /// Contains value-type scalars only — no managed references.
    /// Source of truth for field semantics: <see cref="BlipFilter"/>.
    /// </summary>
    public readonly struct BlipFilterFlat
    {
        /// <summary>Filter type (None disables filtering).</summary>
        public readonly BlipFilterKind kind;

        /// <summary>Static cutoff frequency in Hz.</summary>
        public readonly float cutoffHz;

        /// <summary>
        /// Constructs a <see cref="BlipFilterFlat"/> by copying scalars from <paramref name="src"/>.
        /// Blittable contract: all fields are value types.
        /// </summary>
        public BlipFilterFlat(in BlipFilter src)
        {
            kind     = src.kind;
            cutoffHz = src.cutoffHz;
        }
    }

    // =========================================================================
    // BlipPatchFlat
    // =========================================================================

    /// <summary>
    /// Blittable readonly mirror of <see cref="BlipPatch"/> scalar fields.
    /// <para>
    /// No managed references: no class fields, no <c>string</c>, no
    /// <c>AnimationCurve</c>, no <c>AudioMixerGroup</c>. Safe to pass
    /// <c>in BlipPatchFlat</c> into a DSP kernel without GC allocation.
    /// </para>
    /// <para>
    /// Oscillator layers are stored as three inline slots (<see cref="osc0"/>,
    /// <see cref="osc1"/>, <see cref="osc2"/>) plus <see cref="oscillatorCount"/>
    /// rather than a managed array, preserving blittability.
    /// Unused slots are zero-initialized.
    /// </para>
    /// <para>
    /// FX chain slots are stored as four inline slots (<see cref="fx0"/>,
    /// <see cref="fx1"/>, <see cref="fx2"/>, <see cref="fx3"/>) plus
    /// <see cref="fxSlotCount"/> rather than a managed array, preserving blittability.
    /// Unused slots are zero-initialized (default).
    /// </para>
    /// <para>
    /// <see cref="mixerGroupIndex"/> is an int sentinel (default <c>-1</c>).
    /// <c>BlipMixerRouter</c> (Step 2) populates this after catalog bootstrap;
    /// <c>-1</c> means "no mixer group bound yet".
    /// </para>
    /// <para>
    /// <c>patchHash</c> field is deferred to a follow-up task; this struct ships without it.
    /// </para>
    /// </summary>
    public readonly struct BlipPatchFlat
    {
        // -----------------------------------------------------------------
        // Oscillator inline triplet
        // -----------------------------------------------------------------

        /// <summary>First oscillator layer (zero-init when unused).</summary>
        public readonly BlipOscillatorFlat osc0;

        /// <summary>Second oscillator layer (zero-init when unused).</summary>
        public readonly BlipOscillatorFlat osc1;

        /// <summary>Third oscillator layer (zero-init when unused).</summary>
        public readonly BlipOscillatorFlat osc2;

        /// <summary>Number of active oscillator layers (0..3). Matches <see cref="BlipPatch.Oscillators"/> length at flatten time.</summary>
        public readonly int oscillatorCount;

        // -----------------------------------------------------------------
        // FX chain inline quad
        // -----------------------------------------------------------------

        /// <summary>First FX chain slot (zero-init when unused).</summary>
        public readonly BlipFxSlotFlat fx0;

        /// <summary>Second FX chain slot (zero-init when unused).</summary>
        public readonly BlipFxSlotFlat fx1;

        /// <summary>Third FX chain slot (zero-init when unused).</summary>
        public readonly BlipFxSlotFlat fx2;

        /// <summary>Fourth FX chain slot (zero-init when unused).</summary>
        public readonly BlipFxSlotFlat fx3;

        /// <summary>Number of active FX chain slots (0..4). Matches <see cref="BlipPatch.FxChain"/> length at flatten time (capped at 4).</summary>
        public readonly int fxSlotCount;

        // -----------------------------------------------------------------
        // Envelope + Filter
        // -----------------------------------------------------------------

        /// <summary>AHDSR envelope parameters.</summary>
        public readonly BlipEnvelopeFlat envelope;

        /// <summary>One-pole low-pass filter parameters.</summary>
        public readonly BlipFilterFlat filter;

        // -----------------------------------------------------------------
        // Variation
        // -----------------------------------------------------------------

        /// <summary>Number of round-robin variants (1..8). Source: <see cref="BlipPatch.VariantCount"/>.</summary>
        public readonly int variantCount;

        // -----------------------------------------------------------------
        // Per-invocation jitter
        // -----------------------------------------------------------------

        /// <summary>Per-invocation pitch jitter range in cents (±). Source: <see cref="BlipPatch.PitchJitterCents"/>.</summary>
        public readonly float pitchJitterCents;

        /// <summary>Per-invocation gain jitter range in dB (±). Source: <see cref="BlipPatch.GainJitterDb"/>.</summary>
        public readonly float gainJitterDb;

        /// <summary>Per-invocation pan jitter range ([-1..1]). Source: <see cref="BlipPatch.PanJitter"/>.</summary>
        public readonly float panJitter;

        // -----------------------------------------------------------------
        // Voice management
        // -----------------------------------------------------------------

        /// <summary>Max concurrent voices (1..16). Source: <see cref="BlipPatch.VoiceLimit"/>.</summary>
        public readonly int voiceLimit;

        /// <summary>Voice-steal ranking — higher survives steal. Source: <see cref="BlipPatch.Priority"/>.</summary>
        public readonly int priority;

        /// <summary>Minimum inter-play gap in ms (>= 0). Source: <see cref="BlipPatch.CooldownMs"/>.</summary>
        public readonly float cooldownMs;

        // -----------------------------------------------------------------
        // Flags
        // -----------------------------------------------------------------

        /// <summary>When true, jitter is disabled for test fixtures. Source: <see cref="BlipPatch.Deterministic"/>.</summary>
        public readonly bool deterministic;

        /// <summary>Offline bake length in seconds. Source: <see cref="BlipPatch.DurationSeconds"/>.</summary>
        public readonly float durationSeconds;

        /// <summary>Reserved: LUT oscillator flag (unused MVP). Source: <see cref="BlipPatch.UseLutOscillators"/>.</summary>
        public readonly bool useLutOscillators;

        // -----------------------------------------------------------------
        // LFO slots (2 fixed; blittable — TECH-285)
        // -----------------------------------------------------------------

        /// <summary>First LFO slot flat mirror. Source: <see cref="BlipPatch.Lfo0"/>.</summary>
        public readonly BlipLfoFlat lfo0Flat;

        /// <summary>Second LFO slot flat mirror. Source: <see cref="BlipPatch.Lfo1"/>.</summary>
        public readonly BlipLfoFlat lfo1Flat;

        // -----------------------------------------------------------------
        // Mixer routing index (blittable replacement for AudioMixerGroup ref)
        // -----------------------------------------------------------------

        /// <summary>
        /// Index into the <c>BlipMixerRouter</c> group table.
        /// Default <c>-1</c> sentinel = "no mixer group bound".
        /// Populated by <c>BlipMixerRouter</c> (Step 2) after catalog bootstrap.
        /// Blittable-friendly: avoids a managed <c>AudioMixerGroup</c> reference.
        /// </summary>
        public readonly int mixerGroupIndex;

        // -----------------------------------------------------------------
        // Construction
        // -----------------------------------------------------------------

        /// <summary>
        /// Flattens a <see cref="BlipPatch"/> ScriptableObject into an immutable blittable struct.
        /// Call on the main thread during catalog bootstrap (e.g., <c>BlipCatalog.Awake</c>).
        /// </summary>
        /// <param name="so">Source <see cref="BlipPatch"/> ScriptableObject. Must not be null.</param>
        /// <param name="mixerGroupIndex">
        /// Optional mixer group index populated by <c>BlipMixerRouter</c>.
        /// Defaults to <c>-1</c> (unbound sentinel); pass actual index once router resolves the group.
        /// </param>
        public BlipPatchFlat(BlipPatch so, int mixerGroupIndex = -1)
        {
            var oscs = so.Oscillators;
            int count = oscs != null ? UnityEngine.Mathf.Min(oscs.Length, 3) : 0;

            oscillatorCount    = count;
            osc0               = count > 0 ? new BlipOscillatorFlat(oscs[0]) : default;
            osc1               = count > 1 ? new BlipOscillatorFlat(oscs[1]) : default;
            osc2               = count > 2 ? new BlipOscillatorFlat(oscs[2]) : default;

            var fx     = so.FxChain;
            int fxCount = fx != null ? UnityEngine.Mathf.Min(fx.Length, 4) : 0;
            fxSlotCount = fxCount;
            fx0         = fxCount > 0 ? new BlipFxSlotFlat(in fx[0]) : default;
            fx1         = fxCount > 1 ? new BlipFxSlotFlat(in fx[1]) : default;
            fx2         = fxCount > 2 ? new BlipFxSlotFlat(in fx[2]) : default;
            fx3         = fxCount > 3 ? new BlipFxSlotFlat(in fx[3]) : default;

            envelope           = new BlipEnvelopeFlat(so.Envelope);
            filter             = new BlipFilterFlat(so.Filter);

            variantCount       = so.VariantCount;
            pitchJitterCents   = so.PitchJitterCents;
            gainJitterDb       = so.GainJitterDb;
            panJitter          = so.PanJitter;
            voiceLimit         = so.VoiceLimit;
            priority           = so.Priority;
            cooldownMs         = so.CooldownMs;
            deterministic      = so.Deterministic;
            durationSeconds    = so.DurationSeconds;
            useLutOscillators  = so.UseLutOscillators;

            lfo0Flat = new BlipLfoFlat(so.Lfo0);
            lfo1Flat = new BlipLfoFlat(so.Lfo1);

            this.mixerGroupIndex = mixerGroupIndex;
        }

        /// <summary>
        /// Convenience factory: flattens <paramref name="so"/> with <see cref="mixerGroupIndex"/> = -1.
        /// <c>BlipMixerRouter</c> overrides the index post-flatten when routing is configured.
        /// </summary>
        public static BlipPatchFlat FromSO(BlipPatch so) => new BlipPatchFlat(so);
    }
}
