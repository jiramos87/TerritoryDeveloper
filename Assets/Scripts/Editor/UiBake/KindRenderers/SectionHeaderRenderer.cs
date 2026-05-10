using TMPro;
using UnityEngine;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Renders a section-header widget (text label only).</summary>
    public sealed class SectionHeaderRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("section-header", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, worldPositionStays: false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty;
            tmp.fontSize = 12f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.raycastTarget = false;

            return go;
        }
    }
}
