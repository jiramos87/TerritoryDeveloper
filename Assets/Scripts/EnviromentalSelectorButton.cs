using System.Collections.Generic;
using UnityEngine;

public class EnvironmentalSelectorButton : MonoBehaviour
{
    [Header("UI References")]
    public BuildingSelectorMenuController popupController; // Reference to the PopupController
    public UIManager uiManager; // Reference to the UIManager
    
    [Header("Environmental Items")]
    public List<BuildingSelectorMenuManager.ItemType> environmentalItems; // List of environmental items

    public void OnEnvironmentalButtonClick()
    {
        uiManager.RestoreMouseCursor();
        popupController.ShowPopup(environmentalItems, OnEnvironmentalTypeSelected, "Environmental");
        
        // Default to first item (trees)
        if (environmentalItems.Count > 0)
        {
            OnEnvironmentalTypeSelected(environmentalItems[0]);
        }
    }

    private void OnEnvironmentalTypeSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Sparse forest":
            case "Sparse trees":
                uiManager.OnSparseForestButtonClicked();
                break;
                
            case "Medium forest":
            case "Medium trees":
                uiManager.OnMediumForestButtonClicked();
                break;
                
            case "Dense forest":
            case "Dense trees":
            case "Trees":
                uiManager.OnDenseForestButtonClicked();
                break;
                    
            case "Water":
                uiManager.OnPlaceWaterButtonClicked();
                break;
                    
            case "Grass":
                uiManager.OnGrassButtonClicked();
                break;
                    
            default:
                Debug.LogWarning($"Unknown environmental type selected: {selectedItem.name}");
                break;
        }
    }

    void Start()
    {
        if (environmentalItems == null || environmentalItems.Count == 0)
        {
            InitializeDefaultEnvironmentalItems();
        }
    }

    private void InitializeDefaultEnvironmentalItems()
    {
        environmentalItems = new List<BuildingSelectorMenuManager.ItemType>();

        // Sparse Forest item
        BuildingSelectorMenuManager.ItemType sparseForestItem = new BuildingSelectorMenuManager.ItemType
        {
            name = "Sparse forest",
            price = 0, // Free to place, but requires water
            icon = null // Assign sparse forest icon in inspector
        };
        environmentalItems.Add(sparseForestItem);

        // Medium Forest item
        BuildingSelectorMenuManager.ItemType mediumForestItem = new BuildingSelectorMenuManager.ItemType
        {
            name = "Medium forest",
            price = 0, // Free to place, but requires water
            icon = null // Assign medium forest icon in inspector
        };
        environmentalItems.Add(mediumForestItem);

        // Dense Forest item
        BuildingSelectorMenuManager.ItemType denseForestItem = new BuildingSelectorMenuManager.ItemType
        {
            name = "Dense forest",
            price = 0, // Free to place, but requires water
            icon = null // Assign dense forest icon in inspector
        };
        environmentalItems.Add(denseForestItem);

        // Water item
        BuildingSelectorMenuManager.ItemType waterItem = new BuildingSelectorMenuManager.ItemType
        {
            name = "Water",
            price = 100, // Cost for water placement
            icon = null // Assign water icon in inspector
        };
        environmentalItems.Add(waterItem);

        // Grass item (for restoration)
        BuildingSelectorMenuManager.ItemType grassItem = new BuildingSelectorMenuManager.ItemType
        {
            name = "Grass",
            price = 0, // Free grass placement
            icon = null // Assign grass icon in inspector
        };
        environmentalItems.Add(grassItem);
    }
}
