using System;

namespace Domains.UI.Data
{
    /// <summary>Token block — five subblocks, one per kind.</summary>
    [Serializable]
    public class IrTokens
    {
        public IrTokenPalette[] palette;
        public IrTokenFrameStyle[] frame_style;
        public IrTokenFontFace[] font_face;
        public IrTokenMotionCurve[] motion_curve;
        public IrTokenIllumination[] illumination;
    }

    [Serializable]
    public class IrTokenPalette
    {
        public string slug;
        /// <summary>Ordered hex stops (low → high).</summary>
        public string[] ramp;
    }

    [Serializable]
    public class IrTokenFrameStyle
    {
        public string slug;
        /// <summary>`single` | `double` (CD partner extends).</summary>
        public string edge;
        public float innerShadowAlpha;
    }

    [Serializable]
    public class IrTokenFontFace
    {
        public string slug;
        public string family;
        public int weight;
    }

    [Serializable]
    public class IrTokenMotionCurve
    {
        public string slug;
        /// <summary>`spring` | `cubic-bezier` | other.</summary>
        public string kind;
        public float stiffness;
        public float damping;
        public float[] c1;
        public float[] c2;
        public float durationMs;
    }

    [Serializable]
    public class IrTokenIllumination
    {
        public string slug;
        public string color;
        public float haloRadiusPx;
    }
}
