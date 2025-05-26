using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPopupController : MonoBehaviour
{
    public GameObject statsPanel; // Reference to the stats panel
    public GameObject taxPanel; // Reference to the tax panel

    public void ShowStats()
    {
        statsPanel.SetActive(true); // Show the stats panel
        taxPanel.SetActive(false); // Hide the tax panel
    }

    public void ToggleStats()
    {
        statsPanel.SetActive(!statsPanel.activeSelf); // Toggle the stats panel visibility
        taxPanel.SetActive(false); // Hide the tax panel
    }

    public void ToggleTaxes()
    {
        statsPanel.SetActive(false); // Hide the stats panel
        taxPanel.SetActive(!taxPanel.activeSelf); // Toggle the tax panel visibility
    }
}