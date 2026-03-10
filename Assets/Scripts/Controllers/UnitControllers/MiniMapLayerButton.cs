using UnityEngine;
using UnityEngine.UI;
using Territory.UI;

namespace Territory.UI
{
/// <summary>
/// UI button that toggles a mini-map layer on or off. Uses color feedback for on/off state.
/// </summary>
public class MiniMapLayerButton : MonoBehaviour
{
    public MiniMapController miniMapController;
    public MiniMapLayer layer;
    public Image buttonImage;
    [Header("Visual feedback")]
    public Color colorWhenOn = new Color(0.3f, 0.7f, 0.3f);
    public Color colorWhenOff = new Color(0.5f, 0.5f, 0.5f);

    void Start()
    {
        if (miniMapController == null)
            miniMapController = FindObjectOfType<MiniMapController>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        RefreshVisual();
    }

    public void OnClick()
    {
        if (miniMapController != null)
        {
            miniMapController.ToggleLayer(layer);
            RefreshVisual();
        }
    }

    void RefreshVisual()
    {
        if (miniMapController == null) return;
        bool isOn = miniMapController.IsLayerActive(layer);
        Color c = isOn ? colorWhenOn : colorWhenOff;
        var target = buttonImage != null ? buttonImage : GetComponent<Image>();
        if (target != null)
            target.color = c;
    }

    void LateUpdate()
    {
        RefreshVisual();
    }
}
}
