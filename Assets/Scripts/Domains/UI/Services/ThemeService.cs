using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Services
{
    /// <summary>
    /// Pure static UI theming utilities extracted from UIManager.Theme.cs (Stage 19 atomization).
    /// Invariant: no UIManager fields accessed — all inputs are explicit parameters.
    /// Strategy γ: partial retention — UIManager.Theme.cs partial kept; field-dependent methods remain on UIManager.
    /// </summary>
    public static class ThemeService
    {
        /// <summary>
        /// Style all sibling Text components (excluding <paramref name="valueTransform"/> itself)
        /// with caption size and color.
        /// </summary>
        public static void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
        {
            Transform parent = valueTransform.parent;
            if (parent == null)
                return;
            foreach (Transform child in parent)
            {
                if (child == valueTransform)
                    continue;
                var t = child.GetComponent<Text>();
                if (t == null)
                    continue;
                t.fontSize = captionSize;
                t.color = captionColor;
            }
        }

        /// <summary>
        /// Walk ancestors of <paramref name="t"/> returning the first whose name matches <paramref name="exactName"/>.
        /// Returns null if not found.
        /// </summary>
        public static Transform FindNamedAncestor(Transform t, string exactName)
        {
            while (t != null)
            {
                if (t.name == exactName)
                    return t;
                t = t.parent;
            }
            return null;
        }

        /// <summary>
        /// Axis-aligned bounds of <paramref name="childRt"/> in <paramref name="parentRt"/> local space.
        /// Returns false when either argument is null or bounds are degenerate.
        /// </summary>
        public static bool TryGetRectBoundsInParent(
            RectTransform parentRt,
            RectTransform childRt,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (parentRt == null || childRt == null)
                return false;
            Rect r = childRt.rect;
            Vector3[] corners =
            {
                new Vector3(r.xMin, r.yMin, 0f),
                new Vector3(r.xMin, r.yMax, 0f),
                new Vector3(r.xMax, r.yMax, 0f),
                new Vector3(r.xMax, r.yMin, 0f),
            };
            minX = minY = float.PositiveInfinity;
            maxX = maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 pl = parentRt.InverseTransformPoint(childRt.TransformPoint(corners[i]));
                minX = Mathf.Min(minX, pl.x);
                maxX = Mathf.Max(maxX, pl.x);
                minY = Mathf.Min(minY, pl.y);
                maxY = Mathf.Max(maxY, pl.y);
            }
            return maxX > minX && maxY > minY;
        }

        /// <summary>
        /// Create a thin horizontal divider stripe Image under <paramref name="parent"/>.
        /// Extracted from UIManager.Theme.cs: CreateTaxPanelDivider.
        /// </summary>
        public static void CreateDividerStripe(
            Transform parent,
            string objectName,
            Sprite sprite,
            Color lineColor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = lineColor;
        }
    }
}
