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
                if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
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
