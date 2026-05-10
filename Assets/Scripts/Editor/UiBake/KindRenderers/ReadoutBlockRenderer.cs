using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>
    /// Renders a readout-block widget: label TMP_Text + value TMP_Text + delta-color marker.
    /// Wave B3 (TECH-27088) — budget-panel treasury / projected-balance readouts.
    /// deltaColorRule from params_json: "positive-green" (default) tints value green when > 0.
    /// </summary>
    public sealed class ReadoutBlockRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("readout-block", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = string.Empty;
            labelTmp.fontSize = 13f;
            labelTmp.raycastTarget = false;

            var valueGo = new GameObject("Value", typeof(RectTransform));
            valueGo.transform.SetParent(go.transform, worldPositionStays: false);
            var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.text = "$0";
            valueTmp.fontSize = 16f;
            valueTmp.fontStyle = TMPro.FontStyles.Bold;
            valueTmp.raycastTarget = false;

            // Delta-color marker GO — runtime adapter reads this to apply color logic.
            bool positiveGreen = string.IsNullOrEmpty(paramsJson) || !paramsJson.Contains("\"deltaColorRule\":\"negative-red\"");
            var deltaMarker = new GameObject("DeltaColorMarker", typeof(RectTransform));
            deltaMarker.transform.SetParent(go.transform, worldPositionStays: false);
            var markerImg = deltaMarker.AddComponent<Image>();
            // Tint: green for positive-green rule, red for negative-red (set at runtime by adapter).
            markerImg.color = positiveGreen ? new Color(0.2f, 0.8f, 0.3f, 0.5f) : new Color(0.9f, 0.2f, 0.2f, 0.5f);
            markerImg.raycastTarget = false;

            return go;
        }
    }
}
