using UnityEngine;

namespace Territory.Editor.UiBake.SlotResolver
{
    /// <summary>
    /// Stage 2 static helper: resolves a slot anchor Transform by panel slug using suffix-match fallback.
    /// Finding F2 — bake emits panel-prefixed slot names (e.g. "main-menu-content-slot"),
    /// controllers hard-code generic names (e.g. "content-slot"). Suffix-match bridges the gap.
    /// Inv #3: runs at mount/apply only, never per-frame.
    /// </summary>
    public static class SlotAnchorResolver
    {
        /// <summary>
        /// Resolve a slot Transform whose name ends with "-content-slot" under <paramref name="searchRoot"/>.
        /// Match priority: exact name "{panel_slug}-content-slot" → suffix "-content-slot" → null.
        /// </summary>
        /// <param name="panelSlug">Panel slug (e.g. "main-menu").</param>
        /// <param name="searchRoot">Root Transform to search beneath (walks all descendants).</param>
        /// <returns>First matching Transform or null when not found.</returns>
        public static Transform ResolveByPanel(string panelSlug, Transform searchRoot)
        {
            if (searchRoot == null || string.IsNullOrEmpty(panelSlug)) return null;

            string exactName = $"{panelSlug}-content-slot";

            // BFS walk — check direct children first, then recurse.
            return WalkForSlot(searchRoot, exactName);
        }

        private static Transform WalkForSlot(Transform root, string exactName)
        {
            // Exact match pass.
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == exactName) return child;
            }

            // Suffix fallback pass.
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith("-content-slot", System.StringComparison.Ordinal)) return child;
            }

            // Recurse into children.
            for (int i = 0; i < root.childCount; i++)
            {
                var found = WalkForSlot(root.GetChild(i), exactName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
