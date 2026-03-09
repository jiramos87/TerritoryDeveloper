using UnityEngine;

namespace Territory.UI
{
/// <summary>
/// UI button that toggles the mini-map visibility via MiniMapController.
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
