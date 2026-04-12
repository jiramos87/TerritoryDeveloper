using UnityEngine;

namespace Territory.UI
{
/// <summary>
/// UI button → toggles mini-map visibility via <see cref="MiniMapController"/>.
/// </summary>
public class ShowMiniMapButton : MonoBehaviour
{
    public MiniMapController miniMapController;

    public void OnShowMiniMapButtonClick()
    {
        if (miniMapController != null)
            miniMapController.SetVisible(!miniMapController.IsVisible);
    }
}
}
