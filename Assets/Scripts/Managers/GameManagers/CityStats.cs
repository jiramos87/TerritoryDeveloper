using UnityEngine;
using System.Collections.Generic;
using Territory.Timing;
using Territory.Terrain;
using Territory.Forests;
using Territory.Buildings;
using Territory.Core;
using Territory.Zones;

namespace Territory.Economy
{
/// <summary>
/// Global statistics aggregator for the city. Tracks population, money, happiness, employment,
/// water/power capacity and consumption, zone counts, and resource budgets. Many managers read
/// from CityStats to make decisions. Updated by TimeManager, WaterManager, and ForestManager.
/// </summary>
public class CityStats : MonoBehaviour, ICityStats
{
    #region Dependencies
    public TimeManager timeManager;
    public WaterManager waterManager;
    public ForestManager forestManager;
    private EmploymentManager _employmentManager;
    private EconomyManager _economyManager;
    private StatisticsManager _statisticsManager;
    #endregion

    #region City Data Fields
    public System.DateTime currentDate;

    public int population;
    public int money;

    public float happiness = 50f;
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
    #endregion

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
    }

    /// <summary>
    /// Adds (or subtracts, if negative) the given amount to the city population.
    /// </summary>
    /// <param name="value">The population delta.</param>
    public void AddPopulation(int value)
    {
        population += value;
    }

    /// <summary>
    /// Adds the specified amount to the city treasury.
    /// </summary>
    /// <param name="value">The amount of money to add.</param>
    public void AddMoney(int value)
    {
        money += value;
    }

    /// <summary>
    /// Subtracts the specified amount from the city treasury.
    /// </summary>
    /// <param name="value">The amount of money to remove.</param>
    public void RemoveMoney(int value)
    {
        money -= value;
    }

    #endregion

    #region Zone Statistics
    /// <summary>
    /// Increments the residential zone count by one.
    /// </summary>
    public void AddResidentialZoneCount()
    {
        residentialZoneCount++;
    }

    /// <summary>
    /// Decrements the residential zone count by one.
    /// </summary>
    public void RemoveResidentialZoneCount()
    {
        residentialZoneCount--;
    }

    /// <summary>
    /// Increments the residential building count by one.
    /// </summary>
    public void AddResidentialBuildingCount()
    {
        residentialBuildingCount++;
    }

    /// <summary>
    /// Decrements the residential building count by one.
    /// </summary>
    public void RemoveResidentialBuildingCount()
    {
        residentialBuildingCount--;
    }

    /// <summary>
    /// Increments the commercial zone count by one.
    /// </summary>
    public void AddCommercialZoneCount()
    {
        commercialZoneCount++;
    }

    /// <summary>
    /// Decrements the commercial zone count by one.
    /// </summary>
    public void RemoveCommercialZoneCount()
    {
        commercialZoneCount--;
    }

    /// <summary>
    /// Increments the commercial building count by one.
    /// </summary>
    public void AddCommercialBuildingCount()
    {
        commercialBuildingCount++;
    }

    /// <summary>
    /// Decrements the commercial building count by one.
    /// </summary>
    public void RemoveCommercialBuildingCount()
    {
        commercialBuildingCount--;
    }

    /// <summary>
    /// Increments the industrial zone count by one.
    /// </summary>
    public void AddIndustrialZoneCount()
    {
        industrialZoneCount++;
    }

    /// <summary>
    /// Decrements the industrial zone count by one.
    /// </summary>
    public void RemoveIndustrialZoneCount()
    {
        industrialZoneCount--;
    }

    /// <summary>
    /// Increments the industrial building count by one.
    /// </summary>
    public void AddIndustrialBuildingCount()
    {
        industrialBuildingCount++;
    }

    /// <summary>
    /// Decrements the industrial building count by one.
    /// </summary>
    public void RemoveIndustrialBuildingCount()
    {
        industrialBuildingCount--;
    }

    /// <summary>
    /// Increments the residential light building count and the aggregate residential building count.
    /// </summary>
    public void AddResidentialLightBuildingCount()
    {
        residentialLightBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>
    /// Decrements the residential light building count and the aggregate residential building count.
    /// </summary>
    public void RemoveResidentialLightBuildingCount()
    {
        residentialLightBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>
    /// Increments the residential light zoning count and the aggregate residential zone count.
    /// </summary>
    public void AddResidentialLightZoningCount()
    {
        residentialLightZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>
    /// Decrements the residential light zoning count and the aggregate residential zone count.
    /// </summary>
    public void RemoveResidentialLightZoningCount()
    {
        residentialLightZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>
    /// Increments the residential medium building count and the aggregate residential building count.
    /// </summary>
    public void AddResidentialMediumBuildingCount()
    {
        residentialMediumBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>
    /// Decrements the residential medium building count and the aggregate residential building count.
    /// </summary>
    public void RemoveResidentialMediumBuildingCount()
    {
        residentialMediumBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>
    /// Increments the residential medium zoning count and the aggregate residential zone count.
    /// </summary>
    public void AddResidentialMediumZoningCount()
    {
        residentialMediumZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>
    /// Decrements the residential medium zoning count and the aggregate residential zone count.
    /// </summary>
    public void RemoveResidentialMediumZoningCount()
    {
        residentialMediumZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>
    /// Increments the residential heavy building count and the aggregate residential building count.
    /// </summary>
    public void AddResidentialHeavyBuildingCount()
    {
        residentialHeavyBuildingCount++;
        AddResidentialBuildingCount();
    }

    /// <summary>
    /// Decrements the residential heavy building count and the aggregate residential building count.
    /// </summary>
    public void RemoveResidentialHeavyBuildingCount()
    {
        residentialHeavyBuildingCount--;
        RemoveResidentialBuildingCount();
    }

    /// <summary>
    /// Increments the residential heavy zoning count and the aggregate residential zone count.
    /// </summary>
    public void AddResidentialHeavyZoningCount()
    {
        residentialHeavyZoningCount++;
        AddResidentialZoneCount();
    }

    /// <summary>
    /// Decrements the residential heavy zoning count and the aggregate residential zone count.
    /// </summary>
    public void RemoveResidentialHeavyZoningCount()
    {
        residentialHeavyZoningCount--;
        RemoveResidentialZoneCount();
    }

    /// <summary>
    /// Increments the commercial light building count and the aggregate commercial building count.
    /// </summary>
    public void AddCommercialLightBuildingCount()
    {
        commercialLightBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>
    /// Decrements the commercial light building count and the aggregate commercial building count.
    /// </summary>
    public void RemoveCommercialLightBuildingCount()
    {
        commercialLightBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>
    /// Increments the commercial light zoning count and the aggregate commercial zone count.
    /// </summary>
    public void AddCommercialLightZoningCount()
    {
        commercialLightZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>
    /// Decrements the commercial light zoning count and the aggregate commercial zone count.
    /// </summary>
    public void RemoveCommercialLightZoningCount()
    {
        commercialLightZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>
    /// Increments the commercial medium building count and the aggregate commercial building count.
    /// </summary>
    public void AddCommercialMediumBuildingCount()
    {
        commercialMediumBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>
    /// Decrements the commercial medium building count and the aggregate commercial building count.
    /// </summary>
    public void RemoveCommercialMediumBuildingCount()
    {
        commercialMediumBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>
    /// Increments the commercial medium zoning count and the aggregate commercial zone count.
    /// </summary>
    public void AddCommercialMediumZoningCount()
    {
        commercialMediumZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>
    /// Decrements the commercial medium zoning count and the aggregate commercial zone count.
    /// </summary>
    public void RemoveCommercialMediumZoningCount()
    {
        commercialMediumZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>
    /// Increments the commercial heavy building count and the aggregate commercial building count.
    /// </summary>
    public void AddCommercialHeavyBuildingCount()
    {
        commercialHeavyBuildingCount++;
        AddCommercialBuildingCount();
    }

    /// <summary>
    /// Decrements the commercial heavy building count and the aggregate commercial building count.
    /// </summary>
    public void RemoveCommercialHeavyBuildingCount()
    {
        commercialHeavyBuildingCount--;
        RemoveCommercialBuildingCount();
    }

    /// <summary>
    /// Increments the commercial heavy zoning count and the aggregate commercial zone count.
    /// </summary>
    public void AddCommercialHeavyZoningCount()
    {
        commercialHeavyZoningCount++;
        AddCommercialZoneCount();
    }

    /// <summary>
    /// Decrements the commercial heavy zoning count and the aggregate commercial zone count.
    /// </summary>
    public void RemoveCommercialHeavyZoningCount()
    {
        commercialHeavyZoningCount--;
        RemoveCommercialZoneCount();
    }

    /// <summary>
    /// Increments the industrial light building count and the aggregate industrial building count.
    /// </summary>
    public void AddIndustrialLightBuildingCount()
    {
        industrialLightBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>
    /// Decrements the industrial light building count and the aggregate industrial building count.
    /// </summary>
    public void RemoveIndustrialLightBuildingCount()
    {
        industrialLightBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>
    /// Increments the industrial light zoning count and the aggregate industrial zone count.
    /// </summary>
    public void AddIndustrialLightZoningCount()
    {
        industrialLightZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>
    /// Decrements the industrial light zoning count and the aggregate industrial zone count.
    /// </summary>
    public void RemoveIndustrialLightZoningCount()
    {
        industrialLightZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>
    /// Increments the industrial medium building count and the aggregate industrial building count.
    /// </summary>
    public void AddIndustrialMediumBuildingCount()
    {
        industrialMediumBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>
    /// Decrements the industrial medium building count and the aggregate industrial building count.
    /// </summary>
    public void RemoveIndustrialMediumBuildingCount()
    {
        industrialMediumBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>
    /// Increments the industrial medium zoning count and the aggregate industrial zone count.
    /// </summary>
    public void AddIndustrialMediumZoningCount()
    {
        industrialMediumZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>
    /// Decrements the industrial medium zoning count and the aggregate industrial zone count.
    /// </summary>
    public void RemoveIndustrialMediumZoningCount()
    {
        industrialMediumZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>
    /// Increments the industrial heavy building count and the aggregate industrial building count.
    /// </summary>
    public void AddIndustrialHeavyBuildingCount()
    {
        industrialHeavyBuildingCount++;
        AddIndustrialBuildingCount();
    }

    /// <summary>
    /// Decrements the industrial heavy building count and the aggregate industrial building count.
    /// </summary>
    public void RemoveIndustrialHeavyBuildingCount()
    {
        industrialHeavyBuildingCount--;
        RemoveIndustrialBuildingCount();
    }

    /// <summary>
    /// Increments the industrial heavy zoning count and the aggregate industrial zone count.
    /// </summary>
    public void AddIndustrialHeavyZoningCount()
    {
        industrialHeavyZoningCount++;
        AddIndustrialZoneCount();
    }

    /// <summary>
    /// Decrements the industrial heavy zoning count and the aggregate industrial zone count.
    /// </summary>
    public void RemoveIndustrialHeavyZoningCount()
    {
        industrialHeavyZoningCount--;
        RemoveIndustrialZoneCount();
    }

    /// <summary>
    /// Increments the road count by one.
    /// </summary>
    public void AddRoadCount()
    {
        roadCount++;
    }

    /// <summary>
    /// Increments the grass tile count by one.
    /// </summary>
    public void AddGrassCount()
    {
        grassCount++;
    }

    /// <summary>
    /// Increments the appropriate zone or building counter for the given zone type.
    /// </summary>
    /// <param name="zoneType">The zone type whose counter should be incremented.</param>
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

    /// <summary>
    /// Decrements the appropriate zone or building counter for the given zone type.
    /// </summary>
    /// <param name="zoneType">The zone type whose counter should be decremented.</param>
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
    /// <summary>
    /// Returns true if the city treasury has at least the specified amount.
    /// </summary>
    /// <param name="cost">The cost to check against.</param>
    /// <returns>True if the city can afford the cost.</returns>
    public bool CanAfford(int cost)
    {
        return money >= cost;
    }

    /// <summary>
    /// Registers a power plant and recalculates total city power output.
    /// </summary>
    /// <param name="powerPlant">The power plant to register.</param>
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

    /// <summary>
    /// Unregisters a power plant and recalculates total city power output.
    /// </summary>
    /// <param name="powerPlant">The power plant to unregister.</param>
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

    /// <summary>
    /// Clears all registered power plants and resets city power output to zero.
    /// </summary>
    public void ResetPowerPlants()
    {
        powerPlants.Clear();
        cityPowerOutput = 0;
    }

    /// <summary>
    /// Returns how many power plants are registered for output aggregation (used for upkeep and pollution).
    /// </summary>
    public int GetRegisteredPowerPlantCount()
    {
        return powerPlants.Count;
    }

    /// <summary>
    /// Returns the total power output from all registered power plants.
    /// </summary>
    /// <returns>Total city power output.</returns>
    public int GetTotalPowerOutput()
    {
        return cityPowerOutput;
    }

    /// <summary>
    /// Adds the specified amount to the city's total power consumption.
    /// </summary>
    /// <param name="value">The power consumption to add.</param>
    public void AddPowerConsumption(int value)
    {
        cityPowerConsumption += value;
    }

    /// <summary>
    /// Subtracts the specified amount from the city's total power consumption.
    /// </summary>
    /// <param name="value">The power consumption to remove.</param>
    public void RemovePowerConsumption(int value)
    {
        cityPowerConsumption -= value;
    }

    /// <summary>
    /// Returns the total power consumption across the city.
    /// </summary>
    /// <returns>Total city power consumption.</returns>
    public int GetTotalPowerConsumption()
    {
        return cityPowerConsumption;
    }
    #endregion

    #region Update Methods
    /// <summary>
    /// Performs monthly update logic (placeholder for future monthly calculations).
    /// </summary>
    public void PerformMonthlyUpdates()
    {

    }

    /// <summary>
    /// Performs daily update logic: syncs the date, updates employment, statistics, and forest data.
    /// </summary>
    public void PerformDailyUpdates()
    {
        currentDate = timeManager.GetCurrentDate();

        if (_employmentManager != null) _employmentManager.UpdateEmployment();
        if (_statisticsManager != null) _statisticsManager.UpdateStatistics();

        // Update forest statistics
        UpdateForestStatistics();

        // Recalculate pollution then happiness (order matters: pollution feeds into happiness)
        RecalculatePollution();
        RecalculateHappiness();

        // Demand uses same-tick happiness targets and tax rates (see EmploymentManager.RefreshRCIDemandAfterDailyStats)
        if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats();
    }

    /// <summary>
    /// Re-runs pollution, happiness, and R/C/I demand when tax or other policy inputs change from the UI mid-day
    /// (daily ticks already call <see cref="PerformDailyUpdates"/>).
    /// </summary>
    public void RefreshHappinessAfterPolicyChange()
    {
        RecalculatePollution();
        RecalculateHappiness();
        if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats();
    }

    /// <summary>
    /// Returns true if the city's power output exceeds its power consumption.
    /// </summary>
    /// <returns>True if power supply is sufficient.</returns>
    public bool GetCityPowerAvailability()
    {
        return cityPowerOutput > cityPowerConsumption;
    }
    #endregion

    #region Economy
    /// <summary>
    /// Applies the cost, population, happiness, power, and water effects of placing a new zone or building.
    /// </summary>
    /// <param name="zoneType">The type of zone or building being placed.</param>
    /// <param name="zoneAttributes">The attributes defining costs and stat contributions.</param>
    public void HandleZoneBuildingPlacement(Zone.ZoneType zoneType, ZoneAttributes zoneAttributes)
    {
        RemoveMoney(zoneAttributes.ConstructionCost);
        AddPopulation(zoneAttributes.Population);
        AddZoneBuildingCount(zoneType);
        AddPowerConsumption(zoneAttributes.PowerConsumption);
        AddWaterConsumption(zoneAttributes.WaterConsumption);
    }

    /// <summary>
    /// Reverses the stat effects of a demolished building, refunding a portion of the construction cost.
    /// </summary>
    /// <param name="zoneType">The type of zone or building being demolished.</param>
    /// <param name="zoneAttributes">The attributes used to reverse stat contributions.</param>
    public void HandleBuildingDemolition(Zone.ZoneType zoneType, ZoneAttributes zoneAttributes)
    {
        AddMoney(zoneAttributes.ConstructionCost / 5);
        AddPopulation(-zoneAttributes.Population);
        RemoveZoneBuildingCount(zoneType);
        RemovePowerConsumption(zoneAttributes.PowerConsumption);
        RemoveWaterConsumption(zoneAttributes.WaterConsumption);
    }

    /// <summary>
    /// Update forest statistics (called by ForestManager)
    /// </summary>
    public void UpdateForestStats(ForestStatistics forestStats)
    {
        forestCellCount = forestStats.totalForestCells;
        forestCoveragePercentage = forestStats.forestCoveragePercentage;
    }

    /// <summary>
    /// Update forest statistics from ForestManager
    /// </summary>
    private void UpdateForestStatistics()
    {
        if (forestManager != null)
        {
            var forestStats = forestManager.GetForestStatistics();
            UpdateForestStats(forestStats);
        }
    }

    /// <summary>
    /// Returns the forest happiness bonus normalized to 0–1.
    /// Diminishing returns above 20 forest cells; capped at maxForestBonus.
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
    /// Recalculates city-wide pollution from industrial buildings and power plants,
    /// minus forest absorption. Called once per simulation tick before RecalculateHappiness.
    /// </summary>
    public void RecalculatePollution()
    {
        float rawPollution =
            industrialHeavyBuildingCount * POLLUTION_INDUSTRIAL_HEAVY +
            industrialMediumBuildingCount * POLLUTION_INDUSTRIAL_MEDIUM +
            industrialLightBuildingCount * POLLUTION_INDUSTRIAL_LIGHT;

        // All power plants currently use nuclear; add per-type when more types ship
        int nuclearCount = powerPlants.Count;
        rawPollution += nuclearCount * POLLUTION_NUCLEAR;

        float forestAbsorption = Mathf.Min(forestCellCount * FOREST_ABSORPTION_RATE, rawPollution);
        pollution = Mathf.Max(rawPollution - forestAbsorption, 0f);
    }

    /// <summary>
    /// Computes the immediate happiness target from employment, tax, services, forest, development, and pollution.
    /// Used for display convergence and for same-tick demand feedback.
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
    /// Recalculates happiness as a 0–100 score from weighted factors, converging smoothly.
    /// Called once per simulation tick after employment, economy, and pollution updates.
    /// </summary>
    public void RecalculateHappiness()
    {
        float targetHappiness = ComputeTargetHappiness();

        // Convergence rate scales with population (larger cities change more slowly)
        float convergenceRate = BASE_CONVERGENCE_RATE / (1f + population / POPULATION_SCALE_FACTOR);
        happiness = Mathf.Lerp(happiness, targetHappiness, convergenceRate);
    }

    /// <summary>
    /// Returns happiness normalized to 0–1 for UI or diagnostics (current converged value).
    /// </summary>
    public float GetNormalizedHappiness()
    {
        return Mathf.Clamp01(happiness / 100f);
    }

    /// <summary>
    /// Demand multiplier derived from <b>today's</b> happiness target (not the lerped score), so tax and
    /// employment changes affect R/C/I demand on the same daily tick.
    /// </summary>
    public float GetHappinessDemandMultiplier()
    {
        float normalized = Mathf.Clamp01(ComputeTargetHappiness() / 100f);
        return DEMAND_HAPPINESS_MULT_MIN + normalized * (DEMAND_HAPPINESS_MULT_MAX - DEMAND_HAPPINESS_MULT_MIN);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Snapshots all city statistics into a serializable data struct for saving.
    /// </summary>
    /// <returns>A CityStatsData struct containing all current city statistics.</returns>
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

    /// <summary>
    /// Restores all city statistics from a previously saved data struct.
    /// </summary>
    /// <param name="cityStatsData">The saved data to restore from.</param>
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

    /// <summary>
    /// Resets all city statistics to their default values (new game state).
    /// </summary>
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
    /// Set the city name (e.g. when the player renames the city). This becomes the canonical name until changed again or loaded from save.
    /// </summary>
    public void SetCityName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            cityName = newName.Trim();
    }

    /// <summary>
    /// Finds and returns the EmploymentManager in the scene.
    /// </summary>
    /// <returns>The EmploymentManager instance, or null if not found.</returns>
    public EmploymentManager GetEmploymentManager() => _employmentManager;

    /// <summary>
    /// Adds the specified amount to both the local and WaterManager water consumption trackers.
    /// </summary>
    /// <param name="value">The water consumption to add.</param>
    public void AddWaterConsumption(int value)
    {
        cityWaterConsumption += value;
        if (waterManager != null)
        {
            waterManager.AddWaterConsumption(value);
        }
    }

    /// <summary>
    /// Subtracts the specified amount from both the local and WaterManager water consumption trackers.
    /// </summary>
    /// <param name="value">The water consumption to remove.</param>
    public void RemoveWaterConsumption(int value)
    {
        cityWaterConsumption -= value;
        if (waterManager != null)
        {
            waterManager.RemoveWaterConsumption(value);
        }
    }

    /// <summary>
    /// Returns the total water consumption across the city.
    /// </summary>
    /// <returns>Total city water consumption.</returns>
    public int GetTotalWaterConsumption()
    {
        return cityWaterConsumption;
    }

    /// <summary>
    /// Returns the total water output across the city.
    /// </summary>
    /// <returns>Total city water output.</returns>
    public int GetTotalWaterOutput()
    {
        return cityWaterOutput;
    }

    /// <summary>
    /// Returns true if the city's water output exceeds its water consumption. Syncs with WaterManager first.
    /// </summary>
    /// <returns>True if water supply is sufficient.</returns>
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

    /// <summary>
    /// Syncs the local water output value from the WaterManager.
    /// </summary>
    public void UpdateWaterOutput()
    {
        if (waterManager != null)
        {
            cityWaterOutput = waterManager.GetTotalWaterOutput();
        }
    }

    /// <summary>
    /// Returns the total number of forest cells in the city.
    /// </summary>
    /// <returns>Forest cell count.</returns>
    public int GetForestCellCount() => forestCellCount;
    /// <summary>
    /// Returns the percentage of the grid covered by forest.
    /// </summary>
    /// <returns>Forest coverage as a percentage.</returns>
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
