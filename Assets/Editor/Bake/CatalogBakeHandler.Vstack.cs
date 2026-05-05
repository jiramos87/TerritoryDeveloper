// TECH-11935 / game-ui-catalog-bake Stage 4 — Vstack partial.
//
// Bakes a vertical-row panel: root carries VerticalLayoutGroup +
// ContentSizeFitter (vertical PreferredSize). When `params_json.scroll=true`
// root is a ScrollRect whose viewport child carries RectMask2D and whose
// content child carries the VerticalLayoutGroup. Children dispatched per
// `kind`: "button" → BuildChildButton (reused from Hstack), "label" →
// BuildVstackChildLabel (UnityEngine.UI.Text), "row" → BuildChildRow
// ([icon | label | value | vu? | delta?] per ui-design-system §3.6 D4).
//
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
        // ui-design-system §3.6 D4 row layout — locked Stage 13 closeout.
        private const float VstackRowHeightPx = 28f;
        private const float VstackRowIconPx   = 20f;

        /// <summary>
        /// Bake a single vstack panel into a prefab at `{outDir}/{row.slug}.prefab`.
        /// Returns the asset path written, or empty string on no-op.
        /// </summary>
        internal static string BakeVstack(PanelRow row, IReadOnlyList<PanelChildRow> children, string outDir)
        {
            if (row == null || string.IsNullOrEmpty(row.slug)) return string.Empty;

            var assetPath = Path.Combine(outDir, row.slug + ".prefab").Replace('\\', '/');

            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            bool scroll = ParseScrollFlag(row.params_json);
            var padding = ParseVstackPadding(row.padding_json);

            var root = new GameObject(row.slug);
            try
            {
                var rootRT = root.AddComponent<RectTransform>();

                Transform contentParent;
                if (scroll)
                {
                    // Stretch-stretch root: ScrollRect must occupy a defined rect.
                    rootRT.anchorMin = new Vector2(0f, 0f);
                    rootRT.anchorMax = new Vector2(1f, 1f);
                    rootRT.pivot     = new Vector2(0.5f, 0.5f);
                    rootRT.offsetMin = Vector2.zero;
                    rootRT.offsetMax = Vector2.zero;

                    var scrollRect = root.AddComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical   = true;

                    var viewportGo = new GameObject("Viewport");
                    viewportGo.transform.SetParent(root.transform, false);
                    var viewportRT = viewportGo.AddComponent<RectTransform>();
                    viewportRT.anchorMin = Vector2.zero;
                    viewportRT.anchorMax = Vector2.one;
                    viewportRT.pivot     = new Vector2(0.5f, 0.5f);
                    viewportRT.offsetMin = Vector2.zero;
                    viewportRT.offsetMax = Vector2.zero;
                    viewportGo.AddComponent<RectMask2D>();

                    var contentGo = new GameObject("Content");
                    contentGo.transform.SetParent(viewportGo.transform, false);
                    var contentRT = contentGo.AddComponent<RectTransform>();
                    // Top-anchored content stretches horizontally; height grows downward.
                    contentRT.anchorMin = new Vector2(0f, 1f);
                    contentRT.anchorMax = new Vector2(1f, 1f);
                    contentRT.pivot     = new Vector2(0.5f, 1f);
                    contentRT.anchoredPosition = Vector2.zero;
                    contentRT.sizeDelta = new Vector2(0f, 0f);

                    AddVerticalLayout(contentGo, padding, row.gap_px);
                    AddContentSizeFitter(contentGo);

                    scrollRect.viewport = viewportRT;
                    scrollRect.content  = contentRT;

                    contentParent = contentGo.transform;
                }
                else
                {
                    // Top-stretch root: vertical content grows downward from anchor.
                    rootRT.anchorMin = new Vector2(0f, 1f);
                    rootRT.anchorMax = new Vector2(1f, 1f);
                    rootRT.pivot     = new Vector2(0.5f, 1f);
                    rootRT.anchoredPosition = Vector2.zero;
                    rootRT.sizeDelta = new Vector2(0f, 0f);

                    AddVerticalLayout(root, padding, row.gap_px);
                    AddContentSizeFitter(root);

                    contentParent = root.transform;
                }

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        switch (child.kind)
                        {
                            case "button":
                                BuildChildButton(contentParent, child);
                                break;
                            case "label":
                                BuildVstackChildLabel(contentParent, child);
                                break;
                            case "row":
                                BuildChildRow(contentParent, child);
                                break;
                            default:
                                // Unknown child kinds are ignored — bake stays additive.
                                break;
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

        private static void AddVerticalLayout(GameObject host, VstackPadding padding, int gapPx)
        {
            var vlg = host.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = gapPx;
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
        }

        private static void AddContentSizeFitter(GameObject host)
        {
            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// Build a label child carrying a UnityEngine.UI.Text component.
        /// Distinct from Modal's placeholder BuildChildLabel (Image-only).
        /// </summary>
        private static void BuildVstackChildLabel(Transform parent, PanelChildRow child)
        {
            var go = new GameObject("Label_" + child.ord);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, VstackRowHeightPx);

            var text = go.AddComponent<Text>();
            text.text             = child.params_json ?? string.Empty;
            text.alignment        = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow   = VerticalWrapMode.Truncate;
            text.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize         = 14;
            text.color            = Color.white;
            text.raycastTarget    = false;
        }

        /// <summary>
        /// Build a stat-row child: [icon | label | value | vu? | delta?] per
        /// ui-design-system §3.6 D4. 28 px row, 20 px icon. Label / value /
        /// vu / delta are Text columns laid out left-to-right by an inner
        /// HorizontalLayoutGroup.
        /// </summary>
        private static void BuildChildRow(Transform parent, PanelChildRow child)
        {
            var go = new GameObject("Row_" + child.ord);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, VstackRowHeightPx);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 4f;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // Icon column — fixed 20 px square.
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(VstackRowIconPx, VstackRowIconPx);
            var iconLE = iconGo.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = VstackRowIconPx;
            iconLE.preferredHeight = VstackRowIconPx;
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite        = ResolveSprite(child.sprite_ref);

            // Label column — flexes; reads label text from params_json.
            var labelText = ExtractRowField(child.params_json, "label");
            BuildRowTextColumn(go.transform, "Label", labelText, flexible: true);

            // Value column — fixed-ish; reads value text from params_json.
            var valueText = ExtractRowField(child.params_json, "value");
            BuildRowTextColumn(go.transform, "Value", valueText, flexible: false);

            // Optional vu / delta columns when present in params_json.
            var vuText = ExtractRowField(child.params_json, "vu");
            if (!string.IsNullOrEmpty(vuText))
                BuildRowTextColumn(go.transform, "Vu", vuText, flexible: false);

            var deltaText = ExtractRowField(child.params_json, "delta");
            if (!string.IsNullOrEmpty(deltaText))
                BuildRowTextColumn(go.transform, "Delta", deltaText, flexible: false);
        }

        private static void BuildRowTextColumn(Transform parent, string name, string content, bool flexible)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, VstackRowHeightPx);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = VstackRowHeightPx;
            if (flexible)
                le.flexibleWidth = 1f;
            else
                le.preferredWidth = 64f;

            var text = go.AddComponent<Text>();
            text.text             = content ?? string.Empty;
            text.alignment        = flexible ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow   = VerticalWrapMode.Truncate;
            text.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize         = 14;
            text.color            = Color.white;
            text.raycastTarget    = false;
        }

        // ─── params_json parsers ───────────────────────────────────────────────

        private static bool ParseScrollFlag(string paramsJson)
        {
            if (string.IsNullOrEmpty(paramsJson)) return false;
            try
            {
                var p = JsonUtility.FromJson<VstackParams>(paramsJson);
                return p != null && p.scroll;
            }
            catch
            {
                return false;
            }
        }

        [Serializable]
        private class VstackParams
        {
            public bool scroll;
        }

        private struct VstackPadding { public int top, right, bottom, left; }

        private static VstackPadding ParseVstackPadding(string paddingJson)
        {
            if (string.IsNullOrEmpty(paddingJson)) return new VstackPadding();
            try
            {
                var p = JsonUtility.FromJson<VstackPaddingDto>(paddingJson);
                return new VstackPadding { top = p.top, right = p.right, bottom = p.bottom, left = p.left };
            }
            catch
            {
                return new VstackPadding();
            }
        }

        [Serializable]
        private class VstackPaddingDto
        {
            public int top;
            public int right;
            public int bottom;
            public int left;
        }

        // Lightweight sub-key extractor for {"label":"...","value":"...","vu":"...","delta":"..."}.
        // JsonUtility cannot deserialize free-form keys → tolerant manual scan.
        private static string ExtractRowField(string paramsJson, string key)
        {
            if (string.IsNullOrEmpty(paramsJson) || string.IsNullOrEmpty(key)) return string.Empty;
            var needle = "\"" + key + "\"";
            int idx = paramsJson.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            int colon = paramsJson.IndexOf(':', idx + needle.Length);
            if (colon < 0) return string.Empty;
            int firstQuote = paramsJson.IndexOf('"', colon + 1);
            if (firstQuote < 0) return string.Empty;
            int secondQuote = paramsJson.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return string.Empty;
            return paramsJson.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }
    }
}
