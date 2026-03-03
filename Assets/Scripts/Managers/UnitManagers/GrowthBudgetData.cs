using UnityEngine;

[System.Serializable]
public class GrowthBudgetData
{
    public int totalGrowthBudget = 5000;
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
