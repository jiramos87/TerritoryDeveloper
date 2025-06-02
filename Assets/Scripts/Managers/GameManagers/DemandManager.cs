using UnityEngine;

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
        if (demandLevel > 75f) demandStatus = "Very High Demand";
        else if (demandLevel > 50f) demandStatus = "High Demand";
        else if (demandLevel > 25f) demandStatus = "Moderate Demand";
        else if (demandLevel > -25f) demandStatus = "Balanced";
        else if (demandLevel > -50f) demandStatus = "Low Demand";
        else if (demandLevel > -75f) demandStatus = "Very Low Demand";
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
    public float demandSensitivity = 2.0f; // Increased for more responsive demand
    public float demandDecayRate = 0.1f;

    [Header("Environmental Demand Bonuses")]
    public float forestDemandBoostPerCell = 0.5f; // Demand boost per forest cell
    public float desirabilityDemandMultiplier = 0.1f; // How much desirability affects demand
    public float waterAvailabilityBonus = 5.0f; // Bonus when water is available

    [Header("Building Tracking")]
    public BuildingTracker buildingTracker;

    [Header("Auto-Growth Settings")]
    public bool autoGrowthEnabled = true;
    public float growthThreshold = 5f; // Lower threshold since we start with 10
    public float growthCooldown = 3f; // Faster growth for more responsive gameplay
    private float lastGrowthTime = 0f;

    private EmploymentManager employmentManager;
    private GrowthManager growthManager;
    private CityStats cityStats;
    private ForestManager forestManager;

    // Track previous values to detect new buildings
    private int previousResidentialBuildings = 0;
    private int previousCommercialBuildings = 0;
    private int previousIndustrialBuildings = 0;

    void Start()
    {
        InitializeDemand();
        employmentManager = FindObjectOfType<EmploymentManager>();
        growthManager = GetComponent<GrowthManager>();
        cityStats = FindObjectOfType<CityStats>();
        forestManager = FindObjectOfType<ForestManager>();
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
        CheckAutoGrowth();
        buildingTracker.Reset(); // Reset new building counters after processing
    }

    private void UpdateBuildingTracking()
    {
        if (cityStats == null) return;

        // Calculate zones without buildings
        buildingTracker.residentialZonesWithoutBuildings =
            (cityStats.residentialLightZoningCount + cityStats.residentialMediumZoningCount + cityStats.residentialHeavyZoningCount);

        buildingTracker.commercialZonesWithoutBuildings =
            (cityStats.commercialLightZoningCount + cityStats.commercialMediumZoningCount + cityStats.commercialHeavyZoningCount);

        buildingTracker.industrialZonesWithoutBuildings =
            (cityStats.industrialLightZoningCount + cityStats.industrialMediumZoningCount + cityStats.industrialHeavyZoningCount);

        // Track new buildings since last update
        int currentResidentialBuildings = cityStats.residentialLightBuildingCount +
            cityStats.residentialMediumBuildingCount + cityStats.residentialHeavyBuildingCount;
        int currentCommercialBuildings = cityStats.commercialLightBuildingCount +
            cityStats.commercialMediumBuildingCount + cityStats.commercialHeavyBuildingCount;
        int currentIndustrialBuildings = cityStats.industrialLightBuildingCount +
            cityStats.industrialMediumBuildingCount + cityStats.industrialHeavyBuildingCount;

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
    }

    private void UpdateResidentialDemand()
    {
        int availableJobs = GetCurrentAvailableJobs();
        float environmentalBonus = GetEnvironmentalDemandBonus();

        if (availableJobs <= 0)
        {
            // No jobs available → negative residential demand
            float targetDemand = -30f + environmentalBonus; // Environmental bonus can still help
            residentialDemand.demandLevel = Mathf.Lerp(residentialDemand.demandLevel, targetDemand, demandSensitivity * Time.deltaTime);
        }
        else
        {
            // Jobs are available → positive residential demand
            float targetDemand = startingResidentialDemand + environmentalBonus;

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

            residentialDemand.demandLevel = Mathf.Lerp(residentialDemand.demandLevel, targetDemand, demandSensitivity * Time.deltaTime);
        }

        residentialDemand.demandLevel = Mathf.Clamp(residentialDemand.demandLevel, -100f, 100f);
    }

    private void UpdateCommercialDemand()
    {
        float demandChange = 0f;
        float environmentalBonus = GetEnvironmentalDemandBonus();

        // Rule: If residential zoning is placed → commercial demand increases
        if (buildingTracker.residentialZonesWithoutBuildings > 0)
        {
            demandChange += 10f;
        }

        // Rule: If residential buildings are placed → commercial demand increases
        if (buildingTracker.newResidentialBuildings > 0)
        {
            demandChange += buildingTracker.newResidentialBuildings * 12f; // Strong boost per new building
        }

        // Apply environmental bonus
        demandChange += environmentalBonus;

        // Gradual decay toward 0 if no residential activity (but environmental bonus remains)
        if (demandChange == environmentalBonus) // Only environmental bonus, no other activity
        {
            demandChange += (environmentalBonus - commercialDemand.demandLevel) * demandDecayRate;
        }

        commercialDemand.demandLevel += demandChange * demandSensitivity * Time.deltaTime;
        commercialDemand.demandLevel = Mathf.Clamp(commercialDemand.demandLevel, -100f, 100f);
    }

    private void UpdateIndustrialDemand()
    {
        float targetDemand = startingIndustrialDemand;
        float environmentalBonus = GetEnvironmentalDemandBonus();

        // Rule: If residential buildings are placed → industrial demand increases
        if (buildingTracker.newResidentialBuildings > 0)
        {
            targetDemand += buildingTracker.newResidentialBuildings * 10f; // Boost per new residential building
        }

        // Apply environmental bonus
        targetDemand += environmentalBonus;

        // Early game bonus: encourage initial industrial development
        if (cityStats != null)
        {
            int totalIndustrialBuildings = cityStats.industrialLightBuildingCount +
                                         cityStats.industrialMediumBuildingCount +
                                         cityStats.industrialHeavyBuildingCount;

            if (totalIndustrialBuildings < 5) // First 5 industrial buildings
            {
                targetDemand = Mathf.Max(targetDemand, 25f + environmentalBonus); // Ensure good demand for early industrial
            }
        }

        industrialDemand.demandLevel = Mathf.Lerp(industrialDemand.demandLevel, targetDemand, demandSensitivity * Time.deltaTime);
        industrialDemand.demandLevel = Mathf.Clamp(industrialDemand.demandLevel, -100f, 100f);
    }

    /// <summary>
    /// Calculate environmental bonus to demand based on forests and other factors
    /// </summary>
    private float GetEnvironmentalDemandBonus()
    {
        float bonus = 0f;

        // Forest bonus
        if (forestManager != null)
        {
            var forestStats = forestManager.GetForestStatistics();
            bonus += forestStats.totalForestCells * forestDemandBoostPerCell;
        }

        // Water availability bonus
        if (cityStats != null && cityStats.GetCityWaterAvailability())
        {
            bonus += waterAvailabilityBonus;
        }

        // Power availability bonus (existing infrastructure should boost demand)
        if (cityStats != null && cityStats.GetCityPowerAvailability())
        {
            bonus += 3.0f; // Small bonus for power availability
        }

        return bonus;
    }

    /// <summary>
    /// Get demand bonus for a specific cell based on its desirability
    /// </summary>
    public float GetCellDesirabilityBonus(int x, int y)
    {
        GameObject cell = FindObjectOfType<GridManager>().gridArray[x, y];
        if (cell != null)
        {
            Cell cellComponent = cell.GetComponent<Cell>();
            if (cellComponent != null)
            {
                return cellComponent.desirability * desirabilityDemandMultiplier;
            }
        }
        return 0f;
    }

    private void UpdateDemandStatus()
    {
        residentialDemand.UpdateStatus();
        commercialDemand.UpdateStatus();
        industrialDemand.UpdateStatus();
    }

    private void CheckAutoGrowth()
    {
        if (!autoGrowthEnabled || growthManager == null) return;
        if (Time.time - lastGrowthTime < growthCooldown) return;

        // Prioritize residential growth first
        if (residentialDemand.canGrow && residentialDemand.demandLevel > growthThreshold)
        {
            growthManager.TriggerAutoGrowth(Zone.ZoneType.ResidentialLightZoning);
            lastGrowthTime = Time.time;
        }
        else if (commercialDemand.canGrow && commercialDemand.demandLevel > growthThreshold)
        {
            growthManager.TriggerAutoGrowth(Zone.ZoneType.CommercialLightZoning);
            lastGrowthTime = Time.time;
        }
        else if (industrialDemand.canGrow && industrialDemand.demandLevel > growthThreshold)
        {
            growthManager.TriggerAutoGrowth(Zone.ZoneType.IndustrialLightZoning);
            lastGrowthTime = Time.time;
        }
    }

    // Public method to check if a zone type can grow (used for auto-growth)
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
                return industrialDemand.canGrow;

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

        // Environmental bonuses are already included in the base demand calculations
        return baseDemand;
    }

    // Public getters for UI
    public DemandData GetResidentialDemand() => residentialDemand;
    public DemandData GetCommercialDemand() => commercialDemand;
    public DemandData GetIndustrialDemand() => industrialDemand;

    // Settings for player control
    public void SetAutoGrowthEnabled(bool enabled) => autoGrowthEnabled = enabled;
    public void SetGrowthThreshold(float threshold) => growthThreshold = Mathf.Clamp(threshold, 0f, 100f);
    public void SetGrowthCooldown(float cooldown) => growthCooldown = Mathf.Max(1f, cooldown);

    // Public getter for environmental bonus (for UI display)
    public float GetCurrentEnvironmentalBonus() => GetEnvironmentalDemandBonus();
}
