using Territory.Simulation;

namespace Territory.Economy
{
/// <summary>
/// Contract for growth-budget category accounting. Core-leaf — Domains.Roads consumes via interface to avoid Game asmdef ref.
/// </summary>
public interface IGrowthBudgetManager
{
    int GetAvailableBudget(GrowthCategory category);
    bool TrySpend(GrowthCategory category, int amount);
}
}
