using UnityEngine;
using Territory.Economy;
using Territory.Simulation.Signals;

namespace Territory.Simulation
{
/// <summary>
/// Central orchestrator for auto city growth sim.
/// Called by TimeManager each day. Runs roads → zoning → resources when simulateGrowth true. Proposal flow disabled.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [SerializeField] private MetricsRecorder _metricsRecorder;
    [SerializeField] private CityStatsFacade _facade;

    public CityStats cityStats;
    public GrowthBudgetManager growthBudgetManager;
    public AutoRoadBuilder autoRoadBuilder;
    public AutoZoningManager autoZoningManager;
    public AutoResourcePlanner autoResourcePlanner;
    public UrbanizationProposalManager urbanizationProposalManager;
    public UrbanCentroidService urbanCentroidService;
    [SerializeField] private SignalTickScheduler signalTickScheduler;

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
        if (signalTickScheduler == null)
            signalTickScheduler = FindObjectOfType<SignalTickScheduler>();

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

    /// <summary>Called by TimeManager once per in-game day. Runs all auto-growth systems in order when simulateGrowth true.</summary>
    public void ProcessSimulationTick()
    {
        bool statsTickBracket = false;
        try
        {
            if (cityStats == null)
                return;
            if (!cityStats.simulateGrowth)
                return;

            statsTickBracket = true;
            _facade?.BeginTick();

            if (growthBudgetManager != null)
                growthBudgetManager.EnsureBudgetValid();

            if (urbanCentroidService != null)
                urbanCentroidService.RecalculateFromGrid();

            if (signalTickScheduler != null)
                signalTickScheduler.Tick(1f);

            if (autoRoadBuilder != null)
                autoRoadBuilder.ProcessTick();

            if (autoZoningManager != null)
                autoZoningManager.ProcessTick();

            if (autoResourcePlanner != null)
                autoResourcePlanner.ProcessTick();
        }
        finally
        {
            if (statsTickBracket)
                _facade?.EndTick();
            if (_metricsRecorder != null && cityStats != null)
                _metricsRecorder.RecordAfterSimulationTick();
        }
    }

    /// <summary>Called by TimeManager day 1 of each month. Reset per-cycle budget spending.</summary>
    public void ProcessMonthlyReset()
    {
        if (growthBudgetManager != null)
            growthBudgetManager.ResetMonthlyCycle();
    }
}
}
