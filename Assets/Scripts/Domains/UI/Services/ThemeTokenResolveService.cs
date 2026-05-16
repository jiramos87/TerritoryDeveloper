using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Services
{
    /// <summary>
    /// Token resolution + spatial look-up helpers — pure static utility; no MonoBehaviour.
    /// Split from ThemeService (Stage 7.4 Tier-E atomization).
    /// </summary>
    public static class ThemeTokenResolveService
    {
        // ─── Panel name constants (shared across sub-services) ──────────────────
        public const string CellDataPanelName = "CellDataPanel";
        public const string CellDataPanelNameAlt = "CellDataPanelAlt";
        public const string CellDataPanelTextInsetName = "CellDataPanelTextInset";
        public const string CellDataPanelTextHolderAlt = "CellDataPanelText";
        public const string CellDataPanelScrollRootName = "CellDataPanelScrollRoot";
        public const string CellDataPanelViewportName = "CellDataPanelViewport";
        public const string CellDataPanelContentName = "CellDataPanelContent";
        public const string ControlPanelObjectName = "ControlPanel";
        public const string DataPanelButtonsObjectName = "DataPanelButtons";

        // ─── Layout constants ────────────────────────────────────────────────────
        public const float CellDataPanelGapAboveControlPanel = 10f;
        public const float CellDataPanelGapBelowDataPanelButtons = 8f;
        public const float CellDataPanelGapAboveMinimap = 30f;
        public const float CellDataPanelMaxSquareSide = 220f;

        // ─── Name predicates ────────────────────────────────────────────────────
        /// <summary>True if name matches cell-data panel root.</summary>
        public static bool IsCellDataPanelRootName(string n) => n == CellDataPanelName || n == CellDataPanelNameAlt;
        /// <summary>True if name matches cell-data panel text holder.</summary>
        public static bool IsCellDataPanelTextHolderName(string n) => n == CellDataPanelTextInsetName || n == CellDataPanelTextHolderAlt;

        // ─── Ancestor / root finders ────────────────────────────────────────────
        /// <summary>Walk ancestors for transform matching exact name.</summary>
        public static Transform FindNamedAncestor(Transform t, string exactName)
        {
            while (t != null) { if (t.name == exactName) return t; t = t.parent; }
            return null;
        }

        /// <summary>Walk up to find cell-data panel root transform.</summary>
        public static Transform FindCellDataPanelRoot(Transform from)
        {
            for (Transform p = from; p != null; p = p.parent) { if (IsCellDataPanelRootName(p.name)) return p; }
            return null;
        }

        /// <summary>Find cell-data panel text inset rect under chrome.</summary>
        public static RectTransform FindCellDataPanelInset(RectTransform chromeRt)
        {
            if (chromeRt == null) return null;
            Transform t = chromeRt.Find(CellDataPanelTextInsetName);
            if (t == null) t = chromeRt.Find(CellDataPanelTextHolderAlt);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        /// <summary>Walk up to find HUD layout root (parent of ControlPanel/MiniMap).</summary>
        public static Transform FindHudLayoutRoot(Transform from)
        {
            for (Transform p = from; p != null; p = p.parent)
            { if (p.Find(ControlPanelObjectName) != null || p.Find("MiniMapPanel") != null) return p; }
            return null;
        }

        /// <summary>Find HUD layout root via scene scan — post-rebuild fallback.</summary>
        public static Transform FindHudLayoutRootForRebuild()
        {
            GameObject mm = GameObject.Find("MiniMapPanel");
            if (mm != null && mm.transform.parent != null) return mm.transform.parent;
            GameObject cp = GameObject.Find(ControlPanelObjectName);
            if (cp != null && cp.transform.parent != null) return cp.transform.parent;
            Canvas c = UnityEngine.Object.FindObjectOfType<Canvas>();
            return c != null ? c.transform : null;
        }

        // ─── Rect bounds helper ─────────────────────────────────────────────────
        /// <summary>Get child rect bounds in parent rect coord space.</summary>
        public static bool TryGetRectBoundsInParent(RectTransform parentRt, RectTransform childRt, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (parentRt == null || childRt == null) return false;
            Rect r = childRt.rect;
            Vector3[] corners = { new Vector3(r.xMin, r.yMin, 0f), new Vector3(r.xMin, r.yMax, 0f), new Vector3(r.xMax, r.yMax, 0f), new Vector3(r.xMax, r.yMin, 0f) };
            minX = minY = float.PositiveInfinity; maxX = maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 pl = parentRt.InverseTransformPoint(childRt.TransformPoint(corners[i]));
                minX = Mathf.Min(minX, pl.x); maxX = Mathf.Max(maxX, pl.x); minY = Mathf.Min(minY, pl.y); maxY = Mathf.Max(maxY, pl.y);
            }
            return maxX > minX && maxY > minY;
        }
    }
}
