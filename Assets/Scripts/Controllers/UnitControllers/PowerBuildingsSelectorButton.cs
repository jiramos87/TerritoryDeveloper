using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerBuildingsSelectButton : MonoBehaviour
{
    public BuildingSelectorMenuController popupController;
    public List<BuildingSelectorMenuManager.ItemType> powerBuildingItems;
    public UIManager uiManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"No Button component found on {gameObject.name}");
        }
    }

    public void OnPowerBuildingsButtonClick()
    {
        Debug.Log("Power Buildings button clicked");
        uiManager.RestoreMouseCursor();

        popupController.ShowPopup(powerBuildingItems, OnPowerBuildingSelected, "Power");

        if (powerBuildingItems.Count > 0)
        {
            OnPowerBuildingSelected(powerBuildingItems[0]);
        }
    }

    private void OnPowerBuildingSelected(BuildingSelectorMenuManager.ItemType selectedItem)
    {
        switch (selectedItem.name)
        {
            case "Nuclear plant":
                uiManager.OnNuclearPowerPlantButtonClicked();
                break;
            default:
                Debug.LogWarning($"Unknown power building selected: {selectedItem.name}");
                break;
        }
    }

    /// <summary>
    /// Resets this button to its default visual state
    /// </summary>
    public void DeselectButton()
    {
        if (button != null)
        {
            // Force deselection and return to normal state
            button.OnDeselect(null);
            button.targetGraphic.CrossFadeColor(button.colors.normalColor, 0f, true, true);
            Debug.Log($"Deselected button: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Button component not found on {gameObject.name}");
        }
    }
}
