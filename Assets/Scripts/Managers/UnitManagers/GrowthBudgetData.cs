using UnityEngine;

namespace Territory.Simulation
{
/// <summary>
/// Serializable growth budget allocation across residential, commercial, industrial.
/// </summary>
[System.Serializable]
public class GrowthBudgetData
{
    /// <summary>Legacy: fixed dollar amount. Kept for save/load compat; migrated to growthBudgetPercent on load.</summary>
    public int totalGrowthBudget = 5000;
    /// <summary>User pref: % of projected monthly income for growth (0-100).</summary>
    public int growthBudgetPercent = 10;
    public int roadBudgetPercent = 25;
    public int energyBudgetPercent = 25;
    public int waterBudgetPercent = 25;
    public int zoningBudgetPercent = 25;

    public int roadSpentThisCycle;
    public int energySpentThisCycle;
    public int waterSpentThisCycle;
    public int zoningSpentThisCycle;

    public GrowthBudgetData Clone()
    {
        return new GrowthBudgetData
        {
            totalGrowthBudget = totalGrowthBudget,
            growthBudgetPercent = growthBudgetPercent,
            roadBudgetPercent = roadBudgetPercent,
            energyBudgetPercent = energyBudgetPercent,
            waterBudgetPercent = waterBudgetPercent,
            zoningBudgetPercent = zoningBudgetPercent,
            roadSpentThisCycle = roadSpentThisCycle,
            energySpentThisCycle = energySpentThisCycle,
            waterSpentThisCycle = waterSpentThisCycle,
            zoningSpentThisCycle = zoningSpentThisCycle
        };
    }
}
}
