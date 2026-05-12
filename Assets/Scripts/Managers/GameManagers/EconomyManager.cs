using System.Collections.Generic;
using UnityEngine;
using Territory.Audio;
using Territory.Simulation.Signals;
using Territory.Zones;
using Territory.UI;
using Territory.Timing;
using Territory.Utilities;
using Domains.Economy.Services;

namespace Territory.Economy
{
/// <summary>
/// THIN hub — delegates economy logic to <see cref="EconomyService"/>. Serialized fields UNCHANGED (locked #3).
/// Manages city economy: taxation, monthly maintenance, money, financial transactions.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    [Header("Manager References")]
    public CityStats cityStats;
    public TimeManager timeManager;
    public GameNotificationManager gameNotificationManager;
    /// <summary>Enforces non-negative treasury clamp on spend operations.</summary>
    [SerializeField] private TreasuryFloorClampService treasuryFloorClamp;
    /// <summary>Allocates tax revenue across budget categories (R/C/I multipliers).</summary>
    [SerializeField] private BudgetAllocationService budgetAllocation;
    /// <summary>Stage 7 (TECH-1892) — signal tuning weights asset; consulted at GetProjectedMonthlyIncome.</summary>
    [SerializeField] private SignalTuningWeightsAsset tuningWeights;

    [Header("Monthly maintenance")]
    [Tooltip("Upkeep charged per road cell (ZoneType.Road) on the first day of each month, after tax collection.")]
    public int maintenanceCostPerRoadCell = 8;
    [Tooltip("Upkeep charged per registered power plant on the first day of each month, after tax collection.")]
    public int maintenanceCostPerPowerPlant = 350;

    /// <summary>Ordered registry of maintenance contributors.</summary>
    private readonly List<IMaintenanceContributor> maintenanceContributors = new List<IMaintenanceContributor>();

    [Header("Tax Rates")]
    public int residentialIncomeTax = 10;
    public int commercialIncomeTax = 10;
    public int industrialIncomeTax = 10;

    [Header("Tax Rate Limits")]
    public int minTaxRate = 0;
    public int maxTaxRate = 50;

    private EconomyService _svc;

    void Awake()
    {
        if (treasuryFloorClamp == null) treasuryFloorClamp = FindObjectOfType<TreasuryFloorClampService>();
        if (treasuryFloorClamp == null) Debug.LogWarning("EconomyManager: TreasuryFloorClampService not found.");
        if (budgetAllocation == null)   budgetAllocation   = FindObjectOfType<BudgetAllocationService>();
        if (budgetAllocation == null)   Debug.LogWarning("EconomyManager: BudgetAllocationService not found.");
        if (tuningWeights == null)      tuningWeights      = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
    }

    void Start()
    {
        if (cityStats == null)              cityStats              = FindObjectOfType<CityStats>();
        if (timeManager == null)            timeManager            = FindObjectOfType<TimeManager>();
        if (gameNotificationManager == null) gameNotificationManager = FindObjectOfType<GameNotificationManager>();
        _svc = new EconomyService();
        _svc.WireDependencies(cityStats, gameNotificationManager, treasuryFloorClamp, budgetAllocation,
            tuningWeights, maintenanceCostPerRoadCell, maintenanceCostPerPowerPlant, minTaxRate, maxTaxRate);
    }

    // ── Daily / monthly lifecycle ────────────────────────────────────────────────

    /// <summary>Process daily economic activities.</summary>
    public void ProcessDailyEconomy()
    {
        if (timeManager != null && timeManager.GetCurrentDate().Day == 1)
            _svc?.ProcessMonthlyEconomy(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax, maintenanceContributors);
    }

    // ── Maintenance Contributor Registry ────────────────────────────────────────

    /// <summary>Register a maintenance contributor. Duplicates silently ignored.</summary>
    public void RegisterMaintenanceContributor(IMaintenanceContributor contributor)
    {
        if (contributor == null) return;
        if (!maintenanceContributors.Contains(contributor))
            maintenanceContributors.Add(contributor);
    }

    /// <summary>Unregister a maintenance contributor. No-op if not found.</summary>
    public void UnregisterMaintenanceContributor(IMaintenanceContributor contributor)
    {
        if (contributor == null) return;
        maintenanceContributors.Remove(contributor);
    }

    /// <summary>Clear all registered contributors. Call on scene load / save restore.</summary>
    public void ClearMaintenanceContributors() => maintenanceContributors.Clear();

    /// <summary>Current contributor count. Exposed for tests.</summary>
    public int MaintenanceContributorCount => maintenanceContributors.Count;

    /// <summary>Snapshot of registered contributors for sorted iteration.</summary>
    public List<IMaintenanceContributor> GetMaintenanceContributorsSnapshot()
        => new List<IMaintenanceContributor>(maintenanceContributors);

    // ── Money Management ─────────────────────────────────────────────────────────

    /// <summary>Spend money from city treasury. Returns true on success.</summary>
    public bool SpendMoney(int amount, string contextForInsufficientFunds = null, bool notifyInsufficientFunds = true)
        => _svc != null && _svc.SpendMoney(amount, contextForInsufficientFunds, notifyInsufficientFunds);

    /// <summary>Set initial treasury before the first sim tick.</summary>
    public void SetStartingFunds(int amount) => _svc?.SetStartingFunds(amount);

    /// <summary>Add money to city treasury.</summary>
    public void AddMoney(int amount) => _svc?.AddMoney(amount);

    /// <summary>Get current money in city treasury.</summary>
    public int GetCurrentMoney() => _svc != null ? _svc.GetCurrentMoney() : 0;

    /// <summary>Check if city can afford amount.</summary>
    public bool CanAfford(int amount) => _svc != null && _svc.CanAfford(amount);

    /// <summary>Transfer money. Returns true on success.</summary>
    public bool TransferMoney(int amount, string description = "")
        => _svc != null && _svc.TransferMoney(amount, description);

    // ── Tax Management + Getters ─────────────────────────────────────────────────

    public void RaiseResidentialTax() { if (_svc != null) residentialIncomeTax = _svc.RaiseTaxRate(residentialIncomeTax, "Residential", gameNotificationManager); NotifyTaxRatesAffectHappiness(); }
    public void LowerResidentialTax() { if (_svc != null) residentialIncomeTax = _svc.LowerTaxRate(residentialIncomeTax, "Residential", gameNotificationManager); NotifyTaxRatesAffectHappiness(); }
    public void RaiseCommercialTax()  { if (_svc != null) commercialIncomeTax  = _svc.RaiseTaxRate(commercialIncomeTax,  "Commercial",  gameNotificationManager); NotifyTaxRatesAffectHappiness(); }
    public void LowerCommercialTax()  { if (_svc != null) commercialIncomeTax  = _svc.LowerTaxRate(commercialIncomeTax,  "Commercial",  gameNotificationManager); NotifyTaxRatesAffectHappiness(); }
    public void RaiseIndustrialTax()  { if (_svc != null) industrialIncomeTax  = _svc.RaiseTaxRate(industrialIncomeTax,  "Industrial",  gameNotificationManager); NotifyTaxRatesAffectHappiness(); }
    public void LowerIndustrialTax()  { if (_svc != null) industrialIncomeTax  = _svc.LowerTaxRate(industrialIncomeTax,  "Industrial",  gameNotificationManager); NotifyTaxRatesAffectHappiness(); }

    public void SetTaxRate(Zone.ZoneType z, int r)
    {
        if (_svc == null) return;
        r = _svc.ClampTaxRate(r);
        if      (_svc.IsResidentialZone(z)) { residentialIncomeTax = r; NotifyTaxRatesAffectHappiness(); }
        else if (_svc.IsCommercialZone(z))  { commercialIncomeTax  = r; NotifyTaxRatesAffectHappiness(); }
        else if (_svc.IsIndustrialZone(z))  { industrialIncomeTax  = r; NotifyTaxRatesAffectHappiness(); }
        else DebugHelper.LogWarning($"Cannot set tax rate for zone type: {z}");
    }

    public int GetResidentialTax() => residentialIncomeTax;
    public int GetCommercialTax()  => commercialIncomeTax;
    public int GetIndustrialTax()  => industrialIncomeTax;
    public int GetTaxRate(Zone.ZoneType z)
    {
        if (_svc == null) return 0;
        if (_svc.IsResidentialZone(z)) return residentialIncomeTax;
        if (_svc.IsCommercialZone(z))  return commercialIncomeTax;
        if (_svc.IsIndustrialZone(z))  return industrialIncomeTax;
        return 0;
    }

    // ── Zone Type Helpers + Economic Statistics ───────────────────────────────────

    public bool IsResidentialZone(Zone.ZoneType z)  => _svc != null && _svc.IsResidentialZone(z);
    public bool IsCommercialZone(Zone.ZoneType z)   => _svc != null && _svc.IsCommercialZone(z);
    public bool IsIndustrialZone(Zone.ZoneType z)   => _svc != null && _svc.IsIndustrialZone(z);
    public bool IsBuildingZone(Zone.ZoneType z)     => _svc != null && _svc.IsBuildingZone(z);
    public bool IsZoningType(Zone.ZoneType z)       => _svc != null && _svc.IsZoningType(z);
    public bool IsStateServiceZone(Zone.ZoneType z) => _svc != null && _svc.IsStateServiceZone(z);
    public string GetZoneMainCategory(Zone.ZoneType z) => _svc != null ? _svc.GetZoneMainCategory(z) : "Other";
    public string GetZoneDensity(Zone.ZoneType z)      => _svc != null ? _svc.GetZoneDensity(z) : "";

    public int GetProjectedMonthlyIncome()    => _svc != null ? _svc.GetProjectedMonthlyIncome(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax) : 0;
    public int GetMonthlyIncomeDelta()        => _svc != null ? _svc.GetMonthlyIncomeDelta(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax, maintenanceContributors) : 0;
    public int GetHudEstimatedMonthlySurplus()=> _svc != null ? _svc.GetHudEstimatedMonthlySurplus(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax, maintenanceContributors) : 0;
    public int GetProjectedMonthlyMaintenance()=> _svc != null ? _svc.GetProjectedMonthlyMaintenance(maintenanceContributors) : 0;
    public float GetEconomicHealth()          => _svc != null ? _svc.GetEconomicHealth(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax) : 0f;
    public EconomicSummary GetEconomicSummary()=> _svc != null ? _svc.GetEconomicSummary(residentialIncomeTax, commercialIncomeTax, industrialIncomeTax) : default;

    private void NotifyTaxRatesAffectHappiness() { if (cityStats != null) cityStats.RefreshHappinessAfterPolicyChange(); }
}

/// <summary>Economic summary data struct.</summary>
[System.Serializable]
public struct EconomicSummary
{
    public int   currentMoney;
    public int   projectedMonthlyIncome;
    public int   projectedMonthlyMaintenance;
    public int   residentialTaxRate;
    public int   commercialTaxRate;
    public int   industrialTaxRate;
    public float economicHealth;

    // Additional zone information
    public int totalResidentialZones;
    public int totalCommercialZones;
    public int totalIndustrialZones;
}
}
