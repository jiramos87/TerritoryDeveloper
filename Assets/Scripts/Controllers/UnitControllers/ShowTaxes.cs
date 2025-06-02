using System.Collections.Generic;
using UnityEngine;

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
