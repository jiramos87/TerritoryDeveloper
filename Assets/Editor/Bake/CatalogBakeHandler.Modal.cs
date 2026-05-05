// TECH-11933 / game-ui-catalog-bake Stage 3 — Modal partial.
//
// Bakes a modal-layout panel: centered on viewport, viewport-clamped, dim
// backdrop child covering the full screen, vertical content child stack
// honoring padding_json + gap_px from panel_detail.
//
// Design decisions (per Stage 3 §Plan Digest §Pending Decisions):
//   - viewport bounds: CanvasScaler.referenceResolution (default 1920x1080);
//     EditMode bake has no live Screen.width/height.
//   - safe-area margin: 48px symmetric.
//   - intrinsic size: width = max(child.preferredWidth) + padding.left + padding.right;
//     height = sum(child.preferredHeight) + (childCount-1)*gap_px + padding.top + padding.bottom.
//   - backdrop: solid black Image, alpha 0.5, no Button, raycastTarget=true.
//   - unknown-layout fallback: NotSupportedException (unchanged from dispatcher).

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TerritoryDeveloper.Editor.Bake
{
    public static partial class CatalogBakeHandler
    {
        // Safe-area margin applied on each side when clamping sizeDelta to viewport.
        public const float ModalSafeAreaMarginPx = 48f;

        // Default child preferred sizes (placeholder — real content uses LayoutElement or
        // Unity's preferred-size API; in EditMode bake without live Canvas the fallbacks
        // are constant so the math is deterministic across runs).
        private const float DefaultChildPreferredWidth  = 200f;
        private const float DefaultChildPreferredHeight = 40f;

        // Test-only viewport override. Null → fall back to (1920, 1080) project default.
        // Tests set this before calling BakeModal and clear it in [TearDown].
        public static Vector2? BakeViewportOverride = null;

        /// <summary>
        /// Bake a single modal panel into a prefab at `{outDir}/{row.slug}.prefab`.
        /// Returns the asset path written, or empty string on no-op.
        /// </summary>
        internal static string BakeModal(PanelRow row, IReadOnlyList<PanelChildRow> children, string outDir)
        {
            if (row == null || string.IsNullOrEmpty(row.slug)) return string.Empty;

            var assetPath = System.IO.Path.Combine(outDir, row.slug + ".prefab").Replace('\\', '/');

            // Idempotency: drop any existing prefab at this path before re-emit.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var root = new GameObject(row.slug);
            try
            {
                // ── Root RectTransform: anchored + pivoted at center (0.5, 0.5) ──────
                var rootRT = root.AddComponent<RectTransform>();
                rootRT.anchorMin = new Vector2(0.5f, 0.5f);
                rootRT.anchorMax = new Vector2(0.5f, 0.5f);
                rootRT.pivot     = new Vector2(0.5f, 0.5f);
                rootRT.anchoredPosition = Vector2.zero;

                // ── Compute intrinsic size from padding + children ─────────────────
                var padding = ParsePadding(row.padding_json);
                int childCount = children?.Count ?? 0;

                float intrinsicWidth  = DefaultChildPreferredWidth  + padding.left + padding.right;
                float intrinsicHeight = (DefaultChildPreferredHeight * Mathf.Max(childCount, 1))
                                      + (Mathf.Max(childCount - 1, 0) * row.gap_px)
                                      + padding.top + padding.bottom;

                // ── Viewport clamp: reference resolution of bake-target canvas ─────
                // Default: 1920x1080 per project convention. Tests override via
                // CanvasScaler.referenceResolution before invoking the bake entry point.
                var viewportSize = GetBakeViewportSize(root);
                float maxW = viewportSize.x - 2f * ModalSafeAreaMarginPx;
                float maxH = viewportSize.y - 2f * ModalSafeAreaMarginPx;

                rootRT.sizeDelta = new Vector2(
                    Mathf.Min(intrinsicWidth,  Mathf.Max(maxW, 1f)),
                    Mathf.Min(intrinsicHeight, Mathf.Max(maxH, 1f))
                );

                // ── Backdrop: first child, full-screen dim overlay ─────────────────
                var backdropGo = new GameObject("Backdrop");
                backdropGo.transform.SetParent(root.transform, false);

                var backdropRT = backdropGo.AddComponent<RectTransform>();
                backdropRT.anchorMin = Vector2.zero;     // (0, 0)
                backdropRT.anchorMax = Vector2.one;      // (1, 1)
                backdropRT.pivot     = new Vector2(0.5f, 0.5f);
                backdropRT.offsetMin = Vector2.zero;
                backdropRT.offsetMax = Vector2.zero;

                var backdropImg = backdropGo.AddComponent<Image>();
                backdropImg.color         = new Color(0f, 0f, 0f, 0.5f);
                backdropImg.raycastTarget = true;        // intercepts clicks behind modal

                // ── Content container: second child, vertical stack ────────────────
                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(root.transform, false);

                var contentRT = contentGo.AddComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.pivot     = new Vector2(0.5f, 0.5f);
                // Inset by padding
                contentRT.offsetMin = new Vector2(padding.left, padding.bottom);
                contentRT.offsetMax = new Vector2(-padding.right, -padding.top);

                var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                vlg.spacing              = row.gap_px;
                vlg.childAlignment       = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth  = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth    = true;
                vlg.childControlHeight   = false;
                vlg.padding = new RectOffset(0, 0, 0, 0);

                // ── Content children ───────────────────────────────────────────────
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        if (child.kind == "button")
                            BuildChildButton(contentGo.transform, child);
                        else
                            BuildChildLabel(contentGo.transform, child);
                    }
                }

                EnsureFolderForAsset(assetPath);
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return assetPath;
        }

        /// <summary>
        /// Returns the viewport size to use for modal sizeDelta clamping.
        /// Priority: `BakeViewportOverride` (test injection) → (1920, 1080) project default.
        /// </summary>
        private static Vector2 GetBakeViewportSize(GameObject root)
        {
            if (BakeViewportOverride.HasValue && BakeViewportOverride.Value != Vector2.zero)
                return BakeViewportOverride.Value;

            return new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// Build a label child under `parent` (plain Image + placeholder sizing).
        /// </summary>
        private static void BuildChildLabel(Transform parent, PanelChildRow child)
        {
            var go = new GameObject("Label_" + child.ord);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(DefaultChildPreferredWidth, DefaultChildPreferredHeight);

            // Placeholder visual — no Text component in bake (font asset scope out of Stage 3).
            var img = go.AddComponent<Image>();
            img.color         = new Color(0f, 0f, 0f, 0f); // transparent placeholder
            img.raycastTarget = false;
        }

        // ─── Padding parser ────────────────────────────────────────────────────

        private struct Padding { public float top, right, bottom, left; }

        private static Padding ParsePadding(string paddingJson)
        {
            // Fast path for null/empty — return zero padding.
            if (string.IsNullOrEmpty(paddingJson))
                return new Padding();

            // JsonUtility requires [Serializable] objects; parse manually via
            // simple substring extraction for the 4-key CSS-style object.
            try
            {
                var p = JsonUtility.FromJson<PaddingDto>(paddingJson);
                return new Padding { top = p.top, right = p.right, bottom = p.bottom, left = p.left };
            }
            catch
            {
                return new Padding();
            }
        }

        [Serializable]
        private class PaddingDto
        {
            public float top;
            public float right;
            public float bottom;
            public float left;
        }
    }
}
