using System;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipVoiceState — per-voice DSP state for BlipVoice.Render kernel.
    // Blittable struct (zero managed refs). Caller-owned; mutated via ref.
    // default(BlipVoiceState) is valid: envStage = Idle, all phases 0, silent.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Per-voice DSP state passed by <c>ref</c> to <c>BlipVoice.Render</c>.
    /// All fields are primitive or enum — struct is blittable (zero managed refs).
    /// <c>default(BlipVoiceState)</c> is valid: <c>envStage = Idle</c>, all
    /// phases 0, <c>envLevel = 0</c>, silent output.
    /// </summary>
    public struct BlipVoiceState
    {
        /// <summary>Oscillator slot 0 phase accumulator (0..2π). Written by the oscillator bank.</summary>
        public double phaseA;

        /// <summary>Oscillator slot 1 phase accumulator (0..2π). Written by the oscillator bank.</summary>
        public double phaseB;

        /// <summary>Oscillator slot 2 phase accumulator (0..2π). Written by the oscillator bank.</summary>
        public double phaseC;

        /// <summary>LFO reserve phase accumulator — post-MVP; unused in MVP kernel. Written by the oscillator bank.</summary>
        public double phaseD;

        /// <summary>Current envelope output level [0..1]. Written by <see cref="BlipEnvelopeStepper.ComputeLevel"/>.</summary>
        public float envLevel;

        /// <summary>AHDSR stage pointer. Default = <c>BlipEnvStage.Idle</c>. Written by <see cref="BlipEnvelopeStepper"/>.</summary>
        public BlipEnvStage envStage;

        /// <summary>Samples elapsed since the current envelope stage was entered. Written by <see cref="BlipEnvelopeStepper"/>.</summary>
        public int samplesElapsed;

        /// <summary>One-pole low-pass filter delay memory (z-1 state).</summary>
        public float filterZ1;

        /// <summary>
        /// xorshift32 RNG state — used for noise + per-invocation jitter.
        /// Caller seeds before first <see cref="BlipVoice.Render"/> call (voice-hash of patch id + voice slot).
        /// Re-seeded per invocation by <see cref="BlipVoice.Render"/>.
        /// Written by the oscillator bank (noise waveform) and the jitter pre-compute block.
        /// </summary>
        public uint rngState;

        /// <summary>
        /// Pan offset stashed by the most recent <see cref="BlipVoice.Render"/> invocation.
        /// Range: [-1, 1]. 0 when <c>patch.deterministic == true</c> or <c>patch.panJitter == 0</c>.
        /// Consumed by Step 2 <c>BlipBaker</c> mixer for stereo placement (MVP: mono kernel, pan applied externally).
        /// </summary>
        public float panOffset;
    }
}
