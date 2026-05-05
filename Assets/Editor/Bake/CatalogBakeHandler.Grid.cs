// TECH-11937 / game-ui-catalog-bake Stage 5 — Grid partial.
//
// Bakes a grid-layout panel: root carries RectTransform + GridLayoutGroup +
// ContentSizeFitter (vertical PreferredSize). Children dispatched per `kind`:
// "button" → BuildGridChildCell (Cell_{ord} carrying Button + Icon Image).
// Non-button kinds throw NotSupportedException — picker semantics are icons only.
//
// params_json keys (PanelRow.params_json):
//   grid_cols      — FixedColumnCount constraintCount.
//   cell_w_px      — GridLayoutGroup.cellSize.x.
//   cell_h_px      — GridLayoutGroup.cellSize.y.
//   spacing_x_px   — GridLayoutGroup.spacing.x (default 0).
//   spacing_y_px   — GridLayoutGroup.spacing.y (default 0).
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
        /// <summary>
        /// Bake a single grid panel into a prefab at `{outDir}/{row.slug}.prefab`.
        /// Returns the asset path written, or empty string on no-op.
        /// </summary>
        internal static string BakeGrid(PanelRow row, IReadOnlyList<PanelChildRow> children, string outDir)
        {
            if (row == null || string.IsNullOrEmpty(row.slug)) return string.Empty;

            var assetPath = Path.Combine(outDir, row.slug + ".prefab").Replace('\\', '/');

            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var gridParams = ParseGridParams(row.params_json);
            var padding = ParseGridPadding(row.padding_json);

            var root = new GameObject(row.slug);
            try
            {
                var rootRT = root.AddComponent<RectTransform>();
                // Top-stretch root: vertical content grows downward from anchor.
                rootRT.anchorMin = new Vector2(0f, 1f);
                rootRT.anchorMax = new Vector2(1f, 1f);
                rootRT.pivot     = new Vector2(0.5f, 1f);
                rootRT.anchoredPosition = Vector2.zero;
                rootRT.sizeDelta = new Vector2(0f, 0f);

                var grid = root.AddComponent<GridLayoutGroup>();
                grid.cellSize        = new Vector2(gridParams.cell_w_px, gridParams.cell_h_px);
                grid.spacing         = new Vector2(gridParams.spacing_x_px, gridParams.spacing_y_px);
                grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment  = TextAnchor.UpperLeft;
                grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = Math.Max(1, gridParams.grid_cols);
                grid.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);

                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        if (child.kind != "button")
                            throw new NotSupportedException(
                                $"grid layout supports kind='button' only; got '{child.kind}' at ord={child.ord}");
                        BuildGridChildCell(root.transform, child);
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
        /// Build a grid cell: Button with Icon child Image. Name shape `Cell_{ord}`
        /// matches plan-digest acceptance for fixture-driven tests.
        /// </summary>
        private static void BuildGridChildCell(Transform parent, PanelChildRow child)
        {
            var go = new GameObject("Cell_" + child.ord);
            go.transform.SetParent(parent, false);

            // GridLayoutGroup drives RectTransform size — local sizeDelta ignored at runtime.
            go.AddComponent<RectTransform>();

            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            bgImage.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImage;
            btn.transition    = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = ResolveSprite(child.hover_sprite_ref),
                pressedSprite     = ResolveSprite(child.pressed_sprite_ref),
                disabledSprite    = ResolveSprite(child.disabled_sprite_ref),
            };

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);

            var iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot     = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(32f, 32f);
            iconRT.anchoredPosition = Vector2.zero;

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite = ResolveSprite(child.sprite_ref);
        }

        // ─── params_json parsers ───────────────────────────────────────────────

        [Serializable]
        private class GridParamsDto
        {
            public int grid_cols;
            public int cell_w_px;
            public int cell_h_px;
            public int spacing_x_px;
            public int spacing_y_px;
        }

        private struct GridParams
        {
            public int grid_cols;
            public int cell_w_px;
            public int cell_h_px;
            public int spacing_x_px;
            public int spacing_y_px;
        }

        private static GridParams ParseGridParams(string paramsJson)
        {
            var defaults = new GridParams { grid_cols = 1, cell_w_px = 64, cell_h_px = 64, spacing_x_px = 0, spacing_y_px = 0 };
            if (string.IsNullOrEmpty(paramsJson)) return defaults;
            try
            {
                var dto = JsonUtility.FromJson<GridParamsDto>(paramsJson);
                if (dto == null) return defaults;
                return new GridParams
                {
                    grid_cols     = dto.grid_cols     > 0 ? dto.grid_cols     : defaults.grid_cols,
                    cell_w_px     = dto.cell_w_px     > 0 ? dto.cell_w_px     : defaults.cell_w_px,
                    cell_h_px     = dto.cell_h_px     > 0 ? dto.cell_h_px     : defaults.cell_h_px,
                    spacing_x_px  = dto.spacing_x_px  >= 0 ? dto.spacing_x_px : defaults.spacing_x_px,
                    spacing_y_px  = dto.spacing_y_px  >= 0 ? dto.spacing_y_px : defaults.spacing_y_px,
                };
            }
            catch
            {
                return defaults;
            }
        }

        private struct GridPadding { public int top, right, bottom, left; }

        [Serializable]
        private class GridPaddingDto
        {
            public int top;
            public int right;
            public int bottom;
            public int left;
        }

        private static GridPadding ParseGridPadding(string paddingJson)
        {
            if (string.IsNullOrEmpty(paddingJson)) return new GridPadding();
            try
            {
                var p = JsonUtility.FromJson<GridPaddingDto>(paddingJson);
                if (p == null) return new GridPadding();
                return new GridPadding { top = p.top, right = p.right, bottom = p.bottom, left = p.left };
            }
            catch
            {
                return new GridPadding();
            }
        }
    }
}
