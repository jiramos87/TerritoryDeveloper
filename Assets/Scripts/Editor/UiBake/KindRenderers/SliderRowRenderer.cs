using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Renders a slider-row widget (track + fill + thumb + label).</summary>
    public sealed class SliderRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("slider-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(go.transform, worldPositionStays: false);
            track.AddComponent<Image>().raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, worldPositionStays: false);
            fill.AddComponent<Image>().raycastTarget = false;

            var thumb = new GameObject("Thumb", typeof(RectTransform));
            thumb.transform.SetParent(go.transform, worldPositionStays: false);
            thumb.AddComponent<Image>();

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, worldPositionStays: false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty;
            tmp.fontSize = 14f;
            tmp.raycastTarget = false;

            return go;
        }
    }
}
