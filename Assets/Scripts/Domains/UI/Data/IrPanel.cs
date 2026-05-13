using System;

namespace Domains.UI.Data
{
    /// <summary>Stage 1.4 (T1.4.1) — panel spacing overrides from IR `panel.detail`.</summary>
    [Serializable]
    public class IrPanelDetail
    {
        public float paddingX;
        public float paddingY;
        public float gap;
        public float dividerThickness;
    }

    [Serializable]
    public class IrPanel
    {
        public string slug;
        public string archetype;
        public string kind;
        public IrPanelSlot[] slots;
        /// <summary>Stage 1.4 (T1.4.1) — optional spacing overrides; null when absent from IR.</summary>
        public IrPanelDetail detail;
        /// <summary>Stage 1.4 (T1.4.3) — atlas frame sprite slug.</summary>
        public string frame_style_slug;
        /// <summary>Stage 1.4 (T1.4.3) — illumination token slug.</summary>
        public string illumination_slug;
        /// <summary>Stage 13.1+ — IR v2 tab descriptors. Null/empty on tabless panels.</summary>
        public IrTab[] tabs;
        /// <summary>Stage 13.1+ — IR v2 flat row list. Null/empty on rowless panels.</summary>
        public IrRow[] rows;
        /// <summary>Stage 13.4 (TECH-9867) — IR v2 default tab index.</summary>
        public int defaultTabIndex;
    }

    [Serializable]
    public class IrPanelSlot
    {
        public string name;
        public string[] accepts;
        public string[] children;
        // Step 12 — optional per-child label content; parallel to children[] when present.
        public string[] labels;
        // Step 16.D — optional per-child icon sprite slug; parallel to children[].
        public string[] iconSpriteSlugs;
    }

    /// <summary>Stage 13.1+ — IR v2 tab descriptor.</summary>
    [Serializable]
    public class IrTab
    {
        public string id;
        public string label;
        public bool active;
        /// <summary>Stage 13.3 — optional icon slug.</summary>
        public string iconSlug;
    }

    /// <summary>Stage 13.1+ — IR v2 row descriptor. Flat list per panel; `kind` discriminates render shape.</summary>
    [Serializable]
    public class IrRow
    {
        /// <summary>Render shape — `stat`, `detail`, or `header`.</summary>
        public string kind;
        public string label;
        public string value;
        public int segments;
        public string fontSlug;
        /// <summary>Stage 13.3 — optional icon slug.</summary>
        public string iconSlug;
    }
}
