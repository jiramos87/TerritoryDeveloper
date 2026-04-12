using UnityEngine;
using Territory.Economy;

namespace Territory.Simulation
{
public enum GrowthCategory { Roads, Energy, Water, Zoning }

/// <summary>
/// Growth budget allocation across roads, energy, water, zoning. Monthly pool derived from projected
/// net cash flow (tax − maintenance) when positive, else from treasury.
/// </summary>
public class GrowthBudgetManager : MonoBehaviour
{
    public CityStats cityStats;
    public EconomyManager economyManager;
    public GrowthBudgetData data = new GrowthBudgetData();

    private int cachedEffectiveTotalBudget;
    private bool cacheValid;

    void Start()
    {
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();
        MigrateFromLegacyIfNeeded();
    }

    void MigrateFromLegacyIfNeeded()
    {
        if (data.growthBudgetPercent == 0 && data.totalGrowthBudget > 0)
        {
            int money = cityStats != null ? cityStats.money : 20000;
            data.growthBudgetPercent = Mathf.Clamp(money > 0 ? (data.totalGrowthBudget * 100 / money) : 10, 0, 100);
        }
        else if (data.growthBudgetPercent == 0)
        {
            data.growthBudgetPercent = 10;
        }
    }

    public void EnsureBudgetValid()
    {
        data.growthBudgetPercent = Mathf.Clamp(data.growthBudgetPercent, 0, 100);
        data.roadBudgetPercent = Mathf.Clamp(data.roadBudgetPercent, 0, 100);
        data.energyBudgetPercent = Mathf.Clamp(data.energyBudgetPercent, 0, 100);
        data.waterBudgetPercent = Mathf.Clamp(data.waterBudgetPercent, 0, 100);
        data.zoningBudgetPercent = Mathf.Clamp(data.zoningBudgetPercent, 0, 100);
    }

    /// <summary>Min available budget per category when percent &gt; 0 → growth never fully stops mid-month.</summary>
    public int minAvailablePerCategory = 100;

    public int GetAvailableBudget(GrowthCategory cat)
    {
        int total = GetTotalBudget();
        int pct = GetPercent(cat);
        int budgetForCat = total * pct / 100;
        int spent = GetSpent(cat);
        int available = Mathf.Max(0, budgetForCat - spent);
        if (pct > 0 && minAvailablePerCategory > 0 && available < minAvailablePerCategory)
            return minAvailablePerCategory;
        return available;
    }

    public bool TrySpend(GrowthCategory cat, int amount)
    {
        if (amount <= 0) return false;
        if (cityStats != null && !cityStats.CanAfford(amount)) return false;
        if (GetAvailableBudget(cat) < amount) return false;
        if (cityStats != null)
            cityStats.RemoveMoney(amount);
        AddSpent(cat, amount);
        return true;
    }

    /// <summary>Refund prior TrySpend (e.g. placement fail). Add money back to city + subtract from spent.</summary>
    public void RefundSpend(GrowthCategory cat, int amount)
    {
        if (amount <= 0) return;
        if (cityStats != null)
            cityStats.AddMoney(amount);
        AddSpent(cat, -amount);
    }

    public void ResetMonthlyCycle()
    {
        ComputeAndCacheBudget();
        data.roadSpentThisCycle = 0;
        data.energySpentThisCycle = 0;
        data.waterSpentThisCycle = 0;
        data.zoningSpentThisCycle = 0;
    }

    void ComputeAndCacheBudget()
    {
        if (cityStats == null) return;
        int pct = Mathf.Clamp(data.growthBudgetPercent, 0, 100);
        int projectedNet = economyManager != null ? economyManager.GetMonthlyIncomeDelta() : 0;
        int baseAmount = projectedNet > 0 ? projectedNet : cityStats.money;
        cachedEffectiveTotalBudget = baseAmount * pct / 100;
        cacheValid = true;
    }

    /// <summary>Set growth budget % of projected net monthly cash flow (tax − maintenance), 0–100. Falls back to city money when projection not positive.</summary>
    public void SetGrowthBudgetPercent(int percent)
    {
        data.growthBudgetPercent = Mathf.Clamp(percent, 0, 100);
        cacheValid = false;
    }

    /// <summary>User growth budget % (0–100).</summary>
    public int GetGrowthBudgetPercent() => data.growthBudgetPercent;

    public void SetCategoryPercent(GrowthCategory cat, int pct)
    {
        pct = Mathf.Clamp(pct, 0, 100);
        switch (cat)
        {
            case GrowthCategory.Roads: data.roadBudgetPercent = pct; break;
            case GrowthCategory.Energy: data.energyBudgetPercent = pct; break;
            case GrowthCategory.Water: data.waterBudgetPercent = pct; break;
            case GrowthCategory.Zoning: data.zoningBudgetPercent = pct; break;
        }
    }

    /// <summary>Effective total growth budget (cached at month start). Computes on first use if not set.</summary>
    public int GetTotalBudget()
    {
        if (!cacheValid)
            ComputeAndCacheBudget();
        return cachedEffectiveTotalBudget;
    }
    public int GetCategoryPercent(GrowthCategory cat) => GetPercent(cat);

    int GetPercent(GrowthCategory cat)
    {
        switch (cat)
        {
            case GrowthCategory.Roads: return data.roadBudgetPercent;
            case GrowthCategory.Energy: return data.energyBudgetPercent;
            case GrowthCategory.Water: return data.waterBudgetPercent;
            case GrowthCategory.Zoning: return data.zoningBudgetPercent;
            default: return 0;
        }
    }

    int GetSpent(GrowthCategory cat)
    {
        switch (cat)
        {
            case GrowthCategory.Roads: return data.roadSpentThisCycle;
            case GrowthCategory.Energy: return data.energySpentThisCycle;
            case GrowthCategory.Water: return data.waterSpentThisCycle;
            case GrowthCategory.Zoning: return data.zoningSpentThisCycle;
            default: return 0;
        }
    }

    void AddSpent(GrowthCategory cat, int amount)
    {
        switch (cat)
        {
            case GrowthCategory.Roads: data.roadSpentThisCycle = Mathf.Max(0, data.roadSpentThisCycle + amount); break;
            case GrowthCategory.Energy: data.energySpentThisCycle = Mathf.Max(0, data.energySpentThisCycle + amount); break;
            case GrowthCategory.Water: data.waterSpentThisCycle = Mathf.Max(0, data.waterSpentThisCycle + amount); break;
            case GrowthCategory.Zoning: data.zoningSpentThisCycle = Mathf.Max(0, data.zoningSpentThisCycle + amount); break;
        }
    }
}
}
