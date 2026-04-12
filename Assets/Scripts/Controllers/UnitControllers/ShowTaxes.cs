using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI
{
/// <summary>
/// UI button → toggles tax mgmt popup via <see cref="DataPopupController"/>.
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
