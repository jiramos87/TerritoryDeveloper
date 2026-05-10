using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Territory.Economy;
using Territory.Terrain;
using Territory.Timing;
using Territory.Persistence;
using Territory.Forests;
using Territory.Buildings;
using Territory.Utilities;

namespace Territory.UI
{
// Pop-up stack and Esc close coordination (partial of UIManager).
public partial class UIManager
{
    #region Popup Management
    /// <summary>Call when pop-up opens → Esc closes last-opened first.</summary>
    public void RegisterPopupOpened(PopupType type)
    {
        popupStack.Push(type);
    }

    /// <summary>
    /// TECH-14102 / Stage 8 D9: register tool-selected escape frame. Idempotent — only pushes when frame absent from stack.
    /// Caller responsible for push at tool-button click site (toolbar handlers).
    /// </summary>
    public void RegisterToolSelected()
    {
        if (StackContainsFrame(PopupType.ToolSelected)) return;
        popupStack.Push(PopupType.ToolSelected);
    }

    /// <summary>TECH-14102: removes a specific frame from anywhere in the popup stack (rebuild). Used when tool deselects via path other than Esc (right-click cancel, ClearCurrentTool).</summary>
    public void RemoveFrameFromStack(PopupType type)
    {
        if (!StackContainsFrame(type)) return;
        var preserved = new System.Collections.Generic.List<PopupType>();
        while (popupStack.Count > 0)
        {
            var frame = popupStack.Pop();
            if (frame != type) preserved.Add(frame);
        }
        // Re-push in reverse to preserve original order.
        for (int i = preserved.Count - 1; i >= 0; i--)
        {
            popupStack.Push(preserved[i]);
        }
    }

    /// <summary>TECH-14102: read-only stack contents accessor for EditMode tests + push idempotence checks.</summary>
    public bool StackContainsFrame(PopupType type)
    {
        foreach (var f in popupStack) if (f == type) return true;
        return false;
    }

    /// <summary>TECH-14102: read-only stack depth accessor for EditMode tests.</summary>
    public int PopupStackCount => popupStack.Count;

    /// <summary>TECH-14102: read-only top-of-stack accessor for EditMode tests. Returns null when empty.</summary>
    public PopupType? PopupStackPeek() => popupStack.Count > 0 ? popupStack.Peek() : (PopupType?)null;

    /// <summary>Stage 12 trigger-side helper: activate a Stage 8 modal root + register on stack. Mirror of <see cref="ClosePopup"/>. Idempotent (no-op if root null or already active).</summary>
    public void OpenPopup(PopupType type)
    {
        switch (type)
        {
            case PopupType.InfoPanel:
                if (infoPanelRoot != null && !infoPanelRoot.activeSelf)
                {
                    infoPanelRoot.SetActive(true);
                    RegisterPopupOpened(type);
                }
                break;
            case PopupType.PauseMenu:
                if (pauseMenuRoot != null && !pauseMenuRoot.activeSelf)
                {
                    // Wave B4 (TECH-27095): enforce exclusive group via ModalCoordinator.
                    if (_modalCoordinator != null) _modalCoordinator.TryOpen("pause-menu");
                    pauseMenuRoot.SetActive(true);
                    RegisterPopupOpened(type);
                }
                break;
            case PopupType.SettingsScreen:
                if (settingsScreenRoot != null && !settingsScreenRoot.activeSelf)
                {
                    settingsScreenRoot.SetActive(true);
                    RegisterPopupOpened(type);
                }
                break;
            case PopupType.SaveLoadScreen:
                if (saveLoadScreenRoot != null && !saveLoadScreenRoot.activeSelf)
                {
                    saveLoadScreenRoot.SetActive(true);
                    RegisterPopupOpened(type);
                }
                break;
            case PopupType.NewGameScreen:
                if (newGameScreenRoot != null && !newGameScreenRoot.activeSelf)
                {
                    newGameScreenRoot.SetActive(true);
                    RegisterPopupOpened(type);
                }
                break;
        }
    }

    /// <summary>Stage 12: public trigger-side close — pops from stack if currently top, then deactivates root. Used by both Esc handler and external triggers (PauseMenu Resume).</summary>
    public void ClosePopup(PopupType type)
    {
        if (popupStack.Count > 0 && popupStack.Peek() == type)
            popupStack.Pop();
        switch (type)
        {
            case PopupType.LoadGame:
                CloseLoadGameMenu();
                break;
            case PopupType.BuildingSelector:
                if (buildingSelectorMenuController != null)
                {
                    buildingSelectorMenuController.ClosePopup();
                    buildingSelectorMenuController.DeselectAndUnpressAllButtons();
                }
                break;
            case PopupType.TaxPanel:
                if (dataPopupController != null)
                    dataPopupController.CloseTaxes();
                break;
            case PopupType.SubTypePicker:
                if (SubtypePickerController != null)
                    SubtypePickerController.Hide(cancelled: true);
                break;
            case PopupType.BudgetPanel:
                if (budgetPanel != null)
                    budgetPanel.Hide();
                break;
            case PopupType.InfoPanel:
                if (infoPanelRoot != null) infoPanelRoot.SetActive(false);
                break;
            case PopupType.PauseMenu:
                if (pauseMenuRoot != null)
                {
                    pauseMenuRoot.SetActive(false);
                    // Wave B4 (TECH-27095): deregister from ModalCoordinator.
                    if (_modalCoordinator != null) _modalCoordinator.Close("pause-menu");
                }
                break;
            case PopupType.SettingsScreen:
                if (settingsScreenRoot != null) settingsScreenRoot.SetActive(false);
                break;
            case PopupType.SaveLoadScreen:
                if (saveLoadScreenRoot != null) saveLoadScreenRoot.SetActive(false);
                break;
            case PopupType.NewGameScreen:
                if (newGameScreenRoot != null) newGameScreenRoot.SetActive(false);
                break;
            case PopupType.ToolSelected:
                // TECH-14102 / Stage 8 D9: Esc on tool-selected frame deselects tool (no popup root).
                // Frame already popped at ClosePopup head; ClearCurrentTool's RemoveFrameFromStack is idempotent (no-op).
                ClearCurrentTool();
                break;
        }
    }

    private void CloseAllPopups()
    {
        CloseLoadGameMenu();
        if (buildingSelectorMenuController != null)
        {
            buildingSelectorMenuController.ClosePopup();
            buildingSelectorMenuController.DeselectAndUnpressAllButtons();
        }
        var dataPopup = dataPopupController != null ? dataPopupController : FindObjectOfType<DataPopupController>();
        if (dataPopup != null)
            dataPopup.CloseAll();
    }
    #endregion
}
}
