using System;

namespace Territory.Audio
{
    // -------------------------------------------------------------------------
    // BlipWaveform — oscillator waveform selector (MVP subset)
    // -------------------------------------------------------------------------
    public enum BlipWaveform
    {
        Sine       = 0,
        Triangle   = 1,
        Square     = 2,
        Pulse      = 3,
        NoiseWhite = 4,
    }

    // -------------------------------------------------------------------------
    // BlipFilterKind — filter type selector (MVP subset)
    // -------------------------------------------------------------------------
    public enum BlipFilterKind
    {
        None    = 0,
        LowPass = 1,
    }

    // -------------------------------------------------------------------------
    // BlipFxKind — FX chain effect type selector
    // Values pinned for switch-dispatch stability (BlipFxChain.ProcessFx).
    // Comb/Allpass/Chorus/Flanger kernels land Stage 5.2; slots using those values
    // return passthrough until then.
    // -------------------------------------------------------------------------
    public enum BlipFxKind
    {
        None      = 0,
        BitCrush  = 1,
        RingMod   = 2,
        SoftClip  = 3,
        DcBlocker = 4,
        Comb      = 5,
        Allpass   = 6,
        Chorus    = 7,
        Flanger   = 8,
    }

    // -------------------------------------------------------------------------
    // BlipEnvStage — AHDSR state machine stage identifiers
    // -------------------------------------------------------------------------
    public enum BlipEnvStage
    {
        Idle    = 0,
        Attack  = 1,
        Hold    = 2,
        Decay   = 3,
        Sustain = 4,
        Release = 5,
    }

    // -------------------------------------------------------------------------
    // BlipEnvShape — per-stage envelope curve shape (MVP: linear + exponential)
    // -------------------------------------------------------------------------
    public enum BlipEnvShape
    {
        Linear      = 0,
        Exponential = 1,
    }

    // -------------------------------------------------------------------------
    // BlipId — central SFX identifier; 10 MVP rows + None
    // Grow policy: add rows under prefix Ui*, Tool*, World*, Eco*, Sys*.
    // -------------------------------------------------------------------------
    public enum BlipId
    {
        None              = 0,

        // UI
        UiButtonHover     = 1,
        UiButtonClick     = 2,

        // Tool / build
        ToolRoadTick      = 3,
        ToolRoadComplete  = 4,
        ToolBuildingPlace = 5,
        ToolBuildingDenied = 6,

        // World
        WorldCellSelected = 7,

        // Economy
        EcoMoneyEarned    = 8,
        EcoMoneySpent     = 9,

        // System
        SysSaveGame       = 10,
    }

    // -------------------------------------------------------------------------
    // BlipOscillator — single oscillator layer inside BlipPatch (no curve fields)
    // -------------------------------------------------------------------------
    [Serializable]
    public struct BlipOscillator
    {
        /// <summary>Waveform shape for this oscillator.</summary>
        public BlipWaveform waveform;

        /// <summary>Base frequency in Hz.</summary>
        public float frequencyHz;

        /// <summary>Fine-tuning offset in cents (+-1200 max).</summary>
        public float detuneCents;

        /// <summary>Pulse-width duty cycle [0..1]; only used when waveform == Pulse.</summary>
        public float pulseDuty;

        /// <summary>Linear gain multiplier [0..1].</summary>
        public float gain;
    }

    // -------------------------------------------------------------------------
    // BlipEnvelope — AHDSR envelope; per-stage shape; no AnimationCurve
    // -------------------------------------------------------------------------
    [Serializable]
    public struct BlipEnvelope
    {
        /// <summary>Attack time in ms (clamped >= 1 by BlipPatch.OnValidate).</summary>
        public float attackMs;

        /// <summary>Shape applied during attack stage.</summary>
        public BlipEnvShape attackShape;

        /// <summary>Hold time in ms.</summary>
        public float holdMs;

        /// <summary>Decay time in ms (clamped >= 0 by BlipPatch.OnValidate; 0 = instant drop to sustain).</summary>
        public float decayMs;

        /// <summary>Shape applied during decay stage.</summary>
        public BlipEnvShape decayShape;

        /// <summary>Sustain level [0..1] (clamped by BlipPatch.OnValidate).</summary>
        public float sustainLevel;

        /// <summary>Release time in ms (clamped >= 1 by BlipPatch.OnValidate).</summary>
        public float releaseMs;

        /// <summary>Shape applied during release stage.</summary>
        public BlipEnvShape releaseShape;
    }

    // -------------------------------------------------------------------------
    // BlipFilter — one-pole low-pass filter; no cutoff envelope curve
    // -------------------------------------------------------------------------
    [Serializable]
    public struct BlipFilter
    {
        /// <summary>Filter type (None disables filtering).</summary>
        public BlipFilterKind kind;

        /// <summary>Static cutoff frequency in Hz.</summary>
        public float cutoffHz;
    }

    // -------------------------------------------------------------------------
    // BlipFxSlot — authoring-side FX chain slot (Inspector-serializable)
    // Max 4 slots per patch; enforced by BlipPatch.OnValidate.
    // param0/param1/param2 semantics are kind-specific — see BlipFxChain.ProcessFx
    // for per-kind interpretations.
    // -------------------------------------------------------------------------
    [Serializable]
    public struct BlipFxSlot
    {
        /// <summary>Effect type. None = slot inactive (passthrough).</summary>
        public BlipFxKind kind;

        /// <summary>Effect parameter 0. Semantics depend on kind (e.g. bit-depth for BitCrush).</summary>
        public float param0;

        /// <summary>Effect parameter 1. Semantics depend on kind (e.g. carrier Hz for RingMod).</summary>
        public float param1;

        /// <summary>Effect parameter 2. Reserved; currently unused by all Stage 5.1 kernels.</summary>
        public float param2;
    }

    // -------------------------------------------------------------------------
    // BlipFxSlotFlat — blittable runtime mirror of BlipFxSlot
    // readonly struct + scalar-only fields → qualifies as unmanaged (blittable).
    // Mirrors BlipPatchFlat blittable discipline (ia/specs/audio-blip.md §2).
    // No managed refs, no heap allocation.
    // -------------------------------------------------------------------------
    public readonly struct BlipFxSlotFlat
    {
        /// <summary>Effect type. None = slot inactive (passthrough).</summary>
        public readonly BlipFxKind kind;

        /// <summary>Effect parameter 0 (kind-specific).</summary>
        public readonly float param0;

        /// <summary>Effect parameter 1 (kind-specific).</summary>
        public readonly float param1;

        /// <summary>Effect parameter 2 (reserved).</summary>
        public readonly float param2;

        /// <summary>Copy constructor from authoring BlipFxSlot. No managed refs copied.</summary>
        public BlipFxSlotFlat(in BlipFxSlot s)
        {
            kind   = s.kind;
            param0 = s.param0;
            param1 = s.param1;
            param2 = s.param2;
        }
    }
}
