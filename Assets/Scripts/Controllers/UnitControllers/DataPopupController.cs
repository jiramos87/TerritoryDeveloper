using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPopupController : MonoBehaviour
{
    public GameObject statsPanel; // Reference to the stats panel
    public GameObject taxPanel; // Reference to the tax panel
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CityStats cityStats;
    [Tooltip("Growth budget sliders container; shown when tax panel is open and simulate growth is on.")]
    public GameObject growthBudgetSlidersContainer;

    public void ShowStats()
    {
        statsPanel.SetActive(true);
        taxPanel.SetActive(false);
        RegisterWithUIManager(PopupType.StatsPanel);
    }

    public void ToggleStats()
    {
        statsPanel.SetActive(!statsPanel.activeSelf);
        taxPanel.SetActive(false);
        if (statsPanel.activeSelf)
            RegisterWithUIManager(PopupType.StatsPanel);
    }

    public void ToggleTaxes()
    {
        statsPanel.SetActive(false);
        taxPanel.SetActive(!taxPanel.activeSelf);
        if (taxPanel.activeSelf)
        {
            RegisterWithUIManager(PopupType.TaxPanel);
            if (growthBudgetSlidersContainer != null && cityStats != null)
                growthBudgetSlidersContainer.SetActive(cityStats.simulateGrowth);
        }
    }

    public void CloseAll()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (taxPanel != null) taxPanel.SetActive(false);
    }

    public void CloseStats()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
    }

    public void CloseTaxes()
    {
        if (taxPanel != null) taxPanel.SetActive(false);
    }

    private void RegisterWithUIManager(PopupType type)
    {
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.RegisterPopupOpened(type);
    }
}
