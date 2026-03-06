using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI
{
/// <summary>
/// UI controller for the cell details popup. Shows detailed information about a selected grid cell.
/// </summary>
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
}
