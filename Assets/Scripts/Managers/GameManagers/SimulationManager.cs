using UnityEngine;
using Territory.Economy;

namespace Territory.Simulation
{
/// <summary>
/// Central orchestrator for automatic city growth simulation.
/// Called by TimeManager each day; runs roads -> zoning -> resources when simulateGrowth is true. Proposal flow disabled.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public CityStats cityStats;
    public GrowthBudgetManager growthBudgetManager;
    public AutoRoadBuilder autoRoadBuilder;
    public AutoZoningManager autoZoningManager;
    public AutoResourcePlanner autoResourcePlanner;
    public UrbanizationProposalManager urbanizationProposalManager;

    void Start()
    {
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (growthBudgetManager == null)
            growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (autoRoadBuilder == null)
            autoRoadBuilder = FindObjectOfType<AutoRoadBuilder>();
        if (autoZoningManager == null)
            autoZoningManager = FindObjectOfType<AutoZoningManager>();
        if (autoResourcePlanner == null)
            autoResourcePlanner = FindObjectOfType<AutoResourcePlanner>();
        if (urbanizationProposalManager == null)
            urbanizationProposalManager = FindObjectOfType<UrbanizationProposalManager>();

        if (autoRoadBuilder != null && autoZoningManager != null && autoRoadBuilder.autoZoningManager == null)
            autoRoadBuilder.autoZoningManager = autoZoningManager;
    }

    string SimDateStr()
    {
        return cityStats != null ? cityStats.currentDate.ToString("yyyy-MM-dd") : "?";
    }

    /// <summary>
    /// Called by TimeManager once per in-game day. Runs all auto-growth systems in order when simulateGrowth is true.
    /// </summary>
    public void ProcessSimulationTick()
    {
        string d = SimDateStr();
        if (cityStats == null)
        {
            return;
        }
        if (!cityStats.simulateGrowth)
            return;

        if (growthBudgetManager != null)
            growthBudgetManager.EnsureBudgetValid();

        if (autoRoadBuilder != null)
            autoRoadBuilder.ProcessTick();

        if (autoZoningManager != null)
            autoZoningManager.ProcessTick();

        if (autoResourcePlanner != null)
            autoResourcePlanner.ProcessTick();
    }

    /// <summary>
    /// Called by TimeManager on day 1 of each month. Resets per-cycle budget spending.
    /// </summary>
    public void ProcessMonthlyReset()
    {
        if (growthBudgetManager != null)
            growthBudgetManager.ResetMonthlyCycle();
    }
}
}
