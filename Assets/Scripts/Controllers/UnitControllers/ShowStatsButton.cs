using System.Collections.Generic;
using UnityEngine;

public class ShowStatsButton : MonoBehaviour
{
  public DataPopupController popupController; // Reference to the PopupController
  public UIManager uiManager; // Reference to the UIManager

  public void OnShowStatsButtonClick()
  {
    uiManager.UpdateUI();
    popupController.ToggleStats();  
  }
}