using System;

namespace Domains.UI.Data
{
    /// <summary>Bridge-mutation argument bag for `bake_ui_from_ir`.</summary>
    [Serializable]
    public class BakeArgs
    {
        public string ir_path;
        /// <summary>Stage 9.10 — canonical panels snapshot path (panels.json).</summary>
        public string panels_path;
        public string out_dir;
        public string theme_so;
        /// <summary>Visual regression — when true, capture baseline candidate PNG per baked panel.</summary>
        public bool captureBaselines;
        /// <summary>Visual regression — CSV of panel slugs to capture.</summary>
        public string capturePanelsCsv;
    }
}
