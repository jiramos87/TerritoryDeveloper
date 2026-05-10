using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Renders a list-row widget (icon + primary label + secondary value).</summary>
    public sealed class ListRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("list-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(go.transform, worldPositionStays: false);
            icon.AddComponent<Image>().raycastTarget = false;

            var primary = new GameObject("PrimaryLabel", typeof(RectTransform));
            primary.transform.SetParent(go.transform, worldPositionStays: false);
            var primaryTmp = primary.AddComponent<TextMeshProUGUI>();
            primaryTmp.text = string.Empty;
            primaryTmp.fontSize = 14f;
            primaryTmp.raycastTarget = false;

            var secondary = new GameObject("SecondaryValue", typeof(RectTransform));
            secondary.transform.SetParent(go.transform, worldPositionStays: false);
            var secondaryTmp = secondary.AddComponent<TextMeshProUGUI>();
            secondaryTmp.text = string.Empty;
            secondaryTmp.fontSize = 12f;
            secondaryTmp.raycastTarget = false;

            return go;
        }
    }
}
