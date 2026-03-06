using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI
{
/// <summary>
/// UI button that toggles the tax management popup via DataPopupController.
/// </summary>
public class ShowTaxesButton : MonoBehaviour
{
    public DataPopupController popupController;
    public UIManager uiManager;

    public void OnShowTaxesButtonClick()
    {
        uiManager.UpdateUI();
        popupController.ToggleTaxes();
    }
}
}
