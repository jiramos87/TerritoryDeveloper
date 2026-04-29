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

    private void ClosePopup(PopupType type)
    {
        switch (type)
        {
            case PopupType.LoadGame:
                CloseLoadGameMenu();
                break;
            case PopupType.Details:
                if (detailsPopupController != null)
                    detailsPopupController.CloseDetails();
                break;
            case PopupType.BuildingSelector:
                if (buildingSelectorMenuController != null)
                {
                    buildingSelectorMenuController.ClosePopup();
                    buildingSelectorMenuController.DeselectAndUnpressAllButtons();
                }
                break;
            case PopupType.StatsPanel:
                if (dataPopupController != null)
                    dataPopupController.CloseStats();
                break;
            case PopupType.TaxPanel:
                if (dataPopupController != null)
                    dataPopupController.CloseTaxes();
                break;
            case PopupType.SubTypePicker:
                if (subTypePickerModal != null)
                    subTypePickerModal.Hide(cancelled: true);
                break;
            case PopupType.BudgetPanel:
                if (budgetPanel != null)
                    budgetPanel.Hide();
                break;
            case PopupType.BondIssuance:
                if (bondIssuanceModal != null)
                    bondIssuanceModal.Hide();
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
        if (detailsPopupController != null)
            detailsPopupController.CloseDetails();
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
