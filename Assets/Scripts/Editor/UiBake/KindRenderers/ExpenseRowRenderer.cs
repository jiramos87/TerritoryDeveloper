using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>
    /// Renders an expense-row widget: icon Image + label TMP_Text + amount TMP_Text.
    /// Wave B3 (TECH-27088) — budget-panel service-funding rows.
    /// </summary>
    public sealed class ExpenseRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("expense-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(go.transform, worldPositionStays: false);
            var iconImage = icon.AddComponent<Image>();
            iconImage.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = string.Empty;
            labelTmp.fontSize = 14f;
            labelTmp.raycastTarget = false;

            var amountGo = new GameObject("Amount", typeof(RectTransform));
            amountGo.transform.SetParent(go.transform, worldPositionStays: false);
            var amountTmp = amountGo.AddComponent<TextMeshProUGUI>();
            amountTmp.text = "$0";
            amountTmp.fontSize = 14f;
            amountTmp.alignment = TMPro.TextAlignmentOptions.Right;
            amountTmp.raycastTarget = false;

            return go;
        }
    }
}
