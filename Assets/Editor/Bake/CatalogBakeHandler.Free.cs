// TECH-11938 / game-ui-catalog-bake Stage 5 — Free partial.
//
// Bakes a free-layout panel: no auto-layout component, children placed at
// literal absolute coordinates from `panel_child.params_json.{x_px,y_px,w_px,h_px}`.
// Y-axis convention: catalog `y_px` is "px from top"; Unity Y-up with top-anchor
// (anchorMin=anchorMax=(0,1)) requires `anchoredPosition.y = -y_px`.
//
// Supported child kinds: "sprite" (Image with sprite ref), "text" (legacy
// UnityEngine.UI.Text whose string comes from `params_json.text`). Other kinds
// throw NotSupportedException — picker/popup semantics only.
//
// Root sizing: `panel.params_json.{width_px, height_px}` with defaults 200×100.
// Idempotent: deletes any existing prefab at the target path before save.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TerritoryDeveloper.Editor.Bake
{
    public static partial class CatalogBakeHandler
    {
        /// <summary>
        /// Bake a single free-layout panel into a prefab at `{outDir}/{row.slug}.prefab`.
        /// Returns the asset path written, or empty string on no-op.
        /// </summary>
        internal static string BakeFree(PanelRow row, IReadOnlyList<PanelChildRow> children, string outDir)
        {
            if (row == null || string.IsNullOrEmpty(row.slug)) return string.Empty;

            var assetPath = Path.Combine(outDir, row.slug + ".prefab").Replace('\\', '/');

            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var rootDims = ParseFreeRootDims(row.params_json);

            var root = new GameObject(row.slug);
            try
            {
                var rootRT = root.AddComponent<RectTransform>();
                // Top-left anchor: child absolute coords are relative to root top-left.
                rootRT.anchorMin = new Vector2(0f, 1f);
                rootRT.anchorMax = new Vector2(0f, 1f);
                rootRT.pivot     = new Vector2(0f, 1f);
                rootRT.anchoredPosition = Vector2.zero;
                rootRT.sizeDelta = new Vector2(rootDims.width_px, rootDims.height_px);

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        switch (child.kind)
                        {
                            case "sprite":
                                BuildFreeSpriteChild(root.transform, child);
                                break;
                            case "text":
                                BuildFreeTextChild(root.transform, child);
                                break;
                            default:
                                throw new NotSupportedException(
                                    $"free layout supports kind in (sprite|text); got '{child.kind}' at ord={child.ord}");
                        }
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

        private static void BuildFreeSpriteChild(Transform parent, PanelChildRow child)
        {
            var dims = ParseFreeChildDims(child.params_json);

            var go = new GameObject("Sprite_" + child.ord);
            go.transform.SetParent(parent, false);

            ApplyFreeChildRect(go.AddComponent<RectTransform>(), dims);

            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.sprite = ResolveSprite(child.sprite_ref);
        }

        private static void BuildFreeTextChild(Transform parent, PanelChildRow child)
        {
            var dims = ParseFreeChildDims(child.params_json);

            var go = new GameObject("Text_" + child.ord);
            go.transform.SetParent(parent, false);

            ApplyFreeChildRect(go.AddComponent<RectTransform>(), dims);

            var text = go.AddComponent<Text>();
            text.text = dims.text ?? string.Empty;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
        }

        private static void ApplyFreeChildRect(RectTransform rt, FreeChildDims dims)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            // Y inverted: catalog top-down → Unity top-anchor with negative Y.
            rt.anchoredPosition = new Vector2(dims.x_px, -dims.y_px);
            rt.sizeDelta        = new Vector2(dims.w_px, dims.h_px);
        }

        // ─── params_json parsers ───────────────────────────────────────────────

        [Serializable]
        private class FreeRootDimsDto
        {
            public int width_px;
            public int height_px;
        }

        private struct FreeRootDims
        {
            public int width_px;
            public int height_px;
        }

        private static FreeRootDims ParseFreeRootDims(string paramsJson)
        {
            var defaults = new FreeRootDims { width_px = 200, height_px = 100 };
            if (string.IsNullOrEmpty(paramsJson)) return defaults;
            try
            {
                var dto = JsonUtility.FromJson<FreeRootDimsDto>(paramsJson);
                if (dto == null) return defaults;
                return new FreeRootDims
                {
                    width_px  = dto.width_px  > 0 ? dto.width_px  : defaults.width_px,
                    height_px = dto.height_px > 0 ? dto.height_px : defaults.height_px,
                };
            }
            catch
            {
                return defaults;
            }
        }

        [Serializable]
        private class FreeChildDimsDto
        {
            public int x_px;
            public int y_px;
            public int w_px;
            public int h_px;
            public string text;
        }

        private struct FreeChildDims
        {
            public int x_px;
            public int y_px;
            public int w_px;
            public int h_px;
            public string text;
        }

        private static FreeChildDims ParseFreeChildDims(string paramsJson)
        {
            var defaults = new FreeChildDims { x_px = 0, y_px = 0, w_px = 24, h_px = 24, text = string.Empty };
            if (string.IsNullOrEmpty(paramsJson)) return defaults;
            try
            {
                var dto = JsonUtility.FromJson<FreeChildDimsDto>(paramsJson);
                if (dto == null) return defaults;
                return new FreeChildDims
                {
                    x_px = dto.x_px,
                    y_px = dto.y_px,
                    w_px = dto.w_px > 0 ? dto.w_px : defaults.w_px,
                    h_px = dto.h_px > 0 ? dto.h_px : defaults.h_px,
                    text = dto.text ?? string.Empty,
                };
            }
            catch
            {
                return defaults;
            }
        }
    }
}
