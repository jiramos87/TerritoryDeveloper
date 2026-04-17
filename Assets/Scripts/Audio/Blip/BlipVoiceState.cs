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

        // Per-slot FX state — slot N triplet; consumed by BlipFxChain kernel + BlipVoice dispatch.
        // Blittable discipline: all floats, default(BlipVoiceState) yields silent voice.
        // dcZ1_N   — DC blocker input z-1 for FX slot N.   Read+written by BlipFxChain.ProcessFx.
        // dcY1_N   — DC blocker output z-1 for FX slot N.  Read+written by BlipFxChain.ProcessFx.
        // ringModPhase_N — ring-mod carrier phase (0..2π) for FX slot N. Read+written by BlipFxChain.ProcessFx.

        /// <summary>DC blocker input z-1 for FX slot 0. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcZ1_0;
        /// <summary>DC blocker output z-1 for FX slot 0. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcY1_0;
        /// <summary>Ring-mod carrier phase (0..2π) for FX slot 0. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float ringModPhase_0;

        /// <summary>DC blocker input z-1 for FX slot 1. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcZ1_1;
        /// <summary>DC blocker output z-1 for FX slot 1. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcY1_1;
        /// <summary>Ring-mod carrier phase (0..2π) for FX slot 1. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float ringModPhase_1;

        /// <summary>DC blocker input z-1 for FX slot 2. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcZ1_2;
        /// <summary>DC blocker output z-1 for FX slot 2. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcY1_2;
        /// <summary>Ring-mod carrier phase (0..2π) for FX slot 2. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float ringModPhase_2;

        /// <summary>DC blocker input z-1 for FX slot 3. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcZ1_3;
        /// <summary>DC blocker output z-1 for FX slot 3. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float dcY1_3;
        /// <summary>Ring-mod carrier phase (0..2π) for FX slot 3. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public float ringModPhase_3;

        // Delay-line write-heads — circular buffer index (sample offset) per FX slot.
        // Driven by Stage 5.2 delay-line kernels (Comb / Allpass / Chorus / Flanger).
        // Blittable int; default(BlipVoiceState) = 0 is a valid circular-buffer start.

        /// <summary>Circular write-head (sample index) for delay-line FX in slot 0. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public int delayWritePos_0;
        /// <summary>Circular write-head (sample index) for delay-line FX in slot 1. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public int delayWritePos_1;
        /// <summary>Circular write-head (sample index) for delay-line FX in slot 2. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public int delayWritePos_2;
        /// <summary>Circular write-head (sample index) for delay-line FX in slot 3. Read+written by <c>BlipFxChain.ProcessFx</c> via <c>ref</c>.</summary>
        public int delayWritePos_3;
    }
}
