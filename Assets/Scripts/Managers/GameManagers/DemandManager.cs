using System;
using UnityEngine;
using Territory.Core;
using Territory.Forests;
using Territory.Zones;
using Domains.Demand.Services;

namespace Territory.Economy
{
[System.Serializable]
public class DemandData
{
    [Range(-100f, 100f)] public float demandLevel;
    public float trendDirection;
    public string demandStatus;
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
    [Header("Zone Tracking")] public int residentialZonesWithoutBuildings; public int commercialZonesWithoutBuildings; public int industrialZonesWithoutBuildings;
    [Header("Building Tracking")] public int newResidentialBuildings; public int newCommercialBuildings; public int newIndustrialBuildings;
    public void Reset() { newResidentialBuildings = 0; newCommercialBuildings = 0; newIndustrialBuildings = 0; }
}

/// <summary>
/// THIN hub — delegates RCI demand logic to <see cref="DemandService"/>. Serialized fields UNCHANGED (locked #3).
/// Invariant #11: UrbanizationProposal pre-scan complete (Stage 5.4) — zero runtime references found; flag in spec.
/// </summary>
public class DemandManager : MonoBehaviour
{
    [Header("RCI Demand")] public DemandData residentialDemand; public DemandData commercialDemand; public DemandData industrialDemand;
    [Header("Starting Demand")] public float startingResidentialDemand = 10f; public float startingIndustrialDemand = 10f; public float startingCommercialDemand = 0f;
    [Header("Demand Configuration")]
    [Tooltip("Fraction of distance to target per day. 0.2 = ~90% in ~10 days.")] public float demandSmoothingPerDay = 0.2f;
    public float demandDecayRate = 0.1f;
    [Header("Unemployment-Based Demand")]
    [Tooltip("Unemployment rate (%) above which residential demand drops and C/I demand rises.")] public float unemploymentThreshold = 15f;
    [Tooltip("Residential target reduction per % unemployment above threshold.")] public float unemploymentResidentialPenalty = 1.2f;
    [Tooltip("Commercial/Industrial target boost per % unemployment above threshold.")] public float unemploymentJobBoost = 1.2f;
    [Header("CityCell Desirability")]
    [Tooltip("Multiplier for per-cell desirability in GetCellDesirabilityBonus. Used for geographic attraction, not demand.")] public float desirabilityDemandMultiplier = 0.1f;
    [Header("Building Tracking")] public BuildingTracker buildingTracker;
    [Header("Auto-Growth Settings")] public bool autoGrowthEnabled = true; public float growthThreshold = 5f;
    [Header("Tax pressure on demand (per sector)")]
    [Tooltip("Rates at or below this (percent) do not reduce demand for that sector.")] [Range(0f, 30f)] public float comfortableTaxRateForDemand = 10f;
    [Tooltip("Upper tax rate (percent) used to normalize excess above the comfort threshold.")] [Range(20f, 100f)] public float maxTaxRateForDemandScale = 50f;
    [Tooltip("At maximum excess tax, demand for that sector is multiplied by (1 - this).")] [Range(0f, 0.6f)] public float taxDemandPenaltyAtMax = 0.35f;
    [Header("Parent stub integration")] [SerializeField] private GridManager _gridManager;

    private EmploymentManager employmentManager; private CityStats cityStats; private EconomyManager economyManager; private ForestManager forestManager;
    private int previousResidentialBuildings; private int previousCommercialBuildings; private int previousIndustrialBuildings;
    private DemandService _svc;

    private void Awake() { if (_gridManager == null) _gridManager = FindObjectOfType<GridManager>(); }

    void Start()
    {
        residentialDemand = new DemandData { demandLevel = startingResidentialDemand };
        commercialDemand  = new DemandData { demandLevel = startingCommercialDemand };
        industrialDemand  = new DemandData { demandLevel = startingIndustrialDemand };
        employmentManager = FindObjectOfType<EmploymentManager>();
        cityStats = FindObjectOfType<CityStats>();
        economyManager = FindObjectOfType<EconomyManager>();
        forestManager = FindObjectOfType<ForestManager>();
        buildingTracker = new BuildingTracker();
        _svc = new DemandService();
        _svc.WireDependencies(_gridManager, employmentManager, cityStats, economyManager,
            startingResidentialDemand, startingCommercialDemand, startingIndustrialDemand,
            demandSmoothingPerDay, unemploymentThreshold, unemploymentResidentialPenalty,
            unemploymentJobBoost, desirabilityDemandMultiplier,
            comfortableTaxRateForDemand, maxTaxRateForDemandScale, taxDemandPenaltyAtMax);
    }

    public void UpdateRCIDemand(EmploymentManager employment)
    {
        _svc.UpdateBuildingTracking(buildingTracker, ref previousResidentialBuildings, ref previousCommercialBuildings, ref previousIndustrialBuildings);
        _svc.UpdateRCIDemand(residentialDemand, commercialDemand, industrialDemand, buildingTracker);
        residentialDemand.UpdateStatus(); commercialDemand.UpdateStatus(); industrialDemand.UpdateStatus();
        buildingTracker.Reset();
    }

    public float GetExternalDemandModifier() => _svc != null ? _svc.GetExternalDemandModifier() : 1.0f;
    public float GetCellDesirabilityBonus(int x, int y) => _svc != null ? _svc.GetCellDesirabilityBonus(x, y) : 0f;

    public bool CanZoneTypeGrow(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.CommercialLightZoning: case Zone.ZoneType.CommercialMediumZoning: case Zone.ZoneType.CommercialHeavyZoning:
                return commercialDemand.canGrow;
            default: return true;
        }
    }

    public int GetAvailableJobs() => _svc != null ? _svc.GetAvailableJobs() : 0;
    public bool CanPlaceResidentialBuilding() => _svc != null && _svc.HasAvailableJobs();

    public bool CanPlaceCommercialOrIndustrialBuilding(Zone.ZoneType buildingType)
    {
        if (IsIndustrialBuilding(buildingType)) return true;
        if (IsCommercialBuilding(buildingType))
            return (cityStats != null && cityStats.residentialBuildingCount > 0) || buildingTracker.newResidentialBuildings > 0;
        return true;
    }

    private bool IsCommercialBuilding(Zone.ZoneType z) => z == Zone.ZoneType.CommercialLightBuilding || z == Zone.ZoneType.CommercialMediumBuilding || z == Zone.ZoneType.CommercialHeavyBuilding;
    private bool IsIndustrialBuilding(Zone.ZoneType z) => z == Zone.ZoneType.IndustrialLightBuilding || z == Zone.ZoneType.IndustrialMediumBuilding || z == Zone.ZoneType.IndustrialHeavyBuilding;

    public float GetDemandLevel(Zone.ZoneType zoneType) => _svc != null ? _svc.GetDemandLevel(zoneType, residentialDemand, commercialDemand, industrialDemand) : 0f;
    public float GetDemandSpawnFactor(Zone.ZoneType zoneType) => _svc != null ? _svc.GetDemandSpawnFactor(GetDemandLevel(zoneType)) : 0.5f;
    public DemandData GetResidentialDemand() => residentialDemand;
    public DemandData GetCommercialDemand()  => commercialDemand;
    public DemandData GetIndustrialDemand()  => industrialDemand;
    public void SetAutoGrowthEnabled(bool enabled) => autoGrowthEnabled = enabled;
    public void SetGrowthThreshold(float threshold) => growthThreshold = Mathf.Clamp(threshold, 0f, 100f);
    public float GetCurrentEnvironmentalBonus() => 0f;
}
}
