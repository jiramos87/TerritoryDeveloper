using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Truth source loader for `web/design-refs/step-1-game-ui/layout-rects.json` —
    /// the CD-bundle-extracted per-element rect catalog (`tools/scripts/extract-cd-layout-rects.ts`).
    /// Used by <see cref="UiBakeHandler.SavePanelPrefab"/> to derive panel root
    /// anchors / size from the design mockup instead of hardcoded placeholder
    /// (0.5, 0.5) 600×800. Reference viewport = 1920×1080 (matches Canvas
    /// reference resolution).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Anti-loss invariant: every UI prefab regen pass MUST consult this loader.
    /// When a panel slug has no entry, callers (bake handler) gate on
    /// <see cref="HasEntry"/> + emit explicit error rather than silently
    /// defaulting — see Stage 13.7 fallout where centered 600×800 placeholder
    /// wiped baked positioning across 19 prefabs.
    /// </para>
    /// <para>
    /// Cache lifetime: loaded once per AssetDatabase refresh window. Manual
    /// invalidation via <see cref="Invalidate"/> when the JSON file is
    /// regenerated mid-session.
    /// </para>
    /// </remarks>
    public static class LayoutRectsLoader
    {
        public const float ReferenceViewportWidth = 1920f;
        public const float ReferenceViewportHeight = 1080f;

        private const string LayoutRectsRelativePath =
            "../web/design-refs/step-1-game-ui/layout-rects.json";

        public readonly struct LayoutRect
        {
            public readonly string NodeKind;
            public readonly string CdSlug;
            public readonly string ParentKind;
            public readonly string ParentCdSlug;
            public readonly Rect ViewportRect;
            public readonly Rect ParentRelativeRect;

            public LayoutRect(string nodeKind, string cdSlug, string parentKind,
                string parentCdSlug, Rect viewportRect, Rect parentRelativeRect)
            {
                NodeKind = nodeKind;
                CdSlug = cdSlug;
                ParentKind = parentKind;
                ParentCdSlug = parentCdSlug;
                ViewportRect = viewportRect;
                ParentRelativeRect = parentRelativeRect;
            }
        }

        private static Dictionary<string, LayoutRect> _panelRects;

        /// <summary>
        /// Drop the in-memory cache. Call after regenerating layout-rects.json
        /// in the same Editor session.
        /// </summary>
        public static void Invalidate()
        {
            _panelRects = null;
        }

        /// <summary>True when the loader has a panel-kind entry for this slug.</summary>
        public static bool HasEntry(string panelSlug)
        {
            EnsureLoaded();
            return _panelRects != null && !string.IsNullOrEmpty(panelSlug)
                && _panelRects.ContainsKey(panelSlug);
        }

        /// <summary>
        /// Resolve panel rect by slug. Returns false when missing — caller must
        /// fail loud rather than fall back to placeholder anchors.
        /// </summary>
        public static bool TryGetPanelRect(string panelSlug, out LayoutRect rect)
        {
            EnsureLoaded();
            if (_panelRects != null && !string.IsNullOrEmpty(panelSlug)
                && _panelRects.TryGetValue(panelSlug, out rect))
            {
                return true;
            }
            rect = default;
            return false;
        }

        /// <summary>
        /// Convert a 1920×1080 viewport rect to Canvas-relative anchorMin /
        /// anchorMax (bottom-left origin Unity UI convention). HTML uses
        /// top-left origin; we flip Y.
        /// </summary>
        public static void ViewportRectToCanvasAnchors(Rect viewportRect,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            float vw = ReferenceViewportWidth;
            float vh = ReferenceViewportHeight;
            float htmlBottom = vh - (viewportRect.y + viewportRect.height);
            anchorMin = new Vector2(viewportRect.x / vw, htmlBottom / vh);
            anchorMax = new Vector2((viewportRect.x + viewportRect.width) / vw,
                (vh - viewportRect.y) / vh);
        }

        public static string GetLayoutRectsAbsolutePath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath,
                LayoutRectsRelativePath));
        }

        private static void EnsureLoaded()
        {
            if (_panelRects != null) return;
            _panelRects = new Dictionary<string, LayoutRect>();

            string absPath = GetLayoutRectsAbsolutePath();
            if (!File.Exists(absPath))
            {
                Debug.LogWarning(
                    $"[LayoutRectsLoader] truth source missing at {absPath} — bake will fail loud on every panel.");
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(absPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LayoutRectsLoader] read failed: {e.Message}");
                return;
            }

            // JsonUtility cannot deserialize the top-level array shape; parse
            // via lightweight DTO + manual nested-rect mapping.
            LayoutRectsRoot root;
            try
            {
                root = JsonUtility.FromJson<LayoutRectsRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LayoutRectsLoader] parse failed: {e.Message}");
                return;
            }

            if (root?.nodes == null) return;
            int panelCount = 0;
            foreach (var node in root.nodes)
            {
                if (node == null || node.node_kind != "panel") continue;
                if (string.IsNullOrEmpty(node.cd_slug)) continue;
                var lr = new LayoutRect(
                    node.node_kind,
                    node.cd_slug,
                    node.parent_kind,
                    node.parent_cd_slug,
                    new Rect(node.viewport_rect.x, node.viewport_rect.y,
                        node.viewport_rect.width, node.viewport_rect.height),
                    new Rect(node.parent_relative_rect.x, node.parent_relative_rect.y,
                        node.parent_relative_rect.width, node.parent_relative_rect.height));
                _panelRects[node.cd_slug] = lr;
                panelCount++;
            }
            Debug.Log($"[LayoutRectsLoader] loaded {panelCount} panel rects from {absPath}");
        }

        // ── JSON DTOs ───────────────────────────────────────────────────────

        [Serializable]
        private class LayoutRectsRoot
        {
            public int schema_version;
            public LayoutRectsViewport viewport;
            public List<LayoutRectsNode> nodes;
        }

        [Serializable]
        private class LayoutRectsViewport
        {
            public float width;
            public float height;
        }

        [Serializable]
        private class LayoutRectsNode
        {
            public string node_kind;
            public string cd_slug;
            public string parent_kind;
            public string parent_cd_slug;
            public LayoutRectsRect viewport_rect;
            public LayoutRectsRect parent_relative_rect;
        }

        [Serializable]
        private class LayoutRectsRect
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }
    }
}
