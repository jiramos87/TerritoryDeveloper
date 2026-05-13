using System;

namespace Domains.UI.Data
{
    /// <summary>Stage 1.4 (T1.4.4) — button state override block from IR `button.detail`.</summary>
    [Serializable]
    public class IrButtonDetail
    {
        public float paddingX;
        public float paddingY;
    }

    /// <summary>Stage 1.4 (T1.4.4) — per-state color table from IR `button.palette_ramp`.</summary>
    [Serializable]
    public class IrButtonPaletteRamp
    {
        public string normal;
        public string highlighted;
        public string pressed;
        public string disabled;
    }

    /// <summary>Stage 1.4 (T1.4.4) — atlas slot enums for sprite-swap states.</summary>
    [Serializable]
    public class IrButtonAtlasSlotEnum
    {
        public string normal;
        public string highlighted;
        public string pressed;
    }

    /// <summary>Stage 1.4 (T1.4.4) — motion-curve override for button fade duration.</summary>
    [Serializable]
    public class IrButtonMotionCurve
    {
        public float fadeDuration;
    }

    /// <summary>Stage 1.4 (T1.4.4) — full button state IR detail block.</summary>
    [Serializable]
    public class IrButtonStateDetail
    {
        public IrButtonPaletteRamp palette_ramp;
        public IrButtonAtlasSlotEnum atlas_slot_enum;
        public IrButtonMotionCurve motion_curve;
    }
}
