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
    [SerializeField] private MetricsRecorder _metricsRecorder;

    public CityStats cityStats;
    public GrowthBudgetManager growthBudgetManager;
    public AutoRoadBuilder autoRoadBuilder;
    public AutoZoningManager autoZoningManager;
    public AutoResourcePlanner autoResourcePlanner;
    public UrbanizationProposalManager urbanizationProposalManager;
    public UrbanCentroidService urbanCentroidService;

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
        if (urbanCentroidService == null)
            urbanCentroidService = FindObjectOfType<UrbanCentroidService>();

        if (autoRoadBuilder != null && autoZoningManager != null)
        {
            if (autoRoadBuilder.autoZoningManager == null)
                autoRoadBuilder.autoZoningManager = autoZoningManager;
            if (autoZoningManager.autoRoadBuilder == null)
                autoZoningManager.autoRoadBuilder = autoRoadBuilder;
        }
        if (urbanCentroidService != null)
        {
            if (autoZoningManager != null && autoZoningManager.urbanCentroidService == null)
                autoZoningManager.urbanCentroidService = urbanCentroidService;
            if (autoRoadBuilder != null && autoRoadBuilder.urbanCentroidService == null)
                autoRoadBuilder.urbanCentroidService = urbanCentroidService;
            if (urbanizationProposalManager != null && urbanizationProposalManager.urbanCentroidService == null)
                urbanizationProposalManager.urbanCentroidService = urbanCentroidService;
        }

        if (_metricsRecorder == null)
            _metricsRecorder = FindObjectOfType<MetricsRecorder>();
    }

    /// <summary>
    /// Called by TimeManager once per in-game day. Runs all auto-growth systems in order when simulateGrowth is true.
    /// </summary>
    public void ProcessSimulationTick()
    {
        try
        {
            if (cityStats == null)
                return;
            if (!cityStats.simulateGrowth)
                return;

            if (growthBudgetManager != null)
                growthBudgetManager.EnsureBudgetValid();

            if (urbanCentroidService != null)
                urbanCentroidService.RecalculateFromGrid();

            if (autoRoadBuilder != null)
                autoRoadBuilder.ProcessTick();

            if (autoZoningManager != null)
                autoZoningManager.ProcessTick();

            if (autoResourcePlanner != null)
                autoResourcePlanner.ProcessTick();
        }
        finally
        {
            if (_metricsRecorder != null && cityStats != null)
                _metricsRecorder.RecordAfterSimulationTick();
        }
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
