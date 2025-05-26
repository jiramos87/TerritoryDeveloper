using System.Collections.Generic;
using UnityEngine;

public class RoadsSelectorButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController; // Reference to the PopupController
    public List<BuildingSelectorMenuManager.ItemType> roadItems; // List of road items
    public UIManager uiManager; // Reference to the UIManager

    public void OnRoadsButtonClick()
    {
        uiManager.RestoreMouseCursor();
        popupController.ShowPopup(roadItems, OnRoadTypeSelected, "Roads");

        OnRoadTypeSelected(roadItems[0]);
    }

    private void OnRoadTypeSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Two-way road":
                uiManager.OnTwoWayRoadButtonClicked();
                break;
            default:
                break;
        }
    }
}
