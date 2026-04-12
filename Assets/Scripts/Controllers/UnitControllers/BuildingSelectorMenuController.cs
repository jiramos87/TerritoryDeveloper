using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Utilities;

namespace Territory.UI
{
/// <summary>
/// UI controller for building selector popup. Manages item display, selection callbacks,
/// coords with <see cref="CursorManager"/> (placement preview), applies faster <see cref="ScrollRect"/> wheel sensitivity for popup list.
/// </summary>
public class BuildingSelectorMenuController : MonoBehaviour
{
    public BuildingSelectorMenuManager menuManager;
    public CursorManager cursorManager;
    public GameObject popupPanel;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private float popupFadeSeconds = 0.12f;
    [Tooltip("Mouse wheel speed inside building selector ScrollRect (higher = faster).")]
    [SerializeField] private float buildingMenuScrollSensitivity = 3.5f;

    // Keep track of all selector buttons that can be pressed
    private List<Button> allSelectorButtons = new List<Button>();
    private Coroutine popupFadeRoutine;

    private float FadeDuration => uiManager != null ? uiManager.PopupFadeDurationSeconds : popupFadeSeconds;

    public void Start()
    {
        popupPanel.SetActive(false);
        CacheAllSelectorButtons();
        ApplyBuildingMenuScrollSpeed();
    }

    /// <summary>
    /// Bump <see cref="ScrollRect.scrollSensitivity"/> for lists inside building selector popup.
    /// </summary>
    private void ApplyBuildingMenuScrollSpeed()
    {
        if (popupPanel == null)
            return;
        ScrollRect[] scrolls = popupPanel.GetComponentsInChildren<ScrollRect>(true);
        foreach (ScrollRect sr in scrolls)
        {
            if (sr != null)
                sr.scrollSensitivity = buildingMenuScrollSensitivity;
        }
    }

    /// <summary>
    /// Cache all scene selector buttons → efficient deselection.
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
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
            uiManager.RegisterPopupOpened(PopupType.BuildingSelector);
        if (popupFadeRoutine != null)
            StopCoroutine(popupFadeRoutine);
        popupPanel.SetActive(true);
        popupFadeRoutine = StartCoroutine(OpenPopupFadeRoutine());
    }

    private IEnumerator OpenPopupFadeRoutine()
    {
        CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(popupPanel);
        cg.blocksRaycasts = true;
        cg.interactable = false;
        cg.alpha = 0f;
        yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, FadeDuration);
        cg.interactable = true;
        popupFadeRoutine = null;
    }

    public void ClosePopup()
    {
        if (popupFadeRoutine != null)
            StopCoroutine(popupFadeRoutine);
        if (popupPanel == null || !popupPanel.activeSelf)
            return;
        popupFadeRoutine = StartCoroutine(ClosePopupFadeRoutine());
    }

    private IEnumerator ClosePopupFadeRoutine()
    {
        CanvasGroup cg = popupPanel.GetComponent<CanvasGroup>();
        if (cg != null)
            yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, FadeDuration);
        popupPanel.SetActive(false);
        popupFadeRoutine = null;
    }

    public bool IsPopupActive()
    {
        return popupPanel.activeSelf;
    }

    /// <summary>
    /// Deselect all selector buttons + reset visual state → target graphic.
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
    /// Reset button visual state → target graphic.
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
    /// Call when new selector buttons added → refresh cache.
    /// </summary>
    public void RefreshSelectorButtonCache()
    {
        CacheAllSelectorButtons();
    }
}
}
