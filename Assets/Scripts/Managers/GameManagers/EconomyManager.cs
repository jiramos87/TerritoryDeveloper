using System.Collections.Generic;
using UnityEngine;
using Territory.Audio;
using Territory.Simulation.Signals;
using Territory.Zones;
using Territory.UI;
using Territory.Timing;
using Territory.Utilities;

namespace Territory.Economy
{
/// <summary>
/// Manage city economy: taxation, monthly maintenance, money, financial transactions.
/// Main interface for all economic ops.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    [Header("Manager References")]
    public CityStats cityStats;
    public TimeManager timeManager;
    public GameNotificationManager gameNotificationManager;
    /// <summary>
    /// Enforces non-negative treasury clamp on spend operations.
    /// EconomyManager owns balance state via <see cref="cityStats"/>;
    /// TreasuryFloorClampService is a composition helper (same GO) that
    /// encapsulates the floor-clamp policy so SpendMoney can delegate balance mutation.
    /// </summary>
    [SerializeField] private TreasuryFloorClampService treasuryFloorClamp;
    /// <summary>
    /// Allocates tax revenue across budget categories (R/C/I multipliers).
    /// Composition helper on same GO — consumed by this manager once Phase 2 wires call sites.
    /// </summary>
    [SerializeField] private BudgetAllocationService budgetAllocation;

    /// <summary>
    /// Stage 7 (TECH-1892) — signal tuning weights asset; consulted at <see cref="GetProjectedMonthlyIncome"/>
    /// for the per-tick land-value tax-base bonus multiplier (<see cref="SignalTuningWeightsAsset.LandValueIncomeMultiplier"/>).
    /// Inspector-wired primary; Resources fallback under <c>Resources/SignalTuningWeights</c>.
    /// </summary>
    [SerializeField] private SignalTuningWeightsAsset tuningWeights;

    [Header("Monthly maintenance")]
    [Tooltip("Upkeep charged per road cell (ZoneType.Road) on the first day of each month, after tax collection.")]
    public int maintenanceCostPerRoadCell = 8;
    [Tooltip("Upkeep charged per registered power plant on the first day of each month, after tax collection.")]
    public int maintenanceCostPerPowerPlant = 350;

    /// <summary>
    /// Ordered registry of maintenance contributors. Iterated deterministically by
    /// <see cref="IMaintenanceContributor.GetContributorId"/> (ordinal sort) in
    /// <see cref="ProcessMonthlyMaintenance"/>. Cleared on scene load / save restore.
    /// </summary>
    private readonly List<IMaintenanceContributor> maintenanceContributors = new List<IMaintenanceContributor>();

    [Header("Tax Rates")]
    public int residentialIncomeTax = 10;
    public int commercialIncomeTax = 10;
    public int industrialIncomeTax = 10;

    [Header("Tax Rate Limits")]
    public int minTaxRate = 0;
    public int maxTaxRate = 50;

    void Awake()
    {
        if (treasuryFloorClamp == null)
            treasuryFloorClamp = FindObjectOfType<TreasuryFloorClampService>();
        if (treasuryFloorClamp == null)
            Debug.LogWarning("EconomyManager: TreasuryFloorClampService not found. Attach it to the EconomyManager GameObject in the scene.");
        if (budgetAllocation == null)
            budgetAllocation = FindObjectOfType<BudgetAllocationService>();
        if (budgetAllocation == null)
            Debug.LogWarning("EconomyManager: BudgetAllocationService not found. Attach it to the EconomyManager GameObject in the scene.");
        if (tuningWeights == null)
            tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
        // Soft warn — if asset is absent, GetProjectedMonthlyIncome falls back to no land-value bonus.
    }

    void Start()
    {
        // Find references if not assigned
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();
        if (gameNotificationManager == null)
            gameNotificationManager = FindObjectOfType<GameNotificationManager>();
    }

    /// <summary>
    /// Process daily economic activities.
    /// </summary>
    public void ProcessDailyEconomy()
    {
        // Add daily calculations if needed
        if (timeManager != null && timeManager.GetCurrentDate().Day == 1)
        {
            ProcessMonthlyEconomy();
        }
    }

    /// <summary>
    /// Process monthly economy: tax collection first, then maintenance.
    /// </summary>
    private void ProcessMonthlyEconomy()
    {
        if (cityStats == null) return;

        ApplyMonthlyTaxCollection();
        ProcessMonthlyMaintenance();
        if (budgetAllocation != null) budgetAllocation.MonthlyReset();
    }

    /// <summary>
    /// Collect building-based tax income + notify player.
    /// </summary>
    private void ApplyMonthlyTaxCollection()
    {
        int residentialIncome = cityStats.residentialBuildingCount * residentialIncomeTax;
        int commercialIncome = cityStats.commercialBuildingCount * commercialIncomeTax;
        int industrialIncome = cityStats.industrialBuildingCount * industrialIncomeTax;

        int totalTaxIncome = residentialIncome + commercialIncome + industrialIncome;

        cityStats.AddMoney(totalTaxIncome);

        if (gameNotificationManager != null)
            gameNotificationManager.PostInfo(
                "Monthly Tax Collection\n" +
                $"Collected ${totalTaxIncome} in taxes this month.\n" +
                $"Residential: ${residentialIncome}, Commercial: ${commercialIncome}, Industrial: ${industrialIncome}"
            );
    }

    /// <summary>
    /// Iterate maintenance contributors sorted by <see cref="IMaintenanceContributor.GetContributorId"/>
    /// (ordinal). Sub-type contributors (id >= 0) draw from <see cref="BudgetAllocationService.TryDraw"/>;
    /// general-pool contributors (id == -1) spend through <see cref="TreasuryFloorClampService.TrySpend"/>
    /// via <see cref="SpendMoney"/>.
    /// </summary>
    private void ProcessMonthlyMaintenance()
    {
        var snapshot = GetMaintenanceContributorsSnapshot();
        if (snapshot.Count == 0) return;

        snapshot.Sort((a, b) => string.Compare(a.GetContributorId(), b.GetContributorId(), System.StringComparison.Ordinal));

        int totalPaid = 0;
        int totalUnpaid = 0;

        foreach (var contributor in snapshot)
        {
            int cost = contributor.GetMonthlyMaintenance();
            if (cost <= 0) continue;

            int subType = contributor.GetSubTypeId();
            if (subType >= 0)
            {
                if (budgetAllocation != null && budgetAllocation.TryDraw(subType, cost))
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

        if (gameNotificationManager != null)
        {
            if (totalUnpaid > 0)
            {
                gameNotificationManager.PostError(
                    "Maintenance Unpaid\n" +
                    $"Upkeep of ${totalUnpaid} could not be paid. Balance: ${GetCurrentMoney()}."
                );
            }

            if (totalPaid > 0)
            {
                gameNotificationManager.PostInfo(
                    "Monthly Maintenance\n" +
                    $"Paid ${totalPaid} for upkeep this month."
                );
            }
        }
    }

    #region Maintenance Contributor Registry

    /// <summary>
    /// Register a maintenance contributor. Duplicates (by reference) are silently ignored.
    /// </summary>
    public void RegisterMaintenanceContributor(IMaintenanceContributor contributor)
    {
        if (contributor == null) return;
        if (!maintenanceContributors.Contains(contributor))
            maintenanceContributors.Add(contributor);
    }

    /// <summary>
    /// Unregister a maintenance contributor. No-op if not found.
    /// </summary>
    public void UnregisterMaintenanceContributor(IMaintenanceContributor contributor)
    {
        if (contributor == null) return;
        maintenanceContributors.Remove(contributor);
    }

    /// <summary>
    /// Clear all registered contributors. Called on scene load / save restore
    /// before adapters re-register from their own lifecycle hooks.
    /// </summary>
    public void ClearMaintenanceContributors()
    {
        maintenanceContributors.Clear();
    }

    /// <summary>Current contributor count. Exposed for tests.</summary>
    public int MaintenanceContributorCount => maintenanceContributors.Count;

    /// <summary>
    /// Snapshot of registered contributors for sorted iteration.
    /// Returns a copy to avoid re-entrancy during spend.
    /// </summary>
    public List<IMaintenanceContributor> GetMaintenanceContributorsSnapshot()
    {
        return new List<IMaintenanceContributor>(maintenanceContributors);
    }

    #endregion

    #region Money Management Methods
    /// <summary>
    /// Spend money from city treasury.
    /// </summary>
    /// <param name="amount">Amount to spend.</param>
    /// <param name="contextForInsufficientFunds">Optional short label for insufficient-funds notification (e.g. "Monthly maintenance").</param>
    /// <param name="notifyInsufficientFunds">If false, no notification on failure (caller handles messaging).</param>
    /// <returns>True if transaction successful, false if insufficient funds.</returns>
    public bool SpendMoney(int amount, string contextForInsufficientFunds = null, bool notifyInsufficientFunds = true)
    {
        if (cityStats == null)
        {
            Debug.LogError("EconomyManager: CityStats reference is null!");
            return false;
        }

        if (amount < 0)
        {
            Debug.LogError("EconomyManager: Cannot spend negative amount!");
            return false;
        }

        if (treasuryFloorClamp == null)
        {
            Debug.LogError("EconomyManager: TreasuryFloorClampService reference is null!");
            return false;
        }

        // Pre-gate on CanAfford so we own the failure-notification path.
        // TrySpend unconditionally posts PostError on insufficient funds;
        // callers with notifyInsufficientFunds=false (e.g. ProcessMonthlyMaintenance)
        // need notification suppressed. Pre-gating preserves parity without touching the
        // archived TrySpend API.
        if (!treasuryFloorClamp.CanAfford(amount))
        {
            if (notifyInsufficientFunds && gameNotificationManager != null)
            {
                string prefix = string.IsNullOrEmpty(contextForInsufficientFunds) ? "" : contextForInsufficientFunds + "\n";
                gameNotificationManager.PostError(
                    "Insufficient Funds\n" + prefix +
                    $"Cannot spend ${amount}. Current balance is ${GetCurrentMoney()}."
                );
            }
            return false;
        }

        bool ok = treasuryFloorClamp.TrySpend(amount, contextForInsufficientFunds ?? string.Empty);
        if (ok && notifyInsufficientFunds) BlipEngine.Play(BlipId.EcoMoneySpent);
        return ok;
    }

    /// <summary>
    /// Add money to city treasury.
    /// </summary>
    /// <param name="amount">Amount to add.</param>
    public void AddMoney(int amount)
    {
        if (cityStats == null)
        {
            Debug.LogError("EconomyManager: CityStats reference is null!");
            return;
        }

        if (amount < 0)
        {
            Debug.LogError("EconomyManager: Cannot add negative amount! Use SpendMoney instead.");
            return;
        }

        cityStats.AddMoney(amount);
        if (amount > 0) BlipEngine.Play(BlipId.EcoMoneyEarned);
    }

    /// <summary>
    /// Get current money in city treasury.
    /// </summary>
    /// <returns>Current money amount.</returns>
    public int GetCurrentMoney()
    {
        if (cityStats == null)
        {
            Debug.LogError("EconomyManager: CityStats reference is null!");
            return 0;
        }

        return cityStats.money;
    }

    /// <summary>
    /// Check if city can afford amount.
    /// </summary>
    /// <param name="amount">Amount to check.</param>
    /// <returns>True if affordable.</returns>
    public bool CanAfford(int amount)
    {
        return GetCurrentMoney() >= amount;
    }

    /// <summary>
    /// Transfer money between accounts (future: trade, loans, etc.).
    /// </summary>
    /// <param name="amount">Amount to transfer.</param>
    /// <param name="description">Transfer description.</param>
    /// <returns>True if transfer successful.</returns>
    public bool TransferMoney(int amount, string description = "")
    {
        if (SpendMoney(amount))
        {
            // In a more complex system, this would transfer to another account
            // For now, it just removes money from the treasury
            DebugHelper.Log($"EconomyManager: Transfer completed - ${amount} ({description})");
            return true;
        }
        return false;
    }
    /// <summary>
    /// Get main category of zone type (Residential, Commercial, Industrial, Other).
    /// </summary>
    /// <param name="zoneType">Zone type to categorize.</param>
    /// <returns>Main zone category as string.</returns>
    public string GetZoneMainCategory(Zone.ZoneType zoneType)
    {
        if (IsResidentialZone(zoneType))
            return "Residential";
        else if (IsCommercialZone(zoneType))
            return "Commercial";
        else if (IsIndustrialZone(zoneType))
            return "Industrial";
        else
            return "Other";
    }
    #endregion

    #region Tax Management Methods
    private void NotifyTaxRatesAffectHappiness()
    {
        if (cityStats != null)
            cityStats.RefreshHappinessAfterPolicyChange();
    }

    /// <summary>
    /// Raise residential tax rate.
    /// </summary>
    public void RaiseResidentialTax()
    {
        if (residentialIncomeTax < maxTaxRate)
        {
            residentialIncomeTax += 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Residential tax raised to {residentialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Residential tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower residential tax rate.
    /// </summary>
    public void LowerResidentialTax()
    {
        if (residentialIncomeTax > minTaxRate)
        {
            residentialIncomeTax -= 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Residential tax lowered to {residentialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Residential tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Raise commercial tax rate.
    /// </summary>
    public void RaiseCommercialTax()
    {
        if (commercialIncomeTax < maxTaxRate)
        {
            commercialIncomeTax += 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Commercial tax raised to {commercialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Commercial tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower commercial tax rate.
    /// </summary>
    public void LowerCommercialTax()
    {
        if (commercialIncomeTax > minTaxRate)
        {
            commercialIncomeTax -= 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Commercial tax lowered to {commercialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Commercial tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Raise industrial tax rate.
    /// </summary>
    public void RaiseIndustrialTax()
    {
        if (industrialIncomeTax < maxTaxRate)
        {
            industrialIncomeTax += 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Industrial tax raised to {industrialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Industrial tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower industrial tax rate.
    /// </summary>
    public void LowerIndustrialTax()
    {
        if (industrialIncomeTax > minTaxRate)
        {
            industrialIncomeTax -= 1;
            NotifyTaxRatesAffectHappiness();
            if (gameNotificationManager != null)
                gameNotificationManager.PostInfo($"Industrial tax lowered to {industrialIncomeTax}%");
        }
        else
        {
            DebugHelper.LogWarning($"Industrial tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Set tax rate for specific zone type.
    /// </summary>
    /// <param name="zoneType">Zone type.</param>
    /// <param name="newRate">New tax rate.</param>
    public void SetTaxRate(Zone.ZoneType zoneType, int newRate)
    {
        newRate = Mathf.Clamp(newRate, minTaxRate, maxTaxRate);

        if (IsResidentialZone(zoneType))
        {
            residentialIncomeTax = newRate;
            NotifyTaxRatesAffectHappiness();
        }
        else if (IsCommercialZone(zoneType))
        {
            commercialIncomeTax = newRate;
            NotifyTaxRatesAffectHappiness();
        }
        else if (IsIndustrialZone(zoneType))
        {
            industrialIncomeTax = newRate;
            NotifyTaxRatesAffectHappiness();
        }
        else
        {
            DebugHelper.LogWarning($"Cannot set tax rate for zone type: {zoneType}");
        }
    }
    #endregion

    #region Tax Getters
    /// <summary>
    /// Get residential tax rate.
    /// </summary>
    public int GetResidentialTax()
    {
        return residentialIncomeTax;
    }

    /// <summary>
    /// Get commercial tax rate.
    /// </summary>
    public int GetCommercialTax()
    {
        return commercialIncomeTax;
    }

    /// <summary>
    /// Get industrial tax rate.
    /// </summary>
    public int GetIndustrialTax()
    {
        return industrialIncomeTax;
    }

    /// <summary>
    /// Get tax rate for specific zone type.
    /// </summary>
    /// <param name="zoneType">Zone type.</param>
    /// <returns>Tax rate for zone type.</returns>
    public int GetTaxRate(Zone.ZoneType zoneType)
    {
        if (IsResidentialZone(zoneType))
        {
            return residentialIncomeTax;
        }
        else if (IsCommercialZone(zoneType))
        {
            return commercialIncomeTax;
        }
        else if (IsIndustrialZone(zoneType))
        {
            return industrialIncomeTax;
        }
        else
        {
            return 0;
        }
    }
    #endregion

    #region Zone Type Helper Methods
    /// <summary>
    /// Check if zone type is residential.
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if residential.</returns>
    public bool IsResidentialZone(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightBuilding ||
               zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
               zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
               zoneType == Zone.ZoneType.ResidentialLightZoning ||
               zoneType == Zone.ZoneType.ResidentialMediumZoning ||
               zoneType == Zone.ZoneType.ResidentialHeavyZoning;
    }

    /// <summary>
    /// Check if zone type is commercial.
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if commercial.</returns>
    public bool IsCommercialZone(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.CommercialLightZoning ||
               zoneType == Zone.ZoneType.CommercialMediumZoning ||
               zoneType == Zone.ZoneType.CommercialHeavyZoning;
    }

    /// <summary>
    /// Check if zone type is industrial.
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if industrial.</returns>
    public bool IsIndustrialZone(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightZoning ||
               zoneType == Zone.ZoneType.IndustrialMediumZoning ||
               zoneType == Zone.ZoneType.IndustrialHeavyZoning;
    }

    /// <summary>
    /// Check if zone type is building (not zoning).
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if building.</returns>
    public bool IsBuildingZone(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightBuilding ||
               zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
               zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
               zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding ||
               zoneType == Zone.ZoneType.Building ||
               zoneType == Zone.ZoneType.StateServiceLightBuilding ||
               zoneType == Zone.ZoneType.StateServiceMediumBuilding ||
               zoneType == Zone.ZoneType.StateServiceHeavyBuilding;
    }

    /// <summary>
    /// Check if zone type is zoning (not building).
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if zoning.</returns>
    public bool IsZoningType(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightZoning ||
               zoneType == Zone.ZoneType.ResidentialMediumZoning ||
               zoneType == Zone.ZoneType.ResidentialHeavyZoning ||
               zoneType == Zone.ZoneType.CommercialLightZoning ||
               zoneType == Zone.ZoneType.CommercialMediumZoning ||
               zoneType == Zone.ZoneType.CommercialHeavyZoning ||
               zoneType == Zone.ZoneType.IndustrialLightZoning ||
               zoneType == Zone.ZoneType.IndustrialMediumZoning ||
               zoneType == Zone.ZoneType.IndustrialHeavyZoning ||
               zoneType == Zone.ZoneType.StateServiceLightZoning ||
               zoneType == Zone.ZoneType.StateServiceMediumZoning ||
               zoneType == Zone.ZoneType.StateServiceHeavyZoning;
    }

    /// <summary>
    /// Check if zone type is Zone S (State Service).
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if any of the 6 State Service sub-types.</returns>
    public bool IsStateServiceZone(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.StateServiceLightBuilding ||
               zoneType == Zone.ZoneType.StateServiceMediumBuilding ||
               zoneType == Zone.ZoneType.StateServiceHeavyBuilding ||
               zoneType == Zone.ZoneType.StateServiceLightZoning ||
               zoneType == Zone.ZoneType.StateServiceMediumZoning ||
               zoneType == Zone.ZoneType.StateServiceHeavyZoning;
    }

    /// <summary>
    /// Get density level of zone type.
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>Density level (Light, Medium, Heavy) or empty string if N/A.</returns>
    public string GetZoneDensity(Zone.ZoneType zoneType)
    {
        string zoneTypeName = zoneType.ToString();

        if (zoneTypeName.Contains("Light"))
            return "Light";
        else if (zoneTypeName.Contains("Medium"))
            return "Medium";
        else if (zoneTypeName.Contains("Heavy"))
            return "Heavy";
        else
            return "";
    }
    #endregion

    #region Economic Statistics
    /// <summary>
    /// Calc projected monthly income from current zones + tax rates.
    /// </summary>
    /// <returns>Projected monthly income.</returns>
    public int GetProjectedMonthlyIncome()
    {
        if (cityStats == null) return 0;

        int projectedResidentialIncome = cityStats.residentialBuildingCount * residentialIncomeTax;
        int projectedCommercialIncome = cityStats.commercialBuildingCount * commercialIncomeTax;
        int projectedIndustrialIncome = cityStats.industrialBuildingCount * industrialIncomeTax;

        int baseSum = projectedResidentialIncome + projectedCommercialIncome + projectedIndustrialIncome;

        // Stage 7 (TECH-1892) — apply per-tick land-value tax-base bonus.
        // Multiplier = 1f + cityLandValueMean * LandValueIncomeMultiplier.
        // At cityLandValueMean=100 (default cap) and LandValueIncomeMultiplier=0.005 → +50% bonus.
        // Asset absent or NaN-guard tripped → no bonus (multiplier=1f), preserving legacy behaviour.
        if (tuningWeights == null)
        {
            return baseSum;
        }
        float landValueMean = cityStats.cityLandValueMean;
        if (float.IsNaN(landValueMean) || float.IsInfinity(landValueMean) || landValueMean < 0f)
        {
            return baseSum;
        }
        float multiplier = 1f + landValueMean * tuningWeights.LandValueIncomeMultiplier;
        if (multiplier < 0f)
        {
            multiplier = 0f;
        }
        return Mathf.RoundToInt(baseSum * multiplier);
    }

    /// <summary>
    /// Return projected net monthly cash flow (tax revenue − recurring maintenance).
    /// </summary>
    public int GetMonthlyIncomeDelta()
    {
        return GetProjectedMonthlyIncome() - GetProjectedMonthlyMaintenance();
    }

    /// <summary>
    /// HUD surplus hint: projected tax − maintenance − Zone S envelope cap.
    /// Uses <see cref="CityStats"/> read-model fields populated by <see cref="CityStats.RefreshEconomyReadModel"/>.
    /// </summary>
    public int GetHudEstimatedMonthlySurplus()
    {
        int baseDelta = GetMonthlyIncomeDelta();
        if (cityStats == null) return baseDelta;
        int surplus = baseDelta - cityStats.totalEnvelopeCap;
        return surplus;
    }

    /// <summary>
    /// Return projected monthly maintenance from all registered contributors.
    /// </summary>
    public int GetProjectedMonthlyMaintenance()
    {
        int total = 0;
        foreach (var c in maintenanceContributors)
        {
            int cost = c.GetMonthlyMaintenance();
            if (cost > 0) total += cost;
        }
        return total;
    }

    /// <summary>
    /// Get economic health indicator from tax rates + income.
    /// </summary>
    /// <returns>Economic health score (0-100).</returns>
    public float GetEconomicHealth()
    {
        if (cityStats == null) return 0f;

        // Base health starts at 100
        float health = 100f;

        // High tax rates reduce economic health
        float avgTaxRate = (residentialIncomeTax + commercialIncomeTax + industrialIncomeTax) / 3f;
        health -= (avgTaxRate - 10f) * 2f; // Optimal tax rate is around 10%

        // Low money reserves reduce economic health
        int currentMoney = GetCurrentMoney();
        if (currentMoney < 1000)
        {
            health -= (1000 - currentMoney) * 0.01f;
        }

        return Mathf.Clamp(health, 0f, 100f);
    }

    /// <summary>
    /// Get economic summary for UI display.
    /// </summary>
    /// <returns>Economic summary data.</returns>
    public EconomicSummary GetEconomicSummary()
    {
        return new EconomicSummary
        {
            currentMoney = GetCurrentMoney(),
            projectedMonthlyIncome = GetProjectedMonthlyIncome(),
            projectedMonthlyMaintenance = GetProjectedMonthlyMaintenance(),
            residentialTaxRate = residentialIncomeTax,
            commercialTaxRate = commercialIncomeTax,
            industrialTaxRate = industrialIncomeTax,
            economicHealth = GetEconomicHealth()
        };
    }
    #endregion
}

/// <summary>
/// Economic summary data struct.
/// </summary>
[System.Serializable]
public struct EconomicSummary
{
    public int currentMoney;
    public int projectedMonthlyIncome;
    public int projectedMonthlyMaintenance;
    public int residentialTaxRate;
    public int commercialTaxRate;
    public int industrialTaxRate;
    public float economicHealth;

    // Additional zone information
    public int totalResidentialZones;
    public int totalCommercialZones;
    public int totalIndustrialZones;
}
}
