using UnityEngine;
using Territory.Audio;
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

    [Header("Monthly maintenance")]
    [Tooltip("Upkeep charged per road cell (ZoneType.Road) on the first day of each month, after tax collection.")]
    public int maintenanceCostPerRoadCell = 8;
    [Tooltip("Upkeep charged per registered power plant on the first day of each month, after tax collection.")]
    public int maintenanceCostPerPowerPlant = 350;

    [Header("Tax Rates")]
    public int residentialIncomeTax = 10;
    public int commercialIncomeTax = 10;
    public int industrialIncomeTax = 10;

    [Header("Tax Rate Limits")]
    public int minTaxRate = 0;
    public int maxTaxRate = 50;

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
    /// Charge street + utility upkeep for current month (after taxes).
    /// </summary>
    private void ProcessMonthlyMaintenance()
    {
        int streetCost = ComputeMonthlyStreetMaintenanceCost();
        int utilityCost = ComputeMonthlyUtilityMaintenanceCost();
        int total = streetCost + utilityCost;
        if (total <= 0)
            return;

        if (!SpendMoney(total, "Monthly maintenance", notifyInsufficientFunds: false))
        {
            if (gameNotificationManager != null)
            {
                gameNotificationManager.PostError(
                    "Maintenance Unpaid\n" +
                    $"Upkeep of ${total} could not be paid (streets: ${streetCost}, power plants: ${utilityCost}). Balance: ${GetCurrentMoney()}."
                );
            }
            return;
        }

        if (gameNotificationManager != null)
        {
            int roads = cityStats.roadCount;
            int plants = cityStats.GetRegisteredPowerPlantCount();
            gameNotificationManager.PostInfo(
                "Monthly Maintenance\n" +
                $"Paid ${total} for upkeep.\n" +
                $"Streets ({roads} cells): ${streetCost}, Power plants ({plants}): ${utilityCost}"
            );
        }
    }

    /// <summary>
    /// Compute monthly street upkeep from road cell count (interstate + ordinary roads).
    /// </summary>
    private int ComputeMonthlyStreetMaintenanceCost()
    {
        if (maintenanceCostPerRoadCell <= 0 || cityStats == null)
            return 0;
        return Mathf.Max(0, cityStats.roadCount) * maintenanceCostPerRoadCell;
    }

    /// <summary>
    /// Compute monthly utility upkeep from registered power plants.
    /// </summary>
    private int ComputeMonthlyUtilityMaintenanceCost()
    {
        if (maintenanceCostPerPowerPlant <= 0 || cityStats == null)
            return 0;
        return Mathf.Max(0, cityStats.GetRegisteredPowerPlantCount()) * maintenanceCostPerPowerPlant;
    }

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

        if (GetCurrentMoney() >= amount)
        {
            cityStats.RemoveMoney(amount);
            if (notifyInsufficientFunds) BlipEngine.Play(BlipId.EcoMoneySpent);
            return true;
        }

        if (notifyInsufficientFunds && gameNotificationManager != null)
        {
            string prefix = string.IsNullOrEmpty(contextForInsufficientFunds)
                ? ""
                : contextForInsufficientFunds + "\n";
            gameNotificationManager.PostError(
                "Insufficient Funds\n" +
                prefix +
                $"Cannot spend ${amount}. Current balance is ${GetCurrentMoney()}."
            );
        }
        return false;
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
               zoneType == Zone.ZoneType.Building;
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
               zoneType == Zone.ZoneType.IndustrialHeavyZoning;
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

        return projectedResidentialIncome + projectedCommercialIncome + projectedIndustrialIncome;
    }

    /// <summary>
    /// Return projected net monthly cash flow (tax revenue − recurring maintenance).
    /// </summary>
    public int GetMonthlyIncomeDelta()
    {
        return GetProjectedMonthlyIncome() - GetProjectedMonthlyMaintenance();
    }

    /// <summary>
    /// Return projected monthly maintenance (streets + registered power plants) at current rates.
    /// </summary>
    public int GetProjectedMonthlyMaintenance()
    {
        return ComputeMonthlyStreetMaintenanceCost() + ComputeMonthlyUtilityMaintenanceCost();
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
