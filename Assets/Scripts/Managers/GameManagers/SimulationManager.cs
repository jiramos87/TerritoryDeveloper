using UnityEngine;

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

        bool simOn = cityStats != null && cityStats.simulateGrowth;
        Debug.Log($"[SimulationManager] Start: cityStats={cityStats != null} (simulateGrowth={simOn}), budget={growthBudgetManager != null}, roads={autoRoadBuilder != null}, zoning={autoZoningManager != null}, resources={autoResourcePlanner != null}");
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
            Debug.Log($"[Sim {d}] [SimulationManager] ProcessSimulationTick: cityStats is null, skipping.");
            return;
        }
        if (!cityStats.simulateGrowth)
            return; // No log every day when off to avoid spam

        Debug.Log($"[Sim {d}] [SimulationManager] ProcessSimulationTick: simulateGrowth ON, running subsystems (roads -> zoning -> resources).");

        if (growthBudgetManager != null)
            growthBudgetManager.EnsureBudgetValid();

        if (autoRoadBuilder != null)
            autoRoadBuilder.ProcessTick();
        else
            Debug.LogWarning($"[Sim {d}] [SimulationManager] autoRoadBuilder is null, roads skipped.");

        if (autoZoningManager != null)
            autoZoningManager.ProcessTick();
        else
            Debug.LogWarning($"[Sim {d}] [SimulationManager] autoZoningManager is null, zoning skipped.");

        if (autoResourcePlanner != null)
            autoResourcePlanner.ProcessTick();

        Debug.Log($"[Sim {d}] [SimulationManager] ProcessSimulationTick done.");
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
