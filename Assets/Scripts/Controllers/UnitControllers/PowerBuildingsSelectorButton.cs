using System.Collections.Generic;
using UnityEngine;

public class PowerBuildingsSelectButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController;
    public List<BuildingSelectorMenuManager.ItemType> powerBuildingItems;
    public UIManager uiManager;
    public void OnPowerBuildingsButtonClick()
    {
        Debug.Log("Power Buildings button clicked");
        uiManager.RestoreMouseCursor();

        popupController.ShowPopup(powerBuildingItems, OnPowerBuildingSelected, "Power");

        OnPowerBuildingSelected(powerBuildingItems[0]);
    }

    private void OnPowerBuildingSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Nuclear plant":
                uiManager.OnNuclearPowerPlantButtonClicked();
                break;
            default:
                break;
        }
    }
}
