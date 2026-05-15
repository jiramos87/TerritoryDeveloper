// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using Territory.UI;
using Territory.Economy;
using Territory.Core;
using Territory.Zones;
using Territory.Roads;
using Territory.Simulation;
using Territory.Geography;
using Territory.IsoSceneCore;

namespace Territory.Timing
{
/// <summary>
/// Controls game speed, pause state, sim tick timing. Drives sim loop by calling
/// <see cref="SimulationManager"/> + notifying <see cref="EconomyManager"/>, <see cref="ZoneManager"/>,
/// other time-dependent managers.
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("Geography Gate")]
    [SerializeField] private GeographyManager geographyManager;

    [Header("UI References")]
    public UIManager uiManager;
    public SpeedButtonsController speedButtonsController; // Add this field

    public CityStats cityStats;
    public EconomyManager economyManager;
    public GridManager gridManager;
    public AnimatorManager animatorManager;
    public ZoneManager zoneManager;
    public InterstateManager interstateManager;
    public SimulationManager simulationManager;
    // Wave B2 (TECH-27085) — stats history recorder.
    [SerializeField] private Territory.Simulation.StatsHistoryRecorder statsHistoryRecorder;
    public float[] timeSpeeds = new float[] { 0f, 0.50f, 1.0f, 2.0f, 4.0f };
    private int currentTimeSpeedIndex = 0;

    private float timeMultiplier = 1f;
    private float timeElapsed = 0f;
    private System.DateTime currentDate;
    // IsoSceneTickBus bridge — registered by GameManager.Start (Stage 1.1)
    private IsoSceneTickBus _tickBus;

    /// <summary>Wire tick bus bridge. Called by GameManager.Start (invariant #12).</summary>
    public void RegisterTickBus(IsoSceneTickBus bus) => _tickBus = bus;

    void Awake()
    {
        // Cache GeographyManager in Awake (invariant #3 — never FindObjectOfType in Update).
        // Inspector wire is preferred; FindObjectOfType is safety-net only.
        if (geographyManager == null)
            geographyManager = FindObjectOfType<GeographyManager>();
        // Wave B2 (TECH-27085) — lazy-resolve recorder so monthly ticks accumulate history
        // even when scene wiring is missing.
        if (statsHistoryRecorder == null)
            statsHistoryRecorder = FindObjectOfType<Territory.Simulation.StatsHistoryRecorder>();
    }

    void Start()
    {
        currentDate = new System.DateTime(2024, 8, 27);
        currentDate = currentDate.Date;
        uiManager.UpdateUI();
        // Re-resolve recorder in Start in case adapter auto-created it after our Awake ran.
        if (statsHistoryRecorder == null)
            statsHistoryRecorder = FindObjectOfType<Territory.Simulation.StatsHistoryRecorder>();
    }

    // Default to 1.0× (index 2) on first Update so fresh new-game start isn't stuck at
    // index 0 (paused) and the day tick actually runs PlaceAllZonedBuildings (the
    // building-growth pump). Players can still pause / change speed via the HUD buttons.
    bool _initialSpeedApplied;

    void Update()
    {
        HandleOnKeyInput();

        if (!_initialSpeedApplied)
        {
            if (currentTimeSpeedIndex == 0)
                SetTimeSpeedIndex(2);
            _initialSpeedApplied = true;
        }

        timeElapsed += UnityEngine.Time.deltaTime * timeMultiplier;

        // Geography-init gate: block sim-state reads until geography pipeline complete.
        // HandleOnKeyInput + timeElapsed accumulator above remain active so UI stays responsive during load.
        if (timeElapsed >= 1f &&
            geographyManager != null && geographyManager.IsInitialized)
        {
            currentDate = currentDate.AddDays(1);
            timeElapsed = 0f;

            cityStats.PerformDailyUpdates();
            zoneManager.CalculateAvailableSquareZonedSections();
            PlaceAllZonedBuildings();
            _tickBus?.Publish(IsoTickKind.GlobalTick);
            if (simulationManager != null)
                simulationManager.ProcessSimulationTick();
            if (currentDate.Day == 1)
            {
                cityStats.PerformMonthlyUpdates();
                economyManager.ProcessDailyEconomy();
                if (simulationManager != null)
                    simulationManager.ProcessMonthlyReset();
                if (interstateManager != null)
                    interstateManager.CheckInterstateConnectivity();
                // Wave B2 (TECH-27085) — snapshot monthly aggregates.
                if (statsHistoryRecorder != null)
                    statsHistoryRecorder.OnMonthlyTick();
            }
            uiManager.UpdateUI();
        }
    }

    void PlaceAllZonedBuildings()
    {
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialMediumZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialHeavyZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialMediumZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialHeavyZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialMediumZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialHeavyZoning);
    }

    public void HandleOnKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetTimeSpeedIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetTimeSpeedIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetTimeSpeedIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetTimeSpeedIndex(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetTimeSpeedIndex(4);
        }
    }

    public void ChangeTimeSpeed()
    {
        currentTimeSpeedIndex = (currentTimeSpeedIndex + 1) % timeSpeeds.Length;
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];

        // Update button states when speed changes
        if (speedButtonsController != null)
        {
            speedButtonsController.OnSpeedChangedExternally(currentTimeSpeedIndex);
        }
    }

    public void SetTimeSpeedIndex(int index)
    {
        if (index < 0 || index >= timeSpeeds.Length)
            return;

        currentTimeSpeedIndex = index;
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];
        animatorManager.SetAnimatorSpeed(timeMultiplier);

        // Update button visual states when speed changes via keyboard
        if (speedButtonsController != null)
        {
            speedButtonsController.OnSpeedChangedExternally(currentTimeSpeedIndex);
        }
    }

    public System.DateTime GetCurrentDate()
    {
        return currentDate;
    }

    public float GetCurrentTimeMultiplier()
    {
        return timeMultiplier;
    }

    /// <summary>Read-only accessor for the current speed index (0..timeSpeeds.Length-1) — exposed for HUD adapters that mirror speed-button state. No setter; mutate via <see cref="SetTimeSpeedIndex"/>.</summary>
    public int CurrentTimeSpeedIndex => currentTimeSpeedIndex;

    // ── Modal pause-owner API (Wave B2 TECH-27084) ───────────────────────

    private string _modalPauseOwner;

    /// <summary>Returns true when sim is paused by a modal owner.</summary>
    public bool IsPaused => _modalPauseOwner != null;

    /// <summary>Set modal pause-owner and freeze sim (timeMultiplier → 0). Single-owner initially.</summary>
    public void SetModalPauseOwner(string owner)
    {
        if (string.IsNullOrEmpty(owner)) return;
        _modalPauseOwner = owner;
        timeMultiplier = 0f;
    }

    /// <summary>Clear modal pause-owner and resume sim only when <paramref name="owner"/> matches current owner.</summary>
    public void ClearModalPauseOwner(string owner)
    {
        if (_modalPauseOwner != owner) return;
        _modalPauseOwner = null;
        // Resume at last user-selected speed.
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];
    }

    public InGameTime GetCurrentInGameTime()
    {
        InGameTime inGameTime = new InGameTime();
        inGameTime.day = currentDate.Day;
        inGameTime.month = currentDate.Month;
        inGameTime.year = currentDate.Year;
        return inGameTime;
    }

    public void RestoreInGameTime(InGameTime inGameTime)
    {
        currentDate = new System.DateTime(inGameTime.year, inGameTime.month, inGameTime.day);
        if (uiManager != null)
            uiManager.UpdateUI();
    }

    public void ResetInGameTime()
    {
        currentDate = new System.DateTime(2024, 8, 27);
        if (uiManager != null)
            uiManager.UpdateUI();
    }
}

[System.Serializable]
public struct InGameTime
{
    public int day;
    public int month;
    public int year;
}
}
