using UnityEngine;
using Territory.Core;
using Territory.Forests;
using Territory.Zones;

namespace Territory.Economy
{
[System.Serializable]
public class DemandData
{
    [Range(-100f, 100f)]
    public float demandLevel; // -100 (oversupply) to +100 (high demand)
    public float trendDirection; // -1 (decreasing), 0 (stable), +1 (increasing)
    public string demandStatus;

    // Can this zone type grow based on current demand?
    public bool canGrow => demandLevel > 0f;

    public void UpdateStatus()
    {
        if (demandLevel > 75f) demandStatus = "Very High";
        else if (demandLevel > 50f) demandStatus = "High";
        else if (demandLevel > 25f) demandStatus = "Moderate";
        else if (demandLevel > -25f) demandStatus = "Balanced";
        else if (demandLevel > -50f) demandStatus = "Low";
        else if (demandLevel > -75f) demandStatus = "Very Low";
        else demandStatus = "Oversupplied";
    }
}

[System.Serializable]
public class BuildingTracker
{
    [Header("Zone Tracking")]
    public int residentialZonesWithoutBuildings;
    public int commercialZonesWithoutBuildings;
    public int industrialZonesWithoutBuildings;

    [Header("Building Tracking")]
    public int newResidentialBuildings; // Track new buildings since last demand update
    public int newCommercialBuildings;
    public int newIndustrialBuildings;

    public void Reset()
    {
        newResidentialBuildings = 0;
        newCommercialBuildings = 0;
        newIndustrialBuildings = 0;
    }
}

/// <summary>
/// Calculates residential, commercial, and industrial demand levels based on population,
/// employment ratios, forest coverage, and zone capacity. Provides demand data that drives
/// zone growth decisions in GrowthManager and AutoZoningManager. Updated periodically by
/// EmploymentManager and CityStats.
/// </summary>
public class DemandManager : MonoBehaviour
{
    [Header("RCI Demand")]
    public DemandData residentialDemand;
    public DemandData commercialDemand;
    public DemandData industrialDemand;

    [Header("Starting Demand")]
    public float startingResidentialDemand = 10f;
    public float startingIndustrialDemand = 10f;
    public float startingCommercialDemand = 0f;

    [Header("Demand Configuration")]
    [Tooltip("Fraction of distance to target per day. 0.2 = ~90% in ~10 days.")]
    public float demandSmoothingPerDay = 0.2f;
    public float demandDecayRate = 0.1f;

    [Header("Unemployment-Based Demand")]
    [Tooltip("Unemployment rate (%) above which residential demand drops and C/I demand rises.")]
    public float unemploymentThreshold = 15f;
    [Tooltip("Residential target reduction per % unemployment above threshold.")]
    public float unemploymentResidentialPenalty = 1.2f;
    [Tooltip("Commercial/Industrial target boost per % unemployment above threshold.")]
    public float unemploymentJobBoost = 1.2f;

    [Header("Cell Desirability")]
    [Tooltip("Multiplier for per-cell desirability in GetCellDesirabilityBonus. Used for geographic attraction, not demand.")]
    public float desirabilityDemandMultiplier = 0.1f;

    [Header("Building Tracking")]
    public BuildingTracker buildingTracker;

    [Header("Auto-Growth Settings")]
    public bool autoGrowthEnabled = true;
    public float growthThreshold = 5f; // Lower threshold since we start with 10

    private EmploymentManager employmentManager;
    private CityStats cityStats;
    private ForestManager forestManager;
    private GridManager gridManager;

    // Track previous values to detect new buildings
    private int previousResidentialBuildings = 0;
    private int previousCommercialBuildings = 0;
    private int previousIndustrialBuildings = 0;

    void Start()
    {
        InitializeDemand();
        employmentManager = FindObjectOfType<EmploymentManager>();
        cityStats = FindObjectOfType<CityStats>();
        forestManager = FindObjectOfType<ForestManager>();
        gridManager = FindObjectOfType<GridManager>();
        buildingTracker = new BuildingTracker();
    }

    private void InitializeDemand()
    {
        residentialDemand = new DemandData();
        commercialDemand = new DemandData();
        industrialDemand = new DemandData();

        // Set starting demand levels
        residentialDemand.demandLevel = startingResidentialDemand;
        commercialDemand.demandLevel = startingCommercialDemand;
        industrialDemand.demandLevel = startingIndustrialDemand;
    }

    public void UpdateRCIDemand(EmploymentManager employment)
    {
        UpdateBuildingTracking();
        UpdateDemandLogic();
        UpdateDemandStatus();
        buildingTracker.Reset(); // Reset new building counters after processing
    }

    private void UpdateBuildingTracking()
    {
        if (cityStats == null) return;

        // Calculate zones without buildings
        // Track current building totals (needed for both zone counting and new-building tracking)
        int currentResidentialBuildings = cityStats.residentialLightBuildingCount +
            cityStats.residentialMediumBuildingCount + cityStats.residentialHeavyBuildingCount;
        int currentCommercialBuildings = cityStats.commercialLightBuildingCount +
            cityStats.commercialMediumBuildingCount + cityStats.commercialHeavyBuildingCount;
        int currentIndustrialBuildings = cityStats.industrialLightBuildingCount +
            cityStats.industrialMediumBuildingCount + cityStats.industrialHeavyBuildingCount;

        // Calculate zones without buildings (total zones minus zones that already have buildings)
        buildingTracker.residentialZonesWithoutBuildings = Mathf.Max(0,
            (cityStats.residentialLightZoningCount + cityStats.residentialMediumZoningCount + cityStats.residentialHeavyZoningCount)
            - currentResidentialBuildings);

        buildingTracker.commercialZonesWithoutBuildings = Mathf.Max(0,
            (cityStats.commercialLightZoningCount + cityStats.commercialMediumZoningCount + cityStats.commercialHeavyZoningCount)
            - currentCommercialBuildings);

        buildingTracker.industrialZonesWithoutBuildings = Mathf.Max(0,
            (cityStats.industrialLightZoningCount + cityStats.industrialMediumZoningCount + cityStats.industrialHeavyZoningCount)
            - currentIndustrialBuildings);

        buildingTracker.newResidentialBuildings = Mathf.Max(0, currentResidentialBuildings - previousResidentialBuildings);
        buildingTracker.newCommercialBuildings = Mathf.Max(0, currentCommercialBuildings - previousCommercialBuildings);
        buildingTracker.newIndustrialBuildings = Mathf.Max(0, currentIndustrialBuildings - previousIndustrialBuildings);

        // Update previous counts
        previousResidentialBuildings = currentResidentialBuildings;
        previousCommercialBuildings = currentCommercialBuildings;
        previousIndustrialBuildings = currentIndustrialBuildings;
    }

    private void UpdateDemandLogic()
    {
        UpdateResidentialDemand();
        UpdateCommercialDemand();
        UpdateIndustrialDemand();
        ApplyHappinessModifier();
    }

    /// <summary>
    /// Scales all RCI demand levels by a happiness multiplier.
    /// Happiness 80 = 1.1x, happiness 50 = 1.0x, happiness 30 = 0.9x (linear remap from 0–100 to 0.8–1.2).
    /// </summary>
    private void ApplyHappinessModifier()
    {
        if (cityStats == null) return;
        float normalized = cityStats.GetNormalizedHappiness(); // 0–1
        float multiplier = 0.8f + normalized * 0.4f; // maps 0→0.8, 0.5→1.0, 1.0→1.2
        residentialDemand.demandLevel *= multiplier;
        commercialDemand.demandLevel *= multiplier;
        industrialDemand.demandLevel *= multiplier;

        residentialDemand.demandLevel = Mathf.Clamp(residentialDemand.demandLevel, -100f, 100f);
        commercialDemand.demandLevel = Mathf.Clamp(commercialDemand.demandLevel, -100f, 100f);
        industrialDemand.demandLevel = Mathf.Clamp(industrialDemand.demandLevel, -100f, 100f);
    }

    private void UpdateResidentialDemand()
    {
        int availableJobs = GetCurrentAvailableJobs();
        float unemploymentRate = GetUnemploymentRate();

        if (availableJobs <= 0)
        {
            // No jobs available → negative residential demand
            float targetDemand = -30f;
            residentialDemand.demandLevel = Mathf.Lerp(residentialDemand.demandLevel, targetDemand, demandSmoothingPerDay);
        }
        else
        {
            // Jobs are available → positive residential demand
            float targetDemand = startingResidentialDemand;

            // Rule: If there are industrial zones without buildings AND residential zones without buildings
            // → boost residential demand even more
            if (buildingTracker.industrialZonesWithoutBuildings > 0 &&
                buildingTracker.residentialZonesWithoutBuildings > 0)
            {
                targetDemand += 15f; // Extra boost
            }

            // Additional boost based on job availability
            if (availableJobs > 10)
            {
                targetDemand += 10f; // More jobs = higher demand
            }

            // High unemployment → reduce residential demand (need more jobs, not more housing)
            if (unemploymentRate > unemploymentThreshold)
            {
                float excessUnemployment = unemploymentRate - unemploymentThreshold;
                targetDemand -= excessUnemployment * unemploymentResidentialPenalty;
            }

            residentialDemand.demandLevel = Mathf.Lerp(residentialDemand.demandLevel, targetDemand, demandSmoothingPerDay);
        }

        residentialDemand.demandLevel = Mathf.Clamp(residentialDemand.demandLevel, -100f, 100f);
    }

    private void UpdateCommercialDemand()
    {
        float targetDemand = startingCommercialDemand;

        // Rule: If residential zoning is placed → commercial demand increases
        if (buildingTracker.residentialZonesWithoutBuildings > 0)
        {
            targetDemand += 10f;
        }

        // Rule: If residential buildings are placed → commercial demand increases
        if (buildingTracker.newResidentialBuildings > 0)
        {
            targetDemand += buildingTracker.newResidentialBuildings * 12f; // Strong boost per new building
        }

        // High unemployment → boost commercial demand (need more jobs)
        float unemploymentRate = GetUnemploymentRate();
        if (unemploymentRate > unemploymentThreshold)
        {
            float excessUnemployment = unemploymentRate - unemploymentThreshold;
            targetDemand += excessUnemployment * unemploymentJobBoost;
        }

        commercialDemand.demandLevel = Mathf.Lerp(commercialDemand.demandLevel, targetDemand, demandSmoothingPerDay);
        commercialDemand.demandLevel = Mathf.Clamp(commercialDemand.demandLevel, -100f, 100f);
    }

    private void UpdateIndustrialDemand()
    {
        float targetDemand = startingIndustrialDemand;

        // Rule: If residential buildings are placed → industrial demand increases
        if (buildingTracker.newResidentialBuildings > 0)
        {
            targetDemand += buildingTracker.newResidentialBuildings * 10f; // Boost per new residential building
        }

        // Early game bonus: encourage initial industrial development
        if (cityStats != null)
        {
            int totalIndustrialBuildings = cityStats.industrialLightBuildingCount +
                                         cityStats.industrialMediumBuildingCount +
                                         cityStats.industrialHeavyBuildingCount;

            if (totalIndustrialBuildings < 5) // First 5 industrial buildings
            {
                targetDemand = Mathf.Max(targetDemand, 25f);
            }
        }

        // High unemployment → boost industrial demand (need more jobs)
        float unemploymentRate = GetUnemploymentRate();
        if (unemploymentRate > unemploymentThreshold)
        {
            float excessUnemployment = unemploymentRate - unemploymentThreshold;
            targetDemand += excessUnemployment * unemploymentJobBoost;
        }

        industrialDemand.demandLevel = Mathf.Lerp(industrialDemand.demandLevel, targetDemand, demandSmoothingPerDay);
        industrialDemand.demandLevel = Mathf.Clamp(industrialDemand.demandLevel, -100f, 100f);
    }

    /// <summary>
    /// Environmental bonus no longer affects RCI demand. Kept for API compatibility; returns 0.
    /// Geographic attraction uses per-cell desirability in ZoneManager.GetWeightedSection.
    /// </summary>
    private float GetEnvironmentalDemandBonus()
    {
        return 0f;
    }

    /// <summary>
    /// Get demand bonus for a specific cell based on its desirability
    /// </summary>
    public float GetCellDesirabilityBonus(int x, int y)
    {
        if (gridManager == null) return 0f;
        Cell cellComponent = gridManager.GetCell(x, y);
        if (cellComponent != null)
            return cellComponent.desirability * desirabilityDemandMultiplier;
        return 0f;
    }

    private void UpdateDemandStatus()
    {
        residentialDemand.UpdateStatus();
        commercialDemand.UpdateStatus();
        industrialDemand.UpdateStatus();
    }

    // Public method to check if a zone type can grow (used by AutoZoningManager)
    public bool CanZoneTypeGrow(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
            case Zone.ZoneType.ResidentialMediumZoning:
            case Zone.ZoneType.ResidentialHeavyZoning:
                // For manual placement, we don't block based on demand, just check jobs
                return true; // Allow manual placement, but building spawning checks jobs

            case Zone.ZoneType.CommercialLightZoning:
            case Zone.ZoneType.CommercialMediumZoning:
            case Zone.ZoneType.CommercialHeavyZoning:
                return commercialDemand.canGrow;

            case Zone.ZoneType.IndustrialLightZoning:
            case Zone.ZoneType.IndustrialMediumZoning:
            case Zone.ZoneType.IndustrialHeavyZoning:
                return true; // No demand gate; RCI balance to be tuned separately

            default:
                return true; // Roads, grass, etc. can always be placed
        }
    }

    // Get available jobs from employment manager
    private int GetCurrentAvailableJobs()
    {
        if (employmentManager == null) return 0;
        return employmentManager.GetAvailableJobs();
    }

    private float GetUnemploymentRate()
    {
        return employmentManager != null ? employmentManager.unemploymentRate : 0f;
    }

    // Public getter for available jobs (for debugging)
    public int GetAvailableJobs()
    {
        return GetCurrentAvailableJobs();
    }

    // Special check for residential building placement (needs available jobs)
    public bool CanPlaceResidentialBuilding()
    {
        return GetCurrentAvailableJobs() > 0;
    }

    public bool CanPlaceCommercialOrIndustrialBuilding(Zone.ZoneType buildingType)
    {
        // Rule: Commercial/Industrial buildings can be placed if:
        // 1. Industrial buildings can always be placed initially (to create jobs)
        // 2. Commercial buildings need existing residents OR recent residential growth
        if (IsIndustrialBuilding(buildingType))
        {
            // Industrial buildings can always be placed (they create the initial job base)
            return true;
        }

        if (IsCommercialBuilding(buildingType))
        {
            // Commercial buildings need residents to serve
            return (cityStats != null && cityStats.residentialBuildingCount > 0) ||
                   buildingTracker.newResidentialBuildings > 0;
        }

        return true;
    }

    private bool IsCommercialBuilding(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding;
    }

    private bool IsIndustrialBuilding(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    // Get demand level for specific zone type (now includes environmental bonuses)
    public float GetDemandLevel(Zone.ZoneType zoneType)
    {
        float baseDemand = 0f;

        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
            case Zone.ZoneType.ResidentialMediumZoning:
            case Zone.ZoneType.ResidentialHeavyZoning:
                baseDemand = residentialDemand.demandLevel;
                break;

            case Zone.ZoneType.CommercialLightZoning:
            case Zone.ZoneType.CommercialMediumZoning:
            case Zone.ZoneType.CommercialHeavyZoning:
                baseDemand = commercialDemand.demandLevel;
                break;

            case Zone.ZoneType.IndustrialLightZoning:
            case Zone.ZoneType.IndustrialMediumZoning:
            case Zone.ZoneType.IndustrialHeavyZoning:
                baseDemand = industrialDemand.demandLevel;
                break;

            default:
                return 100f; // Always allow infrastructure
        }

        return baseDemand;
    }

    /// <summary>
    /// Maps demand level [-100, 100] to spawn probability factor [0, 1].
    /// Used by AutoZoningManager and ZoneManager for demand-weighted decisions.
    /// </summary>
    public float GetDemandSpawnFactor(Zone.ZoneType zoneType)
    {
        float level = GetDemandLevel(zoneType);
        return Mathf.Clamp01((level + 100f) / 200f);
    }

    // Public getters for UI
    public DemandData GetResidentialDemand() => residentialDemand;
    public DemandData GetCommercialDemand() => commercialDemand;
    public DemandData GetIndustrialDemand() => industrialDemand;

    // Settings for player control
    public void SetAutoGrowthEnabled(bool enabled) => autoGrowthEnabled = enabled;
    public void SetGrowthThreshold(float threshold) => growthThreshold = Mathf.Clamp(threshold, 0f, 100f);
    // Public getter for environmental bonus (for UI display)
    public float GetCurrentEnvironmentalBonus() => GetEnvironmentalDemandBonus();
}
}
