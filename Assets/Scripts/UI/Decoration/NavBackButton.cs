using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Decoration
{
    /// <summary>
    /// Official UI back-arrow factory. Single source of truth for the back-button
    /// visual across the game (pause-menu sub-views, modal-card panels, etc.).
    /// 40x40 dark chip + "<" TMP glyph. Caller wires onClick.
    /// Future visual changes here propagate everywhere.
    /// </summary>
    public static class NavBackButton
    {
        public const float DefaultSize = 40f;

        public static GameObject Spawn(GameObject parent, float size = DefaultSize)
        {
            var backGo = new GameObject("back-button", typeof(RectTransform),
                typeof(Image), typeof(Button), typeof(LayoutElement));
            if (parent != null)
            {
                backGo.transform.SetParent(parent.transform, worldPositionStays: false);
            }

            var backImg = backGo.GetComponent<Image>();
            backImg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

            var backLe = backGo.GetComponent<LayoutElement>();
            backLe.preferredWidth = size;
            backLe.preferredHeight = size;
            backLe.minWidth = size;
            backLe.minHeight = size;

            var backBtn = backGo.GetComponent<Button>();
            backBtn.targetGraphic = backImg;

            var backLabelGo = new GameObject("Label", typeof(RectTransform));
            backLabelGo.transform.SetParent(backGo.transform, worldPositionStays: false);
            var backTmp = backLabelGo.AddComponent<TextMeshProUGUI>();
            backTmp.text = "<";
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.fontSize = Mathf.Round(size * 0.6f);
            backTmp.fontStyle = FontStyles.Bold;
            backTmp.color = Color.white;
            backTmp.raycastTarget = false;
            var backLabelRt = backLabelGo.GetComponent<RectTransform>();
            backLabelRt.anchorMin = Vector2.zero;
            backLabelRt.anchorMax = Vector2.one;
            backLabelRt.offsetMin = backLabelRt.offsetMax = Vector2.zero;

            return backGo;
        }
    }
}
