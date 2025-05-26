using System.Collections.Generic;
using UnityEngine;

public class ResidentialZoningSelectorButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController;
    public List<BuildingSelectorMenuManager.ItemType> residentialZoningItems;
    public UIManager uiManager;

    public void OnResidentialZoningButtonClick()
    {
        uiManager.RestoreMouseCursor();
        popupController.ShowPopup(residentialZoningItems, OnResidentialZoningTypeSelected, "Residential");

        OnResidentialZoningTypeSelected(residentialZoningItems[0]);
    }

    private void OnResidentialZoningTypeSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Light":
                uiManager.OnLightResidentialButtonClicked();
                break;
            case "Medium":
                uiManager.OnMediumResidentialButtonClicked();
                break;
            case "Heavy":
                uiManager.OnHeavyResidentialButtonClicked();
                break;
            default:
                break;
        }
    }
}