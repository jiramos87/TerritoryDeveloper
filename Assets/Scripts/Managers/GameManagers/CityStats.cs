using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Territory.Timing;
using Territory.Terrain;
using Territory.Forests;
using Territory.Buildings;
using Territory.Core;
using Territory.Zones;
using Territory.Simulation;
using Territory.Simulation.Signals;

namespace Territory.Economy
{
/// <summary>
/// Global stats aggregator for city. Tracks population, money, happiness, employment,
/// water/power capacity + consumption, zone counts, resource budgets. Many managers read
/// from <see cref="CityStats"/> to decide. Updated by <see cref="TimeManager"/>, <see cref="WaterManager"/>, <see cref="ForestManager"/>.
/// </summary>
public class CityStats : MonoBehaviour, ICityStats
{
    #region Dependencies
    public TimeManager timeManager;
    public WaterManager waterManager;
    public ForestManager forestManager;
    [SerializeField] private HappinessComposer happinessComposer;
    [SerializeField] private SignalFieldRegistry signalFieldRegistry;
    [SerializeField] private SignalTickScheduler signalTickScheduler;
    private EmploymentManager _employmentManager;
    private EconomyManager _economyManager;
    private StatisticsManager _statisticsManager;
    #endregion

    #region City Data Fields
    public System.DateTime currentDate;

    public int population;
    public int money;

    // Stage 4 facade migration — happiness + pollution scalars now delegate to the
    // signal layer (HappinessComposer / SignalFieldRegistry). Backing fields keep
    // [SerializeField] + [FormerlySerializedAs] so legacy save-load round-trips
    // continue to populate the same Inspector slot. Setter still writes the backing
    // field for save-restore + reset paths (RestoreCityStatsData / ResetCityStats).
    [SerializeField] [FormerlySerializedAs("happiness")] private float _happiness = 50f;
    [SerializeField] [FormerlySerializedAs("pollution")] private float _pollution;

    public float happiness
    {
        get { return happinessComposer != null ? happinessComposer.Current : _happiness; }
        set { _happiness = value; }
    }

    public float pollution
    {
        get { return signalFieldRegistry != null ? ComputePollutionMean() : _pollution; }
        set { _pollution = value; }
    }

    public int residentialZoneCount;

    public int residentialBuildingCount;
    public int commercialZoneCount;
    public int commercialBuildingCount;
    public int industrialZoneCount;
    public int industrialBuildingCount;

    /// <summary>Stage 7 (TECH-1892) — daily-refreshed mean of <see cref="SimulationSignal.LandValue"/> across all districts. Populated by <see cref="RefreshEconomyReadModel"/> via <see cref="SignalTickScheduler"/>.<see cref="SignalTickScheduler.Cache"/>.MeanForSignal. <c>0f</c> when scheduler/cache absent or no district has emitted yet (no NaN propagation downstream).</summary>
    public float cityLandValueMean;

    public int residentialLightBuildingCount;
    public int residentialLightZoningCount;
    public int residentialMediumBuildingCount;
    public int residentialMediumZoningCount;
    public int residentialHeavyBuildingCount;
    public int residentialHeavyZoningCount;

    public int commercialLightBuildingCount;
    public int commercialLightZoningCount;
    public int commercialMediumBuildingCount;
    public int commercialMediumZoningCount;
    public int commercialHeavyBuildingCount;
    public int commercialHeavyZoningCount;

    public int industrialLightBuildingCount;
    public int industrialLightZoningCount;
    public int industrialMediumBuildingCount;
    public int industrialMediumZoningCount;
    public int industrialHeavyBuildingCount;
    public int industrialHeavyZoningCount;

    public int roadCount;
    public int grassCount;

    public int cityPowerConsumption;
    public int cityPowerOutput;

    private List<PowerPlant> powerPlants = new List<PowerPlant>();

    public string cityName;

    public int cityWaterConsumption;
    public int cityWaterOutput;

    [Header("Forest Statistics")]
    public int forestCellCount;
    public float forestCoveragePercentage;

    [Header("Simulation")]
    public bool simulateGrowth = false;
    public List<CommuneData> communes = new List<CommuneData>();

    [Header("Economy read-model (envelope)")]
    /// <summary>Global monthly envelope cap from <see cref="BudgetAllocationService.GlobalMonthlyCap"/>.</summary>
    public int totalEnvelopeCap;
    /// <summary>Remaining draw per Zone S sub-type (length 7).</summary>
    public int[] envelopeRemainingPerSubType = new int[7];
    #endregion

    private BudgetAllocationService budgetAllocationService;

    #region Population and Demographics
    void Start()
    {
        population = 0;
        money = 20000;
        residentialZoneCount = 0;
        commercialZoneCount = 0;
        industrialZoneCount = 0;
        roadCount = 0;
        grassCount = 0;
        cityPowerConsumption = 0;
        cityPowerOutput = 0;
        cityWaterConsumption = 0;
        cityWaterOutput = 0;
        // cityName is set by RegionalMapManager.InitializeRegionalMap() from the player territory

        // Initialize forest statistics
        forestCellCount = 0;
        forestCoveragePercentage = 0f;

        if (forestManager == null)
            forestManager = FindObjectOfType<ForestManager>();
        if (_employmentManager == null)
            _employmentManager = FindObjectOfType<EmploymentManager>();
        if (_economyManager == null)
            _economyManager = FindObjectOfType<EconomyManager>();
        if (_statisticsManager == null)
            _statisticsManager = FindObjectOfType<StatisticsManager>();
        if (budgetAllocationService == null)
            budgetAllocationService = FindObjectOfType<BudgetAllocationService>();
        // Stage 4 facade refs — Inspector primary, FindObjectOfType fallback per invariant #4.
        if (happinessComposer == null)
            happinessComposer = FindObjectOfType<HappinessComposer>();
        if (signalFieldRegistry == null)
            signalFieldRegistry = FindObjectOfType<SignalFieldRegistry>();
        if (signalTickScheduler == null)
            signalTickScheduler = FindObjectOfType<SignalTickScheduler>();
    }

    /// <summary>
    /// Refresh derived envelope fields for HUD + stats panel. Call from daily tick.
    /// </summary>
    public void RefreshEconomyReadModel()
    {
        if (envelopeRemainingPerSubType == null || envelopeRemainingPerSubType.Length != 7)
            envelopeRemainingPerSubType = new int[7];

        if (budgetAllocationService != null)
        {
            totalEnvelopeCap = budgetAllocationService.GlobalMonthlyCap;
            for (int i = 0; i < 7; i++)
                envelopeRemainingPerSubType[i] = budgetAllocationService.GetRemaining(i);
        }

        // Stage 7 (TECH-1892) — refresh per-tick mean LandValue from district cache.
        // Empty cache or absent scheduler → 0f (no NaN propagation into EconomyManager bonus).
        if (signalTickScheduler != null && signalTickScheduler.Cache != null)
        {
            cityLandValueMean = signalTickScheduler.Cache.MeanForSignal(SimulationSignal.LandValue);
        }
        else
        {
            cityLandValueMean = 0f;
        }
    }

    /// <summary>Add (or subtract if negative) amount to city population.</summary>
    /// <param name="value">Population delta.</param>
    public void AddPopulation(int value)
    {
        population += value;
    }

    /// <summary>Add amount to city treasury.</summary>
    /// <param name="value">Money to add.</param>
    public void AddMoney(int value)
    {
        money += value;
    }

    /// <summary>Subtract amount from city treasury.</summary>
    /// <param name="value">Money to remove.</param>
    public void RemoveMoney(int value)
    {
        money -= value;
    }

    #endregion

    #region Zone Statistics
    /// <summary>Inc residential zone count by 1.</summary>
    public void AddResidentialZoneCount()
    {
        residentialZoneCount++;
    }

    /// <summary>Dec residential zone count by 1.</summary>
    public void RemoveResidentialZoneCount()
    {
        residentialZoneCount--;
    }

    /// <summary>Inc residential building count by 1.</summary>
    public void AddResidentialBuildingCount()
    {
        residentialBuildingCount++;
    }

    /// <summary>Dec residential building count by 1.</summary>
    public void RemoveResidentialBuildingCount()
    {
        residentialBuildingCount--;
    }

    /// <summary>Inc commercial zone count by 1.</summary>
    public void AddCommercialZoneCount()
    {
        commercialZoneCount++;
    }

    /// <summary>Dec commercial zone count by 1.</summary>
    public void RemoveCommercialZoneCount()
    {
        commercialZoneCount--;
    }

    /// <summary>Inc commercial building count by 1.</summary>
    public void AddCommercialBuildingCount()
    {
        commercialBuildingCount++;
    }

    /// <summary>Dec commercial building count by 1.</summary>
    public void RemoveCommercialBuildingCount()
    {
        commercialBuildingCount--;
    }

    /// <summary>Inc industrial zone count by 1.</summary>
    public void AddIndustrialZoneCount()
    {
        industrialZoneCount++;
    }

    /// <summary>Dec industrial zone count by 1.</summary>
    public void RemoveIndustrialZoneCount()
    {
        industrialZoneCount--;
    }

    /// <summary>Inc industrial building count by 1.</summary>
    public void AddIndustrialBuildingCount()
    {
        industrialBuildingCount++;
    }

    /// <summary>Dec industrial building count by 1.</summary>
    public void RemoveIndustrialBuildingCount()
    {
        industrialBuildingCount--;
    }

    /// <summary>Inc residential light building count + aggregate residential building count.</summary>
    public void AddResidentialLightBuildingCount()
    {
        residentialLightBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>Dec residential light building count + aggregate residential building count.</summary>
    public void RemoveResidentialLightBuildingCount()
    {
        residentialLightBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>Inc residential light zoning count + aggregate residential zone count.</summary>
    public void AddResidentialLightZoningCount()
    {
        residentialLightZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>Dec residential light zoning count + aggregate residential zone count.</summary>
    public void RemoveResidentialLightZoningCount()
    {
        residentialLightZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>Inc residential medium building count + aggregate residential building count.</summary>
    public void AddResidentialMediumBuildingCount()
    {
        residentialMediumBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>Dec residential medium building count + aggregate residential building count.</summary>
    public void RemoveResidentialMediumBuildingCount()
    {
        residentialMediumBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>Inc residential medium zoning count + aggregate residential zone count.</summary>
    public void AddResidentialMediumZoningCount()
    {
        residentialMediumZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>Dec residential medium zoning count + aggregate residential zone count.</summary>
    public void RemoveResidentialMediumZoningCount()
    {
        residentialMediumZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>Inc residential heavy building count + aggregate residential building count.</summary>
    public void AddResidentialHeavyBuildingCount()
    {
        residentialHeavyBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>Dec residential heavy building count + aggregate residential building count.</summary>
    public void RemoveResidentialHeavyBuildingCount()
    {
        residentialHeavyBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>Inc residential heavy zoning count + aggregate residential zone count.</summary>
    public void AddResidentialHeavyZoningCount()
    {
        residentialHeavyZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>Dec residential heavy zoning count + aggregate residential zone count.</summary>
    public void RemoveResidentialHeavyZoningCount()
    {
        residentialHeavyZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>Inc commercial light building count + aggregate commercial building count.</summary>
    public void AddCommercialLightBuildingCount()
    {
        commercialLightBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>Dec commercial light building count + aggregate commercial building count.</summary>
    public void RemoveCommercialLightBuildingCount()
    {
        commercialLightBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>Inc commercial light zoning count + aggregate commercial zone count.</summary>
    public void AddCommercialLightZoningCount()
    {
        commercialLightZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>Dec commercial light zoning count + aggregate commercial zone count.</summary>
    public void RemoveCommercialLightZoningCount()
    {
        commercialLightZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>Inc commercial medium building count + aggregate commercial building count.</summary>
    public void AddCommercialMediumBuildingCount()
    {
        commercialMediumBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>Dec commercial medium building count + aggregate commercial building count.</summary>
    public void RemoveCommercialMediumBuildingCount()
    {
        commercialMediumBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>Inc commercial medium zoning count + aggregate commercial zone count.</summary>
    public void AddCommercialMediumZoningCount()
    {
        commercialMediumZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>Dec commercial medium zoning count + aggregate commercial zone count.</summary>
    public void RemoveCommercialMediumZoningCount()
    {
        commercialMediumZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>Inc commercial heavy building count + aggregate commercial building count.</summary>
    public void AddCommercialHeavyBuildingCount()
    {
        commercialHeavyBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>Dec commercial heavy building count + aggregate commercial building count.</summary>
    public void RemoveCommercialHeavyBuildingCount()
    {
        commercialHeavyBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>Inc commercial heavy zoning count + aggregate commercial zone count.</summary>
    public void AddCommercialHeavyZoningCount()
    {
        commercialHeavyZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>Dec commercial heavy zoning count + aggregate commercial zone count.</summary>
    public void RemoveCommercialHeavyZoningCount()
    {
        commercialHeavyZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>Inc industrial light building count + aggregate industrial building count.</summary>
    public void AddIndustrialLightBuildingCount()
    {
        industrialLightBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>Dec industrial light building count + aggregate industrial building count.</summary>
    public void RemoveIndustrialLightBuildingCount()
    {
        industrialLightBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>Inc industrial light zoning count + aggregate industrial zone count.</summary>
    public void AddIndustrialLightZoningCount()
    {
        industrialLightZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>Dec industrial light zoning count + aggregate industrial zone count.</summary>
    public void RemoveIndustrialLightZoningCount()
    {
        industrialLightZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>Inc industrial medium building count + aggregate industrial building count.</summary>
    public void AddIndustrialMediumBuildingCount()
    {
        industrialMediumBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>Dec industrial medium building count + aggregate industrial building count.</summary>
    public void RemoveIndustrialMediumBuildingCount()
    {
        industrialMediumBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>Inc industrial medium zoning count + aggregate industrial zone count.</summary>
    public void AddIndustrialMediumZoningCount()
    {
        industrialMediumZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>Dec industrial medium zoning count + aggregate industrial zone count.</summary>
    public void RemoveIndustrialMediumZoningCount()
    {
        industrialMediumZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>Inc industrial heavy building count + aggregate industrial building count.</summary>
    public void AddIndustrialHeavyBuildingCount()
    {
        industrialHeavyBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>Dec industrial heavy building count + aggregate industrial building count.</summary>
    public void RemoveIndustrialHeavyBuildingCount()
    {
        industrialHeavyBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>Inc industrial heavy zoning count + aggregate industrial zone count.</summary>
    public void AddIndustrialHeavyZoningCount()
    {
        industrialHeavyZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>Dec industrial heavy zoning count + aggregate industrial zone count.</summary>
    public void RemoveIndustrialHeavyZoningCount()
    {
        industrialHeavyZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>Inc road count by 1.</summary>
    public void AddRoadCount()
    {
        roadCount++;
    }

    /// <summary>Inc grass tile count by 1.</summary>
    public void AddGrassCount()
    {
        grassCount++;
    }

    /// <summary>Inc appropriate zone or building counter for given zone type.</summary>
    /// <param name="zoneType">Zone type whose counter to increment.</param>
    public void AddZoneBuildingCount(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightBuilding:
                AddResidentialLightBuildingCount();
                break;
            case Zone.ZoneType.ResidentialLightZoning:
                AddResidentialLightZoningCount();
                break;
            case Zone.ZoneType.ResidentialMediumBuilding:
                AddResidentialMediumBuildingCount();
                break;
            case Zone.ZoneType.ResidentialMediumZoning:
                AddResidentialMediumZoningCount();
                break;
            case Zone.ZoneType.ResidentialHeavyBuilding:
                AddResidentialHeavyBuildingCount();
                break;
            case Zone.ZoneType.ResidentialHeavyZoning:
                AddResidentialHeavyZoningCount();
                break;
            case Zone.ZoneType.CommercialLightBuilding:
                AddCommercialLightBuildingCount();
                break;
            case Zone.ZoneType.CommercialLightZoning:
                AddCommercialLightZoningCount();
                break;
            case Zone.ZoneType.CommercialMediumBuilding:
                AddCommercialMediumBuildingCount();
                break;
            case Zone.ZoneType.CommercialMediumZoning:
                AddCommercialMediumZoningCount();
                break;
            case Zone.ZoneType.CommercialHeavyBuilding:
                AddCommercialHeavyBuildingCount();
                break;
            case Zone.ZoneType.CommercialHeavyZoning:
                AddCommercialHeavyZoningCount();
                break;
            case Zone.ZoneType.IndustrialLightBuilding:
                AddIndustrialLightBuildingCount();
                break;
            case Zone.ZoneType.IndustrialLightZoning:
                AddIndustrialLightZoningCount();
                break;
            case Zone.ZoneType.IndustrialMediumBuilding:
                AddIndustrialMediumBuildingCount();
                break;
            case Zone.ZoneType.IndustrialMediumZoning:
                AddIndustrialMediumZoningCount();
                break;
            case Zone.ZoneType.IndustrialHeavyBuilding:
                AddIndustrialHeavyBuildingCount();
                break;
            case Zone.ZoneType.IndustrialHeavyZoning:
                AddIndustrialHeavyZoningCount();
                break;
            case Zone.ZoneType.Road:
                AddRoadCount();
                break;
            case Zone.ZoneType.Grass:
                AddGrassCount();
                break;
        }
    }

    /// <summary>Dec appropriate zone or building counter for given zone type.</summary>
    /// <param name="zoneType">Zone type whose counter to decrement.</param>
    public void RemoveZoneBuildingCount(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightBuilding:
                RemoveResidentialLightBuildingCount();
                break;
            case Zone.ZoneType.ResidentialLightZoning:
                RemoveResidentialLightZoningCount();
                break;
            case Zone.ZoneType.ResidentialMediumBuilding:
                RemoveResidentialMediumBuildingCount();
                break;
            case Zone.ZoneType.ResidentialMediumZoning:
                RemoveResidentialMediumZoningCount();
                break;
            case Zone.ZoneType.ResidentialHeavyBuilding:
                RemoveResidentialHeavyBuildingCount();
                break;
            case Zone.ZoneType.ResidentialHeavyZoning:
                RemoveResidentialHeavyZoningCount();
                break;
            case Zone.ZoneType.CommercialLightBuilding:
                RemoveCommercialLightBuildingCount();
                break;
            case Zone.ZoneType.CommercialLightZoning:
                RemoveCommercialLightZoningCount();
                break;
            case Zone.ZoneType.CommercialMediumBuilding:
                RemoveCommercialMediumBuildingCount();
                break;
            case Zone.ZoneType.CommercialMediumZoning:
                RemoveCommercialMediumZoningCount();
                break;
            case Zone.ZoneType.CommercialHeavyBuilding:
                RemoveCommercialHeavyBuildingCount();
                break;
            case Zone.ZoneType.CommercialHeavyZoning:
                RemoveCommercialHeavyZoningCount();
                break;
            case Zone.ZoneType.IndustrialLightBuilding:
                RemoveIndustrialLightBuildingCount();
                break;
            case Zone.ZoneType.IndustrialLightZoning:
                RemoveIndustrialLightZoningCount();
                break;
            case Zone.ZoneType.IndustrialMediumBuilding:
                RemoveIndustrialMediumBuildingCount();
                break;
            case Zone.ZoneType.IndustrialMediumZoning:
                RemoveIndustrialMediumZoningCount();
                break;
            case Zone.ZoneType.IndustrialHeavyBuilding:
                RemoveIndustrialHeavyBuildingCount();
                break;
            case Zone.ZoneType.IndustrialHeavyZoning:
                RemoveIndustrialHeavyZoningCount();
                break;
            case Zone.ZoneType.Road:
                roadCount--;
                break;
            case Zone.ZoneType.Grass:
                grassCount--;
                break;
        }
    }
    #endregion

    #region Resource Capacity
    /// <summary>Return true if treasury ≥ cost.</summary>
    /// <param name="cost">Cost to check.</param>
    /// <returns>True if affordable.</returns>
    public bool CanAfford(int cost)
    {
        return money >= cost;
    }

    /// <summary>Register power plant + recalc total city power output.</summary>
    /// <param name="powerPlant">Plant to register.</param>
    public void RegisterPowerPlant(PowerPlant powerPlant)
    {
        powerPlants.Add(powerPlant);

        int totalPowerOutput = 0;
        foreach (var plant in powerPlants)
        {
            totalPowerOutput += plant.PowerOutput;
        }

        cityPowerOutput = totalPowerOutput;
    }

    /// <summary>Unregister power plant + recalc total city power output.</summary>
    /// <param name="powerPlant">Plant to unregister.</param>
    public void UnregisterPowerPlant(PowerPlant powerPlant)
    {
        powerPlants.Remove(powerPlant);

        int totalPowerOutput = 0;
        foreach (var plant in powerPlants)
        {
            totalPowerOutput += plant.PowerOutput;
        }

        cityPowerOutput = totalPowerOutput;
    }

    /// <summary>Clear registered plants + reset city power output to 0.</summary>
    public void ResetPowerPlants()
    {
        powerPlants.Clear();
        cityPowerOutput = 0;
    }

    /// <summary>Count of registered plants for output aggregation (upkeep + pollution).</summary>
    public int GetRegisteredPowerPlantCount()
    {
        return powerPlants.Count;
    }

    /// <summary>Total power output from all registered plants.</summary>
    /// <returns>Total city power output.</returns>
    public int GetTotalPowerOutput()
    {
        return cityPowerOutput;
    }

    /// <summary>Add amount to city total power consumption.</summary>
    /// <param name="value">Power consumption to add.</param>
    public void AddPowerConsumption(int value)
    {
        cityPowerConsumption += value;
    }

    /// <summary>Subtract amount from city total power consumption.</summary>
    /// <param name="value">Power consumption to remove.</param>
    public void RemovePowerConsumption(int value)
    {
        cityPowerConsumption -= value;
    }

    /// <summary>Total city-wide power consumption.</summary>
    /// <returns>Total city power consumption.</returns>
    public int GetTotalPowerConsumption()
    {
        return cityPowerConsumption;
    }
    #endregion

    #region Update Methods
    /// <summary>Monthly update logic (placeholder for future monthly calcs).</summary>
    public void PerformMonthlyUpdates()
    {

    }

    /// <summary>Daily update: sync date, update employment + stats + forest data.</summary>
    public void PerformDailyUpdates()
    {
        currentDate = timeManager.GetCurrentDate();

        if (_employmentManager != null) _employmentManager.UpdateEmployment();
        if (_statisticsManager != null) _statisticsManager.UpdateStatistics();

        // Update forest statistics
        UpdateForestStatistics();

        // Stage 4 — pollution + happiness now driven by SignalTickScheduler (producers + HappinessComposer);
        // RecalculatePollution() / RecalculateHappiness() bodies migrated to no-ops + facade getters above.

        // Demand uses same-tick happiness targets and tax rates (see EmploymentManager.RefreshRCIDemandAfterDailyStats)
        if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats();

        RefreshEconomyReadModel();
    }

    /// <summary>
    /// Re-run pollution + happiness + R/C/I demand when tax or policy inputs change mid-day from UI
    /// (daily ticks already call <see cref="PerformDailyUpdates"/>).
    /// </summary>
    public void RefreshHappinessAfterPolicyChange()
    {
        // Stage 4 — pollution + happiness now driven by SignalTickScheduler; facade getters
        // surface the post-tick value. RecalculatePollution / RecalculateHappiness retained
        // as no-op shims for any external caller that still invokes them.
        if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats();
    }

    /// <summary>True if city power output &gt; consumption.</summary>
    /// <returns>True if power supply sufficient.</returns>
    public bool GetCityPowerAvailability()
    {
        return cityPowerOutput > cityPowerConsumption;
    }
    #endregion

    #region Economy
    /// <summary>Apply cost + population + happiness + power + water effects of new zone/building placement.</summary>
    /// <param name="zoneType">Zone/building type placed.</param>
    /// <param name="zoneAttributes">Attributes → costs + stat contributions.</param>
    public void HandleZoneBuildingPlacement(Zone.ZoneType zoneType, ZoneAttributes zoneAttributes)
    {
        RemoveMoney(zoneAttributes.ConstructionCost);
        AddPopulation(zoneAttributes.Population);
        AddZoneBuildingCount(zoneType);
        AddPowerConsumption(zoneAttributes.PowerConsumption);
        AddWaterConsumption(zoneAttributes.WaterConsumption);
    }

    /// <summary>Reverse stat effects of demolished building + refund fraction of build cost.</summary>
    /// <param name="zoneType">Zone/building type demolished.</param>
    /// <param name="zoneAttributes">Attributes → reverse stat contributions.</param>
    public void HandleBuildingDemolition(Zone.ZoneType zoneType, ZoneAttributes zoneAttributes)
    {
        AddMoney(zoneAttributes.ConstructionCost / 5);
        AddPopulation(-zoneAttributes.Population);
        RemoveZoneBuildingCount(zoneType);
        RemovePowerConsumption(zoneAttributes.PowerConsumption);
        RemoveWaterConsumption(zoneAttributes.WaterConsumption);
    }

    /// <summary>Update forest stats (called by ForestManager).</summary>
    public void UpdateForestStats(ForestStatistics forestStats)
    {
        forestCellCount = forestStats.totalForestCells;
        forestCoveragePercentage = forestStats.forestCoveragePercentage;
    }

    /// <summary>Update forest stats from ForestManager.</summary>
    private void UpdateForestStatistics()
    {
        if (forestManager != null)
        {
            var forestStats = forestManager.GetForestStatistics();
            UpdateForestStats(forestStats);
        }
    }

    /// <summary>
    /// Forest happiness bonus normalized 0–1. Diminishing returns above 20 forest cells; capped at maxForestBonus.
    /// </summary>
    public float GetForestHappinessBonus()
    {
        float bonus = forestCellCount * 1.0f;
        if (forestCellCount > 20)
            bonus = 20f + (forestCellCount - 20) * 0.5f;
        return Mathf.Min(bonus, MAX_FOREST_BONUS);
    }
    #endregion

    #region Happiness & Pollution
    // --- Happiness weights: positives must not push raw sum far above 100 before tax, or Mathf.Clamp hides tax pressure ---
    private const float HAPPINESS_BASELINE = 50f;
    private const float WEIGHT_EMPLOYMENT = 30f;
    private const float WEIGHT_SERVICES = 20f;
    private const float WEIGHT_FOREST = 10f;
    private const float WEIGHT_POLLUTION = 10f;

    [Header("Happiness formula (tuning)")]
    [Tooltip("Applied as taxFactor × this (taxFactor is 0 to -1). Higher = stronger mood hit from rates above the comfort band.")]
    [SerializeField] private float happinessWeightTax = 27f;
    [Tooltip("Weighted by (buildings / zoned cells), 0–1.")]
    [SerializeField] private float happinessWeightDevelopment = 12f;
    [Tooltip("0–1 stub until service coverage ships (FEAT-52). Multiplied by WEIGHT_SERVICES.")]
    [Range(0f, 1f)]
    [SerializeField] private float happinessServiceCoverageStub = 0.4f;

    // Convergence
    private const float BASE_CONVERGENCE_RATE = 0.15f;
    private const float POPULATION_SCALE_FACTOR = 500f;

    /// <summary>Maps target happiness 0–100 to demand multiplier (low happiness suppresses R/C/I appetite).</summary>
    private const float DEMAND_HAPPINESS_MULT_MIN = 0.8f;
    private const float DEMAND_HAPPINESS_MULT_MAX = 1.2f;

    // Tax comfort threshold (average tax rate at or below this has no penalty)
    private const float COMFORTABLE_TAX_RATE = 10f;
    private const float MAX_TAX_RATE_FOR_SCALE = 50f;

    // Forest normalization
    private const float MAX_FOREST_BONUS = 60f;

    // --- Pollution constants ---
    private const float POLLUTION_INDUSTRIAL_HEAVY = 3.0f;
    private const float POLLUTION_INDUSTRIAL_MEDIUM = 2.0f;
    private const float POLLUTION_INDUSTRIAL_LIGHT = 1.0f;
    private const float POLLUTION_NUCLEAR = 2.0f;
    private const float FOREST_ABSORPTION_RATE = 0.3f;
    private const float POLLUTION_CAP = 200f;

    /// <summary>
    /// Recalc city-wide pollution from industrial buildings + power plants, minus forest absorption.
    /// Called once per sim tick before RecalculateHappiness.
    /// </summary>
    public void RecalculatePollution()
    {
        // Migrated to ForestPollutionSink + IndustrialPollutionProducer + PowerPlantPollutionProducer (Stage 4).
        // SignalTickScheduler drives the per-tick recompute via the producer/diffusion pipeline; the
        // pollution getter now reads the per-cell mean of SimulationSignal.PollutionAir.
    }

    /// <summary>Mean of non-zero cells in the PollutionAir signal field; Stage 4 facade for the legacy `pollution` scalar.</summary>
    private float ComputePollutionMean()
    {
        if (signalFieldRegistry == null)
            return _pollution;
        SignalField field = signalFieldRegistry.GetField(SimulationSignal.PollutionAir);
        if (field == null)
            return _pollution;
        int width = field.Width;
        int height = field.Height;
        if (width <= 0 || height <= 0)
            return 0f;
        float sum = 0f;
        int nonZero = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float cell = field.Get(x, y);
                if (Mathf.Abs(cell) > Mathf.Epsilon)
                {
                    sum += cell;
                    nonZero++;
                }
            }
        }
        return nonZero > 0 ? sum / nonZero : 0f;
    }

    /// <summary>
    /// Compute immediate happiness target from employment + tax + services + forest + development + pollution.
    /// Used for display convergence + same-tick demand feedback.
    /// </summary>
    private float ComputeTargetHappiness()
    {
        // Employment factor (0–1): higher employment = happier
        float employmentFactor = 0.5f;
        if (_employmentManager != null)
            employmentFactor = _employmentManager.GetEmploymentRate() / 100f;

        // Tax factor (0 to -1): use the highest R/C/I rate so raising one slider visibly affects happiness (average diluted the effect).
        float taxFactor = 0f;
        if (_economyManager != null)
        {
            float maxTax = Mathf.Max(
                _economyManager.residentialIncomeTax,
                Mathf.Max(_economyManager.commercialIncomeTax, _economyManager.industrialIncomeTax));
            if (maxTax > COMFORTABLE_TAX_RATE)
            {
                taxFactor = -Mathf.Clamp01((maxTax - COMFORTABLE_TAX_RATE) /
                            (MAX_TAX_RATE_FOR_SCALE - COMFORTABLE_TAX_RATE));
            }
        }

        float serviceFactor = Mathf.Clamp01(happinessServiceCoverageStub);

        // Forest factor (0–1)
        float forestFactor = Mathf.Clamp01(GetForestHappinessBonus() / MAX_FOREST_BONUS);

        // Development factor (0–1): ratio of buildings to zoned cells
        int totalBuildings = residentialBuildingCount + commercialBuildingCount + industrialBuildingCount;
        int totalZoned = residentialZoneCount + commercialZoneCount + industrialZoneCount;
        float developmentFactor = totalZoned > 0 ? Mathf.Clamp01((float)totalBuildings / totalZoned) : 0f;

        // Pollution factor (0–1)
        float pollutionFactor = Mathf.Clamp01(pollution / POLLUTION_CAP);

        float targetHappiness = HAPPINESS_BASELINE
            + employmentFactor * WEIGHT_EMPLOYMENT
            + taxFactor * happinessWeightTax
            + serviceFactor * WEIGHT_SERVICES
            + forestFactor * WEIGHT_FOREST
            + developmentFactor * happinessWeightDevelopment
            - pollutionFactor * WEIGHT_POLLUTION;

        return Mathf.Clamp(targetHappiness, 0f, 100f);
    }

    /// <summary>
    /// Recalc happiness as 0–100 score from weighted factors, converging smoothly.
    /// Called once per sim tick after employment + economy + pollution updates.
    /// </summary>
    public void RecalculateHappiness()
    {
        // Migrated to HappinessComposer (Stage 4).
        // SignalTickScheduler drives per-tick happiness convergence; the happiness getter now
        // reads HappinessComposer.Current. ComputeTargetHappiness retained because
        // GetHappinessDemandMultiplier (below) still uses the immediate target.
    }

    /// <summary>Happiness normalized 0–1 for UI/diagnostics (current converged value).</summary>
    public float GetNormalizedHappiness()
    {
        return Mathf.Clamp01(happiness / 100f);
    }

    /// <summary>
    /// Demand multiplier from <b>today's</b> happiness target (not lerped score) → tax + employment changes
    /// affect R/C/I demand on same daily tick.
    /// </summary>
    public float GetHappinessDemandMultiplier()
    {
        float normalized = Mathf.Clamp01(ComputeTargetHappiness() / 100f);
        return DEMAND_HAPPINESS_MULT_MIN + normalized * (DEMAND_HAPPINESS_MULT_MAX - DEMAND_HAPPINESS_MULT_MIN);
    }
    #endregion

    #region Utility Methods
    /// <summary>Snapshot all city stats into serializable data struct for saving.</summary>
    /// <returns>CityStatsData struct with current stats.</returns>
    public CityStatsData GetCityStatsData()
    {
        CityStatsData cityStatsData = new CityStatsData
        {
            currentDate = currentDate,
            population = population,
            money = money,
            happiness = happiness,
            pollution = pollution,
            residentialZoneCount = residentialZoneCount,
            residentialBuildingCount = residentialBuildingCount,
            commercialZoneCount = commercialZoneCount,
            commercialBuildingCount = commercialBuildingCount,
            industrialZoneCount = industrialZoneCount,
            industrialBuildingCount = industrialBuildingCount,
            residentialLightBuildingCount = residentialLightBuildingCount,
            residentialLightZoningCount = residentialLightZoningCount,
            residentialMediumBuildingCount = residentialMediumBuildingCount,
            residentialMediumZoningCount = residentialMediumZoningCount,
            residentialHeavyBuildingCount = residentialHeavyBuildingCount,
            residentialHeavyZoningCount = residentialHeavyZoningCount,
            commercialLightBuildingCount = commercialLightBuildingCount,
            commercialLightZoningCount = commercialLightZoningCount,
            commercialMediumBuildingCount = commercialMediumBuildingCount,
            commercialMediumZoningCount = commercialMediumZoningCount,
            commercialHeavyBuildingCount = commercialHeavyBuildingCount,
            commercialHeavyZoningCount = commercialHeavyZoningCount,
            industrialLightBuildingCount = industrialLightBuildingCount,
            industrialLightZoningCount = industrialLightZoningCount,
            industrialMediumBuildingCount = industrialMediumBuildingCount,
            industrialMediumZoningCount = industrialMediumZoningCount,
            industrialHeavyBuildingCount = industrialHeavyBuildingCount,
            industrialHeavyZoningCount = industrialHeavyZoningCount,
            roadCount = roadCount,
            grassCount = grassCount,
            cityPowerConsumption = cityPowerConsumption,
            cityPowerOutput = cityPowerOutput,
            cityWaterConsumption = cityWaterConsumption,
            cityWaterOutput = cityWaterOutput,
            cityName = cityName,
            // Forest statistics
            forestCellCount = forestCellCount,
            forestCoveragePercentage = forestCoveragePercentage,
            simulateGrowth = simulateGrowth,
            communes = communes
        };

        return cityStatsData;
    }

    /// <summary>Restore all city stats from previously saved data struct.</summary>
    /// <param name="cityStatsData">Saved data to restore from.</param>
    public void RestoreCityStatsData(CityStatsData cityStatsData)
    {
        currentDate = cityStatsData.currentDate;
        population = cityStatsData.population;
        money = cityStatsData.money;
        happiness = Mathf.Clamp(cityStatsData.happiness, 0f, 100f);
        pollution = Mathf.Max(cityStatsData.pollution, 0f);
        residentialZoneCount = cityStatsData.residentialZoneCount;
        residentialBuildingCount = cityStatsData.residentialBuildingCount;
        commercialZoneCount = cityStatsData.commercialZoneCount;
        commercialBuildingCount = cityStatsData.commercialBuildingCount;
        industrialZoneCount = cityStatsData.industrialZoneCount;
        industrialBuildingCount = cityStatsData.industrialBuildingCount;
        residentialLightBuildingCount = cityStatsData.residentialLightBuildingCount;
        residentialLightZoningCount = cityStatsData.residentialLightZoningCount;
        residentialMediumBuildingCount = cityStatsData.residentialMediumBuildingCount;
        residentialMediumZoningCount = cityStatsData.residentialMediumZoningCount;
        residentialHeavyBuildingCount = cityStatsData.residentialHeavyBuildingCount;
        residentialHeavyZoningCount = cityStatsData.residentialHeavyZoningCount;
        commercialLightBuildingCount = cityStatsData.commercialLightBuildingCount;
        commercialLightZoningCount = cityStatsData.commercialLightZoningCount;
        commercialMediumBuildingCount = cityStatsData.commercialMediumBuildingCount;
        commercialMediumZoningCount = cityStatsData.commercialMediumZoningCount;
        commercialHeavyBuildingCount = cityStatsData.commercialHeavyBuildingCount;
        commercialHeavyZoningCount = cityStatsData.commercialHeavyZoningCount;
        industrialLightBuildingCount = cityStatsData.industrialLightBuildingCount;
        industrialLightZoningCount = cityStatsData.industrialLightZoningCount;
        industrialMediumBuildingCount = cityStatsData.industrialMediumBuildingCount;
        industrialMediumZoningCount = cityStatsData.industrialMediumZoningCount;
        industrialHeavyBuildingCount = cityStatsData.industrialHeavyBuildingCount;
        industrialHeavyZoningCount = cityStatsData.industrialHeavyZoningCount;
        roadCount = cityStatsData.roadCount;
        grassCount = cityStatsData.grassCount;
        cityPowerConsumption = cityStatsData.cityPowerConsumption;
        cityPowerOutput = cityStatsData.cityPowerOutput;
        cityWaterConsumption = cityStatsData.cityWaterConsumption;
        cityWaterOutput = cityStatsData.cityWaterOutput;
        cityName = cityStatsData.cityName;

        // Restore forest statistics
        forestCellCount = cityStatsData.forestCellCount;
        forestCoveragePercentage = cityStatsData.forestCoveragePercentage;
        simulateGrowth = cityStatsData.simulateGrowth;
        communes = cityStatsData.communes != null ? new List<CommuneData>(cityStatsData.communes) : new List<CommuneData>();
    }

    /// <summary>Reset all city stats to defaults (new game state).</summary>
    public void ResetCityStats()
    {
        ResetPowerPlants();

        population = 0;
        money = 20000;
        happiness = 50f;
        pollution = 0f;
        currentDate = new System.DateTime(2024, 8, 27);
        residentialZoneCount = 0;
        commercialZoneCount = 0;
        industrialZoneCount = 0;
        roadCount = 0;
        grassCount = 0;
        cityPowerConsumption = 0;
        cityPowerOutput = 0;
        // cityName is preserved; sync from RegionalMap in GameSaveManager.NewGame() if needed

        // Reset forest statistics
        forestCellCount = 0;
        forestCoveragePercentage = 0f;

        simulateGrowth = false;
        communes?.Clear();
        if (communes == null) communes = new List<CommuneData>();
    }

    /// <summary>
    /// Set city name (e.g. player rename). Becomes canonical name until changed again or loaded from save.
    /// </summary>
    public void SetCityName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            cityName = newName.Trim();
    }

    /// <summary>Find + return EmploymentManager in scene.</summary>
    /// <returns>EmploymentManager instance or null.</returns>
    public EmploymentManager GetEmploymentManager() => _employmentManager;

    /// <summary>Add amount to both local + WaterManager water consumption trackers.</summary>
    /// <param name="value">Water consumption to add.</param>
    public void AddWaterConsumption(int value)
    {
        cityWaterConsumption += value;
        if (waterManager != null)
        {
            waterManager.AddWaterConsumption(value);
        }
    }

    /// <summary>Subtract amount from both local + WaterManager water consumption trackers.</summary>
    /// <param name="value">Water consumption to remove.</param>
    public void RemoveWaterConsumption(int value)
    {
        cityWaterConsumption -= value;
        if (waterManager != null)
        {
            waterManager.RemoveWaterConsumption(value);
        }
    }

    /// <summary>Total city-wide water consumption.</summary>
    /// <returns>Total city water consumption.</returns>
    public int GetTotalWaterConsumption()
    {
        return cityWaterConsumption;
    }

    /// <summary>Total city-wide water output.</summary>
    /// <returns>Total city water output.</returns>
    public int GetTotalWaterOutput()
    {
        return cityWaterOutput;
    }

    /// <summary>True if city water output &gt; consumption. Syncs with WaterManager first.</summary>
    /// <returns>True if water supply sufficient.</returns>
    public bool GetCityWaterAvailability()
    {
        // Sync with water manager
        if (waterManager != null)
        {
            cityWaterOutput = waterManager.GetTotalWaterOutput();
            cityWaterConsumption = waterManager.GetTotalWaterConsumption();
        }

        return cityWaterOutput > cityWaterConsumption;
    }

    /// <summary>Sync local water output value from WaterManager.</summary>
    public void UpdateWaterOutput()
    {
        if (waterManager != null)
        {
            cityWaterOutput = waterManager.GetTotalWaterOutput();
        }
    }

    /// <summary>Total forest cell count.</summary>
    /// <returns>Forest cell count.</returns>
    public int GetForestCellCount() => forestCellCount;
    /// <summary>Percentage of grid covered by forest.</summary>
    /// <returns>Forest coverage %.</returns>
    public float GetForestCoveragePercentage() => forestCoveragePercentage;
    #endregion
}

[System.Serializable]
public struct CityStatsData
{
    public System.DateTime currentDate;
    public int population;
    public int money;
    public float happiness;
    public float pollution;
    public int residentialZoneCount;
    public int residentialBuildingCount;
    public int commercialZoneCount;
    public int commercialBuildingCount;
    public int industrialZoneCount;
    public int industrialBuildingCount;
    public int residentialLightBuildingCount;
    public int residentialLightZoningCount;
    public int residentialMediumBuildingCount;
    public int residentialMediumZoningCount;
    public int residentialHeavyBuildingCount;
    public int residentialHeavyZoningCount;
    public int commercialLightBuildingCount;
    public int commercialLightZoningCount;
    public int commercialMediumBuildingCount;
    public int commercialMediumZoningCount;
    public int commercialHeavyBuildingCount;
    public int commercialHeavyZoningCount;
    public int industrialLightBuildingCount;
    public int industrialLightZoningCount;
    public int industrialMediumBuildingCount;
    public int industrialMediumZoningCount;
    public int industrialHeavyBuildingCount;
    public int industrialHeavyZoningCount;
    public int roadCount;
    public int grassCount;
    public int cityPowerConsumption;
    public int cityPowerOutput;
    public int cityWaterConsumption;
    public int cityWaterOutput;
    public string cityName;

    // Forest statistics
    public int forestCellCount;
    public float forestCoveragePercentage;

    public bool simulateGrowth;
    public List<CommuneData> communes;
}
}
