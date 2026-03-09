using UnityEngine;

namespace Territory.Simulation
{
/// <summary>
/// Serializable data container for growth budget allocation across residential, commercial, and industrial categories.
/// </summary>
[System.Serializable]
public class GrowthBudgetData
{
    /// <summary>Legacy: fixed dollar amount. Kept for save/load backward compatibility; migrated to growthBudgetPercent on load.</summary>
    public int totalGrowthBudget = 5000;
    /// <summary>User preference: percentage of city money to allocate for growth (0-100).</summary>
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
