using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>
    /// Renders a slider-row widget (track + fill + thumb + label) with real Slider component. TECH-27542.
    /// When paramsJson contains "numeric":true (slider-row-numeric alias), appends a live value
    /// readout TMP_Text left-aligned next to the label. TECH-27088.
    /// </summary>
    public sealed class SliderRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            bool isNumeric = !string.IsNullOrEmpty(paramsJson) && paramsJson.Contains("\"numeric\":true");

            var go = new GameObject(isNumeric ? "slider-row-numeric" : "slider-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(go.transform, worldPositionStays: false);
            track.AddComponent<Image>().raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, worldPositionStays: false);
            fill.AddComponent<Image>().raycastTarget = false;

            var thumb = new GameObject("Thumb", typeof(RectTransform));
            thumb.transform.SetParent(go.transform, worldPositionStays: false);
            var thumbImage = thumb.AddComponent<Image>();

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, worldPositionStays: false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty;
            tmp.fontSize = 14f;
            tmp.raycastTarget = false;

            // Wire real Slider component (C3 fix — was Image stub).
            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = thumbImage;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = thumb.GetComponent<RectTransform>();

            // Numeric variant — live value readout left-aligned (TECH-27088).
            if (isNumeric)
            {
                var readout = new GameObject("ValueReadout", typeof(RectTransform));
                readout.transform.SetParent(go.transform, worldPositionStays: false);
                var readoutTmp = readout.AddComponent<TextMeshProUGUI>();
                readoutTmp.text = "0";
                readoutTmp.fontSize = 14f;
                readoutTmp.alignment = TMPro.TextAlignmentOptions.Left;
                readoutTmp.raycastTarget = false;
            }

            return go;
        }
    }
}
