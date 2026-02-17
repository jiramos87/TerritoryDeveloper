using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetailsPopupController : MonoBehaviour
{
    public GameObject detailsPanel;
    public Text waterConsumptionText;
    public Text waterOutputText;

    public void ShowDetails()
    {
        detailsPanel.SetActive(true);
    }

    public void CloseDetails()
    {
        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return detailsPanel != null && detailsPanel.activeSelf;
    }
}
