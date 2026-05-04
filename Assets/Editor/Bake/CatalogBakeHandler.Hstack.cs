// TECH-11926 / game-ui-catalog-bake Stage 1.0 — Hstack partial.
//
// Builds a horizontal-row prefab: root carries RectTransform + Image
// background placeholder + HorizontalLayoutGroup (spacing = row.gap_px).
// Each child becomes a Button GameObject with a child Icon GameObject
// carrying an Image whose `sprite` resolves from `child.sprite_ref`
// (Unity-relative asset path).
//
// Idempotent: deletes any existing prefab at the target path before save.

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
        /// Bake a single hstack panel into a prefab at `{outDir}/{row.slug}.prefab`.
        /// Returns the asset path written, or empty string on no-op.
        /// </summary>
        static string BakeHstack(PanelRow row, IReadOnlyList<PanelChildRow> children, string outDir)
        {
            if (row == null || string.IsNullOrEmpty(row.slug)) return string.Empty;

            var assetPath = Path.Combine(outDir, row.slug + ".prefab").Replace('\\', '/');

            // Idempotency: drop any existing prefab at this path before re-emit.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var root = new GameObject(row.slug);
            try
            {
                var rootRT = root.AddComponent<RectTransform>();
                // Top-stretch defaults — caller scene may override anchors after instantiate.
                rootRT.anchorMin = new Vector2(0f, 1f);
                rootRT.anchorMax = new Vector2(1f, 1f);
                rootRT.pivot = new Vector2(0.5f, 1f);
                rootRT.anchoredPosition = Vector2.zero;
                rootRT.sizeDelta = new Vector2(0f, 64f);

                var hlg = root.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = row.gap_px;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        BuildChildButton(root.transform, child);
                    }
                }

                EnsureFolderForAsset(assetPath);
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            return assetPath;
        }

        static void BuildChildButton(Transform parent, PanelChildRow child)
        {
            var go = new GameObject("Button_" + child.ord);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 48f);

            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            bgImage.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImage;

            // Icon child — Image only, raycast off so button receives clicks.
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);

            var iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(32f, 32f);
            iconRT.anchoredPosition = Vector2.zero;

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite = ResolveSprite(child.sprite_ref);
        }

        static Sprite ResolveSprite(string spriteRef)
        {
            if (string.IsNullOrEmpty(spriteRef)) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(spriteRef);
        }

        static void EnsureFolderForAsset(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;

            // AssetDatabase.CreateFolder handles "Assets/X/Y/Z" stepwise.
            var parts = dir.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
