using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Renders a dropdown-row widget (label + value display).</summary>
    public sealed class DropdownRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("dropdown-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, worldPositionStays: false);
            var labelTmp = label.AddComponent<TextMeshProUGUI>();
            labelTmp.text = string.Empty;
            labelTmp.fontSize = 14f;
            labelTmp.raycastTarget = false;

            var value = new GameObject("Value", typeof(RectTransform));
            value.transform.SetParent(go.transform, worldPositionStays: false);
            var valueTmp = value.AddComponent<TextMeshProUGUI>();
            valueTmp.text = string.Empty;
            valueTmp.fontSize = 14f;
            valueTmp.raycastTarget = false;

            var arrow = new GameObject("Arrow", typeof(RectTransform));
            arrow.transform.SetParent(go.transform, worldPositionStays: false);
            arrow.AddComponent<Image>().raycastTarget = false;

            return go;
        }
    }
}
