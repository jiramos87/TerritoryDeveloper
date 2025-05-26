using UnityEngine;

/// <summary>
/// Manages the city's economy including taxation, money management, and financial transactions.
/// Acts as the main interface for all economic operations in the game.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    [Header("Manager References")]
    public CityStats cityStats;
    public TimeManager timeManager;

    [Header("Tax Rates")]
    public int residentialIncomeTax = 10;
    public int commercialIncomeTax = 10;
    public int industrialIncomeTax = 10;
    
    [Header("Tax Rate Limits")]
    public int minTaxRate = 0;
    public int maxTaxRate = 50;

    public GameNotificationManager gameNotificationManager;

    void Start()
    {
        // Find references if not assigned
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();
    }

    /// <summary>
    /// Process daily economic activities
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
    /// Process monthly economic activities including tax collection
    /// </summary>
    private void ProcessMonthlyEconomy()
    {
        if (cityStats == null) return;

        int residentialIncome = cityStats.residentialZoneCount * residentialIncomeTax;
        int commercialIncome = cityStats.commercialZoneCount * commercialIncomeTax;
        int industrialIncome = cityStats.industrialZoneCount * industrialIncomeTax;

        int totalTaxIncome = residentialIncome + commercialIncome + industrialIncome;
        
        cityStats.AddMoney(totalTaxIncome);
        
        gameNotificationManager.PostInfo(
            "Monthly Tax Collection" +
            $"Collected ${totalTaxIncome} in taxes this month.\n" +
            $"Residential: ${residentialIncome}, Commercial: ${commercialIncome}, Industrial: ${industrialIncome}"
        );
    }

    #region Money Management Methods
    /// <summary>
    /// Spend money from the city treasury
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if the transaction was successful, false if insufficient funds</returns>
    public bool SpendMoney(int amount)
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
            cityStats.money -= amount;
            return true;
        }
        else
        {
            gameNotificationManager.PostError(
                "Insufficient Funds" +
                $"Cannot spend ${amount}. Current balance is ${GetCurrentMoney()}."
            );
            return false;
        }
    }

    /// <summary>
    /// Add money to the city treasury
    /// </summary>
    /// <param name="amount">Amount to add</param>
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
    }

    /// <summary>
    /// Get current money in the city treasury
    /// </summary>
    /// <returns>Current money amount</returns>
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
    /// Check if the city can afford a specific amount
    /// </summary>
    /// <param name="amount">Amount to check</param>
    /// <returns>True if the city can afford it, false otherwise</returns>
    public bool CanAfford(int amount)
    {
        return GetCurrentMoney() >= amount;
    }

    /// <summary>
    /// Transfer money between accounts (future use for trade, loans, etc.)
    /// </summary>
    /// <param name="amount">Amount to transfer</param>
    /// <param name="description">Description of the transfer</param>
    /// <returns>True if transfer was successful</returns>
    public bool TransferMoney(int amount, string description = "")
    {
        if (SpendMoney(amount))
        {
            // In a more complex system, this would transfer to another account
            // For now, it just removes money from the treasury
            Debug.Log($"EconomyManager: Transfer completed - ${amount} ({description})");
            return true;
        }
        return false;
    }
    /// <summary>
    /// Get the main category of a zone type (Residential, Commercial, Industrial, Other)
    /// </summary>
    /// <param name="zoneType">Zone type to categorize</param>
    /// <returns>Main zone category as string</returns>
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
    /// <summary>
    /// Raise residential tax rate
    /// </summary>
    public void RaiseResidentialTax()
    {
        if (residentialIncomeTax < maxTaxRate)
        {
            residentialIncomeTax += 1;
            gameNotificationManager.PostInfo($"Residential tax raised to {residentialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Residential tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower residential tax rate
    /// </summary>
    public void LowerResidentialTax()
    {
        if (residentialIncomeTax > minTaxRate)
        {
            residentialIncomeTax -= 1;
            gameNotificationManager.PostInfo($"Residential tax lowered to {residentialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Residential tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Raise commercial tax rate
    /// </summary>
    public void RaiseCommercialTax()
    {
        if (commercialIncomeTax < maxTaxRate)
        {
            commercialIncomeTax += 1;
            gameNotificationManager.PostInfo($"Commercial tax raised to {commercialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Commercial tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower commercial tax rate
    /// </summary>
    public void LowerCommercialTax()
    {
        if (commercialIncomeTax > minTaxRate)
        {
            commercialIncomeTax -= 1;
            gameNotificationManager.PostInfo($"Commercial tax lowered to {commercialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Commercial tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Raise industrial tax rate
    /// </summary>
    public void RaiseIndustrialTax()
    {
        if (industrialIncomeTax < maxTaxRate)
        {
            industrialIncomeTax += 1;
            gameNotificationManager.PostInfo($"Industrial tax raised to {industrialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Industrial tax is already at maximum ({maxTaxRate}%)");
        }
    }

    /// <summary>
    /// Lower industrial tax rate
    /// </summary>
    public void LowerIndustrialTax()
    {
        if (industrialIncomeTax > minTaxRate)
        {
            industrialIncomeTax -= 1;
            gameNotificationManager.PostInfo($"Industrial tax lowered to {industrialIncomeTax}%");
        }
        else
        {
            Debug.LogWarning($"Industrial tax is already at minimum ({minTaxRate}%)");
        }
    }

    /// <summary>
    /// Set tax rate for a specific zone type
    /// </summary>
    /// <param name="zoneType">Type of zone</param>
    /// <param name="newRate">New tax rate</param>
    public void SetTaxRate(Zone.ZoneType zoneType, int newRate)
    {
        newRate = Mathf.Clamp(newRate, minTaxRate, maxTaxRate);
        
        if (IsResidentialZone(zoneType))
        {
            residentialIncomeTax = newRate;
        }
        else if (IsCommercialZone(zoneType))
        {
            commercialIncomeTax = newRate;
        }
        else if (IsIndustrialZone(zoneType))
        {
            industrialIncomeTax = newRate;
        }
        else
        {
            Debug.LogWarning($"Cannot set tax rate for zone type: {zoneType}");
        }
    }
    #endregion

    #region Tax Getters
    /// <summary>
    /// Get residential tax rate
    /// </summary>
    public int GetResidentialTax()
    {
        return residentialIncomeTax;
    }

    /// <summary>
    /// Get commercial tax rate
    /// </summary>
    public int GetCommercialTax()
    {
        return commercialIncomeTax;
    }

    /// <summary>
    /// Get industrial tax rate
    /// </summary>
    public int GetIndustrialTax()
    {
        return industrialIncomeTax;
    }

    /// <summary>
    /// Get tax rate for a specific zone type
    /// </summary>
    /// <param name="zoneType">Type of zone</param>
    /// <returns>Tax rate for the zone type</returns>
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
    /// Check if a zone type is residential
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>True if residential, false otherwise</returns>
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
    /// Check if a zone type is commercial
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>True if commercial, false otherwise</returns>
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
    /// Check if a zone type is industrial
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>True if industrial, false otherwise</returns>
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
    /// Check if a zone type is a building (not zoning)
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>True if building, false otherwise</returns>
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
    /// Check if a zone type is zoning (not building)
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>True if zoning, false otherwise</returns>
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
    /// Get the density level of a zone type
    /// </summary>
    /// <param name="zoneType">Zone type to check</param>
    /// <returns>Density level (Light, Medium, Heavy) or empty string if not applicable</returns>
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
    /// Calculate projected monthly income based on current zones and tax rates
    /// </summary>
    /// <returns>Projected monthly income</returns>
    public int GetProjectedMonthlyIncome()
    {
        if (cityStats == null) return 0;

        int projectedResidentialIncome = cityStats.residentialZoneCount * residentialIncomeTax;
        int projectedCommercialIncome = cityStats.commercialZoneCount * commercialIncomeTax;
        int projectedIndustrialIncome = cityStats.industrialZoneCount * industrialIncomeTax;

        return projectedResidentialIncome + projectedCommercialIncome + projectedIndustrialIncome;
    }

    /// <summary>
    /// Get economic health indicator based on tax rates and income
    /// </summary>
    /// <returns>Economic health score (0-100)</returns>
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
    /// Get economic summary for UI display
    /// </summary>
    /// <returns>Economic summary data</returns>
    public EconomicSummary GetEconomicSummary()
    {
        return new EconomicSummary
        {
            currentMoney = GetCurrentMoney(),
            projectedMonthlyIncome = GetProjectedMonthlyIncome(),
            residentialTaxRate = residentialIncomeTax,
            commercialTaxRate = commercialIncomeTax,
            industrialTaxRate = industrialIncomeTax,
            economicHealth = GetEconomicHealth()
        };
    }
    #endregion
}

/// <summary>
/// Economic summary data structure
/// </summary>
[System.Serializable]
public struct EconomicSummary
{
    public int currentMoney;
    public int projectedMonthlyIncome;
    public int residentialTaxRate;
    public int commercialTaxRate;
    public int industrialTaxRate;
    public float economicHealth;
    
    // Additional zone information
    public int totalResidentialZones;
    public int totalCommercialZones;
    public int totalIndustrialZones;
}
