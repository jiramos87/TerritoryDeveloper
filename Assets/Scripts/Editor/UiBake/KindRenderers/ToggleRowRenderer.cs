using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Renders a toggle-row widget (checkmark + label) with real Toggle component. TECH-27542.</summary>
    public sealed class ToggleRowRenderer : IKindRenderer
    {
        public GameObject Render(string paramsJson, Transform parent)
        {
            var go = new GameObject("toggle-row", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(go.transform, worldPositionStays: false);
            var checkImage = check.AddComponent<Image>();
            checkImage.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, worldPositionStays: false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty;
            tmp.fontSize = 14f;
            tmp.raycastTarget = false;

            // Wire real Toggle component (C3 fix — was Image stub).
            var toggle = go.AddComponent<Toggle>();
            toggle.graphic = checkImage;
            toggle.targetGraphic = checkImage;

            return go;
        }
    }
}
