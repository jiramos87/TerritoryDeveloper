using System.Collections.Generic;
using UnityEngine;

public class WaterBuildingSelectorButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController;
    public List<BuildingSelectorMenuManager.ItemType> waterBuildingItems;
    public UIManager uiManager;
    
    void Start()
    {
        // Ensure we have at least one water building item
        if (waterBuildingItems == null || waterBuildingItems.Count == 0)
        {
            Debug.LogError("No water building items assigned to WaterBuildingSelectorButton!");
        }
        
        // Validate references
        if (popupController == null)
        {
            Debug.LogError("PopupController reference is missing in WaterBuildingSelectorButton!");
        }
        
        if (uiManager == null)
        {
            Debug.LogError("UIManager reference is missing in WaterBuildingSelectorButton!");
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Debug.Log("Found UIManager in scene");
            }
        }
    }
    
    public void OnWaterBuildingsButtonClick()
    {  
        try {
            if (uiManager == null)
            {
                return;
            }
            
            uiManager.RestoreMouseCursor();
            
            if (popupController == null)
            {
                return;
            }
            
            if (waterBuildingItems == null || waterBuildingItems.Count == 0)
            {
                return;
            }
            
            popupController.ShowPopup(waterBuildingItems, OnWaterBuildingSelected, "Water");

            OnWaterBuildingSelected(waterBuildingItems[0]);
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error in OnWaterBuildingsButtonClick: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void OnWaterBuildingSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        try {
            switch (selectedItem.name)
            {
                case "Medium Water Pump":
                    if (uiManager != null)
                    {
                        uiManager.OnMediumWaterPumpPlantButtonClicked();
                    }
                    else
                    {
                        Debug.LogError("Cannot select Water Pump: UIManager is null!");
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown water building selected: {selectedItem.name}");
                    break;
            }
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error in OnWaterBuildingSelected: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
