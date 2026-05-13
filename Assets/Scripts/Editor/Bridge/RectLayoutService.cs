using System.Collections.Generic;
using Domains.UI.Data;
using Territory.UI;
using Territory.UI.Themed;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Static helpers for panel rect layout. Extracted from UiBakeHandler (TECH-31983).
    /// Covers ApplyPanelKindRectDefaults, ApplyPanelRectJsonOverlay, ApplyRootLayoutGroupConfig,
    /// CreateRowGrid, ApplyRoundedBorder.</summary>
    public static class RectLayoutService
    {
        // ── Border token fallback map ────────────────────────────────────────────
        static readonly Dictionary<string, Color> s_BorderTokenHexFallback =
            new Dictionary<string, Color>(System.StringComparer.Ordinal)
            {
                { "led-amber", new Color(1f, 0.835f, 0.541f, 1f) },
                { "white",    Color.white },
            };

        static Color ResolveBorderColor(string token)
        {
            if (string.IsNullOrEmpty(token)) return Color.white;
            var hex = Territory.Editor.Bridge.UiBakeHandler.ResolveColorTokenHex(token);
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var parsed)) return parsed;
            if (s_BorderTokenHexFallback.TryGetValue(token, out var c)) return c;
            return Color.white;
        }

        // ── Per-kind anchor/size defaults ────────────────────────────────────────

        /// <summary>Apply per-kind anchor/size defaults to a panel root RectTransform.</summary>
        public static void ApplyPanelKindRectDefaults(RectTransform rect, PanelKind kind, string position = null)
        {
            if (rect == null) return;
            switch (kind)
            {
                case PanelKind.Hud:
                {
                    bool bottom = string.Equals(position, "bottom", System.StringComparison.OrdinalIgnoreCase);
                    if (bottom)
                    {
                        rect.anchorMin = new Vector2(0f, 0f);
                        rect.anchorMax = new Vector2(1f, 0f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        rect.anchoredPosition = new Vector2(0f, 8f);
                    }
                    else
                    {
                        rect.anchorMin = new Vector2(0f, 1f);
                        rect.anchorMax = new Vector2(1f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        rect.anchoredPosition = new Vector2(0f, -8f);
                    }
                    rect.sizeDelta = new Vector2(-16f, 144f);
                    break;
                }
                case PanelKind.Toolbar:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = new Vector2(8f, 0f);
                    rect.sizeDelta = new Vector2(96f, -16f);
                    break;
                case PanelKind.SideRail:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = new Vector2(-8f, 0f);
                    rect.sizeDelta = new Vector2(96f, -16f);
                    break;
                case PanelKind.Screen:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                    break;
                case PanelKind.Modal:
                default:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(480f, 320f);
                    break;
            }
        }

        // ── DB rect overlay ──────────────────────────────────────────────────────

        /// <summary>Overlay panel_detail.rect_json fields onto an existing RectTransform.</summary>
        public static void ApplyPanelRectJsonOverlay(RectTransform rect, string rectJson)
        {
            if (rect == null || string.IsNullOrWhiteSpace(rectJson)) return;
            var rj = TryParseTypedJson<PanelRectJson>(rectJson);
            if (rj == null) return;
            if (rj.anchor_min != null && rj.anchor_min.Length >= 2)
                rect.anchorMin = new Vector2(rj.anchor_min[0], rj.anchor_min[1]);
            if (rj.anchor_max != null && rj.anchor_max.Length >= 2)
                rect.anchorMax = new Vector2(rj.anchor_max[0], rj.anchor_max[1]);
            if (rj.pivot != null && rj.pivot.Length >= 2)
                rect.pivot = new Vector2(rj.pivot[0], rj.pivot[1]);
            if (rj.anchored_position != null && rj.anchored_position.Length >= 2)
                rect.anchoredPosition = new Vector2(rj.anchored_position[0], rj.anchored_position[1]);
            if (rj.size_delta != null && rj.size_delta.Length >= 2)
                rect.sizeDelta = new Vector2(rj.size_delta[0], rj.size_delta[1]);
        }

        // ── Root layout group config ─────────────────────────────────────────────

        /// <summary>Configure root LayoutGroup from panel fields (padding, gap, rounded border).</summary>
        public static void ApplyRootLayoutGroupConfig(LayoutGroup layoutGroup, PanelKind kind, PanelSnapshotFields fields)
        {
            if (layoutGroup == null) return;
            int padTop = 4, padBottom = 4, padLeft = 8, padRight = 8;
            float gap = fields?.gap_px ?? 8f;
            PanelPaddingJson pad = null;
            var padJson = fields?.padding_json;
            if (!string.IsNullOrEmpty(padJson))
            {
                pad = TryParseTypedJson<PanelPaddingJson>(padJson);
                padTop = pad.top;
                padRight = pad.right;
                padBottom = pad.bottom;
                padLeft = pad.left;
            }
            layoutGroup.padding = new RectOffset(padLeft, padRight, padTop, padBottom);

            if (pad != null && (pad.border_width > 0f || pad.corner_radius > 0f))
            {
                ApplyRoundedBorder(layoutGroup.gameObject, pad);
            }

            switch (layoutGroup)
            {
                case HorizontalLayoutGroup hlg:
                    hlg.spacing = gap;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = (kind == PanelKind.Hud);
                    hlg.childForceExpandHeight = true;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    break;
                case VerticalLayoutGroup vlg:
                    vlg.spacing = gap;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    break;
                case GridLayoutGroup grid:
                    grid.spacing = new Vector2(gap, gap);
                    grid.cellSize = new Vector2(80f, 80f);
                    grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                    grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                    grid.childAlignment = TextAnchor.UpperLeft;
                    break;
            }
        }

        // ── Row grid factory ─────────────────────────────────────────────────────

        /// <summary>Create a RowGrid wrapper with GridLayoutGroup for multi-column list rows.</summary>
        public static GameObject CreateRowGrid(GameObject panelRoot, int cols, int panelWidth, int ordSeed)
        {
            var go = new GameObject($"RowGrid_{ordSeed}", typeof(RectTransform));
            go.transform.SetParent(panelRoot.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var grid = go.AddComponent<GridLayoutGroup>();
            float gridGap = 4f;
            float availableWidth = panelWidth > 0 ? panelWidth - 16f : 700f;
            float cellW = Mathf.Floor((availableWidth - gridGap * (cols - 1)) / cols);
            grid.cellSize = new Vector2(cellW, 28f);
            grid.spacing = new Vector2(gridGap, gridGap);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = panelWidth > 0 ? panelWidth - 16f : 700f;
            le.flexibleWidth = 1f;
            return go;
        }

        // ── Rounded border ───────────────────────────────────────────────────────

        /// <summary>Attach a RoundedBorder child to a panel root. Idempotent — replaces "Border".</summary>
        public static void ApplyRoundedBorder(GameObject panelRoot, PanelPaddingJson pad)
        {
            if (panelRoot == null || pad == null) return;
            var existing = panelRoot.transform.Find("Border");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var rootImage = panelRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                var c = rootImage.color;
                c.a = 0f;
                rootImage.color = c;
                rootImage.raycastTarget = false;
            }

            var go = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(panelRoot.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsFirstSibling();

            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            var border = go.AddComponent<RoundedBorder>();
            border.BorderWidth = pad.border_width;
            border.CornerRadius = pad.corner_radius;
            border.BorderColor = ResolveBorderColor(pad.border_color_token);
            border.FillEnabled = true;
            border.FillColor = new Color(0.196f, 0.196f, 0.196f, 1f);
            border.raycastTarget = false;
        }

        // ── Internal JSON helper ─────────────────────────────────────────────────

        static T TryParseTypedJson<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();
            try
            {
                var parsed = UnityEngine.JsonUtility.FromJson<T>(json);
                return parsed ?? new T();
            }
            catch { return new T(); }
        }
    }
}
