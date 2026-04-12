using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Timing;

namespace Territory.UI
{
/// <summary>
/// UI controller for game speed buttons (pause, 1x, 2x, 3x). Forwards speed changes → <see cref="TimeManager"/>.
/// </summary>
public class SpeedButtonsController : MonoBehaviour
{
    [Header("References")]
    public TimeManager timeManager;

    [Header("Speed Buttons")]
    public Button pauseButton;
    public Button speed1Button;
    public Button speed2Button;
    public Button speed3Button;
    public Button speed4Button;

    private List<Button> allSpeedButtons = new List<Button>();
    private int currentSelectedSpeed = 0;
    private Button currentlySelectedButton = null;

    private void Start()
    {
        InitializeButtons();

        // Set initial button state to match TimeManager's starting speed
        UpdateButtonStates(1); // TimeManager starts at index 1 (speed 1)
    }

    /// <summary>
    /// Init button list → efficient state mgmt.
    /// </summary>
    private void InitializeButtons()
    {
        allSpeedButtons.Clear();

        if (pauseButton != null) allSpeedButtons.Add(pauseButton);
        if (speed1Button != null) allSpeedButtons.Add(speed1Button);
        if (speed2Button != null) allSpeedButtons.Add(speed2Button);
        if (speed3Button != null) allSpeedButtons.Add(speed3Button);
        if (speed4Button != null) allSpeedButtons.Add(speed4Button);
    }

    public void OnPauseButtonClick()
    {
        timeManager.SetTimeSpeedIndex(0);
        UpdateButtonStates(0);
    }

    public void OnSpeed1ButtonClick()
    {
        timeManager.SetTimeSpeedIndex(1);
        UpdateButtonStates(1);
    }

    public void OnSpeed2ButtonClick()
    {
        timeManager.SetTimeSpeedIndex(2);
        UpdateButtonStates(2);
    }

    public void OnSpeed3ButtonClick()
    {
        timeManager.SetTimeSpeedIndex(3);
        UpdateButtonStates(3);
    }

    public void OnSpeed4ButtonClick()
    {
        timeManager.SetTimeSpeedIndex(4);
        UpdateButtonStates(4);
    }

    /// <summary>
    /// Update button visuals → reflect current speed. Called on keyboard or button change.
    /// </summary>
    public void UpdateButtonStates(int selectedSpeedIndex)
    {
        currentSelectedSpeed = selectedSpeedIndex;

        // Deselect all buttons first
        foreach (Button button in allSpeedButtons)
        {
            if (button != null)
            {
                SetButtonToNormalState(button);
            }
        }

        // Select the appropriate button
        Button selectedButton = GetButtonByIndex(selectedSpeedIndex);
        if (selectedButton != null)
        {
            SetButtonToSelectedState(selectedButton);
            currentlySelectedButton = selectedButton;
        }
    }

    /// <summary>Button by speed index.</summary>
    private Button GetButtonByIndex(int speedIndex)
    {
        switch (speedIndex)
        {
            case 0: return pauseButton;
            case 1: return speed1Button;
            case 2: return speed2Button;
            case 3: return speed3Button;
            case 4: return speed4Button;
            default:
                return null;
        }
    }

    /// <summary>Set button → normal visual (deselect).</summary>
    private void SetButtonToNormalState(Button button)
    {
        if (button == null) return;

        // Properly deselect the button using Unity's EventSystem
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Force the button to normal state
        button.OnDeselect(null);
    }

    /// <summary>Set button → selected visual (selected/pressed sprite).</summary>
    private void SetButtonToSelectedState(Button button)
    {
        if (button == null) return;

        // Use Unity's EventSystem to properly select the button
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        // Trigger the selected state
        button.OnSelect(null);
    }

    /// <summary>Called by <see cref="TimeManager"/> on keyboard speed change.</summary>
    public void OnSpeedChangedExternally(int newSpeedIndex)
    {
        if (newSpeedIndex != currentSelectedSpeed)
        {
            UpdateButtonStates(newSpeedIndex);
        }
    }

    /// <summary>Current selected speed index.</summary>
    public int GetCurrentSelectedSpeed()
    {
        return currentSelectedSpeed;
    }

    /// <summary>Force refresh button states. Debug/manual use.</summary>
    public void RefreshButtonStates()
    {
        UpdateButtonStates(currentSelectedSpeed);
    }
}
}
