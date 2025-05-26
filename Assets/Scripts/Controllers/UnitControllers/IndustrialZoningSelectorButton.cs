using System.Collections.Generic;
using UnityEngine;

public class IndustrialZoningSelectorButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController; // Reference to the PopupController
    public List<BuildingSelectorMenuManager.ItemType> industrialZoningItems; // List of industrial zoning items
    public UIManager uiManager; // Reference to the UIManager

    public void OnIndustrialZoningButtonClick()
    {   
        uiManager.RestoreMouseCursor();
        popupController.ShowPopup(industrialZoningItems, OnIndustrialZoningSelected, "Industrial");

        OnIndustrialZoningSelected(industrialZoningItems[0]);
    }

    private void OnIndustrialZoningSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Light":
                uiManager.OnLightIndustrialButtonClicked();
                break;
            case "Medium":
                uiManager.OnMediumIndustrialButtonClicked();
                break;
            case "Heavy":
                uiManager.OnHeavyIndustrialButtonClicked();
                break;
            default:
                break;
        }
    }
}