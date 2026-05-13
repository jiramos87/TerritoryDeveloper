using System.Collections.Generic;
using UnityEngine;
using Territory.Economy;
using Territory.Audio;
using Territory.UI;
using Territory.Zones;
using Territory.Utilities;
using Territory.Simulation.Signals;

namespace Domains.Economy.Services
{
/// <summary>
/// POCO service extracted from EconomyManager (Stage 5.8 Tier-C NO-PORT).
/// Tax management, money management, zone-type helpers, economic statistics.
/// Hub (EconomyManager) owns serialized fields and delegates here via WireDependencies.
/// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
/// autoReferenced:true on TerritoryDeveloper.Game (via .asmref) provides CityStats/GameNotificationManager/TreasuryFloorClampService/etc.
/// </summary>
public class EconomyService
{
    // ── Wired dependencies ──────────────────────────────────────────────────────
    private CityStats                   _cityStats;
    private GameNotificationManager     _notificationManager;
    private TreasuryFloorClampService   _treasuryFloorClamp;
    private BudgetAllocationService     _budgetAllocation;
    private SignalTuningWeightsAsset    _tuningWeights;

    // ── Hub-owned config mirrored at WireDependencies ───────────────────────────
    private int _maintenanceCostPerRoadCell;
    private int _maintenanceCostPerPowerPlant;
    private int _minTaxRate;
    private int _maxTaxRate;

    // ── Tax rates (owned by hub; passed in for each computation) ────────────────
    // NOTE: Tax rates are not stored here — hub owns the serialized fields.
    // Stateful tax-rate mutation (Raise/Lower) writes back to hub via out params.

    // ── Setup ───────────────────────────────────────────────────────────────────

    /// <summary>Wire dependencies + config from hub. Call from hub Awake/Start after reference resolution.</summary>
    public void WireDependencies(
        CityStats                 cityStats,
        GameNotificationManager   notificationManager,
        TreasuryFloorClampService treasuryFloorClamp,
        BudgetAllocationService   budgetAllocation,
        SignalTuningWeightsAsset  tuningWeights,
        int maintenanceCostPerRoadCell,
        int maintenanceCostPerPowerPlant,
        int minTaxRate,
        int maxTaxRate)
    {
        _cityStats                   = cityStats;
        _notificationManager         = notificationManager;
        _treasuryFloorClamp          = treasuryFloorClamp;
        _budgetAllocation            = budgetAllocation;
        _tuningWeights               = tuningWeights;
        _maintenanceCostPerRoadCell  = maintenanceCostPerRoadCell;
        _maintenanceCostPerPowerPlant = maintenanceCostPerPowerPlant;
        _minTaxRate                  = minTaxRate;
        _maxTaxRate                  = maxTaxRate;
    }

    // ── Monthly economy ─────────────────────────────────────────────────────────

    /// <summary>Run monthly tax collection + maintenance cycle.</summary>
    public void ProcessMonthlyEconomy(int residentialTax, int commercialTax, int industrialTax,
        List<IMaintenanceContributor> maintenanceContributors)
    {
        if (_cityStats == null) return;
        ApplyMonthlyTaxCollection(residentialTax, commercialTax, industrialTax);
        ProcessMonthlyMaintenance(maintenanceContributors);
        if (_budgetAllocation != null) _budgetAllocation.MonthlyReset();
    }

    /// <summary>Collect building-based tax income + notify player.</summary>
    public void ApplyMonthlyTaxCollection(int residentialTax, int commercialTax, int industrialTax)
    {
        int residentialIncome = _cityStats.residentialBuildingCount * residentialTax;
        int commercialIncome  = _cityStats.commercialBuildingCount  * commercialTax;
        int industrialIncome  = _cityStats.industrialBuildingCount  * industrialTax;
        int totalTaxIncome    = residentialIncome + commercialIncome + industrialIncome;

        _cityStats.AddMoney(totalTaxIncome);

        if (_notificationManager != null)
            _notificationManager.PostInfo(
                "Monthly Tax Collection\n" +
                $"Collected ${totalTaxIncome} in taxes this month.\n" +
                $"Residential: ${residentialIncome}, Commercial: ${commercialIncome}, Industrial: ${industrialIncome}"
            );
    }

    /// <summary>Iterate maintenance contributors + spend from treasury/budget pools.</summary>
    public void ProcessMonthlyMaintenance(List<IMaintenanceContributor> maintenanceContributors)
    {
        if (maintenanceContributors == null || maintenanceContributors.Count == 0) return;

        var snapshot = new List<IMaintenanceContributor>(maintenanceContributors);
        snapshot.Sort((a, b) => string.Compare(a.GetContributorId(), b.GetContributorId(), System.StringComparison.Ordinal));

        int totalPaid   = 0;
        int totalUnpaid = 0;

        foreach (var contributor in snapshot)
        {
            int cost = contributor.GetMonthlyMaintenance();
            if (cost <= 0) continue;

            int subType = contributor.GetSubTypeId();
            if (subType >= 0)
            {
                if (_budgetAllocation != null && _budgetAllocation.TryDraw(subType, cost))
                    totalPaid += cost;
                else
                    totalUnpaid += cost;
            }
            else
            {
                if (SpendMoney(cost, "Monthly maintenance", notifyInsufficientFunds: false))
                    totalPaid += cost;
                else
                    totalUnpaid += cost;
            }
        }

        if (_notificationManager != null)
        {
            if (totalUnpaid > 0)
                _notificationManager.PostError(
                    "Maintenance Unpaid\n" +
                    $"Upkeep of ${totalUnpaid} could not be paid. Balance: ${GetCurrentMoney()}."
                );
            if (totalPaid > 0)
                _notificationManager.PostInfo(
                    "Monthly Maintenance\n" +
                    $"Paid ${totalPaid} for upkeep this month."
                );
        }
    }

    // ── Money management ────────────────────────────────────────────────────────

    /// <summary>Spend money from city treasury. Returns true on success.</summary>
    public bool SpendMoney(int amount, string contextForInsufficientFunds = null, bool notifyInsufficientFunds = true)
    {
        if (_cityStats == null)
        {
            Debug.LogError("EconomyService: CityStats reference is null!");
            return false;
        }
        if (amount < 0)
        {
            Debug.LogError("EconomyService: Cannot spend negative amount!");
            return false;
        }
        if (_treasuryFloorClamp == null)
        {
            Debug.LogError("EconomyService: TreasuryFloorClampService reference is null!");
            return false;
        }

        if (!_treasuryFloorClamp.CanAfford(amount))
        {
            if (notifyInsufficientFunds && _notificationManager != null)
            {
                string prefix = string.IsNullOrEmpty(contextForInsufficientFunds) ? "" : contextForInsufficientFunds + "\n";
                _notificationManager.PostError(
                    "Insufficient Funds\n" + prefix +
                    $"Cannot spend ${amount}. Current balance is ${GetCurrentMoney()}."
                );
            }
            return false;
        }

        bool ok = _treasuryFloorClamp.TrySpend(amount, contextForInsufficientFunds ?? string.Empty);
        if (ok && notifyInsufficientFunds) BlipEngine.Play(BlipId.EcoMoneySpent);
        return ok;
    }

    /// <summary>Add money to city treasury.</summary>
    public void AddMoney(int amount)
    {
        if (_cityStats == null)
        {
            Debug.LogError("EconomyService: CityStats reference is null!");
            return;
        }
        if (amount < 0)
        {
            Debug.LogError("EconomyService: Cannot add negative amount! Use SpendMoney instead.");
            return;
        }
        _cityStats.AddMoney(amount);
        if (amount > 0) BlipEngine.Play(BlipId.EcoMoneyEarned);
    }

    /// <summary>Set initial treasury before first sim tick.</summary>
    public void SetStartingFunds(int amount)
    {
        if (_cityStats == null)
        {
            Debug.LogWarning("[EconomyService] SetStartingFunds: CityStats not available — deferred.");
            return;
        }
        if (amount < 0) amount = 0;
        _cityStats.money = amount;
    }

    /// <summary>Get current money in city treasury.</summary>
    public int GetCurrentMoney()
    {
        if (_cityStats == null)
        {
            Debug.LogError("EconomyService: CityStats reference is null!");
            return 0;
        }
        return _cityStats.money;
    }

    /// <summary>Check if city can afford amount.</summary>
    public bool CanAfford(int amount) => GetCurrentMoney() >= amount;

    /// <summary>Transfer money — removes from treasury. Returns true on success.</summary>
    public bool TransferMoney(int amount, string description = "")
    {
        if (SpendMoney(amount))
        {
            DebugHelper.Log($"EconomyService: Transfer completed - ${amount} ({description})");
            return true;
        }
        return false;
    }

    // ── Tax management ──────────────────────────────────────────────────────────

    /// <summary>Raise a tax rate by 1 within [min,max]. Returns new value or -1 if already at max.</summary>
    public int RaiseTaxRate(int currentRate, string label, GameNotificationManager notif = null)
    {
        if (currentRate < _maxTaxRate)
        {
            int newRate = currentRate + 1;
            if (notif != null) notif.PostInfo($"{label} tax raised to {newRate}%");
            return newRate;
        }
        DebugHelper.LogWarning($"{label} tax is already at maximum ({_maxTaxRate}%)");
        return currentRate;
    }

    /// <summary>Lower a tax rate by 1 within [min,max]. Returns new value or unchanged if already at min.</summary>
    public int LowerTaxRate(int currentRate, string label, GameNotificationManager notif = null)
    {
        if (currentRate > _minTaxRate)
        {
            int newRate = currentRate - 1;
            if (notif != null) notif.PostInfo($"{label} tax lowered to {newRate}%");
            return newRate;
        }
        DebugHelper.LogWarning($"{label} tax is already at minimum ({_minTaxRate}%)");
        return currentRate;
    }

    /// <summary>Clamp new rate to [min,max].</summary>
    public int ClampTaxRate(int rate) => Mathf.Clamp(rate, _minTaxRate, _maxTaxRate);

    // ── Zone type helpers ────────────────────────────────────────────────────────

    public bool IsResidentialZone(Zone.ZoneType z)
        => z == Zone.ZoneType.ResidentialLightBuilding  || z == Zone.ZoneType.ResidentialMediumBuilding  || z == Zone.ZoneType.ResidentialHeavyBuilding
        || z == Zone.ZoneType.ResidentialLightZoning    || z == Zone.ZoneType.ResidentialMediumZoning    || z == Zone.ZoneType.ResidentialHeavyZoning;

    public bool IsCommercialZone(Zone.ZoneType z)
        => z == Zone.ZoneType.CommercialLightBuilding   || z == Zone.ZoneType.CommercialMediumBuilding   || z == Zone.ZoneType.CommercialHeavyBuilding
        || z == Zone.ZoneType.CommercialLightZoning     || z == Zone.ZoneType.CommercialMediumZoning     || z == Zone.ZoneType.CommercialHeavyZoning;

    public bool IsIndustrialZone(Zone.ZoneType z)
        => z == Zone.ZoneType.IndustrialLightBuilding   || z == Zone.ZoneType.IndustrialMediumBuilding   || z == Zone.ZoneType.IndustrialHeavyBuilding
        || z == Zone.ZoneType.IndustrialLightZoning     || z == Zone.ZoneType.IndustrialMediumZoning     || z == Zone.ZoneType.IndustrialHeavyZoning;

    public bool IsBuildingZone(Zone.ZoneType z)
        => z == Zone.ZoneType.ResidentialLightBuilding  || z == Zone.ZoneType.ResidentialMediumBuilding  || z == Zone.ZoneType.ResidentialHeavyBuilding
        || z == Zone.ZoneType.CommercialLightBuilding   || z == Zone.ZoneType.CommercialMediumBuilding   || z == Zone.ZoneType.CommercialHeavyBuilding
        || z == Zone.ZoneType.IndustrialLightBuilding   || z == Zone.ZoneType.IndustrialMediumBuilding   || z == Zone.ZoneType.IndustrialHeavyBuilding
        || z == Zone.ZoneType.Building
        || z == Zone.ZoneType.StateServiceLightBuilding || z == Zone.ZoneType.StateServiceMediumBuilding || z == Zone.ZoneType.StateServiceHeavyBuilding;

    public bool IsZoningType(Zone.ZoneType z)
        => z == Zone.ZoneType.ResidentialLightZoning    || z == Zone.ZoneType.ResidentialMediumZoning    || z == Zone.ZoneType.ResidentialHeavyZoning
        || z == Zone.ZoneType.CommercialLightZoning     || z == Zone.ZoneType.CommercialMediumZoning     || z == Zone.ZoneType.CommercialHeavyZoning
        || z == Zone.ZoneType.IndustrialLightZoning     || z == Zone.ZoneType.IndustrialMediumZoning     || z == Zone.ZoneType.IndustrialHeavyZoning
        || z == Zone.ZoneType.StateServiceLightZoning   || z == Zone.ZoneType.StateServiceMediumZoning   || z == Zone.ZoneType.StateServiceHeavyZoning;

    public bool IsStateServiceZone(Zone.ZoneType z)
        => z == Zone.ZoneType.StateServiceLightBuilding || z == Zone.ZoneType.StateServiceMediumBuilding || z == Zone.ZoneType.StateServiceHeavyBuilding
        || z == Zone.ZoneType.StateServiceLightZoning   || z == Zone.ZoneType.StateServiceMediumZoning   || z == Zone.ZoneType.StateServiceHeavyZoning;

    public string GetZoneMainCategory(Zone.ZoneType z)
    {
        if (IsResidentialZone(z)) return "Residential";
        if (IsCommercialZone(z))  return "Commercial";
        if (IsIndustrialZone(z))  return "Industrial";
        return "Other";
    }

    public string GetZoneDensity(Zone.ZoneType z)
    {
        string name = z.ToString();
        if (name.Contains("Light"))  return "Light";
        if (name.Contains("Medium")) return "Medium";
        if (name.Contains("Heavy"))  return "Heavy";
        return "";
    }

    // ── Economic statistics ─────────────────────────────────────────────────────

    /// <summary>Calc projected monthly income applying land-value multiplier when tuningWeights available.</summary>
    public int GetProjectedMonthlyIncome(int residentialTax, int commercialTax, int industrialTax)
    {
        if (_cityStats == null) return 0;

        int baseSum = _cityStats.residentialBuildingCount * residentialTax
                    + _cityStats.commercialBuildingCount  * commercialTax
                    + _cityStats.industrialBuildingCount  * industrialTax;

        if (_tuningWeights == null) return baseSum;

        float lv = _cityStats.cityLandValueMean;
        if (float.IsNaN(lv) || float.IsInfinity(lv) || lv < 0f) return baseSum;

        float multiplier = Mathf.Max(0f, 1f + lv * _tuningWeights.LandValueIncomeMultiplier);
        return Mathf.RoundToInt(baseSum * multiplier);
    }

    /// <summary>Return projected monthly maintenance sum from contributor list.</summary>
    public int GetProjectedMonthlyMaintenance(List<IMaintenanceContributor> maintenanceContributors)
    {
        int total = 0;
        if (maintenanceContributors == null) return total;
        foreach (var c in maintenanceContributors)
        {
            int cost = c.GetMonthlyMaintenance();
            if (cost > 0) total += cost;
        }
        return total;
    }

    /// <summary>Net monthly cash flow = projected income − maintenance.</summary>
    public int GetMonthlyIncomeDelta(int residentialTax, int commercialTax, int industrialTax,
        List<IMaintenanceContributor> maintenanceContributors)
        => GetProjectedMonthlyIncome(residentialTax, commercialTax, industrialTax)
         - GetProjectedMonthlyMaintenance(maintenanceContributors);

    /// <summary>HUD surplus hint: delta − Zone S envelope cap.</summary>
    public int GetHudEstimatedMonthlySurplus(int residentialTax, int commercialTax, int industrialTax,
        List<IMaintenanceContributor> maintenanceContributors)
    {
        int baseDelta = GetMonthlyIncomeDelta(residentialTax, commercialTax, industrialTax, maintenanceContributors);
        if (_cityStats == null) return baseDelta;
        return baseDelta - _cityStats.totalEnvelopeCap;
    }

    /// <summary>Economic health score 0–100 from tax rates + money reserve.</summary>
    public float GetEconomicHealth(int residentialTax, int commercialTax, int industrialTax)
    {
        if (_cityStats == null) return 0f;
        float health     = 100f;
        float avgTaxRate = (residentialTax + commercialTax + industrialTax) / 3f;
        health -= (avgTaxRate - 10f) * 2f;
        int money = GetCurrentMoney();
        if (money < 1000) health -= (1000 - money) * 0.01f;
        return Mathf.Clamp(health, 0f, 100f);
    }

    /// <summary>Build EconomicSummary snapshot for UI display.</summary>
    public EconomicSummary GetEconomicSummary(int residentialTax, int commercialTax, int industrialTax)
        => new EconomicSummary
        {
            currentMoney              = GetCurrentMoney(),
            projectedMonthlyIncome    = GetProjectedMonthlyIncome(residentialTax, commercialTax, industrialTax),
            projectedMonthlyMaintenance = 0, // hub passes contributor list separately when needed
            residentialTaxRate        = residentialTax,
            commercialTaxRate         = commercialTax,
            industrialTaxRate         = industrialTax,
            economicHealth            = GetEconomicHealth(residentialTax, commercialTax, industrialTax)
        };
}
}
