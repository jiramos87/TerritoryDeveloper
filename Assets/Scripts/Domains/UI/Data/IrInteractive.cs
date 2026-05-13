using System;

namespace Domains.UI.Data
{
    [Serializable]
    public class IrInteractive
    {
        public string slug;
        /// <summary>StudioControl archetype slug.</summary>
        public string kind;
        /// <summary>Stage 5 (T5.5) juice declarations. Null when absent.</summary>
        public IrJuiceDecl[] juice;
    }

    /// <summary>Stage 5 juice override entry.</summary>
    [Serializable]
    public class IrJuiceDecl
    {
        /// <summary>Optional usage slug for filtering.</summary>
        public string usage_slug;
        /// <summary>Juice slug.</summary>
        public string juice_kind;
        /// <summary>Optional motion-curve slug override.</summary>
        public string curve_slug;
        /// <summary>When true, suppresses the per-kind default attachment.</summary>
        public bool disabled;
    }
}
