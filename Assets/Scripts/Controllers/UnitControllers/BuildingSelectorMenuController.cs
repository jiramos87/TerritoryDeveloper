using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSelectorMenuController : MonoBehaviour
{
    public BuildingSelectorMenuManager menuManager;
    public CursorManager cursorManager;
    public GameObject popupPanel;

    // Keep track of all selector buttons that can be pressed
    private List<Button> allSelectorButtons = new List<Button>();

    public void Start()
    {
        popupPanel.SetActive(false);
        CacheAllSelectorButtons();
    }

    /// <summary>
    /// Cache all selector buttons in the scene for efficient deselection
    /// </summary>
    private void CacheAllSelectorButtons()
    {
        allSelectorButtons.Clear();
        
        // Find all PowerBuildingsSelectButton components and get their buttons
        PowerBuildingsSelectButton[] powerButtons = FindObjectsOfType<PowerBuildingsSelectButton>();
        foreach (var powerButton in powerButtons)
        {
            Button button = powerButton.GetComponent<Button>();
            if (button != null)
            {
                allSelectorButtons.Add(button);
            }
        }

        // Add any other selector button types here as you create them
        // CommercialBuildingsSelectButton[] commercialButtons = FindObjectsOfType<CommercialBuildingsSelectButton>();
        // etc.
    }

    public void ShowPopup(List<BuildingSelectorMenuManager.ItemType> items, System.Action<BuildingSelectorMenuManager.ItemType> onItemSelected, string type)
    {
        OpenPopup();
        menuManager.PopulateItems(items, onItemSelected, type);
        cursorManager.SetDefaultCursor();
    }

    private void TogglePopup(string type)
    {
        if (popupPanel.activeSelf)
        {
            if (menuManager.GetPopupType() == type)
            {
                ClosePopup();
            }
            else
            {
                OpenPopup();
            }
        }
        else
        {
            OpenPopup();
        }
    }

    private void OpenPopup()
    {
        popupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }

    public bool IsPopupActive()
    {
        return popupPanel.activeSelf;
    }

    /// <summary>
    /// Deselects all selector buttons and resets their visual state to target graphic
    /// </summary>
    public void DeselectAndUnpressAllButtons()
    {

        // Clear any selected object in the EventSystem
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Reset all cached selector buttons
        foreach (Button button in allSelectorButtons)
        {
            if (button != null)
            {
                ResetButtonVisualState(button);
            }
        }

        // Also reset any buttons in the popup panel
        var popupButtons = popupPanel.GetComponentsInChildren<Button>();
        foreach (var button in popupButtons)
        {
            ResetButtonVisualState(button);
        }
    }

    /// <summary>
    /// Resets a button's visual state to the target graphic
    /// </summary>
    private void ResetButtonVisualState(Button button)
    {
        if (button == null) return;

        // Force the button to transition to Normal state
        button.OnDeselect(null);
        
        // Ensure the button is interactable (in case it was disabled)
        button.interactable = true;
        
        // Force immediate state transition to Normal
        button.targetGraphic.CrossFadeColor(button.colors.normalColor, 0f, true, true);
    }

    /// <summary>
    /// Call this when new selector buttons are added to update the cache
    /// </summary>
    public void RefreshSelectorButtonCache()
    {
        CacheAllSelectorButtons();
    }
}
